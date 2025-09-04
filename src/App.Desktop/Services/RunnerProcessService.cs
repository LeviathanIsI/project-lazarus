using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.IO;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Service for managing llama.cpp binary process execution and lifecycle
/// </summary>
public class RunnerProcessService : INotifyPropertyChanged, IDisposable
{
    private readonly ILogger<RunnerProcessService> _logger;
    private readonly IDirectoryService _directoryService;
    private readonly HttpClient _httpClient;
    
    private Process? _llamaProcess;
    private bool _isRunning;
    private bool _isHealthy;
    private string _status = "Stopped";
    private readonly string _binaryPath;
    private readonly string _serverExecutable;
    private CancellationTokenSource? _cancellationTokenSource;
    
    // Configuration constants
    private const int DefaultPort = 11712;
    private const int HealthCheckTimeoutMs = 5000;
    private const int StartupTimeoutMs = 90000;
    private const int HealthCheckIntervalMs = 10000;
    
    public RunnerProcessService(ILogger<RunnerProcessService> logger, IDirectoryService directoryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(HealthCheckTimeoutMs) };

        _binaryPath = ResolveBinariesPath();
        _serverExecutable = Path.Combine(_binaryPath, "llama-server.exe");

        _logger.LogInformation("RunnerProcessService initialized with binary path: {BinaryPath}", _binaryPath);
    }

    private static string ResolveBinariesPath()
    {
        // Prefer a workspace-level 'binaries/llama-b6367-bin-win-cuda-12.4-x64' folder.
        var candidateDirNames = new[]
        {
            Path.Combine("binaries", "llama-b6367-bin-win-cuda-12.4-x64"),
            Path.Combine("..", "..", "binaries", "llama-b6367-bin-win-cuda-12.4-x64")
        };

        // Walk up from current directory to find the first matching directory
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var dir = current; dir != null; dir = dir.Parent)
        {
            foreach (var candidate in candidateDirNames)
            {
                var full = Path.GetFullPath(Path.Combine(dir.FullName, candidate));
                if (Directory.Exists(full))
                {
                    return full;
                }
            }
        }

        // Fallback to the relative path used previously
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "binaries", "llama-b6367-bin-win-cuda-12.4-x64"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets a value indicating whether the llama.cpp server is running
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the llama.cpp server is healthy and responsive
    /// </summary>
    public bool IsHealthy
    {
        get => _isHealthy;
        private set
        {
            if (_isHealthy != value)
            {
                _isHealthy = value;
                OnPropertyChanged(nameof(IsHealthy));
            }
        }
    }

    /// <summary>
    /// Gets the current status of the runner process
    /// </summary>
    public string Status
    {
        get => _status;
        private set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                _logger.LogInformation("Runner process status changed to: {Status}", value);
            }
        }
    }

    /// <summary>
    /// Gets the server port
    /// </summary>
    public int ServerPort { get; private set; } = DefaultPort;

    /// <summary>
    /// Gets the server API endpoint
    /// </summary>
    public string ApiEndpoint => $"http://127.0.0.1:{ServerPort}";

    /// <summary>
    /// Starts the llama.cpp server process
    /// </summary>
    /// <param name="modelPath">Optional specific model path to load</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if started successfully</returns>
    public async Task<bool> StartAsync(string? modelPath = null, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Attempt to start runner process when already running");
            return true;
        }

        try
        {
            Status = "Starting";
            _logger.LogInformation("Starting llama.cpp server process");

            // Validate binary exists
            if (!File.Exists(_serverExecutable))
            {
                Status = "Error: Binary not found";
                _logger.LogError("llama-server.exe not found at: {ExecutablePath}", _serverExecutable);
                return false;
            }

            // Determine model path - NO AUTOMATIC LOADING, user-controlled only
            var effectiveModelPath = await ResolveModelPathAsync(modelPath);
            if (string.IsNullOrEmpty(effectiveModelPath))
            {
                _logger.LogInformation("AUTHORIZED STARTUP: Starting llama-server without model - user will select model explicitly");
            }
            else
            {
                _logger.LogInformation("AUTHORIZED STARTUP: Starting llama-server with explicitly provided model: {ModelPath}", effectiveModelPath);
            }

            // Create process start info
            var startInfo = CreateProcessStartInfo(effectiveModelPath ?? string.Empty);
            // Ensure PATH includes binary folder for DLL resolution (CUDA/cuBLAS)
            var existingPath = startInfo.Environment.TryGetValue("PATH", out var pathVar)
                ? pathVar
                : Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            startInfo.Environment["PATH"] = string.IsNullOrEmpty(existingPath)
                ? _binaryPath
                : _binaryPath + ";" + existingPath;
            
            // Start the process
            _llamaProcess = new Process { StartInfo = startInfo };
            _llamaProcess.EnableRaisingEvents = true;
            _llamaProcess.Exited += OnProcessExited;
            _llamaProcess.OutputDataReceived += OnOutputDataReceived;
            _llamaProcess.ErrorDataReceived += OnErrorDataReceived;

            if (!_llamaProcess.Start())
            {
                Status = "Error: Failed to start";
                _logger.LogError("Failed to start llama.cpp server process");
                return false;
            }

            _llamaProcess.BeginOutputReadLine();
            _llamaProcess.BeginErrorReadLine();

            IsRunning = true;
            Status = "Starting server";
            
            _logger.LogInformation("llama.cpp server start: {Exe} {Args}", startInfo.FileName, startInfo.Arguments);
            _logger.LogInformation("WorkingDirectory: {WD}", startInfo.WorkingDirectory);
            _logger.LogInformation("llama.cpp server process started with PID {ProcessId}", _llamaProcess.Id);

            // Wait for server to become ready
            var isReady = await WaitForServerReadyAsync(cancellationToken);
            if (isReady)
            {
                Status = "Running";
                IsHealthy = true;
                StartHealthMonitoring();
                _logger.LogInformation("llama.cpp server is ready and accepting requests");
                return true;
            }
            else
            {
                Status = "Error: Server not ready";
                await StopAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            Status = "Error: Startup failed";
            _logger.LogError(ex, "Error starting llama.cpp server");
            await StopAsync();
            return false;
        }
    }

    /// <summary>
    /// Stops the llama.cpp server process
    /// </summary>
    public Task StopAsync()
    {
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }

        try
        {
            Status = "Stopping";
            _logger.LogInformation("Stopping llama.cpp server process");

            // Cancel health monitoring
            _cancellationTokenSource?.Cancel();

            if (_llamaProcess != null && !_llamaProcess.HasExited)
            {
                // Try graceful shutdown first
                try
                {
                    _llamaProcess.CloseMainWindow();
                    var gracefulShutdown = _llamaProcess.WaitForExit(5000);
                    
                    if (!gracefulShutdown)
                    {
                        _logger.LogWarning("Graceful shutdown timed out, forcing termination");
                        _llamaProcess.Kill();
                        _llamaProcess.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during process shutdown, attempting force kill");
                    try
                    {
                        _llamaProcess.Kill();
                    }
                    catch
                    {
                        // Process might have already exited
                    }
                }

                _llamaProcess?.Dispose();
                _llamaProcess = null;
            }

            IsRunning = false;
            IsHealthy = false;
            Status = "Stopped";
            
            _logger.LogInformation("llama.cpp server process stopped");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Status = "Error: Stop failed";
            _logger.LogError(ex, "Error stopping llama.cpp server");
            return Task.FromException(ex);
        }
    }

    /// <summary>
    /// Restarts the llama.cpp server process
    /// </summary>
    public async Task<bool> RestartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restarting llama.cpp server process");
        await StopAsync();
        await Task.Delay(2000, cancellationToken); // Brief delay before restart
        return await StartAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs a health check on the server
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return false;
        }

        try
        {
            var healthUrl = $"{ApiEndpoint}/health";
            using var response = await _httpClient.GetAsync(healthUrl, cancellationToken);
            var isHealthy = response.IsSuccessStatusCode;
            
            IsHealthy = isHealthy;
            
            if (!isHealthy)
            {
                _logger.LogWarning("Health check failed: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            }

            return isHealthy;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug("Health check HTTP error: {Message}", ex.Message);
            IsHealthy = false;
            return false;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogDebug("Health check timed out");
            IsHealthy = false;
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check error");
            IsHealthy = false;
            return false;
        }
    }

    /// <summary>
    /// Resolves the model path to use for the server - NO AUTOMATIC DISCOVERY
    /// Only returns a model path if explicitly provided and validated
    /// </summary>
    private Task<string?> ResolveModelPathAsync(string? explicitPath)
    {
        // SECURITY: Only load models that are explicitly provided
        // NO AUTOMATIC MODEL DISCOVERY OR LOADING
        if (!string.IsNullOrEmpty(explicitPath) && File.Exists(explicitPath))
        {
            _logger.LogInformation("Using explicitly provided model path: {ModelPath}", explicitPath);
            return Task.FromResult<string?>(explicitPath);
        }

        if (!string.IsNullOrEmpty(explicitPath))
        {
            _logger.LogWarning("Explicitly provided model path does not exist: {ModelPath}", explicitPath);
        }

        // ELIMINATED: Automatic model discovery and loading
        // User must explicitly select a model for loading
        _logger.LogInformation("No explicit model path provided - starting runner without model for user selection");
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// Creates the process start info for llama-server
    /// </summary>
    private ProcessStartInfo CreateProcessStartInfo(string modelPath)
    {
        var args = new List<string>
        {
            "--port", ServerPort.ToString(),
            "--host", "127.0.0.1",
            "--ctx-size", "4096",
            "--threads", Environment.ProcessorCount.ToString(),
            "--n-gpu-layers", "-1",
            "--parallel", "2",
            "--no-mmap",
            "--verbose"
        };

        if (!string.IsNullOrWhiteSpace(modelPath))
        {
            args.Insert(0, $"\"{modelPath}\"");
            args.Insert(0, "--model");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _serverExecutable,
            Arguments = string.Join(" ", args),
            WorkingDirectory = _binaryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
            Environment = { ["CUDA_VISIBLE_DEVICES"] = "0" } // Enable first CUDA device
        };

        return startInfo;
    }

    /// <summary>
    /// Waits for the server to become ready
    /// </summary>
    private async Task<bool> WaitForServerReadyAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(StartupTimeoutMs);
        var delayMs = 200;

        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
        {
            // Early exit if process died
            if (_llamaProcess != null && _llamaProcess.HasExited)
            {
                _logger.LogWarning("llama.cpp server process exited during readiness wait (code {Code})", _llamaProcess.ExitCode);
                return false;
            }

            if (await HealthCheckAsync(cancellationToken))
            {
                return true;
            }

            await Task.Delay(delayMs, cancellationToken);
            delayMs = Math.Min((int)(delayMs * 1.25), 1000);
        }

        return false;
    }

    /// <summary>
    /// Starts continuous health monitoring
    /// </summary>
    private void StartHealthMonitoring()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _ = Task.Run(async () =>
        {
            var token = _cancellationTokenSource.Token;
            
            while (!token.IsCancellationRequested && IsRunning)
            {
                try
                {
                    await Task.Delay(HealthCheckIntervalMs, token);
                    
                    if (!token.IsCancellationRequested && IsRunning)
                    {
                        var isHealthy = await HealthCheckAsync(token);
                        
                        if (!isHealthy && IsRunning)
                        {
                            Status = "Unhealthy";
                            _logger.LogWarning("Health check failed, server may be unresponsive");
                        }
                        else if (isHealthy && Status == "Unhealthy")
                        {
                            Status = "Running";
                            _logger.LogInformation("Server health restored");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in health monitoring loop");
                    await Task.Delay(5000, token); // Wait before retrying
                }
            }
        }, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Handles process exit events
    /// </summary>
    private void OnProcessExited(object? sender, EventArgs e)
    {
        _logger.LogWarning("llama.cpp server process exited unexpectedly");
        
        IsRunning = false;
        IsHealthy = false;
        Status = "Exited";
        
        _cancellationTokenSource?.Cancel();
        
        if (_llamaProcess != null)
        {
            _logger.LogInformation("Process exit code: {ExitCode}", _llamaProcess.ExitCode);
            _llamaProcess?.Dispose();
            _llamaProcess = null;
        }
    }

    /// <summary>
    /// Handles standard output from the process
    /// </summary>
    private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger.LogDebug("llama.cpp: {Output}", e.Data);
        }
    }

    /// <summary>
    /// Handles error output from the process
    /// </summary>
    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger.LogWarning("llama.cpp stderr: {Error}", e.Data);
        }
    }

    /// <summary>
    /// Raises the PropertyChanged event
    /// </summary>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Disposes of resources
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        
        if (IsRunning)
        {
            _ = Task.Run(async () => await StopAsync());
        }
        
        _llamaProcess?.Dispose();
        _httpClient?.Dispose();
    }
}