namespace Lazarus.App.Shared.Models;

/// <summary>
/// API response model for performance metrics
/// </summary>
public class MetricsApiResponse
{
    /// <summary>
    /// Gets or sets the average inference latency in milliseconds
    /// </summary>
    public double AverageInferenceLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the tokens processed per second
    /// </summary>
    public double TokensPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the total number of requests processed
    /// </summary>
    public int TotalRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of failed requests
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when these metrics were collected
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the success rate as a percentage
    /// </summary>
    public double SuccessRate => TotalRequests > 0 ? ((double)(TotalRequests - FailedRequests) / TotalRequests) * 100 : 100;

    /// <summary>
    /// Gets or sets any error message
    /// </summary>
    public string? Error { get; set; }
}