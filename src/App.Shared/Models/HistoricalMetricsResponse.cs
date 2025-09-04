namespace Lazarus.App.Shared.Models;

/// <summary>
/// Historical metrics response model
/// </summary>
public class HistoricalMetricsResponse
{
    /// <summary>
    /// Gets or sets the time range covered by the metrics
    /// </summary>
    public TimeSpan TimeRange { get; set; }

    /// <summary>
    /// Gets or sets the collection of metrics data points
    /// </summary>
    public List<MetricsDataPoint> DataPoints { get; set; } = new();

    /// <summary>
    /// Gets or sets any error message
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Individual metrics data point
/// </summary>
public class MetricsDataPoint
{
    /// <summary>
    /// Gets or sets the timestamp of the data point
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the inference latency at this point
    /// </summary>
    public double InferenceLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the tokens per second at this point
    /// </summary>
    public double TokensPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the request count at this point
    /// </summary>
    public int RequestCount { get; set; }

    /// <summary>
    /// Gets or sets the active runner count at this point
    /// </summary>
    public int ActiveRunners { get; set; }
}