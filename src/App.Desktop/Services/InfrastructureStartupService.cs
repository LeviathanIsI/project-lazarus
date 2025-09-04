using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Service that coordinates the startup and management of infrastructure components
/// including the orchestrator API and llama.cpp runner processes
/// </summary>
public class InfrastructureStartupService : IHostedService, INotifyPropertyChanged, IDisposable
{
    private readonly ILogger<InfrastructureStartupService> _logger;
    private readonly OrchestratorHostService _orchestratorHost;
    private readonly RunnerProcessService _runnerProcess;
    
    private bool _isStarted;
    private bool _isHealthy;
    private string _status = "Not Started";
    private InfrastructureStartupPhase _currentPhase = InfrastructureStartupPhase.NotStarted;
    private CancellationTokenSource? _cancellationTokenSource;
    
    // Configuration constants
    private const int HealthCheckIntervalMs = 30000; // 30 seconds
    private const int StartupTimeoutMs = 60000; // 1 minute total startup timeout

    public InfrastructureStartupService(
        ILogger<InfrastructureStartupService> logger,
        OrchestratorHostService orchestratorHost,
        RunnerProcessService runnerProcess)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _orchestratorHost = orchestratorHost ?? throw new ArgumentNullException(nameof(orchestratorHost));
        _runnerProcess = runnerProcess ?? throw new ArgumentNullException(nameof(runnerProcess));
        
        // Subscribe to status changes from child services
        _orchestratorHost.PropertyChanged += OnChildServicePropertyChanged;
        _runnerProcess.PropertyChanged += OnChildServicePropertyChanged;
        
        _logger.LogInformation("InfrastructureStartupService initialized");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<InfrastructureStartupEventArgs>? StartupPhaseChanged;
    public event EventHandler<InfrastructureHealthEventArgs>? HealthStatusChanged;

    /// <summary>
    /// Gets a value indicating whether the infrastructure has been started
    /// </summary>
    public bool IsStarted
    {
        get => _isStarted;
        private set
        {
            if (_isStarted != value)
            {
                _isStarted = value;
                OnPropertyChanged(nameof(IsStarted));
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether all infrastructure components are healthy
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
                
                HealthStatusChanged?.Invoke(this, new InfrastructureHealthEventArgs
                {
                    IsHealthy = value,
                    OrchestratorHealthy = _orchestratorHost.IsHealthy,
                    RunnerHealthy = _runnerProcess.IsHealthy,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }

    /// <summary>
    /// Gets the current overall status of the infrastructure
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
                _logger.LogInformation("Infrastructure status changed to: {Status}", value);
            }
        }
    }

    /// <summary>
    /// Gets the current startup phase
    /// </summary>
    public InfrastructureStartupPhase CurrentPhase
    {
        get => _currentPhase;
        private set
        {
            if (_currentPhase != value)
            {
                _currentPhase = value;
                OnPropertyChanged(nameof(CurrentPhase));
                
                StartupPhaseChanged?.Invoke(this, new InfrastructureStartupEventArgs
                {
                    Phase = value,
                    PhaseDescription = GetPhaseDescription(value),
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation("Infrastructure startup phase changed to: {Phase}", value);
            }
        }
    }

    /// <summary>
    /// Gets the orchestrator service
    /// </summary>
    public OrchestratorHostService OrchestratorService => _orchestratorHost;

    /// <summary>
    /// Gets the runner process service
    /// </summary>
    public RunnerProcessService RunnerService => _runnerProcess;

    /// <summary>
    /// Starts all infrastructure services
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsStarted)
        {
            _logger.LogWarning("Attempt to start infrastructure when already started");
            return;
        }

        var orchestratorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        orchestratorCts.CancelAfter(StartupTimeoutMs);

        try
        {
            _logger.LogInformation("Starting Lazarus infrastructure components");
            Status = "Starting Infrastructure";
            CurrentPhase = InfrastructureStartupPhase.Initializing;

            // Phase 1: Start Orchestrator API
            CurrentPhase = InfrastructureStartupPhase.StartingOrchestrator;
            Status = "Starting Orchestrator API";
            
            await _orchestratorHost.StartAsync(orchestratorCts.Token);
            _logger.LogInformation("Orchestrator API started successfully");

            // Phase 2+: Continue startup in background to avoid blocking the UI thread
            CurrentPhase = InfrastructureStartupPhase.StartingRunner;
            Status = "Starting llama.cpp Runner";
            IsStarted = true; // Infrastructure orchestration has begun; details will settle asynchronously

            _ = Task.Run(async () =>
            {
                try
                {
                    using var bgCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    bgCts.CancelAfter(StartupTimeoutMs);

                    _logger.LogInformation("LIGHTWEIGHT STARTUP: Starting runner WITHOUT model loading - user will select models explicitly");
                    var runnerStarted = await _runnerProcess.StartAsync(cancellationToken: bgCts.Token);
                    if (runnerStarted)
                    {
                        _logger.LogInformation("Runner process started successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Runner process failed to start; continuing in degraded mode");
                    }

                    // Phase 3: Validate all services are healthy
                    CurrentPhase = InfrastructureStartupPhase.ValidatingHealth;
                    Status = "Validating Service Health";

                    var healthCheckPassed = await ValidateAllServicesHealthyAsync(bgCts.Token);
                    if (!healthCheckPassed)
                    {
                        _logger.LogWarning("Infrastructure health validation failed; continuing in degraded mode");
                    }

                    // Phase 4: Complete startup
                    CurrentPhase = InfrastructureStartupPhase.Ready;
                    Status = healthCheckPassed ? "Infrastructure Ready" : "Infrastructure Degraded";
                    IsHealthy = healthCheckPassed;

                    // Start continuous health monitoring
                    StartHealthMonitoring();

                    _logger.LogInformation("Lazarus infrastructure startup completed (background)");
                }
                catch (Exception bgEx)
                {
                    CurrentPhase = InfrastructureStartupPhase.Failed;
                    Status = "Startup Failed";
                    IsHealthy = false;
                    _logger.LogError(bgEx, "Background infrastructure startup failed");
                }
            });
        }
        catch (OperationCanceledException) when (orchestratorCts.Token.IsCancellationRequested)
        {
            CurrentPhase = InfrastructureStartupPhase.Failed;
            Status = "Startup Timeout";
            _logger.LogError("Infrastructure startup timed out after {TimeoutMs}ms", StartupTimeoutMs);
            
            await StopAsync(CancellationToken.None);
            throw new TimeoutException("Infrastructure startup timed out");
        }
        catch (Exception ex)
        {
            CurrentPhase = InfrastructureStartupPhase.Failed;
            Status = "Startup Failed";
            _logger.LogError(ex, "Infrastructure startup failed");
            
            await StopAsync(CancellationToken.None);
            throw;
        }
        finally
        {
            orchestratorCts.Dispose();
        }
    }

    /// <summary>
    /// Stops all infrastructure services
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsStarted && CurrentPhase == InfrastructureStartupPhase.NotStarted)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Stopping Lazarus infrastructure components");
            Status = "Stopping Infrastructure";
            CurrentPhase = InfrastructureStartupPhase.Stopping;

            // Cancel health monitoring
            _cancellationTokenSource?.Cancel();

            // Stop services in reverse order
            var tasks = new List<Task>
            {
                StopServiceSafelyAsync("Runner Process", () => _runnerProcess.StopAsync()),
                StopServiceSafelyAsync("Orchestrator API", () => _orchestratorHost.StopAsync(cancellationToken))
            };

            await Task.WhenAll(tasks);

            IsStarted = false;
            IsHealthy = false;
            Status = "Stopped";
            CurrentPhase = InfrastructureStartupPhase.NotStarted;

            _logger.LogInformation("Lazarus infrastructure stopped successfully");
        }
        catch (Exception ex)
        {
            Status = "Stop Failed";
            _logger.LogError(ex, "Error stopping infrastructure");
        }
    }

    /// <summary>
    /// Validates that all services are healthy
    /// </summary>
    public async Task<bool> ValidateAllServicesHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var healthTasks = new[]
            {
                _orchestratorHost.HealthCheckAsync(cancellationToken),
                _runnerProcess.HealthCheckAsync(cancellationToken)
            };

            var healthResults = await Task.WhenAll(healthTasks);
            var allHealthy = healthResults.All(result => result);

            _logger.LogInformation("Infrastructure health check - Orchestrator: {OrchestratorHealth}, Runner: {RunnerHealth}, Overall: {OverallHealth}",
                healthResults[0], healthResults[1], allHealthy);

            IsHealthy = allHealthy;
            return allHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during infrastructure health validation");
            IsHealthy = false;
            return false;
        }
    }

    /// <summary>
    /// Restarts all infrastructure services
    /// </summary>
    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restarting Lazarus infrastructure");
        
        await StopAsync(cancellationToken);
        await Task.Delay(2000, cancellationToken); // Brief pause before restart
        await StartAsync(cancellationToken);
    }

    /// <summary>
    /// Safely stops a service with error handling
    /// </summary>
    private async Task StopServiceSafelyAsync(string serviceName, Func<Task> stopAction)
    {
        try
        {
            _logger.LogDebug("Stopping {ServiceName}", serviceName);
            await stopAction();
            _logger.LogDebug("{ServiceName} stopped successfully", serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error stopping {ServiceName}", serviceName);
        }
    }

    /// <summary>
    /// Starts continuous health monitoring for all services
    /// </summary>
    private void StartHealthMonitoring()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        _ = Task.Run(async () =>
        {
            var token = _cancellationTokenSource.Token;
            
            while (!token.IsCancellationRequested && IsStarted)
            {
                try
                {
                    await Task.Delay(HealthCheckIntervalMs, token);
                    
                    if (!token.IsCancellationRequested && IsStarted)
                    {
                        var previousHealth = IsHealthy;
                        await ValidateAllServicesHealthyAsync(token);
                        
                        if (previousHealth != IsHealthy)
                        {
                            Status = IsHealthy ? "Infrastructure Ready" : "Infrastructure Unhealthy";
                            _logger.LogInformation("Infrastructure health status changed to: {IsHealthy}", IsHealthy);
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
                    _logger.LogError(ex, "Error in infrastructure health monitoring loop");
                    await Task.Delay(5000, token); // Wait before retrying
                }
            }
        }, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// Handles property changes from child services
    /// </summary>
    private void OnChildServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RunnerProcessService.IsHealthy) || 
            e.PropertyName == nameof(OrchestratorHostService.IsHealthy))
        {
            // Update overall health when child services change
            var newHealthStatus = _orchestratorHost.IsHealthy && _runnerProcess.IsHealthy;
            if (newHealthStatus != IsHealthy)
            {
                IsHealthy = newHealthStatus;
            }
        }
    }

    /// <summary>
    /// Gets the description for a startup phase
    /// </summary>
    private static string GetPhaseDescription(InfrastructureStartupPhase phase)
    {
        return phase switch
        {
            InfrastructureStartupPhase.NotStarted => "Infrastructure not started",
            InfrastructureStartupPhase.Initializing => "Initializing infrastructure components",
            InfrastructureStartupPhase.StartingOrchestrator => "Starting Lazarus Orchestrator API",
            InfrastructureStartupPhase.StartingRunner => "Starting llama.cpp runner process",
            InfrastructureStartupPhase.ValidatingHealth => "Validating service health",
            InfrastructureStartupPhase.Ready => "All infrastructure components ready",
            InfrastructureStartupPhase.Stopping => "Stopping infrastructure components",
            InfrastructureStartupPhase.Failed => "Infrastructure startup failed",
            _ => "Unknown phase"
        };
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
        
        if (IsStarted)
        {
            _ = Task.Run(async () => await StopAsync());
        }
        
        _orchestratorHost?.Dispose();
        _runnerProcess?.Dispose();
    }
}

/// <summary>
/// Represents the phases of infrastructure startup
/// </summary>
public enum InfrastructureStartupPhase
{
    NotStarted,
    Initializing,
    StartingOrchestrator,
    StartingRunner,
    ValidatingHealth,
    Ready,
    Stopping,
    Failed
}

/// <summary>
/// Event arguments for infrastructure startup phase changes
/// </summary>
public class InfrastructureStartupEventArgs : EventArgs
{
    public InfrastructureStartupPhase Phase { get; set; }
    public string PhaseDescription { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event arguments for infrastructure health status changes
/// </summary>
public class InfrastructureHealthEventArgs : EventArgs
{
    public bool IsHealthy { get; set; }
    public bool OrchestratorHealthy { get; set; }
    public bool RunnerHealthy { get; set; }
    public DateTime Timestamp { get; set; }
}