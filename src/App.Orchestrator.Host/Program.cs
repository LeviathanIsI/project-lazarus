using System.Text.Json;
using Lazarus.Backend.Services;
using Lazarus.Backend.Services.Runners;
using Lazarus.Shared;

DirectoryBootstrap.EnsureAll();

var builder = WebApplication.CreateBuilder(args);

var defaultUrls = new[] { "http://127.0.0.1:11711", "http://localhost:11711" };
var configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrWhiteSpace(configuredUrls))
{
    var urls = configuredUrls
        .Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    builder.WebHost.UseUrls(urls.Length > 0 ? urls : defaultUrls);
}
else
{
    builder.WebHost.UseUrls(defaultUrls);
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddSingleton<IModelInventoryService, ModelInventoryService>();
builder.Services.AddSingleton<IModelPresetService, ModelPresetService>();
builder.Services.AddSingleton<IRunnerRegistry, RunnerRegistry>();
builder.Services.AddSingleton<RunnerStateManager>();

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/models", (IModelInventoryService inventory) =>
{
    var inv = inventory.Scan();
    var models = inv.BaseModels.Select(m => new ModelInfoDto(
        Id: m.ModelKey,
        Name: m.DisplayName,
        Path: m.FilePath,
        SizeBytes: SizeHelper.GetSizeBytes(m.FilePath),
        Architecture: m.Format.ToString(),
        SupportedRunners: new[] { m.PreferredRunner.ToString() }
    )).ToList();

    return Results.Ok(models);
});

app.MapPost("/api/runners", (StartRunnerRequest request, RunnerStateManager manager) =>
{
    if (request is null)
    {
        return Results.BadRequest();
    }

    var info = manager.StartRunner(request);
    return Results.Ok(info);
});

app.MapDelete("/api/runners/{id}", (string id, RunnerStateManager manager) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest();
    }

    var removed = manager.StopRunner(id);
    return removed ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/runners/status", (RunnerStateManager manager) =>
{
    var statuses = manager.GetRunnerStatuses();
    return Results.Ok(statuses);
});

app.MapPost("/runner/load", (LoadRequest request, RunnerStateManager manager) =>
{
    if (request is null)
    {
        return Results.BadRequest();
    }

    var response = manager.LoadRunner(request);
    return Results.Ok(response);
});

app.MapPost("/runner/unload", (RunnerStateManager manager) =>
{
    var response = manager.UnloadRunner();
    return Results.Ok(response);
});

app.MapGet("/runner/status", (RunnerStateManager manager) =>
{
    var status = manager.GetRunnerProcessStatus();
    return Results.Ok(status);
});

app.Run();

internal static class SizeHelper
{
    public static long GetSizeBytes(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return new FileInfo(path).Length;
            }

            if (Directory.Exists(path))
            {
                long total = 0;
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        total += new FileInfo(file).Length;
                    }
                    catch
                    {
                    }
                }

                return total;
            }
        }
        catch
        {
        }

        return 0;
    }
}

internal sealed record ModelInfoDto(
    string Id,
    string Name,
    string Path,
    long SizeBytes,
    string Architecture,
    string[] SupportedRunners
);

internal sealed record StartRunnerRequest(
    string ModelId,
    RunnerConfigurationDto? Configuration
);

internal sealed record RunnerConfigurationDto(
    string RunnerType,
    int Port,
    Dictionary<string, JsonElement>? Parameters
);

internal sealed record RunnerInfoDto(
    string Id,
    string ModelId,
    string RunnerType,
    int Port,
    DateTime StartedAt
);

internal sealed record RunnerStatusDto(
    string Id,
    string ModelId,
    string RunnerType,
    int Port,
    bool IsHealthy,
    DateTime LastHealthCheck,
    string? ErrorMessage
);

internal sealed record LoadRequest(
    string ModelPath,
    List<string>? Loras,
    double? LoraScale
);

internal sealed record LoadRunnerResponseDto(
    string Status,
    bool HotSwapped,
    string RunnerId,
    int Port,
    string RunnerType,
    string ModelId,
    string ModelPath,
    int Pid,
    int? LorasApplied,
    string? LaunchArgs
);

internal sealed record SimpleStatusDto(string Status);

internal sealed record RunnerProcessStatusDto(
    bool IsRunning,
    string? ModelPath,
    int? Pid,
    int? Port,
    string? ExePath,
    string? OutLog,
    string? ErrLog,
    int? LorasApplied,
    string? LaunchArgs,
    string? CmdPath,
    int? LoraEvidenceCount
);

internal sealed class RunnerStateManager
{
    private readonly object _lock = new();
    private readonly Dictionary<string, RunnerInfoDto> _runners = new(StringComparer.OrdinalIgnoreCase);
    private RunnerProcessStatusDto _processStatus = new(false, null, null, null, null, null, null, null, null, null, null);
    private string? _activeRunnerId;

    public RunnerInfoDto StartRunner(StartRunnerRequest request)
    {
        var runnerType = request.Configuration?.RunnerType ?? "llama.cpp";
        var port = request.Configuration?.Port ?? 11712;
        var id = Guid.NewGuid().ToString("n");
        var info = new RunnerInfoDto(id, request.ModelId, runnerType, port, DateTime.UtcNow);

        lock (_lock)
        {
            _runners[id] = info;
            _activeRunnerId ??= id;
            return info;
        }
    }

    public bool StopRunner(string id)
    {
        lock (_lock)
        {
            var removed = _runners.Remove(id);
            if (removed && string.Equals(_activeRunnerId, id, StringComparison.OrdinalIgnoreCase))
            {
                _activeRunnerId = null;
                _processStatus = new RunnerProcessStatusDto(false, null, null, null, null, null, null, null, null, null, null);
            }

            return removed;
        }
    }

    public IReadOnlyList<RunnerStatusDto> GetRunnerStatuses()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            return _runners.Values
                .Select(info => new RunnerStatusDto(
                    info.Id,
                    info.ModelId,
                    info.RunnerType,
                    info.Port,
                    info.Id == _activeRunnerId && _processStatus.IsRunning,
                    now,
                    null))
                .ToList();
        }
    }

    public LoadRunnerResponseDto LoadRunner(LoadRequest request)
    {
        lock (_lock)
        {
            var hotSwap = _processStatus.IsRunning;
            var runnerId = _activeRunnerId ?? EnsureDefaultRunner();
            var runnerInfo = _runners[runnerId];
            var pid = Environment.ProcessId;
            var loraCount = request.Loras?.Count ?? 0;
            var launchArgs = BuildLaunchArgs(request.ModelPath, request.Loras, request.LoraScale);
            var modelId = DeriveModelId(request.ModelPath);

            _processStatus = new RunnerProcessStatusDto(
                true,
                request.ModelPath,
                pid,
                runnerInfo.Port,
                ExePath: null,
                OutLog: null,
                ErrLog: null,
                LorasApplied: loraCount,
                LaunchArgs: launchArgs,
                CmdPath: null,
                LoraEvidenceCount: loraCount);

            _runners[runnerId] = runnerInfo with { ModelId = modelId };

            return new LoadRunnerResponseDto(
                Status: "ok",
                HotSwapped: hotSwap,
                RunnerId: runnerId,
                Port: runnerInfo.Port,
                RunnerType: runnerInfo.RunnerType,
                ModelId: modelId,
                ModelPath: request.ModelPath,
                Pid: pid,
                LorasApplied: loraCount,
                LaunchArgs: launchArgs);
        }
    }

    public SimpleStatusDto UnloadRunner()
    {
        lock (_lock)
        {
            _processStatus = new RunnerProcessStatusDto(false, null, null, null, null, null, null, null, null, null, null);
            return new SimpleStatusDto("ok");
        }
    }

    public RunnerProcessStatusDto GetRunnerProcessStatus()
    {
        lock (_lock)
        {
            return _processStatus;
        }
    }

    private string EnsureDefaultRunner()
    {
        if (_activeRunnerId is not null)
        {
            return _activeRunnerId;
        }

        var id = Guid.NewGuid().ToString("n");
        var info = new RunnerInfoDto(id, string.Empty, "llama.cpp", 11712, DateTime.UtcNow);
        _runners[id] = info;
        _activeRunnerId = id;
        return id;
    }

    private static string DeriveModelId(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "model";
        }

        var name = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = Path.GetFileName(path);
        }

        return string.IsNullOrWhiteSpace(name) ? "model" : name;
    }

    private static string? BuildLaunchArgs(string modelPath, List<string>? loras, double? loraScale)
    {
        var args = new List<string>();
        if (!string.IsNullOrWhiteSpace(modelPath))
        {
            args.Add($"--model \"{modelPath}\"");
        }

        if (loras is { Count: > 0 })
        {
            foreach (var lora in loras)
            {
                if (!string.IsNullOrWhiteSpace(lora))
                {
                    args.Add($"--lora \"{lora}\"");
                }
            }

            if (loraScale.HasValue)
            {
                args.Add($"--lora-scale {loraScale.Value:0.###}");
            }
        }

        return args.Count == 0 ? null : string.Join(' ', args);
    }
}
