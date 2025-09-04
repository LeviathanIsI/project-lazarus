using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.ComponentModel;

namespace Lazarus.App.Desktop.Services;

public class PerformanceAnalyzer : INotifyPropertyChanged, IDisposable
{
    private readonly ILogger<PerformanceAnalyzer> _logger;
    private readonly Timer _analysisTimer;
    private readonly Stopwatch _measurementWindow = new();
    private bool _disposed = false;
    
    // Performance metrics
    private int _updateCount = 0;
    private long _totalUpdateTime = 0;
    private double _averageUpdateTime = 0;
    private double _maxUpdateTime = 0;
    private double _cpuImpact = 0;
    private double _memoryImpact = 0;
    
    // Measurement state
    private double _baselineCpuUsage = 0;
    private long _baselineMemoryUsage = 0;
    private readonly List<double> _updateTimes = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public PerformanceAnalyzer(ILogger<PerformanceAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _measurementWindow.Start();
        EstablishBaseline();
        
        // Analysis every 5 seconds
        _analysisTimer = new Timer(AnalyzePerformance, null, 
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        
        _logger.LogInformation("Performance analyzer initialized");
    }

    #region Public Properties

    public double AverageUpdateTime
    {
        get => _averageUpdateTime;
        private set
        {
            if (Math.Abs(_averageUpdateTime - value) > 0.01)
            {
                _averageUpdateTime = value;
                OnPropertyChanged(nameof(AverageUpdateTime));
            }
        }
    }

    public double MaxUpdateTime
    {
        get => _maxUpdateTime;
        private set
        {
            if (Math.Abs(_maxUpdateTime - value) > 0.01)
            {
                _maxUpdateTime = value;
                OnPropertyChanged(nameof(MaxUpdateTime));
            }
        }
    }

    public double CpuImpact
    {
        get => _cpuImpact;
        private set
        {
            if (Math.Abs(_cpuImpact - value) > 0.1)
            {
                _cpuImpact = value;
                OnPropertyChanged(nameof(CpuImpact));
            }
        }
    }

    public double MemoryImpact
    {
        get => _memoryImpact;
        private set
        {
            if (Math.Abs(_memoryImpact - value) > 0.1)
            {
                _memoryImpact = value;
                OnPropertyChanged(nameof(MemoryImpact));
            }
        }
    }

    public int UpdateCount => _updateCount;

    public bool IsPerformanceHealthy => 
        AverageUpdateTime < 50.0 && // Less than 50ms average
        MaxUpdateTime < 200.0 &&    // Less than 200ms max spike  
        CpuImpact < 5.0 &&          // Less than 5% CPU overhead
        MemoryImpact < 50.0;        // Less than 50MB memory overhead

    public string PerformanceGrade
    {
        get
        {
            if (AverageUpdateTime < 20.0 && CpuImpact < 2.0) return "Excellent";
            if (AverageUpdateTime < 50.0 && CpuImpact < 5.0) return "Good";
            if (AverageUpdateTime < 100.0 && CpuImpact < 10.0) return "Fair";
            return "Poor";
        }
    }

    #endregion

    public void RecordUpdate(TimeSpan updateDuration)
    {
        var updateMs = updateDuration.TotalMilliseconds;
        
        lock (_updateTimes)
        {
            _updateCount++;
            _totalUpdateTime += (long)updateMs;
            _updateTimes.Add(updateMs);
            
            // Keep only last 100 measurements for rolling average
            if (_updateTimes.Count > 100)
            {
                _updateTimes.RemoveAt(0);
            }
        }
    }

    private void EstablishBaseline()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            
            // Get baseline CPU usage
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            Thread.Sleep(1000);
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            _baselineCpuUsage = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;
            
            // Get baseline memory
            _baselineMemoryUsage = process.WorkingSet64;
            
            _logger.LogDebug("Performance baseline established - CPU: {CpuBaseline:F1}%, Memory: {MemoryBaseline} MB",
                _baselineCpuUsage, _baselineMemoryUsage / (1024.0 * 1024.0));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to establish performance baseline");
            _baselineCpuUsage = 1.0; // Default fallback
            _baselineMemoryUsage = 100 * 1024 * 1024; // 100MB fallback
        }
    }

    private void AnalyzePerformance(object? state)
    {
        if (_disposed) return;

        try
        {
            lock (_updateTimes)
            {
                if (_updateTimes.Any())
                {
                    AverageUpdateTime = _updateTimes.Average();
                    MaxUpdateTime = _updateTimes.Max();
                }
            }

            // Measure current impact
            using var process = Process.GetCurrentProcess();
            
            // CPU impact measurement
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            Thread.Sleep(500);
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var currentCpuUsage = (cpuUsedMs / (Environment.ProcessorCount * totalMsPassed)) * 100;
            
            CpuImpact = Math.Max(0, currentCpuUsage - _baselineCpuUsage);
            
            // Memory impact
            var currentMemoryUsage = process.WorkingSet64;
            MemoryImpact = (currentMemoryUsage - _baselineMemoryUsage) / (1024.0 * 1024.0); // MB
            
            // Log performance analysis
            _logger.LogDebug("Performance Analysis - Updates: {Count}, Avg: {AvgMs:F1}ms, Max: {MaxMs:F1}ms, CPU Impact: {CpuImpact:F1}%, Memory Impact: {MemoryImpact:F1}MB, Grade: {Grade}",
                UpdateCount, AverageUpdateTime, MaxUpdateTime, CpuImpact, MemoryImpact, PerformanceGrade);

            // Performance recommendations
            if (!IsPerformanceHealthy)
            {
                GenerateOptimizationRecommendations();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during performance analysis");
        }
    }

    private void GenerateOptimizationRecommendations()
    {
        var recommendations = new List<string>();

        if (AverageUpdateTime > 50.0)
        {
            recommendations.Add("Consider increasing refresh interval to reduce update frequency");
        }

        if (MaxUpdateTime > 200.0)
        {
            recommendations.Add("Investigate performance spikes - consider async processing");
        }

        if (CpuImpact > 5.0)
        {
            recommendations.Add("High CPU usage detected - optimize monitoring algorithms");
        }

        if (MemoryImpact > 50.0)
        {
            recommendations.Add("Memory growth detected - check for memory leaks");
        }

        if (UpdateCount > 1000 && AverageUpdateTime > 30.0)
        {
            recommendations.Add("Consider reducing monitoring frequency for better efficiency");
        }

        _logger.LogWarning("Performance optimization recommendations: {Recommendations}", 
            string.Join("; ", recommendations));
    }

    public RefreshRateRecommendation GetOptimalRefreshRate()
    {
        if (IsPerformanceHealthy && AverageUpdateTime < 20.0)
        {
            return new RefreshRateRecommendation
            {
                RecommendedInterval = TimeSpan.FromSeconds(1),
                Confidence = 0.95,
                Reason = "Excellent performance allows for high frequency updates"
            };
        }

        if (AverageUpdateTime < 50.0 && CpuImpact < 3.0)
        {
            return new RefreshRateRecommendation
            {
                RecommendedInterval = TimeSpan.FromSeconds(2),
                Confidence = 0.85,
                Reason = "Good performance supports current 2-second interval"
            };
        }

        if (AverageUpdateTime > 100.0 || CpuImpact > 8.0)
        {
            return new RefreshRateRecommendation
            {
                RecommendedInterval = TimeSpan.FromSeconds(5),
                Confidence = 0.75,
                Reason = "Performance issues detected - recommend slower refresh rate"
            };
        }

        return new RefreshRateRecommendation
        {
            RecommendedInterval = TimeSpan.FromSeconds(3),
            Confidence = 0.70,
            Reason = "Moderate performance - balanced refresh rate recommended"
        };
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _analysisTimer?.Dispose();
            _measurementWindow?.Stop();
            _logger.LogInformation("Performance analyzer disposed");
        }
    }
}

public class RefreshRateRecommendation
{
    public TimeSpan RecommendedInterval { get; set; }
    public double Confidence { get; set; }
    public string Reason { get; set; } = string.Empty;
}