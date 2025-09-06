namespace Lazarus.Desktop.Configuration;

/// <summary>
/// Configuration options for orchestrator API communication.
/// </summary>
public sealed class OrchestratorOptions
{
    public const string SectionName = "Orchestrator";

    /// <summary>
    /// Base URL for the orchestrator API.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Interval between health checks.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Timeout for HTTP requests to the orchestrator.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMinutes(5);
}