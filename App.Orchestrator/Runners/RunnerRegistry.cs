using System.Text.Json;
using Microsoft.Extensions.Logging;
using Lazarus.Orchestrator.Configuration;
using Lazarus.Orchestrator.Services;

namespace Lazarus.Orchestrator.Runners;

public static class RunnerRegistry
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder =>
        builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug)
    ).CreateLogger("RunnerRegistry");

    private static OrchestratorConfiguration _config = new();
    private static readonly Dictionary<string, IChatRunner> _runners = new();
    private static IChatRunner? _activeRunner;
    private static Timer? _healthCheckTimer;

    public static IChatRunner? Active => _activeRunner;
    public static string? CurrentModel => _activeRunner?.CurrentModel;

    public static async Task<bool> InitializeAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Runner Registry...");

            await LoadConfigurationAsync();
            await InitializeRunnersAsync();

            // Try to select a healthy external runner first
            await SelectDefaultRunnerAsync();

            // If no healthy external runners, try embedded runners
            if (_activeRunner == null)
            {
                Logger.LogInformation("No healthy external runners found - attempting embedded runners");
                await InitializeEmbeddedRunnersAsync();
                
                // Try selecting again after embedded runners are added
                await SelectDefaultRunnerAsync();
            }

            StartHealthMonitoring();

            Logger.LogInformation($"Runner Registry initialized with {_runners.Count} runners. Active: {_activeRunner?.Name ?? "none"}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Runner Registry");
            return false;
        }
    }

    private static async Task LoadConfigurationAsync()
    {
        var configSources = new[]
        {
            LoadFromFile("appsettings.json"),
            LoadFromFile("runners.json"),
            LoadFromEnvironment(),
            LoadFromDefaults()
        };

        foreach (var configTask in configSources)
        {
            try
            {
                var config = await configTask;
                if (config != null)
                {
                    _config = MergeConfigurations(_config, config);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load configuration from source");
            }
        }

        Logger.LogInformation($"Loaded configuration with {_config.Runners.Length} runner definitions");
    }

    private static async Task<OrchestratorConfiguration?> LoadFromFile(string filename)
    {
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
        if (!File.Exists(path)) return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<OrchestratorConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private static async Task<OrchestratorConfiguration> LoadFromEnvironment()
    {
        var envUrl = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_URL");
        if (string.IsNullOrEmpty(envUrl))
        {
            return new OrchestratorConfiguration { Runners = Array.Empty<RunnerConfiguration>() };
        }

        return await Task.FromResult(new OrchestratorConfiguration
        {
            Runners = new[]
            {
                new RunnerConfiguration
                {
                    Type = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_TYPE") ?? "llama-server",
                    Name = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_NAME") ?? "Environment Runner",
                    BaseUrl = envUrl,
                    DefaultModel = Environment.GetEnvironmentVariable("LAZARUS_RUNNER_MODEL")
                }
            }
        });
    }

    private static async Task<OrchestratorConfiguration> LoadFromDefaults()
    {
        return await Task.FromResult(new OrchestratorConfiguration
        {
            Runners = new[]
            {
                new RunnerConfiguration
                {
                    Type = "llama-server",
                    Name = "Local LLaMA Server",
                    BaseUrl = "http://127.0.0.1:8080",
                    DefaultModel = "Qwen2.5-32B-Instruct-Q5_K_M.gguf"
                },
                new RunnerConfiguration
                {
                    Type = "llama-cpp",
                    Name = "Local LLaMA.cpp",
                    BaseUrl = "http://127.0.0.1:8081"
                }
            },
            DefaultRunner = "Local LLaMA Server",
            AllowFallbackEcho = false
        });
    }

    private static OrchestratorConfiguration MergeConfigurations(OrchestratorConfiguration target, OrchestratorConfiguration source)
    {
        var mergedRunners = target.Runners.ToList();

        foreach (var sourceRunner in source.Runners)
        {
            var existing = mergedRunners.FirstOrDefault(r => r.Name == sourceRunner.Name);
            if (existing != null)
            {
                mergedRunners.Remove(existing);
            }
            mergedRunners.Add(sourceRunner);
        }

        return new OrchestratorConfiguration
        {
            Runners = mergedRunners.ToArray(),
            DefaultRunner = !string.IsNullOrEmpty(source.DefaultRunner) ? source.DefaultRunner : target.DefaultRunner,
            AllowFallbackEcho = source.AllowFallbackEcho,
            LogLevel = source.LogLevel
        };
    }

    private static async Task InitializeRunnersAsync()
    {
        _runners.Clear();

        foreach (var runnerConfig in _config.Runners)
        {
            try
            {
                Logger.LogInformation($"Initializing runner: {runnerConfig.Name} ({runnerConfig.Type})");

                if (!Uri.TryCreate(runnerConfig.BaseUrl, UriKind.Absolute, out var baseUri))
                {
                    Logger.LogWarning($"Invalid base URL for runner {runnerConfig.Name}: {runnerConfig.BaseUrl}");
                    continue;
                }

                IChatRunner runner = runnerConfig.Type.ToLowerInvariant() switch
                {
                    "llama-server" => new LlamaServerRunner(runnerConfig.Name, baseUri, runnerConfig.DefaultModel),
                    "llama-cpp" => new LlamaCppRunner(baseUri),
                    _ => throw new ArgumentException($"Unknown runner type: {runnerConfig.Type}")
                };

                _runners[runnerConfig.Name] = runner;
                Logger.LogInformation($"Runner initialized: {runnerConfig.Name}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to initialize runner: {runnerConfig.Name}");
            }
        }
    }

    private static async Task<bool> InitializeEmbeddedRunnersAsync()
    {
        try
        {
            Logger.LogInformation("Checking for embedded llama.cpp capability...");

            if (!await LlamaCppBinaryManager.EnsureBinariesAsync())
            {
                Logger.LogWarning("Embedded llama.cpp not available - skipping embedded runners");
                return false;
            }

            // Auto-discover available models in user's AppData
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lazarus");
            var modelsPath = Path.Combine(appDataPath, "models", "main");
            
            Logger.LogInformation($"Scanning for models in: {modelsPath}");
            
            if (!Directory.Exists(modelsPath))
            {
                Logger.LogInformation($"Models directory not found at: {modelsPath}");
                // Try alternative path without 'main' subdirectory
                modelsPath = Path.Combine(appDataPath, "models");
                if (!Directory.Exists(modelsPath))
                {
                    Logger.LogInformation("No models directory found - creating it");
                    Directory.CreateDirectory(modelsPath);
                    return false;
                }
            }

            var modelFiles = Directory.GetFiles(modelsPath, "*.gguf", SearchOption.TopDirectoryOnly);
            if (modelFiles.Length == 0)
            {
                Logger.LogInformation("No GGUF models found in models directory");
                return false;
            }

            Logger.LogInformation($"Found {modelFiles.Length} models - creating embedded runners");

            var port = 8080;
            foreach (var modelFile in modelFiles)
            {
                var modelName = Path.GetFileNameWithoutExtension(modelFile);
                var runnerName = $"Embedded-{modelName}";

                var config = new LlamaCppConfig
                {
                    ModelPath = modelFile,
                    Port = port++,
                    ContextSize = 4096,
                    UseGpu = true,
                    GpuLayers = 35,
                    Verbose = false
                };

                var embeddedRunner = new LlamaCppEmbeddedRunner(runnerName, config, Logger);

                // Start the embedded server
                if (await embeddedRunner.StartAsync())
                {
                    _runners[runnerName] = embeddedRunner;
                    Logger.LogInformation($"Embedded runner ready: {runnerName} on port {config.Port}");
                }
                else
                {
                    Logger.LogWarning($"Failed to start embedded runner: {runnerName}");
                    embeddedRunner.Dispose();
                }
            }

            return _runners.Count > 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize embedded runners");
            return false;
        }
    }

    private static async Task SelectDefaultRunnerAsync()
    {
        if (_runners.Count == 0)
        {
            Logger.LogWarning("No runners available - system will operate in degraded mode");
            return;
        }

        if (!string.IsNullOrEmpty(_config.DefaultRunner) && _runners.TryGetValue(_config.DefaultRunner, out var defaultRunner))
        {
            if (await TestRunnerHealth(defaultRunner))
            {
                _activeRunner = defaultRunner;
                Logger.LogInformation($"Default runner selected: {_config.DefaultRunner}");
                return;
            }
        }

        foreach (var (name, runner) in _runners)
        {
            if (await TestRunnerHealth(runner))
            {
                _activeRunner = runner;
                Logger.LogInformation($"Fallback runner selected: {name}");
                return;
            }
        }

        Logger.LogError("No healthy runners found - system operating in degraded mode");
    }

    private static async Task<bool> TestRunnerHealth(IChatRunner runner)
    {
        try
        {
            Logger.LogDebug($"Testing health for runner: {runner.Name}");
            return await runner.HealthAsync();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Health check failed for runner: {runner.Name}");
            return false;
        }
    }

    private static void StartHealthMonitoring()
    {
        if (_healthCheckTimer != null) return;

        _healthCheckTimer = new Timer(async _ => await PerformHealthChecks(),
            null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

        Logger.LogInformation("Health monitoring started");
    }

    private static async Task PerformHealthChecks()
    {
        if (_activeRunner == null) return;

        try
        {
            var isHealthy = await _activeRunner.HealthAsync();
            if (!isHealthy)
            {
                Logger.LogWarning($"Active runner {_activeRunner.Name} failed health check - attempting failover");
                await SelectDefaultRunnerAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Health check failed");
        }
    }

    public static async Task<bool> SwitchToRunnerAsync(string runnerName)
    {
        if (!_runners.TryGetValue(runnerName, out var runner))
        {
            Logger.LogWarning($"Runner not found: {runnerName}");
            return false;
        }

        if (await TestRunnerHealth(runner))
        {
            _activeRunner = runner;
            Logger.LogInformation($"Switched to runner: {runnerName}");
            return true;
        }

        Logger.LogWarning($"Cannot switch to unhealthy runner: {runnerName}");
        return false;
    }

    public static bool LoadModel(string modelPath)
    {
        try
        {
            Logger.LogInformation($"Loading model from path: {modelPath}");
            var modelName = Path.GetFileNameWithoutExtension(modelPath);
            Logger.LogInformation($"Model loaded: {modelName}");
            _activeRunner = _activeRunner ?? _runners.Values.FirstOrDefault();
            if (_activeRunner is LlamaServerRunner serverRunner)
            {
                serverRunner.SetCurrentModel(modelName);
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Model load failed: {ex.Message}");
            return false;
        }
    }

    public static bool UnloadModel()
    {
        try
        {
            Logger.LogInformation("Unloading current model");
            if (_activeRunner is null) return true; // nothing to do
            if (_activeRunner is LlamaServerRunner serverRunner)
            {
                serverRunner.SetCurrentModel(null);
                return true;
            }
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Model unload failed");
            return false;
        }
    }

    public static void SwitchTo(string runnerType, string baseUrl, string? model = null)
    {
        Logger.LogInformation($"Manual switch to {runnerType} at {baseUrl} with model {model}");

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
            throw new ArgumentException($"Invalid base URL: {baseUrl}");

        _activeRunner = runnerType.ToLowerInvariant() switch
        {
            "llama-server" => new LlamaServerRunner(runnerType, uri, model),
            "llama-cpp" => new LlamaCppRunner(uri),
            _ => throw new ArgumentException($"Unknown runner type: {runnerType}")
        };

        Logger.LogInformation($"Switched to: {_activeRunner.Name} at {_activeRunner.BaseAddress}");
    }

    public static RunnerStatus GetStatus()
    {
        return new RunnerStatus
        {
            HasActiveRunner = _activeRunner != null,
            RunnerType = _activeRunner?.Name,
            BaseUrl = _activeRunner?.BaseAddress.ToString(),
            CurrentModel = CurrentModel,
            IsHealthy = _activeRunner != null,
            AvailableRunners = _runners.Keys.ToArray(),
            ConfiguredRunners = _config.Runners.Length
        };
    }

    public static void Shutdown()
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;

        // Shutdown all embedded runners
        foreach (var runner in _runners.Values)
        {
            if (runner is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        Logger.LogInformation("Runner Registry shutdown complete");
    }
}

public record RunnerStatus
{
    public bool HasActiveRunner { get; init; }
    public string? RunnerType { get; init; }
    public string? BaseUrl { get; init; }
    public string? CurrentModel { get; init; }
    public bool IsHealthy { get; init; }
    public string[] AvailableRunners { get; init; } = Array.Empty<string>();
    public int ConfiguredRunners { get; init; }
}