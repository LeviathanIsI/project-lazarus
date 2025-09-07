using System.Collections.Concurrent;
using System.Net;
using System.Diagnostics;
using Lazarus.Shared;
using Lazarus.Backend.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Force binding strictly to loopback:11711
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, 11711);
});

// Backend services used by the host
builder.Services.AddSingleton<IModelInventoryService, ModelInventoryService>();
builder.Services.AddSingleton<IModelPresetService, ModelPresetService>();

// Simple in-memory runner registry
var runners = new ConcurrentDictionary<string, RunnerInfo>();

// Ensure first-run directory layout is present
DirectoryBootstrap.EnsureAll();

var app = builder.Build();

// Health endpoint
app.MapGet("/health", () => Results.Json(new
{
    status = "ok",
    runner = runners.IsEmpty ? "idle" : "ok",
    pid = Environment.ProcessId
}));

// Models list (enumerated from LazarusPaths using backend service)
app.MapGet("/api/models", (IModelInventoryService inventory) =>
{
    var inv = inventory.Scan();
    var list = inv.BaseModels.Select(m => new ModelInfo
    {
        Id = m.ModelKey,
        Name = m.DisplayName,
        Path = m.FilePath,
        SizeBytes = HostHelpers.TrySize(m.FilePath),
        Architecture = m.Format.ToString(),
        SupportedRunners = HostHelpers.SupportedRunnersFor(m.PreferredRunner)
    });
    return Results.Json(list);
});

// Presets: list, get, save, delete
app.MapGet("/api/presets", (IModelPresetService presets) => Results.Json(presets.List()));
app.MapGet("/api/presets/{name}", (string name, IModelPresetService presets) =>
{
    var p = presets.Load(name);
    return p is null ? Results.NotFound() : Results.Json(p);
});
app.MapPost("/api/presets", (ModelPreset preset, IModelPresetService presets) =>
{
    presets.Save(preset);
    return Results.Created($"/api/presets/{Uri.EscapeDataString(preset.Name)}", preset);
});
app.MapDelete("/api/presets/{name}", (string name, IModelPresetService presets) =>
{
    var concrete = presets as ModelPresetService;
    if (concrete is null) return Results.StatusCode(501);
    var ok = concrete.Delete(name);
    return ok ? Results.NoContent() : Results.NotFound();
});

// Start runner (simulated)
app.MapPost("/api/runners", (StartRunnerRequest req) =>
{
    var id = Guid.NewGuid().ToString("n");
    var info = new RunnerInfo
    {
        Id = id,
        ModelId = req.ModelId,
        RunnerType = req.Configuration?.RunnerType ?? "unknown",
        Port = req.Configuration?.Port ?? 0,
        StartedAt = DateTimeOffset.UtcNow
    };

    runners[id] = info;
    return Results.Json(info);
});

// Stop runner (simulated)
app.MapDelete("/api/runners/{id}", (string id) =>
{
    if (runners.TryRemove(id, out _))
        return Results.NoContent();
    return Results.NotFound();
});

// Runner status (simulated: mark all as healthy)
app.MapGet("/api/runners/status", () =>
{
    var statuses = runners.Values.Select(r => new RunnerStatus
    {
        Id = r.Id,
        ModelId = r.ModelId,
        RunnerType = r.RunnerType,
        Port = r.Port,
        IsHealthy = true,
        LastHealthCheck = DateTimeOffset.UtcNow,
        ErrorMessage = null
    });
    return Results.Json(statuses);
});

// List current runners
app.MapGet("/api/runners", () => Results.Json(runners.Values));

// Host info (paths, environment)
app.MapGet("/api/info", () => Results.Json(new
{
    lazarusHome = LazarusPaths.Root,
    modelsDir = LazarusPaths.Models.RootDir,
    systemData = LazarusPaths.SystemData.RootDir,
    timestamp = DateTimeOffset.UtcNow
}));

// Simple runner status summary
app.MapGet("/runner/status", (IModelInventoryService inventory) =>
{
    var active = runners.Values.FirstOrDefault();
    string? modelPath = null;
    int? pid = null; // Placeholder until real runner processes are managed

    if (active is not null)
    {
        var inv = inventory.Scan();
        var model = inv.BaseModels.FirstOrDefault(m => string.Equals(m.ModelKey, active.ModelId, StringComparison.OrdinalIgnoreCase));
        modelPath = model?.FilePath;
        // pid remains null in stub implementation
    }

    return Results.Json(new
    {
        isRunning = active is not null,
        modelPath,
        pid
    });
});

// OpenAI-compatible models list: proxy to runner if available; otherwise fallback
app.MapGet("/v1/models", async (IModelInventoryService inventory) =>
{
    var active = runners.Values.FirstOrDefault(r => r.Port > 0);
    if (active is not null)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var url = $"http://127.0.0.1:{active.Port}/v1/models";
            using var resp = await http.GetAsync(url);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                return Results.Content(body, "application/json");
            }
        }
        catch
        {
            // fall through to local fallback
        }
    }

    // Minimal fallback from local inventory
    var inv = inventory.Scan();
    var data = inv.BaseModels.Select(m => new { id = m.ModelKey, @object = "model" });
    return Results.Json(new { data });
});

// OpenAI-compatible chat completions proxy
app.MapPost("/v1/chat/completions", async (HttpRequest incoming) =>
{
    var active = runners.Values.FirstOrDefault(r => r.Port > 0);
    if (active is null)
    {
        return Results.BadRequest(new { error = "runner idle" });
    }

    // Read incoming JSON body
    using var reader = new StreamReader(incoming.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: true);
    var payload = await reader.ReadToEndAsync();

    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        var url = $"http://127.0.0.1:{active.Port}/v1/chat/completions";
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var resp = await http.PostAsync(url, content);
        var body = await resp.Content.ReadAsStringAsync();
        return Results.Text(body, "application/json", statusCode: (int)resp.StatusCode);
    }
    catch
    {
        return Results.BadRequest(new { error = "runner idle" });
    }
});

// Start or hot-swap model into the runner
app.MapPost("/runner/load", (LoadRunnerRequest req, IModelInventoryService inventory) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.ModelPath))
        return Results.BadRequest(new { error = "modelPath required" });

    var fullPath = Path.GetFullPath(req.ModelPath);
    if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        return Results.BadRequest(new { error = "modelPath not found" });

    var runnerType = HostHelpers.InferRunnerType(fullPath);
    var modelId = HostHelpers.InferModelKey(fullPath);

    var existing = runners.Values.FirstOrDefault();
    var hotSwapped = existing is not null;

    if (existing is not null)
    {
        existing.ModelId = modelId;
        existing.RunnerType = runnerType;
        // Keep existing port to simulate in-place hot-swap
        runners[existing.Id] = existing;
        return Results.Json(new { status = "ok", hotSwapped = true, runnerId = existing.Id, port = existing.Port, runnerType, modelId, modelPath = fullPath });
    }

    // Start new simulated runner
    var port = runnerType.Equals("LlamaCpp", StringComparison.OrdinalIgnoreCase) ? 8080 : 8081;
    var id = Guid.NewGuid().ToString("n");
    var info = new RunnerInfo
    {
        Id = id,
        ModelId = modelId,
        RunnerType = runnerType,
        Port = port,
        StartedAt = DateTimeOffset.UtcNow
    };
    runners[id] = info;

    return Results.Json(new { status = "ok", hotSwapped = false, runnerId = id, port, runnerType, modelId, modelPath = fullPath });
});

app.Run();

// DTOs mirroring Desktop client expectations (property names in camelCase)
public sealed class ModelInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public long SizeBytes { get; set; }
    public string Architecture { get; set; } = "unknown";
    public string[] SupportedRunners { get; set; } = Array.Empty<string>();
}

public sealed class RunnerConfiguration
{
    public required string RunnerType { get; set; }
    public int Port { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}

public sealed class StartRunnerRequest
{
    public required string ModelId { get; set; }
    public RunnerConfiguration? Configuration { get; set; }
}

public sealed class RunnerInfo
{
    public required string Id { get; set; }
    public required string ModelId { get; set; }
    public required string RunnerType { get; set; }
    public int Port { get; set; }
    public DateTimeOffset StartedAt { get; set; }
}

public sealed class RunnerStatus
{
    public required string Id { get; set; }
    public required string ModelId { get; set; }
    public required string RunnerType { get; set; }
    public int Port { get; set; }
    public bool IsHealthy { get; set; }
    public DateTimeOffset LastHealthCheck { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class LoadRunnerRequest
{
    public required string ModelPath { get; set; }
}

internal static class HostHelpers
{
    public static long TrySize(string path)
    {
        try
        {
            if (File.Exists(path)) return new FileInfo(path).Length;
            if (Directory.Exists(path))
            {
                long total = 0;
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { total += new FileInfo(f).Length; } catch { }
                }
                return total;
            }
        }
        catch { }
        return 0L;
    }

    public static string[] SupportedRunnersFor(Lazarus.Shared.RunnerKind preferred)
    {
        return preferred switch
        {
            Lazarus.Shared.RunnerKind.LlamaCpp => new[] { "LlamaCpp" },
            Lazarus.Shared.RunnerKind.Vllm => new[] { "VLLM" },
            Lazarus.Shared.RunnerKind.ExLlamaV2 => new[] { "ExLlamaV2" },
            _ => Array.Empty<string>()
        };
    }

    public static string InferRunnerType(string modelPath)
    {
        var ext = Path.GetExtension(modelPath).ToLowerInvariant();
        return ext == ".gguf" ? "LlamaCpp" : "VLLM";
    }

    public static string InferModelKey(string modelPath)
    {
        if (File.Exists(modelPath))
            return Path.GetFileNameWithoutExtension(modelPath);
        return new DirectoryInfo(modelPath).Name;
    }
}
