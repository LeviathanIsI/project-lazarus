using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Database Query Budget Monitor for tracking query performance and enforcement
/// </summary>
public class DatabaseQueryBudgetMonitor : IDisposable
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, QueryMetrics> _queryStats = new();
    private readonly Queue<QueryExecution> _recentQueries = new();
    private readonly object _statsLock = new();
    private bool _disposed = false;

    public DatabaseQueryBudgetMonitor(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register a database query execution for monitoring
    /// </summary>
    public void RegisterQuery(string queryName, TimeSpan executionTime)
    {
        if (_disposed) return;

        var executionTimeMs = executionTime.TotalMilliseconds;
        var queryExecution = new QueryExecution
        {
            QueryName = queryName,
            ExecutionTime = executionTime,
            ExecutedAt = DateTime.UtcNow
        };

        lock (_statsLock)
        {
            // Update query statistics
            if (!_queryStats.ContainsKey(queryName))
            {
                _queryStats[queryName] = new QueryMetrics { QueryName = queryName };
            }

            var metrics = _queryStats[queryName];
            metrics.TotalExecutions++;
            metrics.TotalExecutionTime += executionTime;
            metrics.AverageExecutionTime = metrics.TotalExecutionTime.TotalMilliseconds / metrics.TotalExecutions;
            metrics.MaxExecutionTime = metrics.MaxExecutionTime > executionTime ? metrics.MaxExecutionTime : executionTime;
            metrics.LastExecutedAt = DateTime.UtcNow;

            if (executionTimeMs > ResourceBudgets.MaxDatabaseQueryTime)
            {
                metrics.SlowQueryCount++;
                _logger.LogWarning("Query budget violation: {QueryName} took {ExecutionTime}ms > {Budget}ms",
                    queryName, executionTimeMs, ResourceBudgets.MaxDatabaseQueryTime);
            }

            // Keep recent query history
            _recentQueries.Enqueue(queryExecution);
            if (_recentQueries.Count > 1000) // Keep last 1000 queries
            {
                _recentQueries.Dequeue();
            }
        }
    }

    /// <summary>
    /// Get query performance metrics
    /// </summary>
    public DatabasePerformanceMetrics GetQueryMetrics()
    {
        lock (_statsLock)
        {
            if (!_queryStats.Any())
            {
                return new DatabasePerformanceMetrics
                {
                    AverageQueryTime = 0,
                    MaxQueryTime = 0,
                    QueriesExecuted = 0,
                    SlowQueries = 0,
                    IsWithinBudget = true
                };
            }

            var totalExecutions = _queryStats.Values.Sum(m => m.TotalExecutions);
            var totalExecutionTime = _queryStats.Values.Sum(m => m.TotalExecutionTime.TotalMilliseconds);
            var averageQueryTime = totalExecutionTime / totalExecutions;
            var maxQueryTime = _queryStats.Values.Max(m => m.MaxExecutionTime.TotalMilliseconds);
            var slowQueries = _queryStats.Values.Sum(m => m.SlowQueryCount);

            return new DatabasePerformanceMetrics
            {
                AverageQueryTime = averageQueryTime,
                MaxQueryTime = maxQueryTime,
                QueriesExecuted = totalExecutions,
                SlowQueries = slowQueries,
                IsWithinBudget = averageQueryTime <= ResourceBudgets.MaxDatabaseQueryTime &&
                                maxQueryTime <= ResourceBudgets.MaxDatabaseQueryTime * 3 // Allow 3x budget for max
            };
        }
    }

    /// <summary>
    /// Get detailed statistics for a specific query
    /// </summary>
    public QueryMetrics? GetQueryStatistics(string queryName)
    {
        lock (_statsLock)
        {
            return _queryStats.TryGetValue(queryName, out var metrics) ? metrics : null;
        }
    }

    /// <summary>
    /// Get all query statistics
    /// </summary>
    public Dictionary<string, QueryMetrics> GetAllQueryStatistics()
    {
        lock (_statsLock)
        {
            return _queryStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    /// <summary>
    /// Get recent slow queries
    /// </summary>
    public List<QueryExecution> GetRecentSlowQueries(TimeSpan? within = null)
    {
        var cutoff = within.HasValue ? DateTime.UtcNow - within.Value : DateTime.MinValue;

        lock (_statsLock)
        {
            return _recentQueries
                .Where(q => q.ExecutedAt >= cutoff && 
                           q.ExecutionTime.TotalMilliseconds > ResourceBudgets.MaxDatabaseQueryTime)
                .OrderByDescending(q => q.ExecutionTime)
                .ToList();
        }
    }

    /// <summary>
    /// Generate performance optimization recommendations for queries
    /// </summary>
    public List<string> GenerateQueryOptimizationRecommendations()
    {
        var recommendations = new List<string>();

        lock (_statsLock)
        {
            var slowQueries = _queryStats.Values
                .Where(m => m.AverageExecutionTime > ResourceBudgets.MaxDatabaseQueryTime)
                .OrderByDescending(m => m.AverageExecutionTime)
                .Take(5)
                .ToList();

            if (slowQueries.Any())
            {
                recommendations.Add($"Optimize slow queries: {string.Join(", ", slowQueries.Select(q => q.QueryName))}");
            }

            var frequentQueries = _queryStats.Values
                .Where(m => m.TotalExecutions > 100)
                .OrderByDescending(m => m.TotalExecutions)
                .Take(3)
                .ToList();

            if (frequentQueries.Any())
            {
                recommendations.Add($"Consider caching for frequent queries: {string.Join(", ", frequentQueries.Select(q => q.QueryName))}");
            }

            var inconsistentQueries = _queryStats.Values
                .Where(m => m.MaxExecutionTime.TotalMilliseconds > m.AverageExecutionTime * 5)
                .OrderByDescending(m => m.MaxExecutionTime.TotalMilliseconds / m.AverageExecutionTime)
                .Take(3)
                .ToList();

            if (inconsistentQueries.Any())
            {
                recommendations.Add($"Investigate performance variability in: {string.Join(", ", inconsistentQueries.Select(q => q.QueryName))}");
            }
        }

        return recommendations;
    }

    /// <summary>
    /// Reset all query statistics
    /// </summary>
    public void ResetStatistics()
    {
        lock (_statsLock)
        {
            _queryStats.Clear();
            _recentQueries.Clear();
        }

        _logger.LogInformation("Database query statistics reset");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            lock (_statsLock)
            {
                _queryStats.Clear();
                _recentQueries.Clear();
            }
        }
    }
}

/// <summary>
/// Query performance metrics
/// </summary>
public class QueryMetrics
{
    public string QueryName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public double AverageExecutionTime { get; set; }
    public TimeSpan MaxExecutionTime { get; set; }
    public int SlowQueryCount { get; set; }
    public DateTime LastExecutedAt { get; set; }
}

/// <summary>
/// Individual query execution record
/// </summary>
public record QueryExecution
{
    public string QueryName { get; init; } = string.Empty;
    public TimeSpan ExecutionTime { get; init; }
    public DateTime ExecutedAt { get; init; }
}