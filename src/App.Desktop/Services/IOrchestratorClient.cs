namespace Lazarus.Desktop.Services;

/// <summary>
/// Client service for communicating with the Lazarus orchestrator API.
/// </summary>
public interface IOrchestratorClient : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the orchestrator is currently healthy.
    /// </summary>
    bool IsHealthy { get; }

    /// <summary>
    /// Performs a health check against the orchestrator API.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous health check operation.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available models from the orchestrator.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a model runner with the specified configuration.
    /// </summary>
    /// <param name="modelId">The ID of the model to start.</param>
    /// <param name="configuration">The runner configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<RunnerInfo> StartRunnerAsync(string modelId, RunnerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the specified model runner.
    /// </summary>
    /// <param name="runnerId">The ID of the runner to stop.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopRunnerAsync(string runnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of all active runners.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<IEnumerable<RunnerStatus>> GetRunnerStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the orchestrator health status changes.
    /// </summary>
    event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;
}

/// <summary>
/// Information about an available model.
/// </summary>
public sealed record ModelInfo(
    string Id,
    string Name,
    string Path,
    long SizeBytes,
    string Architecture,
    string[] SupportedRunners
);

/// <summary>
/// Configuration for starting a model runner.
/// </summary>
public sealed record RunnerConfiguration(
    string RunnerType,
    int Port,
    Dictionary<string, object>? Parameters = null
);

/// <summary>
/// Information about a running model instance.
/// </summary>
public sealed record RunnerInfo(
    string Id,
    string ModelId,
    string RunnerType,
    int Port,
    DateTime StartedAt
);

/// <summary>
/// Status information for a model runner.
/// </summary>
public sealed record RunnerStatus(
    string Id,
    string ModelId,
    string RunnerType,
    int Port,
    bool IsHealthy,
    DateTime LastHealthCheck,
    string? ErrorMessage = null
);

/// <summary>
/// Event arguments for health status change events.
/// </summary>
public sealed class HealthStatusChangedEventArgs : EventArgs
{
    public HealthStatusChangedEventArgs(bool isHealthy, string? errorMessage = null)
    {
        IsHealthy = isHealthy;
        ErrorMessage = errorMessage;
    }

    public bool IsHealthy { get; }
    public string? ErrorMessage { get; }
}