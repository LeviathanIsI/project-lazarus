using Lazarus.App.Shared.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Implementation of system status service for monitoring performance and status
/// </summary>
public class SystemStatusService : ISystemStatusService
{
    private readonly ILogger<SystemStatusService> _logger;
    private readonly DispatcherTimer _monitoringTimer;
    private readonly HardwareMonitoringService _hardwareMonitor;
    private readonly OrchestratorHealthService _orchestratorHealth;
    private readonly PerformanceAnalyzer _performanceAnalyzer;
    private readonly Random _random = new();
    
    private SystemStatus _currentStatus = SystemStatus.Ready;
    private SystemPerformanceMetrics _performance = new();
    private int _activeModels = 0;
    private int _runningJobs = 0;
    private int _totalTrainingHours = 0;
    private bool _isMonitoring = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemStatusService"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="hardwareMonitor">Hardware monitoring service</param>
    /// <param name="orchestratorHealth">Orchestrator health service</param>
    public SystemStatusService(
        ILogger<SystemStatusService> logger,
        HardwareMonitoringService hardwareMonitor,
        OrchestratorHealthService orchestratorHealth,
        PerformanceAnalyzer performanceAnalyzer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hardwareMonitor = hardwareMonitor ?? throw new ArgumentNullException(nameof(hardwareMonitor));
        _orchestratorHealth = orchestratorHealth ?? throw new ArgumentNullException(nameof(orchestratorHealth));
        _performanceAnalyzer = performanceAnalyzer ?? throw new ArgumentNullException(nameof(performanceAnalyzer));
        
        // Initialize monitoring timer
        _monitoringTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2) // Update every 2 seconds for smoother real-time feel
        };
        _monitoringTimer.Tick += MonitoringTimer_Tick;
        
        // Subscribe to real-time hardware and orchestrator events
        _hardwareMonitor.MetricsUpdated += OnHardwareMetricsUpdated;
        _orchestratorHealth.HealthUpdated += OnOrchestratorHealthUpdated;
        
        // Initialize with baseline data
        InitializeBaselineData();
        
        _logger.LogInformation("Real-time system status service initialized with hardware and orchestrator monitoring");
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event EventHandler<SystemStatusChangedEventArgs>? StatusChanged;

    /// <inheritdoc/>
    public SystemStatus CurrentStatus => _currentStatus;

    /// <inheritdoc/>
    public SystemPerformanceMetrics Performance => _performance;

    /// <inheritdoc/>
    public int ActiveModels => _activeModels;

    /// <inheritdoc/>
    public int RunningJobs => _runningJobs;

    /// <inheritdoc/>
    public double SystemLoadPercentage => _performance.CpuUsage;

    /// <inheritdoc/>
    public double GpuUtilization => _performance.GpuUsage;

    /// <inheritdoc/>
    public double MemoryUsage => _performance.RamUsage;

    /// <inheritdoc/>
    public int TotalTrainingHours => _totalTrainingHours;

    /// <inheritdoc/>
    public async Task StartMonitoringAsync()
    {
        if (!_isMonitoring)
        {
            _isMonitoring = true;
            _monitoringTimer.Start();
            
            await UpdateSystemStatusAsync(SystemStatus.Ready, "System monitoring started");
            _logger.LogInformation("System monitoring started");
        }
    }

    /// <inheritdoc/>
    public async Task StopMonitoringAsync()
    {
        if (_isMonitoring)
        {
            _isMonitoring = false;
            _monitoringTimer.Stop();
            
            await UpdateSystemStatusAsync(SystemStatus.Shutdown, "System monitoring stopped");
            _logger.LogInformation("System monitoring stopped");
        }
    }

    /// <inheritdoc/>
    public async Task RefreshMetricsAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        await Task.Run(() =>
        {
            try
            {
                UpdatePerformanceMetrics();
                UpdateSystemCounters();
                OnPropertyChanged(nameof(Performance));
                OnPropertyChanged(nameof(ActiveModels));
                OnPropertyChanged(nameof(RunningJobs));
                OnPropertyChanged(nameof(SystemLoadPercentage));
                OnPropertyChanged(nameof(GpuUtilization));
                OnPropertyChanged(nameof(MemoryUsage));
                OnPropertyChanged(nameof(TotalTrainingHours));
                
                _logger.LogDebug("System metrics refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing system metrics");
            }
        });
        
        stopwatch.Stop();
        _performanceAnalyzer.RecordUpdate(stopwatch.Elapsed);
        
        // Periodically check for refresh rate optimization
        if (DateTime.UtcNow.Second % 30 == 0) // Every 30 seconds
        {
            OptimizeRefreshRate();
        }
    }

    /// <inheritdoc/>
    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                return new SystemInfo
                {
                    OperatingSystem = Environment.OSVersion.ToString(),
                    CpuInfo = GetCpuInfo(),
                    TotalRamGb = GetTotalRamGb(),
                    GpuInfo = "Simulated GPU (Development Mode)",
                    TotalGpuMemoryGb = 8.0, // Simulated
                    AvailableDiskSpaceGb = GetAvailableDiskSpaceGb(),
                    Uptime = GetSystemUptime(),
                    ApplicationVersion = GetApplicationVersion(),
                    SupportedFrameworks = new List<string> { "PyTorch", "TensorFlow", "ONNX", "Hugging Face" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system information");
                return new SystemInfo
                {
                    OperatingSystem = "Unknown",
                    CpuInfo = "Unknown",
                    TotalRamGb = 0,
                    GpuInfo = "Unknown",
                    ApplicationVersion = "Unknown"
                };
            }
        });
    }

    /// <summary>
    /// Handles hardware metrics updates from real-time monitoring
    /// </summary>
    private void OnHardwareMetricsUpdated(object? sender, HardwareMetricsEventArgs e)
    {
        // Update performance metrics with real hardware data
        _performance = new SystemPerformanceMetrics
        {
            CpuUsage = e.CpuUsage,
            RamUsage = e.RamUsage,
            GpuUsage = e.GpuUsage,
            GpuMemoryUsage = e.GpuMemoryUsage,
            DiskUsage = e.DiskUsage,
            NetworkUsage = 0, // Network monitoring not implemented yet
            Temperature = e.SystemTemperature,
            PowerConsumption = EstimatePowerConsumption(e),
            Timestamp = e.Timestamp
        };
        
        // Notify property changes for real-time UI updates
        OnPropertyChanged(nameof(Performance));
        OnPropertyChanged(nameof(SystemLoadPercentage));
        OnPropertyChanged(nameof(GpuUtilization));
        OnPropertyChanged(nameof(MemoryUsage));
        
        _logger.LogDebug("Hardware metrics updated - CPU: {CpuUsage:F1}%, RAM: {RamUsage:F1}%, GPU: {GpuUsage:F1}%",
            e.CpuUsage, e.RamUsage, e.GpuUsage);
    }
    
    /// <summary>
    /// Handles orchestrator health updates
    /// </summary>
    private void OnOrchestratorHealthUpdated(object? sender, OrchestratorHealthEventArgs e)
    {
        // Update model and job counts from orchestrator
        _activeModels = e.ActiveRunners;
        _runningJobs = e.TotalRunners;
        
        // Estimate training hours based on runner uptime
        _totalTrainingHours = (int)(e.RunnerStatuses.Sum(r => r.UpTime.TotalHours));
        
        // Update system status based on orchestrator health
        var newStatus = DetermineSystemStatus(e);
        if (newStatus != _currentStatus)
        {
            _ = UpdateSystemStatusAsync(newStatus, GetHealthStatusMessage(e.OverallHealth));
        }
        
        // Notify property changes
        OnPropertyChanged(nameof(ActiveModels));
        OnPropertyChanged(nameof(RunningJobs));
        OnPropertyChanged(nameof(TotalTrainingHours));
        
        _logger.LogDebug("Orchestrator health updated - Active: {Active}/{Total} runners, VRAM: {Vram:F1}GB",
            e.ActiveRunners, e.TotalRunners, e.TotalVramUsageMb / 1024.0);
    }
    
    /// <summary>
    /// Determines system status based on orchestrator health
    /// </summary>
    private SystemStatus DetermineSystemStatus(OrchestratorHealthEventArgs health)
    {
        return health.OverallHealth switch
        {
            HealthStatus.Healthy => health.ActiveRunners > 0 ? SystemStatus.Training : SystemStatus.Ready,
            HealthStatus.Degraded => SystemStatus.Warning,
            HealthStatus.Warning => SystemStatus.Warning,
            HealthStatus.Critical => SystemStatus.Error,
            _ => SystemStatus.Ready
        };
    }
    
    /// <summary>
    /// Gets status message for health status
    /// </summary>
    private string GetHealthStatusMessage(HealthStatus health)
    {
        return health switch
        {
            HealthStatus.Healthy => "All systems operational",
            HealthStatus.Degraded => "System performance degraded",
            HealthStatus.Warning => "System warnings detected",
            HealthStatus.Critical => "Critical system issues",
            _ => "System status unknown"
        };
    }
    
    /// <summary>
    /// Estimates power consumption based on hardware metrics
    /// </summary>
    private double EstimatePowerConsumption(HardwareMetricsEventArgs metrics)
    {
        // Rough estimation: base power + CPU load + GPU load
        var basePower = 80; // Base system power draw
        var cpuPower = (metrics.CpuUsage / 100.0) * 65; // CPU can draw ~65W under load
        var gpuPower = (metrics.GpuUsage / 100.0) * 150; // GPU can draw ~150W under load
        
        return basePower + cpuPower + gpuPower;
    }
    
    /// <summary>
    /// Initializes baseline system data
    /// </summary>
    private void InitializeBaselineData()
    {
        // Start with reasonable defaults - real data will override these
        _activeModels = 0;
        _runningJobs = 0;
        _totalTrainingHours = 0;
        
        // Initialize with empty performance metrics - hardware monitor will populate
        _performance = new SystemPerformanceMetrics
        {
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Handles monitoring timer tick - now primarily for UI refresh coordination
    /// </summary>
    private async void MonitoringTimer_Tick(object? sender, EventArgs e)
    {
        // Refresh metrics to ensure UI stays updated
        await RefreshMetricsAsync();
        
        // Status changes are now driven by real hardware/orchestrator events
        // No more simulation needed
    }

    /// <summary>
    /// Updates performance metrics - now handled by real-time hardware monitoring events
    /// </summary>
    private void UpdatePerformanceMetrics()
    {
        // Performance metrics are now updated via OnHardwareMetricsUpdated event handler
        // This method is kept for compatibility with the timer-based refresh system
        
        // Update timestamp to indicate refresh occurred
        if (_performance.Timestamp < DateTime.UtcNow.AddSeconds(-5))
        {
            _performance.Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Updates system counters - now handled by orchestrator health monitoring events
    /// </summary>
    private void UpdateSystemCounters()
    {
        // System counters are now updated via OnOrchestratorHealthUpdated event handler
        // This method is kept for compatibility with the timer-based refresh system
        
        // No simulation needed - real data comes from orchestrator API
    }

    /// <summary>
    /// Gets real CPU usage if available
    /// </summary>
    /// <returns>CPU usage percentage or null if not available</returns>
    private double? GetRealCpuUsage()
    {
        try
        {
            // Simple CPU usage approximation - this is a simplified approach
            // In production, you might use Performance Counters or WMI
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            Thread.Sleep(100);
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return Math.Min(100, Math.Max(0, cpuUsageTotal * 100));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Simulates CPU usage
    /// </summary>
    private double SimulateCpuUsage()
    {
        return Math.Max(10, Math.Min(90, _performance.CpuUsage + (_random.NextDouble() - 0.5) * 10));
    }

    /// <summary>
    /// Simulates RAM usage
    /// </summary>
    private double SimulateRamUsage()
    {
        return Math.Max(20, Math.Min(80, _performance.RamUsage + (_random.NextDouble() - 0.5) * 5));
    }

    /// <summary>
    /// Simulates GPU usage
    /// </summary>
    private double SimulateGpuUsage()
    {
        return Math.Max(0, Math.Min(100, _performance.GpuUsage + (_random.NextDouble() - 0.5) * 15));
    }

    /// <summary>
    /// Simulates GPU memory usage
    /// </summary>
    private double SimulateGpuMemoryUsage()
    {
        return Math.Max(0, Math.Min(100, _performance.GpuMemoryUsage + (_random.NextDouble() - 0.5) * 8));
    }

    /// <summary>
    /// Simulates disk usage
    /// </summary>
    private double SimulateDiskUsage()
    {
        return Math.Max(0, Math.Min(5, (_random.NextDouble() - 0.5) * 2));
    }

    /// <summary>
    /// Simulates network usage
    /// </summary>
    private double SimulateNetworkUsage()
    {
        return Math.Max(0, _random.NextDouble() * 10);
    }

    /// <summary>
    /// Simulates temperature
    /// </summary>
    private double SimulateTemperature()
    {
        return Math.Max(30, Math.Min(80, 45 + (_random.NextDouble() - 0.5) * 20));
    }

    /// <summary>
    /// Simulates power consumption
    /// </summary>
    private double SimulatePowerConsumption()
    {
        return Math.Max(50, Math.Min(300, 150 + (_random.NextDouble() - 0.5) * 100));
    }

    /// <summary>
    /// Simulates status changes
    /// </summary>
    private SystemStatus SimulateStatusChange()
    {
        var statuses = new[] { SystemStatus.Ready, SystemStatus.Busy, SystemStatus.Training };
        return statuses[_random.Next(statuses.Length)];
    }

    /// <summary>
    /// Gets a status message for a given system status
    /// </summary>
    private string GetStatusMessage(SystemStatus status)
    {
        return status switch
        {
            SystemStatus.Ready => "System is ready for new tasks",
            SystemStatus.Busy => "Processing system tasks",
            SystemStatus.Training => "Model training in progress",
            SystemStatus.Warning => "System warning detected",
            SystemStatus.Error => "System error occurred",
            _ => "Status updated"
        };
    }

    /// <summary>
    /// Updates system status and raises events
    /// </summary>
    private async Task UpdateSystemStatusAsync(SystemStatus newStatus, string message)
    {
        var previousStatus = _currentStatus;
        _currentStatus = newStatus;
        
        OnPropertyChanged(nameof(CurrentStatus));
        
        StatusChanged?.Invoke(this, new SystemStatusChangedEventArgs(previousStatus, newStatus, message));
        
        _logger.LogInformation("System status changed from {PreviousStatus} to {NewStatus}: {Message}", 
            previousStatus, newStatus, message);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets CPU information
    /// </summary>
    private string GetCpuInfo()
    {
        return $"{Environment.ProcessorCount} cores";
    }

    /// <summary>
    /// Gets total RAM in GB
    /// </summary>
    private double GetTotalRamGb()
    {
        try
        {
            // This is a simplified approach - in production you might use WMI
            var totalPhysicalMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0 * 1024.0);
            return Math.Max(1.0, totalPhysicalMemory * 4); // Rough estimation
        }
        catch
        {
            return 8.0; // Default fallback
        }
    }

    /// <summary>
    /// Gets available disk space in GB
    /// </summary>
    private double GetAvailableDiskSpaceGb()
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            return drives.Sum(d => d.AvailableFreeSpace) / (1024.0 * 1024.0 * 1024.0);
        }
        catch
        {
            return 100.0; // Default fallback
        }
    }

    /// <summary>
    /// Gets system uptime
    /// </summary>
    private TimeSpan GetSystemUptime()
    {
        return TimeSpan.FromMilliseconds(Environment.TickCount64);
    }

    /// <summary>
    /// Gets application version
    /// </summary>
    private string GetApplicationVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }

    /// <summary>
    /// Raises the PropertyChanged event
    /// </summary>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Optimizes refresh rate based on performance analysis
    /// </summary>
    private void OptimizeRefreshRate()
    {
        try
        {
            var recommendation = _performanceAnalyzer.GetOptimalRefreshRate();
            var currentInterval = _monitoringTimer.Interval;
            
            if (Math.Abs((currentInterval - recommendation.RecommendedInterval).TotalSeconds) > 0.5)
            {
                _monitoringTimer.Interval = recommendation.RecommendedInterval;
                
                _logger.LogInformation(
                    "Refresh rate optimized from {OldInterval}s to {NewInterval}s - {Reason} (Confidence: {Confidence:P0})",
                    currentInterval.TotalSeconds, 
                    recommendation.RecommendedInterval.TotalSeconds,
                    recommendation.Reason,
                    recommendation.Confidence);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing refresh rate");
        }
    }
}