using System.ComponentModel;

namespace Lazarus.App.Shared.Services;

/// <summary>
/// Service for monitoring system status and performance metrics
/// </summary>
public interface ISystemStatusService : INotifyPropertyChanged
{
    /// <summary>
    /// Event raised when system status changes
    /// </summary>
    event EventHandler<SystemStatusChangedEventArgs>? StatusChanged;

    /// <summary>
    /// Gets the current system status
    /// </summary>
    SystemStatus CurrentStatus { get; }

    /// <summary>
    /// Gets system performance metrics
    /// </summary>
    SystemPerformanceMetrics Performance { get; }

    /// <summary>
    /// Gets the number of active models currently loaded
    /// </summary>
    int ActiveModels { get; }

    /// <summary>
    /// Gets the number of running jobs/tasks
    /// </summary>
    int RunningJobs { get; }

    /// <summary>
    /// Gets the current system load percentage (0-100)
    /// </summary>
    double SystemLoadPercentage { get; }

    /// <summary>
    /// Gets the current GPU utilization percentage (0-100)
    /// </summary>
    double GpuUtilization { get; }

    /// <summary>
    /// Gets the current memory usage percentage (0-100)
    /// </summary>
    double MemoryUsage { get; }

    /// <summary>
    /// Gets the total training hours accumulated
    /// </summary>
    int TotalTrainingHours { get; }

    /// <summary>
    /// Starts monitoring system status
    /// </summary>
    Task StartMonitoringAsync();

    /// <summary>
    /// Stops monitoring system status
    /// </summary>
    Task StopMonitoringAsync();

    /// <summary>
    /// Refreshes system metrics manually
    /// </summary>
    Task RefreshMetricsAsync();

    /// <summary>
    /// Gets detailed system information
    /// </summary>
    Task<SystemInfo> GetSystemInfoAsync();
}

/// <summary>
/// Overall system status enumeration
/// </summary>
public enum SystemStatus
{
    /// <summary>
    /// System is starting up
    /// </summary>
    Starting,

    /// <summary>
    /// System is ready and idle
    /// </summary>
    Ready,

    /// <summary>
    /// System is busy processing tasks
    /// </summary>
    Busy,

    /// <summary>
    /// System is training models
    /// </summary>
    Training,

    /// <summary>
    /// System has warnings but is functional
    /// </summary>
    Warning,

    /// <summary>
    /// System has errors or is in critical state
    /// </summary>
    Error,

    /// <summary>
    /// System is shutting down
    /// </summary>
    Shutdown
}

/// <summary>
/// System performance metrics container
/// </summary>
public class SystemPerformanceMetrics
{
    /// <summary>
    /// Gets or sets CPU usage percentage
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Gets or sets RAM usage percentage
    /// </summary>
    public double RamUsage { get; set; }

    /// <summary>
    /// Gets or sets GPU usage percentage
    /// </summary>
    public double GpuUsage { get; set; }

    /// <summary>
    /// Gets or sets GPU memory usage percentage
    /// </summary>
    public double GpuMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets disk usage percentage
    /// </summary>
    public double DiskUsage { get; set; }

    /// <summary>
    /// Gets or sets network usage in MB/s
    /// </summary>
    public double NetworkUsage { get; set; }

    /// <summary>
    /// Gets or sets current temperature in Celsius
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Gets or sets power consumption in watts
    /// </summary>
    public double PowerConsumption { get; set; }

    /// <summary>
    /// Gets or sets timestamp when metrics were captured
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Detailed system information
/// </summary>
public class SystemInfo
{
    /// <summary>
    /// Gets or sets the operating system information
    /// </summary>
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// Gets or sets the CPU model and specifications
    /// </summary>
    public string? CpuInfo { get; set; }

    /// <summary>
    /// Gets or sets the total system RAM in GB
    /// </summary>
    public double TotalRamGb { get; set; }

    /// <summary>
    /// Gets or sets the GPU model and specifications
    /// </summary>
    public string? GpuInfo { get; set; }

    /// <summary>
    /// Gets or sets the total GPU memory in GB
    /// </summary>
    public double TotalGpuMemoryGb { get; set; }

    /// <summary>
    /// Gets or sets available disk space in GB
    /// </summary>
    public double AvailableDiskSpaceGb { get; set; }

    /// <summary>
    /// Gets or sets the system uptime
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Gets or sets the application version
    /// </summary>
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// Gets or sets supported ML frameworks
    /// </summary>
    public List<string> SupportedFrameworks { get; set; } = new();
}

/// <summary>
/// Event arguments for system status change notifications
/// </summary>
public class SystemStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SystemStatusChangedEventArgs"/> class
    /// </summary>
    /// <param name="previousStatus">The previous system status</param>
    /// <param name="newStatus">The new system status</param>
    /// <param name="message">Optional status message</param>
    public SystemStatusChangedEventArgs(SystemStatus previousStatus, SystemStatus newStatus, string? message = null)
    {
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Message = message;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the previous system status
    /// </summary>
    public SystemStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the new system status
    /// </summary>
    public SystemStatus NewStatus { get; }

    /// <summary>
    /// Gets the status change message
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the timestamp when the status change occurred
    /// </summary>
    public DateTime Timestamp { get; }
}