using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Large Object Heap monitor for detecting memory leaks and excessive allocation patterns
/// </summary>
public class LOHMonitor : IDisposable
{
    private readonly ILogger _logger;
    private readonly Timer _monitorTimer;
    private long _lastLOHSize;
    private long _lastGen2Collections;
    private readonly Queue<LOHSnapshot> _snapshots = new();
    private readonly object _snapshotsLock = new();
    private bool _disposed = false;

    public event EventHandler<LOHGrowthEvent>? LOHGrowthDetected;

    public LOHMonitor(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _lastLOHSize = GC.GetTotalMemory(false);
        _lastGen2Collections = GC.CollectionCount(2);
        
        // Monitor every 30 seconds
        _monitorTimer = new Timer(CheckLOHGrowth, null, 
            TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        _logger.LogInformation("LOH Monitor initialized - tracking Large Object Heap growth patterns");
    }

    /// <summary>
    /// Force an analysis of current LOH state
    /// </summary>
    public LOHAnalysisResult AnalyzeLOHState()
    {
        try
        {
            var beforeGC = GC.GetTotalMemory(false);
            var gen2Collections = GC.CollectionCount(2);
            
            // Trigger a full garbage collection to get accurate LOH size
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced);
            
            var afterGC = GC.GetTotalMemory(false);
            var collectedMemory = beforeGC - afterGC;

            var result = new LOHAnalysisResult
            {
                MemoryBeforeGC = beforeGC,
                MemoryAfterGC = afterGC,
                MemoryCollected = collectedMemory,
                CollectionEfficiency = beforeGC > 0 ? (double)collectedMemory / beforeGC * 100 : 0,
                Gen2Collections = GC.CollectionCount(2),
                AnalysisTime = DateTime.UtcNow
            };

            // Create snapshot for history tracking
            var snapshot = new LOHSnapshot
            {
                TotalMemory = afterGC,
                Gen2Collections = result.Gen2Collections,
                Timestamp = DateTime.UtcNow
            };

            lock (_snapshotsLock)
            {
                _snapshots.Enqueue(snapshot);
                if (_snapshots.Count > 100) // Keep last 100 snapshots
                {
                    _snapshots.Dequeue();
                }
            }

            _logger.LogInformation("LOH Analysis: {BeforeGC}MB -> {AfterGC}MB, Collected: {Collected}MB ({Efficiency:F1}% efficiency)",
                beforeGC / (1024 * 1024), afterGC / (1024 * 1024), 
                collectedMemory / (1024 * 1024), result.CollectionEfficiency);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LOH analysis");
            return new LOHAnalysisResult
            {
                MemoryBeforeGC = GC.GetTotalMemory(false),
                MemoryAfterGC = GC.GetTotalMemory(false),
                AnalysisTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Get LOH growth trends
    /// </summary>
    public LOHGrowthTrends GetGrowthTrends(TimeSpan analysisWindow)
    {
        lock (_snapshotsLock)
        {
            var cutoff = DateTime.UtcNow - analysisWindow;
            var relevantSnapshots = _snapshots
                .Where(s => s.Timestamp >= cutoff)
                .OrderBy(s => s.Timestamp)
                .ToArray();

            if (relevantSnapshots.Length < 2)
            {
                return new LOHGrowthTrends
                {
                    AnalysisWindow = analysisWindow,
                    DataPoints = relevantSnapshots.Length,
                    TrendDirection = TrendDirection.Stable,
                    GrowthRate = 0,
                    AverageMemoryUsage = relevantSnapshots.FirstOrDefault()?.TotalMemory ?? 0,
                    AnalysisTime = DateTime.UtcNow
                };
            }

            var first = relevantSnapshots.First();
            var last = relevantSnapshots.Last();
            var timeSpan = (last.Timestamp - first.Timestamp).TotalHours;
            
            var memoryGrowth = last.TotalMemory - first.TotalMemory;
            var growthRate = timeSpan > 0 ? memoryGrowth / timeSpan : 0; // bytes per hour
            
            var avgMemory = relevantSnapshots.Average(s => s.TotalMemory);
            
            // Determine trend direction
            var trendDirection = TrendDirection.Stable;
            if (Math.Abs(growthRate) > avgMemory * 0.01) // 1% growth per hour threshold
            {
                trendDirection = growthRate > 0 ? TrendDirection.Increasing : TrendDirection.Decreasing;
            }

            return new LOHGrowthTrends
            {
                AnalysisWindow = analysisWindow,
                DataPoints = relevantSnapshots.Length,
                TrendDirection = trendDirection,
                GrowthRate = growthRate,
                AverageMemoryUsage = (long)avgMemory,
                TotalGrowth = memoryGrowth,
                MemoryAtStart = first.TotalMemory,
                MemoryAtEnd = last.TotalMemory,
                AnalysisTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Get recommendations for memory optimization
    /// </summary>
    public List<string> GetMemoryOptimizationRecommendations()
    {
        var recommendations = new List<string>();
        var trends = GetGrowthTrends(TimeSpan.FromHours(1));
        var currentMemory = GC.GetTotalMemory(false);

        if (trends.TrendDirection == TrendDirection.Increasing && trends.GrowthRate > 100 * 1024 * 1024) // 100MB/hour
        {
            recommendations.Add("High memory growth rate detected - investigate potential memory leaks");
        }

        if (currentMemory > 1L * 1024 * 1024 * 1024) // 1GB
        {
            recommendations.Add("High memory usage - consider implementing object pooling or reducing cache sizes");
        }

        var analysis = AnalyzeLOHState();
        if (analysis.CollectionEfficiency < 20) // Less than 20% collected
        {
            recommendations.Add("Low garbage collection efficiency - many objects may have long lifetimes");
        }

        if (GC.CollectionCount(2) - _lastGen2Collections > 10) // Many Gen2 collections
        {
            recommendations.Add("Frequent Gen2 garbage collections - consider reducing allocation rate or object lifetime");
        }

        // Check for large object heap pressure
        var gen2Collections = GC.CollectionCount(2);
        var collectionRate = gen2Collections - _lastGen2Collections;
        if (collectionRate > 5) // More than 5 collections in 30 seconds
        {
            recommendations.Add("High LOH pressure detected - review large object allocations");
        }

        return recommendations;
    }

    private void CheckLOHGrowth(object? state)
    {
        if (_disposed) return;

        try
        {
            var currentLOHSize = GC.GetTotalMemory(false);
            var currentGen2Collections = GC.CollectionCount(2);
            
            var growth = currentLOHSize - _lastLOHSize;
            var newCollections = currentGen2Collections - _lastGen2Collections;

            // Significant growth threshold: 50MB in 30 seconds
            var significantGrowthThreshold = 50 * 1024 * 1024;

            if (growth > significantGrowthThreshold)
            {
                _logger.LogWarning("Significant LOH growth detected: {Growth}MB in 30 seconds (total: {Total}MB)",
                    growth / (1024 * 1024), currentLOHSize / (1024 * 1024));

                var growthEvent = new LOHGrowthEvent
                {
                    GrowthAmount = growth,
                    TotalMemory = currentLOHSize,
                    GrowthRate = growth / 30.0, // bytes per second
                    Gen2Collections = newCollections,
                    DetectedAt = DateTime.UtcNow
                };

                LOHGrowthDetected?.Invoke(this, growthEvent);

                // Trigger detailed analysis for significant growth
                _ = Task.Run(() => AnalyzeLOHContents(growth));
            }

            // Log periodic memory status
            if (newCollections > 2) // Multiple collections in 30 seconds
            {
                _logger.LogInformation("LOH Status: {Memory}MB, {Collections} Gen2 collections in last 30s",
                    currentLOHSize / (1024 * 1024), newCollections);
            }

            _lastLOHSize = currentLOHSize;
            _lastGen2Collections = currentGen2Collections;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during LOH growth check");
        }
    }

    private void AnalyzeLOHContents(long growthAmount)
    {
        try
        {
            _logger.LogInformation("Analyzing LOH contents after {Growth}MB growth", growthAmount / (1024 * 1024));

            // Force full analysis
            var analysis = AnalyzeLOHState();
            
            if (analysis.CollectionEfficiency < 50) // Less than 50% was collected
            {
                _logger.LogWarning("Low collection efficiency ({Efficiency:F1}%) - potential memory leak indicators",
                    analysis.CollectionEfficiency);
            }

            var recommendations = GetMemoryOptimizationRecommendations();
            if (recommendations.Any())
            {
                _logger.LogInformation("Memory optimization recommendations: {Recommendations}",
                    string.Join("; ", recommendations));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during detailed LOH analysis");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _monitorTimer?.Dispose();
            
            lock (_snapshotsLock)
            {
                _snapshots.Clear();
            }

            _logger.LogInformation("LOH Monitor disposed");
        }
    }
}

/// <summary>
/// LOH analysis result
/// </summary>
public record LOHAnalysisResult
{
    public long MemoryBeforeGC { get; init; }
    public long MemoryAfterGC { get; init; }
    public long MemoryCollected { get; init; }
    public double CollectionEfficiency { get; init; }
    public int Gen2Collections { get; init; }
    public DateTime AnalysisTime { get; init; }
}

/// <summary>
/// LOH growth trends analysis
/// </summary>
public record LOHGrowthTrends
{
    public TimeSpan AnalysisWindow { get; init; }
    public int DataPoints { get; init; }
    public TrendDirection TrendDirection { get; init; }
    public double GrowthRate { get; init; } // bytes per hour
    public long AverageMemoryUsage { get; init; }
    public long TotalGrowth { get; init; }
    public long MemoryAtStart { get; init; }
    public long MemoryAtEnd { get; init; }
    public DateTime AnalysisTime { get; init; }
}

/// <summary>
/// LOH memory snapshot
/// </summary>
public record LOHSnapshot
{
    public long TotalMemory { get; init; }
    public int Gen2Collections { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// LOH growth event
/// </summary>
public class LOHGrowthEvent : EventArgs
{
    public long GrowthAmount { get; init; }
    public long TotalMemory { get; init; }
    public double GrowthRate { get; init; }
    public long Gen2Collections { get; init; }
    public DateTime DetectedAt { get; init; }
}