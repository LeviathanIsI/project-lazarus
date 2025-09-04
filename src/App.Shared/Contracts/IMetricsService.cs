using Lazarus.App.Shared.Models;

namespace Lazarus.App.Shared.Contracts;

/// <summary>
/// Service contract for collecting and managing system metrics
/// </summary>
public interface IMetricsService
{
    /// <summary>
    /// Gets current performance metrics for the orchestrator and runners asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current performance metrics</returns>
    Task<MetricsApiResponse> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical metrics over a specified time period asynchronously
    /// </summary>
    /// <param name="hours">Number of hours to look back</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical metrics</returns>
    Task<HistoricalMetricsResponse> GetHistoricalMetricsAsync(int hours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a performance metric data point asynchronously
    /// </summary>
    /// <param name="inferenceLatencyMs">Inference latency in milliseconds</param>
    /// <param name="tokensPerSecond">Tokens processed per second</param>
    /// <param name="success">Whether the operation was successful</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task RecordMetricAsync(double inferenceLatencyMs, double tokensPerSecond, bool success = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets accumulated metrics (useful for testing) asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task ResetMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates metrics based on runner status changes asynchronously
    /// </summary>
    /// <param name="activeRunners">Current number of active runners</param>
    /// <param name="totalVramUsage">Total VRAM usage across all runners</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the operation</returns>
    Task UpdateSystemMetricsAsync(int activeRunners, double totalVramUsage, CancellationToken cancellationToken = default);
}