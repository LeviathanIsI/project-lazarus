using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Real-time performance metrics collection with multi-layer analysis
/// </summary>
public class PerformanceCollector : IDisposable
{
    private readonly ILogger<PerformanceCollector> _logger;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _memoryCounter;
    private readonly Process _currentProcess;
    private readonly Timer _collectionTimer;
    private readonly Queue<SystemMetrics> _metricsHistory = new();
    private readonly object _metricsLock = new();
    private bool _disposed = false;

    // GPU monitoring via nvidia-ml-py or vendor APIs
    private readonly ProcessStartInfo _nvidiaSmiStartInfo;

    public event EventHandler<SystemMetrics>? MetricsCollected;

    public PerformanceCollector(ILogger<PerformanceCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentProcess = Process.GetCurrentProcess();

#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // Prime the counter
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize CPU performance counter");
            }

            try
            {
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize memory performance counter");
            }
        }
#endif

        // Setup NVIDIA-SMI for VRAM monitoring
        _nvidiaSmiStartInfo = new ProcessStartInfo
        {
            FileName = "nvidia-smi",
            Arguments = "--query-gpu=memory.used,memory.total,utilization.gpu --format=csv,noheader,nounits",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        // Collect metrics every 2 seconds
        _collectionTimer = new Timer(CollectMetricsCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        
        _logger.LogInformation("Performance collector initialized");
    }

    /// <summary>
    /// Collect comprehensive system metrics
    /// </summary>
    public async Task<SystemMetrics> CollectMetricsAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var metrics = new SystemMetrics
            {
                CpuUsage = GetCpuUsage(),
                MemoryUsage = GetMemoryUsage(),
                GCPressure = GC.GetTotalMemory(false),
                ThreadCount = _currentProcess.Threads.Count,
                HandleCount = _currentProcess.HandleCount,
                VRAMUsage = await GetVRAMUsageAsync(),
                ApplicationMemory = _currentProcess.WorkingSet64,
                PrivateMemory = _currentProcess.PrivateMemorySize64,
                VirtualMemory = _currentProcess.VirtualMemorySize64,
                Timestamp = DateTime.UtcNow,
                CollectionTime = stopwatch.Elapsed
            };

            // Add to history for trend analysis
            lock (_metricsLock)
            {
                _metricsHistory.Enqueue(metrics);
                if (_metricsHistory.Count > 300) // Keep 10 minutes of history at 2-second intervals
                {
                    _metricsHistory.Dequeue();
                }
            }

            MetricsCollected?.Invoke(this, metrics);
            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting system metrics");
            return CreateFallbackMetrics();
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Get CPU usage percentage
    /// </summary>
    private double GetCpuUsage()
    {
#if WINDOWS
        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (_cpuCounter != null)
                {
                    var usage = _cpuCounter.NextValue();
                    return Math.Min(100, Math.Max(0, usage));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get CPU usage from performance counter");
            }
        }
#endif

        // Fallback to process-based calculation
        return GetProcessCpuUsage();
    }

    /// <summary>
    /// Get memory usage in bytes
    /// </summary>
    private long GetMemoryUsage()
    {
        try
        {
            return _currentProcess.WorkingSet64;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get process memory usage");
            return 0;
        }
    }

    /// <summary>
    /// Get VRAM usage via nvidia-smi
    /// </summary>
    private async Task<VRAMInfo> GetVRAMUsageAsync()
    {
        try
        {
            using var process = new Process { StartInfo = _nvidiaSmiStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    var parts = lines[0].Split(',').Select(p => p.Trim()).ToArray();
                    if (parts.Length >= 3)
                    {
                        if (long.TryParse(parts[0], out var usedMB) &&
                            long.TryParse(parts[1], out var totalMB) &&
                            double.TryParse(parts[2], out var gpuUtilization))
                        {
                            return new VRAMInfo
                            {
                                UsedBytes = usedMB * 1024 * 1024,
                                TotalBytes = totalMB * 1024 * 1024,
                                AvailableBytes = (totalMB - usedMB) * 1024 * 1024,
                                UsagePercent = totalMB > 0 ? (double)usedMB / totalMB * 100 : 0,
                                GpuUtilizationPercent = gpuUtilization
                            };
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to query VRAM usage via nvidia-smi");
        }

        // Return default values if nvidia-smi fails
        return new VRAMInfo
        {
            UsedBytes = 0,
            TotalBytes = 8L * 1024 * 1024 * 1024, // Assume 8GB default
            AvailableBytes = 8L * 1024 * 1024 * 1024,
            UsagePercent = 0,
            GpuUtilizationPercent = 0
        };
    }

    /// <summary>
    /// Fallback CPU usage calculation
    /// </summary>
    private double GetProcessCpuUsage()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = _currentProcess.TotalProcessorTime;

            Thread.Sleep(100);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = _currentProcess.TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return Math.Min(100, Math.Max(0, cpuUsageTotal * 100));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to calculate process CPU usage");
            return 0;
        }
    }

    /// <summary>
    /// Get performance metrics history for trend analysis
    /// </summary>
    public SystemMetrics[] GetMetricsHistory(TimeSpan? duration = null)
    {
        lock (_metricsLock)
        {
            if (duration.HasValue)
            {
                var cutoff = DateTime.UtcNow - duration.Value;
                return _metricsHistory.Where(m => m.Timestamp >= cutoff).ToArray();
            }

            return _metricsHistory.ToArray();
        }
    }

    /// <summary>
    /// Calculate performance trends over time
    /// </summary>
    public PerformanceTrends AnalyzeTrends(TimeSpan analysisWindow)
    {
        var historyData = GetMetricsHistory(analysisWindow);
        
        if (historyData.Length < 2)
        {
            return new PerformanceTrends
            {
                AnalysisWindow = analysisWindow,
                DataPoints = historyData.Length,
                Trends = new Dictionary<string, TrendDirection>()
            };
        }

        var trends = new Dictionary<string, TrendDirection>();
        
        // CPU trend
        var cpuValues = historyData.Select(m => m.CpuUsage).ToArray();
        trends["CPU"] = CalculateTrend(cpuValues);

        // Memory trend
        var memoryValues = historyData.Select(m => (double)m.MemoryUsage).ToArray();
        trends["Memory"] = CalculateTrend(memoryValues);

        // VRAM trend
        var vramValues = historyData.Select(m => (double)m.VRAMUsage.UsedBytes).ToArray();
        trends["VRAM"] = CalculateTrend(vramValues);

        // GC pressure trend
        var gcValues = historyData.Select(m => (double)m.GCPressure).ToArray();
        trends["GC"] = CalculateTrend(gcValues);

        return new PerformanceTrends
        {
            AnalysisWindow = analysisWindow,
            DataPoints = historyData.Length,
            Trends = trends,
            AnalysisTime = DateTime.UtcNow
        };
    }

    private TrendDirection CalculateTrend(double[] values)
    {
        if (values.Length < 2) return TrendDirection.Stable;

        // Simple linear regression to determine trend
        var n = values.Length;
        var x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        
        var meanX = x.Average();
        var meanY = values.Average();
        
        var numerator = x.Zip(values, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
        var denominator = x.Select(xi => Math.Pow(xi - meanX, 2)).Sum();
        
        if (Math.Abs(denominator) < 0.001) return TrendDirection.Stable;
        
        var slope = numerator / denominator;
        
        // Determine trend based on slope magnitude
        var slopeThreshold = meanY * 0.01; // 1% of mean value
        
        if (slope > slopeThreshold) return TrendDirection.Increasing;
        if (slope < -slopeThreshold) return TrendDirection.Decreasing;
        return TrendDirection.Stable;
    }

    private void CollectMetricsCallback(object? state)
    {
        if (!_disposed)
        {
            _ = Task.Run(async () => await CollectMetricsAsync());
        }
    }

    private SystemMetrics CreateFallbackMetrics()
    {
        return new SystemMetrics
        {
            CpuUsage = 0,
            MemoryUsage = _currentProcess.WorkingSet64,
            GCPressure = GC.GetTotalMemory(false),
            ThreadCount = _currentProcess.Threads.Count,
            HandleCount = _currentProcess.HandleCount,
            VRAMUsage = new VRAMInfo(),
            ApplicationMemory = _currentProcess.WorkingSet64,
            Timestamp = DateTime.UtcNow,
            CollectionTime = TimeSpan.Zero
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            _collectionTimer?.Dispose();
            _cpuCounter?.Dispose();
            _memoryCounter?.Dispose();
            _currentProcess?.Dispose();

            _logger.LogInformation("Performance collector disposed");
        }
    }
}

/// <summary>
/// Comprehensive system metrics
/// </summary>
public record SystemMetrics
{
    public double CpuUsage { get; init; }
    public long MemoryUsage { get; init; }
    public long GCPressure { get; init; }
    public int ThreadCount { get; init; }
    public int HandleCount { get; init; }
    public VRAMInfo VRAMUsage { get; init; } = new();
    public long ApplicationMemory { get; init; }
    public long PrivateMemory { get; init; }
    public long VirtualMemory { get; init; }
    public DateTime Timestamp { get; init; }
    public TimeSpan CollectionTime { get; init; }
}

/// <summary>
/// VRAM information
/// </summary>
public record VRAMInfo
{
    public long UsedBytes { get; init; }
    public long TotalBytes { get; init; }
    public long AvailableBytes { get; init; }
    public double UsagePercent { get; init; }
    public double GpuUtilizationPercent { get; init; }
}

/// <summary>
/// Performance trends analysis
/// </summary>
public record PerformanceTrends
{
    public TimeSpan AnalysisWindow { get; init; }
    public int DataPoints { get; init; }
    public Dictionary<string, TrendDirection> Trends { get; init; } = new();
    public DateTime AnalysisTime { get; init; }
}

/// <summary>
/// Trend direction enumeration
/// </summary>
public enum TrendDirection
{
    Decreasing,
    Stable,
    Increasing
}