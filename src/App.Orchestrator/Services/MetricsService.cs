using Lazarus.App.Shared.Contracts;
using Lazarus.App.Shared.Models;
using System.Collections.Concurrent;

namespace Lazarus.App.Orchestrator.Services;

/// <summary>
/// Service for collecting and managing system metrics
/// </summary>
public class MetricsService : IMetricsService
{
    private readonly ILogger<MetricsService> _logger;
    private readonly IRunnerService _runnerService;
    
    // Metrics storage
    private readonly ConcurrentQueue<MetricsDataPoint> _historicalMetrics = new();
    private readonly object _metricsLock = new object();
    
    // Current metrics tracking
    private double _totalInferenceTimeMs = 0;
    private double _totalTokens = 0;
    private int _totalRequests = 0;
    private int _failedRequests = 0;
    private DateTime _lastResetTime = DateTime.UtcNow;
    
    // Performance tracking
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsService"/> class
    /// </summary>
    /// <param name="logger">The logger</param>
    /// <param name="runnerService">The runner service</param>
    public MetricsService(ILogger<MetricsService> logger, IRunnerService runnerService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _runnerService = runnerService ?? throw new ArgumentNullException(nameof(runnerService));

        // Start background task to generate synthetic metrics
        _ = Task.Run(GenerateMetricsLoop);
    }

    /// <inheritdoc />
    public async Task<MetricsApiResponse> GetCurrentMetricsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(25, cancellationToken); // Simulate async operation

        lock (_metricsLock)
        {
            var metrics = new MetricsApiResponse
            {
                AverageInferenceLatencyMs = _totalRequests > 0 ? _totalInferenceTimeMs / _totalRequests : 0,
                TokensPerSecond = CalculateTokensPerSecond(),
                TotalRequests = _totalRequests,
                FailedRequests = _failedRequests,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug("Current metrics - Latency: {Latency:F1}ms, TPS: {TPS:F1}, Requests: {Requests}", 
                metrics.AverageInferenceLatencyMs, metrics.TokensPerSecond, metrics.TotalRequests);

            return metrics;
        }
    }

    /// <inheritdoc />
    public async Task<HistoricalMetricsResponse> GetHistoricalMetricsAsync(int hours = 24, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken); // Simulate async operation

        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        var relevantMetrics = _historicalMetrics
            .Where(m => m.Timestamp >= cutoffTime)
            .OrderBy(m => m.Timestamp)
            .ToList();

        var response = new HistoricalMetricsResponse
        {
            TimeRange = TimeSpan.FromHours(hours),
            DataPoints = relevantMetrics
        };

        _logger.LogDebug("Retrieved {Count} historical metrics for {Hours} hours", relevantMetrics.Count, hours);
        return response;
    }

    /// <inheritdoc />
    public async Task RecordMetricAsync(double inferenceLatencyMs, double tokensPerSecond, bool success = true, CancellationToken cancellationToken = default)
    {
        await Task.Delay(5, cancellationToken); // Simulate async operation

        lock (_metricsLock)
        {
            _totalInferenceTimeMs += inferenceLatencyMs;
            _totalTokens += tokensPerSecond;
            _totalRequests++;
            
            if (!success)
            {
                _failedRequests++;
            }
        }

        // Record historical data point
        var runners = await _runnerService.GetAllRunnersAsync(cancellationToken);
        var activeRunners = runners.Count(r => r.Status == "active");

        var dataPoint = new MetricsDataPoint
        {
            Timestamp = DateTime.UtcNow,
            InferenceLatencyMs = inferenceLatencyMs,
            TokensPerSecond = tokensPerSecond,
            RequestCount = 1,
            ActiveRunners = activeRunners
        };

        _historicalMetrics.Enqueue(dataPoint);

        // Keep only last 48 hours of data
        CleanupOldMetrics();

        _logger.LogTrace("Recorded metric - Latency: {Latency:F1}ms, TPS: {TPS:F1}, Success: {Success}", 
            inferenceLatencyMs, tokensPerSecond, success);
    }

    /// <inheritdoc />
    public async Task ResetMetricsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate async operation

        lock (_metricsLock)
        {
            _totalInferenceTimeMs = 0;
            _totalTokens = 0;
            _totalRequests = 0;
            _failedRequests = 0;
            _lastResetTime = DateTime.UtcNow;
        }

        // Clear historical metrics
        while (_historicalMetrics.TryDequeue(out _)) { }

        _logger.LogInformation("Reset all performance metrics");
    }

    /// <inheritdoc />
    public async Task UpdateSystemMetricsAsync(int activeRunners, double totalVramUsage, CancellationToken cancellationToken = default)
    {
        await Task.Delay(5, cancellationToken); // Simulate async operation

        // This method could be used to adjust metrics based on system state
        // For now, we'll just log the system state for monitoring
        _logger.LogTrace("System state update - Active runners: {ActiveRunners}, VRAM: {VramUsage:F1}MB", 
            activeRunners, totalVramUsage);
    }

    /// <summary>
    /// Calculate tokens per second based on recent activity
    /// </summary>
    private double CalculateTokensPerSecond()
    {
        var elapsedTime = DateTime.UtcNow - _lastResetTime;
        if (elapsedTime.TotalSeconds <= 0)
            return 0;

        return _totalTokens / elapsedTime.TotalSeconds;
    }

    /// <summary>
    /// Clean up metrics older than 48 hours
    /// </summary>
    private void CleanupOldMetrics()
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-48);
        var tempList = new List<MetricsDataPoint>();

        // Extract all metrics
        while (_historicalMetrics.TryDequeue(out var metric))
        {
            if (metric.Timestamp >= cutoffTime)
            {
                tempList.Add(metric);
            }
        }

        // Re-add recent metrics
        foreach (var metric in tempList)
        {
            _historicalMetrics.Enqueue(metric);
        }
    }

    /// <summary>
    /// Background task that generates synthetic metrics for demonstration
    /// </summary>
    private async Task GenerateMetricsLoop()
    {
        await Task.Delay(5000); // Wait 5 seconds before starting

        while (true)
        {
            try
            {
                // Generate metrics every 10-30 seconds
                await Task.Delay(TimeSpan.FromSeconds(10 + _random.Next(0, 20)));

                var runners = await _runnerService.GetAllRunnersAsync();
                var activeRunners = runners.Count(r => r.Status == "active");

                if (activeRunners > 0)
                {
                    // Simulate realistic metrics based on active runners
                    var baseLatency = 150 + (activeRunners * 30); // More runners = higher latency
                    var latency = baseLatency + _random.Next(-50, 100);
                    latency = Math.Max(50, latency); // Minimum 50ms

                    var baseTps = Math.Max(1, 25 - (activeRunners * 2)); // More runners = lower TPS per runner
                    var tokensPerSecond = baseTps + (_random.NextDouble() - 0.5) * 10;
                    tokensPerSecond = Math.Max(0.5, tokensPerSecond);

                    // Occasionally simulate failures
                    var success = _random.NextDouble() > 0.05; // 5% failure rate

                    await RecordMetricAsync(latency, tokensPerSecond, success);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics generation loop");
            }
        }
    }
}