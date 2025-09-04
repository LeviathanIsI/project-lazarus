namespace Lazarus.App.Shared.Models;

/// <summary>
/// Individual runner status information
/// </summary>
public class RunnerStatus
{
    /// <summary>
    /// Gets or sets the runner identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the runner display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status (active, idle, stopped, error)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name this runner is handling
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the VRAM usage in MB
    /// </summary>
    public double VramUsageMb { get; set; }

    /// <summary>
    /// Gets or sets the uptime in seconds
    /// </summary>
    public long UpTimeSeconds { get; set; }

    /// <summary>
    /// Gets or sets the last activity timestamp
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Gets the uptime as a TimeSpan
    /// </summary>
    public TimeSpan UpTime => TimeSpan.FromSeconds(UpTimeSeconds);

    /// <summary>
    /// Gets the VRAM usage in GB
    /// </summary>
    public double VramUsageGb => VramUsageMb / 1024.0;

    /// <summary>
    /// Gets or sets the runner endpoint URL
    /// </summary>
    public string? EndpointUrl { get; set; }

    /// <summary>
    /// Gets or sets the process ID if running locally
    /// </summary>
    public int? ProcessId { get; set; }
}

/// <summary>
/// API response model for runner statuses
/// </summary>
public class RunnerApiResponse
{
    /// <summary>
    /// Gets or sets the collection of runners
    /// </summary>
    public List<RunnerStatus> Runners { get; set; } = new();

    /// <summary>
    /// Gets or sets any error message
    /// </summary>
    public string? Error { get; set; }
}