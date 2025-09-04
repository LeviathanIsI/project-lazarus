using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// UI Performance Tracker for monitoring frame times and rendering performance
/// </summary>
public class UIPerformanceTracker : IDisposable
{
    private readonly ILogger _logger;
    private readonly Stopwatch _frameStopwatch = new();
    private readonly Queue<double> _frameTimes = new();
    private readonly object _frameTimesLock = new();
    private bool _isTracking = false;
    private bool _disposed = false;
    
    private DateTime _lastFrameTime = DateTime.UtcNow;
    private double _frameTimeSum = 0;
    private int _frameCount = 0;

    public UIPerformanceTracker(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start tracking UI performance
    /// </summary>
    public void StartTracking()
    {
        if (_disposed || _isTracking) return;

        _isTracking = true;
        _frameStopwatch.Start();
        _lastFrameTime = DateTime.UtcNow;
        
        _logger.LogInformation("UI performance tracking started");
    }

    /// <summary>
    /// Stop tracking UI performance
    /// </summary>
    public void StopTracking()
    {
        if (_disposed || !_isTracking) return;

        _isTracking = false;
        _frameStopwatch.Stop();
        
        _logger.LogInformation("UI performance tracking stopped");
    }

    /// <summary>
    /// Record a frame render completion
    /// </summary>
    public void RecordFrameCompletion()
    {
        if (_disposed || !_isTracking) return;

        var now = DateTime.UtcNow;
        var frameTime = (now - _lastFrameTime).TotalMilliseconds;

        lock (_frameTimesLock)
        {
            _frameTimes.Enqueue(frameTime);
            _frameTimeSum += frameTime;
            _frameCount++;

            // Keep only the last 300 frames (about 5 seconds at 60 FPS)
            if (_frameTimes.Count > 300)
            {
                var oldestFrame = _frameTimes.Dequeue();
                _frameTimeSum -= oldestFrame;
                _frameCount--;
            }

            // Log budget violations
            if (frameTime > ResourceBudgets.MaxFrameTime)
            {
                _logger.LogWarning("Frame budget violation: {FrameTime:F2}ms > {Budget}ms", 
                    frameTime, ResourceBudgets.MaxFrameTime);
            }
        }

        _lastFrameTime = now;
    }

    /// <summary>
    /// Get current UI performance metrics
    /// </summary>
    public UIPerformanceMetrics GetCurrentMetrics()
    {
        lock (_frameTimesLock)
        {
            if (!_frameTimes.Any())
            {
                return new UIPerformanceMetrics
                {
                    AverageFrameTime = 0,
                    MaxFrameTime = 0,
                    FrameTimeVariance = 0,
                    FramesMeasured = 0,
                    IsWithinBudget = true
                };
            }

            var frameTimesArray = _frameTimes.ToArray();
            var averageFrameTime = frameTimesArray.Average();
            var maxFrameTime = frameTimesArray.Max();
            
            // Calculate variance
            var variance = frameTimesArray.Sum(ft => Math.Pow(ft - averageFrameTime, 2)) / frameTimesArray.Length;

            return new UIPerformanceMetrics
            {
                AverageFrameTime = averageFrameTime,
                MaxFrameTime = maxFrameTime,
                FrameTimeVariance = variance,
                FramesMeasured = frameTimesArray.Length,
                IsWithinBudget = averageFrameTime <= ResourceBudgets.MaxFrameTime && 
                                maxFrameTime <= ResourceBudgets.MaxFrameTime * 2
            };
        }
    }

    /// <summary>
    /// Get frame rate from current metrics
    /// </summary>
    public double GetCurrentFPS()
    {
        var metrics = GetCurrentMetrics();
        return metrics.AverageFrameTime > 0 ? 1000.0 / metrics.AverageFrameTime : 0;
    }

    /// <summary>
    /// Get P95 frame time (95th percentile)
    /// </summary>
    public double GetP95FrameTime()
    {
        lock (_frameTimesLock)
        {
            if (!_frameTimes.Any()) return 0;

            var sortedFrameTimes = _frameTimes.OrderBy(ft => ft).ToArray();
            var p95Index = (int)(sortedFrameTimes.Length * 0.95);
            return sortedFrameTimes[Math.Min(p95Index, sortedFrameTimes.Length - 1)];
        }
    }

    /// <summary>
    /// Reset performance tracking data
    /// </summary>
    public void Reset()
    {
        lock (_frameTimesLock)
        {
            _frameTimes.Clear();
            _frameTimeSum = 0;
            _frameCount = 0;
        }

        _frameStopwatch.Restart();
        _lastFrameTime = DateTime.UtcNow;
        
        _logger.LogInformation("UI performance tracker reset");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            StopTracking();
            _frameStopwatch.Stop();
            
            lock (_frameTimesLock)
            {
                _frameTimes.Clear();
            }
        }
    }
}