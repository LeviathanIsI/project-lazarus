namespace Lazarus.App.Orchestrator.Host.Services;

/// <summary>
/// Configuration options for the Orchestrator Host
/// </summary>
public class OrchestratorHostOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "OrchestratorHost";

    /// <summary>
    /// Gets or sets the interval in seconds for process monitoring
    /// </summary>
    public int ProcessMonitoringIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether resource monitoring is enabled
    /// </summary>
    public bool EnableResourceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the CPU usage threshold percentage for warnings
    /// </summary>
    public double CpuThresholdPercent { get; set; } = 80.0;

    /// <summary>
    /// Gets or sets the memory usage threshold in MB for warnings
    /// </summary>
    public long MemoryThresholdMB { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the disk usage threshold percentage for warnings
    /// </summary>
    public double DiskThresholdPercent { get; set; } = 90.0;

    /// <summary>
    /// Gets or sets the maximum number of concurrent training processes
    /// </summary>
    public int MaxConcurrentTrainingProcesses { get; set; } = 4;

    /// <summary>
    /// Gets or sets the timeout in minutes for training process health checks
    /// </summary>
    public int TrainingProcessTimeoutMinutes { get; set; } = 30;
}