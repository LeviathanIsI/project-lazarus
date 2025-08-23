using System.Net.Http.Json;
using System.Text.Json;
using Lazarus.Shared.OpenAI;
using Microsoft.Extensions.Logging;

namespace Lazarus.Orchestrator.Runners;

public sealed class LlamaCppEmbeddedRunner : IChatRunner, IDisposable
{
    private readonly ProcessRunner _processRunner;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ILogger _logger;

    public string Name { get; }
    public Uri BaseAddress { get; }
    public bool IsProcessRunning => _processRunner.IsRunning;

    private readonly string _executablePath;
    private readonly string _modelPath;
    private readonly LlamaCppConfig _config;

    public LlamaCppEmbeddedRunner(string name, LlamaCppConfig config, ILogger logger)
    {
        Name = name;
        _config = config;
        _logger = logger;

        BaseAddress = new Uri($"http://127.0.0.1:{config.Port}");
        _httpClient = new HttpClient { BaseAddress = BaseAddress, Timeout = TimeSpan.FromMinutes(5) };

        _executablePath = GetLlamaCppExecutable();
        _modelPath = config.ModelPath;

        _processRunner = new ProcessRunner($"llama-server-{config.Port}", logger);
        _processRunner.OutputReceived += OnLlamaOutput;
        _processRunner.ErrorReceived += OnLlamaError;
        _processRunner.ProcessExited += OnLlamaExited;
    }

    private static string GetLlamaCppExecutable()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var binariesPath = Path.Combine(baseDir, "binaries");

        var executable = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "llama-server.exe",
            PlatformID.Unix => "llama-server",
            PlatformID.MacOSX => "llama-server",
            _ => throw new PlatformNotSupportedException("Unsupported platform")
        };

        var fullPath = Path.Combine(binariesPath, executable);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"llama-server executable not found at: {fullPath}");
        }

        // Ensure executable permissions on Unix-like systems
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            try
            {
                var chmod = new ProcessRunner("chmod", LoggerFactory.Create(b => b.AddConsole()).CreateLogger("chmod"));
                _ = chmod.StartAsync("chmod", $"+x {fullPath}");
            }
            catch
            {
                // Best effort - might already be executable
            }
        }

        return fullPath;
    }

    public async Task<bool> StartAsync()
    {
        if (_processRunner.IsRunning)
        {
            _logger.LogInformation($"llama.cpp server already running on port {_config.Port}");
            return true;
        }

        if (!File.Exists(_modelPath))
        {
            _logger.LogError($"Model file not found: {_modelPath}");
            return false;
        }

        var arguments = BuildLlamaServerArguments();
        _logger.LogInformation($"Starting embedded llama.cpp: {arguments}");

        var started = await _processRunner.StartAsync(_executablePath, arguments);
        if (!started) return false;

        // Wait for server to be ready
        return await WaitForServerReady(TimeSpan.FromMinutes(2));
    }

    private string BuildLlamaServerArguments()
    {
        var args = new List<string>
        {
            $"--model \"{_modelPath}\"",
            $"--port {_config.Port}",
            $"--host {_config.Host}",
            $"--ctx-size {_config.ContextSize}",
            $"--threads {_config.Threads}",
            "--log-disable", // Reduce noise unless debugging
        };

        if (_config.UseGpu)
        {
            args.Add($"--n-gpu-layers {_config.GpuLayers}");
        }

        if (_config.UseMlock)
        {
            args.Add("--mlock");
        }

        if (_config.Verbose)
        {
            args.Remove("--log-disable");
            args.Add("--verbose");
        }

        return string.Join(" ", args);
    }

    private async Task<bool> WaitForServerReady(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        _logger.LogInformation($"Waiting for llama.cpp server to be ready on {BaseAddress}");

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (await HealthAsync())
                {
                    _logger.LogInformation("llama.cpp server is ready");
                    return true;
                }
            }
            catch
            {
                // Expected during startup
            }

            await Task.Delay(1000);
        }

        _logger.LogError("llama.cpp server failed to become ready within timeout");
        return false;
    }

    public async Task<bool> HealthAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_processRunner.IsRunning) return false;

            using var response = await _httpClient.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ChatCompletionResponse> ChatAsync(ChatCompletionRequest req, CancellationToken ct = default)
    {
        if (!_processRunner.IsRunning)
        {
            throw new InvalidOperationException("llama.cpp server is not running");
        }

        try
        {
            _logger.LogDebug($"Sending chat request: {JsonSerializer.Serialize(req, _jsonOptions)}");

            using var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", req, _jsonOptions, ct);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogDebug($"Received response: {responseContent}");

            var parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, _jsonOptions);
            return parsed ?? throw new InvalidOperationException("Failed to parse response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat request failed");
            throw;
        }
    }

    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping embedded llama.cpp server");
        await _processRunner.StopAsync();
    }

    private void OnLlamaOutput(object? sender, string output)
    {
        if (_config.Verbose)
        {
            _logger.LogInformation($"[llama.cpp] {output}");
        }
    }

    private void OnLlamaError(object? sender, string error)
    {
        _logger.LogWarning($"[llama.cpp] {error}");
    }

    private void OnLlamaExited(object? sender, int exitCode)
    {
        _logger.LogWarning($"llama.cpp server exited with code {exitCode}");
    }

    public void Dispose()
    {
        Task.Run(StopAsync).Wait(TimeSpan.FromSeconds(10));
        _processRunner?.Dispose();
        _httpClient?.Dispose();
    }
}

public record LlamaCppConfig
{
    public string ModelPath { get; init; } = "";
    public int Port { get; init; } = 8080;
    public string Host { get; init; } = "127.0.0.1";
    public int ContextSize { get; init; } = 4096;
    public int Threads { get; init; } = Environment.ProcessorCount;
    public bool UseGpu { get; init; } = true;
    public int GpuLayers { get; init; } = 35;
    public bool UseMlock { get; init; } = false;
    public bool Verbose { get; init; } = false;
}