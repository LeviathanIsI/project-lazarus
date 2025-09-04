using Microsoft.Extensions.Logging;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Performance.Budgeter - Enforces resource discipline across the Lazarus performance envelope
/// Prevents memory hemorrhaging, maintains response time budgets, and ensures optimal resource utilization
/// </summary>
public class PerformanceBudgeter : IDisposable
{
    private readonly ILogger<PerformanceBudgeter> _logger;
    private readonly PerformanceCollector _performanceCollector;
    private readonly VRAMBudgetManager _vramManager;
    private readonly UIPerformanceTracker _uiTracker;
    private readonly DatabaseQueryBudgetMonitor _queryMonitor;
    private readonly LOHMonitor _lohMonitor;
    private readonly Timer _budgetEnforcementTimer;
    private readonly Queue<BudgetValidationResult> _validationHistory = new();
    private readonly object _validationLock = new();
    private bool _disposed = false;

    // Performance alerts
    public event EventHandler<BudgetViolationEvent>? BudgetViolation;
    public event EventHandler<PerformanceOptimizationEvent>? OptimizationRecommendation;

    public PerformanceBudgeter(
        ILogger<PerformanceBudgeter> logger,
        PerformanceCollector performanceCollector,
        VRAMBudgetManager vramManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceCollector = performanceCollector ?? throw new ArgumentNullException(nameof(performanceCollector));
        _vramManager = vramManager ?? throw new ArgumentNullException(nameof(vramManager));
        
        _uiTracker = new UIPerformanceTracker(_logger);
        _queryMonitor = new DatabaseQueryBudgetMonitor(_logger);
        _lohMonitor = new LOHMonitor(_logger);

        // Subscribe to performance events
        _performanceCollector.MetricsCollected += OnMetricsCollected;

        // Enforce budgets every 5 seconds
        _budgetEnforcementTimer = new Timer(EnforceBudgets, null, 
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        _logger.LogInformation("Performance Budgeter initialized - Resource discipline enforced");
    }

    /// <summary>
    /// Validate current system state against resource budgets
    /// </summary>
    public async Task<BudgetValidationResult> ValidateResourceBudgetsAsync()
    {
        var metrics = await _performanceCollector.CollectMetricsAsync();
        var vramStats = _vramManager.GetAllocationStats();
        var uiMetrics = _uiTracker.GetCurrentMetrics();
        var queryMetrics = _queryMonitor.GetQueryMetrics();

        var systemResourceMetrics = new SystemResourceMetrics
        {
            ApplicationMemory = metrics.ApplicationMemory,
            VramUsage = vramStats.AllocatedVRAM,
            CpuUsagePercent = metrics.CpuUsage,
            MemoryUsagePercent = (double)metrics.MemoryUsage / (16L * 1024 * 1024 * 1024) * 100, // Assume 16GB system RAM
            FrameTime = uiMetrics.AverageFrameTime,
            DatabaseQueryTime = (long)queryMetrics.AverageQueryTime
        };

        var result = ResourceBudgets.ValidateResourceConsumption(systemResourceMetrics);

        // Store validation history
        lock (_validationLock)
        {
            _validationHistory.Enqueue(result);
            if (_validationHistory.Count > 100) // Keep last 100 validations
            {
                _validationHistory.Dequeue();
            }
        }

        // Generate optimization recommendations if needed
        if (!result.IsWithinBudget)
        {
            await GenerateOptimizationRecommendationsAsync(result, systemResourceMetrics);
        }

        return result;
    }

    /// <summary>
    /// Start UI performance tracking
    /// </summary>
    public void StartUIPerformanceTracking()
    {
        _uiTracker.StartTracking();
        _logger.LogInformation("UI performance tracking started");
    }

    /// <summary>
    /// Stop UI performance tracking
    /// </summary>
    public void StopUIPerformanceTracking()
    {
        _uiTracker.StopTracking();
        _logger.LogInformation("UI performance tracking stopped");
    }

    /// <summary>
    /// Register a database query for budget monitoring
    /// </summary>
    public void RegisterDatabaseQuery(string queryName, TimeSpan executionTime)
    {
        _queryMonitor.RegisterQuery(queryName, executionTime);
    }

    /// <summary>
    /// Request VRAM allocation through budget manager
    /// </summary>
    public bool RequestVRAMAllocation(string component, long requiredBytes, VRAMPriority priority = VRAMPriority.Normal)
    {
        return _vramManager.RequestVRAMAllocation(component, requiredBytes, priority);
    }

    /// <summary>
    /// Release VRAM allocation
    /// </summary>
    public bool ReleaseVRAMAllocation(string component, Guid? allocationId = null)
    {
        return _vramManager.ReleaseVRAMAllocation(component, allocationId);
    }

    /// <summary>
    /// Get comprehensive performance report
    /// </summary>
    public async Task<PerformanceReport> GeneratePerformanceReportAsync()
    {
        var budgetValidation = await ValidateResourceBudgetsAsync();
        var systemMetrics = await _performanceCollector.CollectMetricsAsync();
        var trends = _performanceCollector.AnalyzeTrends(TimeSpan.FromMinutes(10));
        var vramStats = _vramManager.GetAllocationStats();
        var uiMetrics = _uiTracker.GetCurrentMetrics();
        var queryMetrics = _queryMonitor.GetQueryMetrics();

        return new PerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            BudgetCompliance = budgetValidation,
            SystemMetrics = systemMetrics,
            PerformanceTrends = trends,
            VRAMAllocationStats = vramStats,
            UIPerformanceMetrics = uiMetrics,
            DatabasePerformanceMetrics = queryMetrics,
            OverallGrade = CalculateOverallPerformanceGrade(budgetValidation, systemMetrics, uiMetrics),
            Recommendations = await GeneratePerformanceRecommendationsAsync(budgetValidation, systemMetrics)
        };
    }

    private void OnMetricsCollected(object? sender, SystemMetrics metrics)
    {
        // Log critical resource conditions
        if (metrics.MemoryUsage > ResourceBudgets.MaxApplicationMemory * 0.9)
        {
            _logger.LogWarning("Memory usage approaching budget limit: {Usage}GB/{Budget}GB",
                metrics.MemoryUsage / (1024.0 * 1024 * 1024),
                ResourceBudgets.MaxApplicationMemory / (1024.0 * 1024 * 1024));
        }

        if (metrics.VRAMUsage.UsagePercent > 85)
        {
            _logger.LogWarning("VRAM usage high: {Usage:F1}% of {Total}GB",
                metrics.VRAMUsage.UsagePercent,
                metrics.VRAMUsage.TotalBytes / (1024.0 * 1024 * 1024));
        }
    }

    private async void EnforceBudgets(object? state)
    {
        if (_disposed) return;

        try
        {
            var validation = await ValidateResourceBudgetsAsync();
            
            foreach (var violation in validation.Violations)
            {
                BudgetViolation?.Invoke(this, new BudgetViolationEvent
                {
                    Violation = violation,
                    DetectedAt = DateTime.UtcNow,
                    SystemState = await _performanceCollector.CollectMetricsAsync()
                });

                _logger.LogWarning("Budget violation detected: {Type} - {Message}", 
                    violation.Type, violation.Message);
            }

            // Auto-remediation for critical violations
            await AttemptAutoRemediationAsync(validation.Violations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during budget enforcement");
        }
    }

    private Task AttemptAutoRemediationAsync(List<BudgetViolation> violations)
    {
        foreach (var violation in violations.Where(v => v.Severity == ViolationSeverity.Critical))
        {
            switch (violation.Type)
            {
                case ViolationType.MemoryBudget:
                    _logger.LogInformation("Attempting memory remediation: forcing GC");
                    GC.Collect(2, GCCollectionMode.Forced);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Forced);
                    break;

                case ViolationType.VramBudget:
                    _logger.LogInformation("Attempting VRAM remediation: cleaning up allocations");
                    // VRAM cleanup would be handled by the VRAMBudgetManager
                    break;

                case ViolationType.FrameBudget:
                    _logger.LogInformation("Frame budget violation - UI performance degraded");
                    // Could trigger UI refresh rate reduction
                    break;
            }
        }
        return Task.CompletedTask;
    }

    private Task GenerateOptimizationRecommendationsAsync(BudgetValidationResult validation, SystemResourceMetrics metrics)
    {
        var recommendations = new List<string>();

        if (validation.Violations.Any(v => v.Type == ViolationType.MemoryBudget))
        {
            recommendations.Add("Consider reducing application memory footprint or implementing more aggressive garbage collection");
        }

        if (validation.Violations.Any(v => v.Type == ViolationType.VramBudget))
        {
            recommendations.Add("Optimize model loading strategy or consider model quantization to reduce VRAM usage");
        }

        if (validation.Violations.Any(v => v.Type == ViolationType.FrameBudget))
        {
            recommendations.Add("UI rendering performance issues - consider reducing visual complexity or optimizing data binding");
        }

        if (metrics.CpuUsagePercent > 80)
        {
            recommendations.Add("High CPU usage detected - consider offloading work to background threads or optimizing algorithms");
        }

        if (recommendations.Any())
        {
            OptimizationRecommendation?.Invoke(this, new PerformanceOptimizationEvent
            {
                Recommendations = recommendations,
                Severity = validation.OverallHealth == ResourceHealth.Critical ? OptimizationSeverity.Critical : OptimizationSeverity.Normal,
                GeneratedAt = DateTime.UtcNow
            });
        }
        return Task.CompletedTask;
    }

    private PerformanceGrade CalculateOverallPerformanceGrade(BudgetValidationResult budget, SystemMetrics metrics, UIPerformanceMetrics ui)
    {
        var score = 100;

        // Deduct points for budget violations
        score -= budget.Violations.Count(v => v.Severity == ViolationSeverity.Critical) * 20;
        score -= budget.Violations.Count(v => v.Severity == ViolationSeverity.Warning) * 10;

        // Deduct points for resource usage
        if (metrics.CpuUsage > 80) score -= 15;
        if (metrics.MemoryUsage > ResourceBudgets.MaxApplicationMemory * 0.8) score -= 15;
        if (ui.AverageFrameTime > ResourceBudgets.MaxFrameTime) score -= 20;

        return score switch
        {
            >= 90 => PerformanceGrade.Excellent,
            >= 75 => PerformanceGrade.Good,
            >= 60 => PerformanceGrade.Fair,
            >= 40 => PerformanceGrade.Poor,
            _ => PerformanceGrade.Critical
        };
    }

    private Task<List<string>> GeneratePerformanceRecommendationsAsync(BudgetValidationResult budget, SystemMetrics metrics)
    {
        var recommendations = new List<string>();

        // Memory recommendations
        if (metrics.ApplicationMemory > ResourceBudgets.MaxApplicationMemory * 0.8)
        {
            recommendations.Add("Memory usage approaching limit - consider implementing memory pooling or reducing cache sizes");
        }

        // CPU recommendations
        if (metrics.CpuUsage > 70)
        {
            recommendations.Add("CPU usage elevated - consider async/await patterns for I/O operations and background processing");
        }

        // VRAM recommendations
        var vramStats = _vramManager.GetAllocationStats();
        if (vramStats.UsagePercent > 80)
        {
            recommendations.Add("VRAM usage high - consider model optimization or batch processing strategies");
        }

        // GC pressure recommendations
        if (metrics.GCPressure > 500 * 1024 * 1024) // 500MB
        {
            recommendations.Add("High garbage collection pressure - review object allocation patterns and consider object pooling");
        }

        return Task.FromResult(recommendations);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            _budgetEnforcementTimer?.Dispose();
            _uiTracker?.Dispose();
            _queryMonitor?.Dispose();
            _lohMonitor?.Dispose();
            _vramManager?.Dispose();
            _performanceCollector?.Dispose();

            _logger.LogInformation("Performance Budgeter disposed - Resource discipline enforcement ended");
        }
    }
}

/// <summary>
/// Budget violation event arguments
/// </summary>
public class BudgetViolationEvent : EventArgs
{
    public BudgetViolation Violation { get; init; } = new();
    public DateTime DetectedAt { get; init; }
    public SystemMetrics SystemState { get; init; } = new();
}

/// <summary>
/// Performance optimization event arguments
/// </summary>
public class PerformanceOptimizationEvent : EventArgs
{
    public List<string> Recommendations { get; init; } = new();
    public OptimizationSeverity Severity { get; init; }
    public DateTime GeneratedAt { get; init; }
}

/// <summary>
/// Comprehensive performance report
/// </summary>
public record PerformanceReport
{
    public DateTime GeneratedAt { get; init; }
    public BudgetValidationResult BudgetCompliance { get; init; } = new();
    public SystemMetrics SystemMetrics { get; init; } = new();
    public PerformanceTrends PerformanceTrends { get; init; } = new();
    public VRAMAllocationStats VRAMAllocationStats { get; init; } = new();
    public UIPerformanceMetrics UIPerformanceMetrics { get; init; } = new();
    public DatabasePerformanceMetrics DatabasePerformanceMetrics { get; init; } = new();
    public PerformanceGrade OverallGrade { get; init; }
    public List<string> Recommendations { get; init; } = new();
}

/// <summary>
/// UI performance metrics
/// </summary>
public record UIPerformanceMetrics
{
    public double AverageFrameTime { get; init; }
    public double MaxFrameTime { get; init; }
    public double FrameTimeVariance { get; init; }
    public int FramesMeasured { get; init; }
    public bool IsWithinBudget { get; init; }
}

/// <summary>
/// Database performance metrics
/// </summary>
public record DatabasePerformanceMetrics
{
    public double AverageQueryTime { get; init; }
    public double MaxQueryTime { get; init; }
    public int QueriesExecuted { get; init; }
    public int SlowQueries { get; init; }
    public bool IsWithinBudget { get; init; }
}

/// <summary>
/// Performance grade enumeration
/// </summary>
public enum PerformanceGrade
{
    Critical,
    Poor,
    Fair,
    Good,
    Excellent
}

/// <summary>
/// Optimization severity levels
/// </summary>
public enum OptimizationSeverity
{
    Normal,
    Important,
    Critical
}