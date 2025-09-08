using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Serilog;
using Lazarus.Shared;
using Lazarus.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Force binding strictly to loopback:11711
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, 11711);
});

// Backend services used by the host
builder.Services.AddSingleton<IModelInventoryService, ModelInventoryService>();
builder.Services.AddSingleton<IModelPresetService, ModelPresetService>();
builder.Services.AddSingleton<IRunnerSupervisor, LlamaCppSupervisor>();
builder.Services.AddHostedService<RunnerAutoStartService>();

// Logging: write to System-Data/Logs and console
try
{
    Directory.CreateDirectory(LazarusPaths.SystemData.Logs);
}
catch { }
var logPath = Path.Combine(LazarusPaths.SystemData.Logs, "orchestrator-.log");
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(path: logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

// Simple in-memory runner registry
var runners = new ConcurrentDictionary<string, RunnerInfo>();

// Ensure first-run directory layout is present
DirectoryBootstrap.EnsureAll();

var app = builder.Build();

// Health endpoint
app.MapGet("/health", (IRunnerSupervisor sup) => Results.Json(new
{
    status = "ok",
    runner = sup.IsRunning ? "ok" : "idle",
    pid = sup.ProcessId ?? Environment.ProcessId
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
app.MapGet("/api/runners/status", (IRunnerSupervisor sup) =>
{
    var list = new List<RunnerStatus>();
    var r = runners.Values.FirstOrDefault();
    if (r is not null)
    {
        list.Add(new RunnerStatus
        {
            Id = r.Id,
            ModelId = r.ModelId,
            RunnerType = r.RunnerType,
            Port = r.Port,
            IsHealthy = sup.IsRunning,
            LastHealthCheck = DateTimeOffset.UtcNow,
            ErrorMessage = sup.IsRunning ? null : "stopped"
        });
    }
    return Results.Json(list);
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
app.MapGet("/runner/status", (IRunnerSupervisor sup, IModelInventoryService inventory) =>
{
    var modelPath = sup.CurrentModelPath;
    return Results.Json(new { isRunning = sup.IsRunning, modelPath, pid = sup.ProcessId });
});

// OpenAI-compatible models list: proxy to runner if available; otherwise fallback
app.MapGet("/v1/models", async (IRunnerSupervisor sup, IModelInventoryService inventory) =>
{
    if (sup.IsRunning)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var url = $"http://127.0.0.1:{sup.Port}/v1/models";
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
app.MapPost("/v1/chat/completions", async (IRunnerSupervisor sup, HttpRequest incoming) =>
{
    if (!sup.IsRunning)
    {
        return Results.BadRequest(new { error = "runner idle" });
    }

    // Read incoming JSON body
    using var reader = new StreamReader(incoming.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: true);
    var payload = await reader.ReadToEndAsync();

    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        var url = $"http://127.0.0.1:{sup.Port}/v1/chat/completions";
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
app.MapPost("/runner/load", async (LoadRunnerRequest req, IRunnerSupervisor sup, IModelInventoryService inventory, CancellationToken ct) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.ModelPath))
        return Results.BadRequest(new { error = "modelPath required" });

    var fullPath = HostHelpers.ResolveModelPath(req.ModelPath) ?? req.ModelPath;
    if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        return Results.BadRequest(new { error = "modelPath not found" });

    var runnerType = HostHelpers.InferRunnerType(fullPath);
    var modelId = HostHelpers.InferModelKey(fullPath);

    var started = await sup.LoadAsync(fullPath, ct);
    if (!started)
        return Results.BadRequest(new { error = "failed to start runner" });

    // Keep a single registry entry to reflect current runner
    var id = "llamacpp";
    var info = new RunnerInfo
    {
        Id = id,
        ModelId = modelId,
        RunnerType = runnerType,
        Port = sup.Port,
        StartedAt = DateTimeOffset.UtcNow
    };
    runners[id] = info;

    return Results.Json(new { status = "ok", hotSwapped = true, runnerId = id, port = sup.Port, runnerType, modelId, modelPath = fullPath, pid = sup.ProcessId });
});

// Stop/unload runner
app.MapPost("/runner/unload", async (IRunnerSupervisor sup, CancellationToken ct) =>
{
    await sup.UnloadAsync(ct);
    // Clear registry entry
    foreach (var k in runners.Keys) { runners.TryRemove(k, out _); }
    return Results.Json(new { status = "ok" });
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

public interface IRunnerSupervisor
{
    bool IsRunning { get; }
    int? ProcessId { get; }
    string? CurrentModelPath { get; }
    int Port { get; }
    Task<bool> LoadAsync(string modelPath, CancellationToken cancellationToken);
    Task UnloadAsync(CancellationToken cancellationToken);
}

internal sealed class LlamaCppSupervisor : IRunnerSupervisor
{
    private readonly IConfiguration _config;
    private readonly ILogger<LlamaCppSupervisor> _logger;
    private Process? _process;
    private string? _currentModelPath;
    private StreamWriter? _stdoutWriter;
    private StreamWriter? _stderrWriter;
    private int _port;
    public int Port => _port;

    public LlamaCppSupervisor(IConfiguration config, ILogger<LlamaCppSupervisor> logger)
    {
        _config = config;
        _logger = logger;
        _port = ResolveBasePort();
    }

    public bool IsRunning => _process is { HasExited: false };
    public int? ProcessId => _process?.HasExited == false ? _process.Id : null;
    public string? CurrentModelPath => _currentModelPath;

    public async Task<bool> LoadAsync(string modelPath, CancellationToken cancellationToken)
    {
        // Stop any existing process
        await UnloadAsync(cancellationToken).ConfigureAwait(false);

        var exe = ResolveLlamaExe();
        if (exe is null)
        {
            _logger.LogError("Unable to locate llama-server.exe. Configure Orchestrator:Runner:BinaryDir or LAZARUS_BINARIES.");
            return false;
        }
        // Prefer GPU offload when hardware seems available; otherwise default to CPU-only.
        var preferGpu = HasCuda() || exe.Contains("cu", StringComparison.OrdinalIgnoreCase) || exe.Contains("cuda", StringComparison.OrdinalIgnoreCase);

        // Resolve a sane initial GPU layer count (user-configurable). Avoid extreme values to reduce startup failures.
        var initialGpuLayers = ResolveGpuLayers(preferGpu);

        if (await StartOnceAsync(exe, modelPath, initialGpuLayers, cancellationToken).ConfigureAwait(false))
            return true;

        // If GPU attempt failed quickly, retry with CPU-only as a fallback.
        if (preferGpu)
        {
            _logger.LogWarning("GPU-start failed; retrying llama-server with CPU-only (n-gpu-layers=0)");
            return await StartOnceAsync(exe, modelPath, 0, cancellationToken).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<bool> StartOnceAsync(string exe, string modelPath, int gpuLayers, CancellationToken cancellationToken)
    {
        // Pick a usable port, avoiding collisions if the preferred is busy
        var preferred = ResolveBasePort();
        var selected = SelectAvailablePortNear(preferred);
        if (selected != preferred)
        {
            _logger.LogWarning("Preferred port {Preferred} is busy; using {Port}", preferred, selected);
        }
        _port = selected;

        var args = $"--api --host 127.0.0.1 --port {Port} --n-gpu-layers {gpuLayers} --model \"{modelPath}\"";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            // Prepare log files under Lazarus tree
            try
            {
                Directory.CreateDirectory(LazarusPaths.SystemData.Logs);
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var outPath = Path.Combine(LazarusPaths.SystemData.Logs, $"llama-server-{stamp}.out.log");
                var errPath = Path.Combine(LazarusPaths.SystemData.Logs, $"llama-server-{stamp}.err.log");
                _stdoutWriter = new StreamWriter(new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
                _stderrWriter = new StreamWriter(new FileStream(errPath, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create runner log files; continuing without file logging");
            }

            _logger.LogInformation("Starting llama-server: {Exe} {Args}", exe, args);
            _process = Process.Start(psi);
            if (_process is null)
            {
                _logger.LogError("Failed to start llama-server process");
                return false;
            }

            try
            {
                if (_stdoutWriter is not null)
                {
                    _process.OutputDataReceived += (s, e) => { if (e.Data is not null) { try { _stdoutWriter.WriteLine(e.Data); } catch { } } };
                    _process.BeginOutputReadLine();
                }
                if (_stderrWriter is not null)
                {
                    _process.ErrorDataReceived += (s, e) => { if (e.Data is not null) { try { _stderrWriter.WriteLine(e.Data); } catch { } } };
                    _process.BeginErrorReadLine();
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed hooking runner stdout/stderr handlers");
            }

            _currentModelPath = modelPath;

            // Health check with configurable startup wait
            var startupTimeout = GetStartupTimeout();
            _logger.LogInformation("Waiting up to {Timeout} for runner health", startupTimeout);
            var deadline = DateTimeOffset.UtcNow.Add(startupTimeout);
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var url = $"http://127.0.0.1:{Port}/health";
            while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var resp = await http.GetAsync(url, cancellationToken).ConfigureAwait(false);
                    if (resp.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("llama-server is healthy on port {Port}", Port);
                        return true;
                    }
                }
                catch
                {
                    // ignore transient errors during startup
                }

                // If process died, stop early
                if (_process.HasExited)
                {
                    _logger.LogWarning("llama-server exited with code {Code} during startup", _process.ExitCode);
                    break;
                }

                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogError("llama-server failed to become healthy within timeout");
            await UnloadAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception starting llama-server");
            await UnloadAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }
    }

    private TimeSpan GetStartupTimeout()
    {
        try
        {
            // Preferred: TimeSpan-formatted value like "00:02:00"
            var tsString = _config["Orchestrator:Runner:StartupTimeout"];
            if (!string.IsNullOrWhiteSpace(tsString) && TimeSpan.TryParse(tsString, out var ts) && ts > TimeSpan.Zero)
                return ts;

            // Fallback 0: Desktop-style key if present in this host's config
            tsString = _config["Runners:LlamaCpp:StartupTimeout"];
            if (!string.IsNullOrWhiteSpace(tsString) && TimeSpan.TryParse(tsString, out ts) && ts > TimeSpan.Zero)
                return ts;

            // Fallback 1: milliseconds value (int)
            var msString = _config["Orchestrator:Runner:StartupTimeoutMs"] ?? _config["Runner.StartupTimeout"];
            if (!string.IsNullOrWhiteSpace(msString) && int.TryParse(msString, out var ms) && ms > 0)
                return TimeSpan.FromMilliseconds(ms);

            // Fallback 1b: Desktop-style milliseconds
            msString = _config["Runners:LlamaCpp:StartupTimeoutMs"];
            if (!string.IsNullOrWhiteSpace(msString) && int.TryParse(msString, out ms) && ms > 0)
                return TimeSpan.FromMilliseconds(ms);

            // Fallback 2: environment variable (seconds)
            var envSeconds = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_STARTUP_TIMEOUT");
            if (!string.IsNullOrWhiteSpace(envSeconds) && int.TryParse(envSeconds, out var sec) && sec > 0)
                return TimeSpan.FromSeconds(sec);
        }
        catch { }

        // Sensible default: 4 minutes (large models + CPU fallback can take longer)
        return TimeSpan.FromMinutes(4);
    }

    private int ResolveGpuLayers(bool preferGpu)
    {
        // 1) Explicit config value wins
        try
        {
            var v = _config["Orchestrator:Runner:GpuLayers"];
            if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var parsed) && parsed >= 0 && parsed <= 32768)
                return parsed;

            // Alternate key (desktop-style if provided)
            v = _config["Runners:LlamaCpp:GpuLayers"];
            if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out parsed) && parsed >= 0 && parsed <= 32768)
                return parsed;
        }
        catch { }

        // 2) Environment variable override
        try
        {
            var env = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_GPU_LAYERS");
            if (!string.IsNullOrWhiteSpace(env) && int.TryParse(env, out var eparsed) && eparsed >= 0 && eparsed <= 32768)
                return eparsed;
        }
        catch { }

        // 3) Heuristic default: moderate offload when GPU is present, else CPU-only
        return preferGpu ? 60 : 0;
    }

    private static bool HasCuda()
    {
        try
        {
            var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var nvcuda = Path.Combine(system32, "nvcuda.dll");
            if (File.Exists(nvcuda)) return true;
        }
        catch { }
        var env = Environment.GetEnvironmentVariable("CUDA_PATH");
        return !string.IsNullOrWhiteSpace(env);
    }

    public async Task UnloadAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_process is { HasExited: false })
            {
                try
                {
                    _process.CloseMainWindow();
                }
                catch { }
                try
                {
                    if (!_process.HasExited)
                        _process.Kill(true);
                }
                catch { }

                // give it a moment
                var sw = Stopwatch.StartNew();
                while (!_process.HasExited && sw.Elapsed < TimeSpan.FromSeconds(5))
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _process?.Dispose();
            _process = null;
            _currentModelPath = null;
            try { _stdoutWriter?.Dispose(); } catch { }
            try { _stderrWriter?.Dispose(); } catch { }
            _stdoutWriter = null;
            _stderrWriter = null;
        }
    }

    private string? ResolveLlamaExe()
    {
        // Priority 1: appsettings Orchestrator:Runner:BinaryDir
        var dir = _config["Orchestrator:Runner:BinaryDir"];
        if (!string.IsNullOrWhiteSpace(dir))
        {
            var normalized = NormalizeDirectory(dir);
            if (!string.Equals(dir, normalized, StringComparison.Ordinal))
            {
                _logger.LogDebug("Normalized Runner BinaryDir from '{Orig}' to '{Norm}'", dir, normalized);
            }
            if (Directory.Exists(normalized))
            {
                var p = Path.Combine(normalized, "llama-server.exe");
                if (File.Exists(p)) return p;
            }
            else
            {
                _logger.LogWarning("Configured Orchestrator:Runner:BinaryDir does not exist: {Dir}", dir);
            }
        }

        // Priority 2: Lazarus Runners folder scan: %LOCALAPPDATA%\Lazarus\Runners\llama.cpp\**\llama-server.exe
        try
        {
            var engineRoot = LazarusPaths.Runners.LlamaCpp;
            if (Directory.Exists(engineRoot))
            {
                var found = Directory.EnumerateFiles(engineRoot, "llama-server.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (found != null) return found;
            }
        }
        catch { }

        // Priority 3: LAZARUS_BINARIES env (try as-is, then with /runners)
        var env = Environment.GetEnvironmentVariable("LAZARUS_BINARIES");
        if (!string.IsNullOrWhiteSpace(env))
        {
            var p1 = Path.Combine(env, "llama-server.exe");
            if (File.Exists(p1)) return p1;
            var p2 = Path.Combine(env, "runners", "llama-server.exe");
            if (File.Exists(p2)) return p2;
        }

        // Priority 4: <Base>/binaries/runners
        var baseDir = AppContext.BaseDirectory;
        var p3 = Path.Combine(baseDir, "binaries", "runners", "llama-server.exe");
        if (File.Exists(p3)) return p3;

        return null;
    }

    private int ResolveBasePort()
    {
        try
        {
            var v = _config["Orchestrator:Runner:Port"];
            if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out var p) && p > 0 && p < 65536)
                return p;

            // Fallback to Desktop-style key if present in this host's config
            v = _config["Runners:LlamaCpp:DefaultPort"];
            if (!string.IsNullOrWhiteSpace(v) && int.TryParse(v, out p) && p > 0 && p < 65536)
                return p;
        }
        catch { }
        return 11888; // default
    }

    private static int SelectAvailablePortNear(int preferred, int scan = 20)
    {
        for (var i = 0; i < scan; i++)
        {
            var p = preferred + i;
            if (IsPortAvailable(p)) return p;
        }
        return preferred;
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            var ep = new IPEndPoint(IPAddress.Loopback, port);
            using var listener = new TcpListener(ep);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeDirectory(string input)
    {
        var s = (input ?? string.Empty).Trim();
        if (s.Length == 0) return s;
        s = s.Replace('/', Path.DirectorySeparatorChar);
        // Insert missing colon for paths like "D\foo\bar" -> "D:\\foo\\bar"
        if (Environment.OSVersion.Platform.ToString().StartsWith("Win", StringComparison.OrdinalIgnoreCase))
        {
            if (s.Length >= 2 && s[1] == '\\' && char.IsLetter(s[0]))
            {
                s = s[0] + ":\\" + s.Substring(2);
            }
        }
        return s;
    }
}

internal sealed class RunnerAutoStartService : IHostedService
{
    private readonly IConfiguration _config;
    private readonly ILogger<RunnerAutoStartService> _logger;
    private readonly IRunnerSupervisor _supervisor;

    public RunnerAutoStartService(IConfiguration config, ILogger<RunnerAutoStartService> logger, IRunnerSupervisor supervisor)
    {
        _config = config;
        _logger = logger;
        _supervisor = supervisor;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var autoStart = string.Equals(_config["Orchestrator:Runner:AutoStart"], "true", StringComparison.OrdinalIgnoreCase);
        var modelPath = _config["Orchestrator:Runner:ModelPath"]; // may be blank

        if (!autoStart)
        {
            _logger.LogInformation("Runner auto-start disabled by configuration");
            return;
        }

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            _logger.LogInformation("Runner ModelPath not set; starting idle");
            return;
        }

        if (!File.Exists(modelPath))
        {
            _logger.LogWarning("Configured Runner ModelPath does not exist or is not a file: {ModelPath}", modelPath);
            return;
        }

        _logger.LogInformation("Attempting runner auto-start with model: {ModelPath}", modelPath);
        var ok = await _supervisor.LoadAsync(modelPath, cancellationToken).ConfigureAwait(false);
        if (!ok)
        {
            _logger.LogWarning("Runner auto-start failed; service remains idle");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_supervisor.IsRunning)
        {
            _logger.LogInformation("Stopping runner due to host shutdown");
            await _supervisor.UnloadAsync(cancellationToken).ConfigureAwait(false);
        }
    }
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

    public static string? ResolveModelPath(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var expanded = Environment.ExpandEnvironmentVariables(input);
        try
        {
            if (Path.IsPathRooted(expanded) || expanded.Contains('\\') || expanded.Contains('/'))
            {
                var p = Path.GetFullPath(expanded);
                if (File.Exists(p) || Directory.Exists(p)) return p;
            }
        }
        catch { }

        // Try under Lazarus model directories
        var candidates = new[]
        {
            Path.Combine(LazarusPaths.Models.BaseModels, expanded),
            Path.Combine(LazarusPaths.Models.RootDir, expanded)
        };
        foreach (var c in candidates)
        {
            try
            {
                if (File.Exists(c) || Directory.Exists(c)) return c;
            }
            catch { }
        }
        return null;
    }
}
