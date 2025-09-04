using System.Diagnostics;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Resource budget matrix enforcing performance discipline across the Lazarus envelope
/// </summary>
public static class ResourceBudgets
{
    // Application memory budgets
    public const long MaxApplicationMemory = 2L * 1024 * 1024 * 1024; // 2GB
    public const long MaxModelMemory = 8L * 1024 * 1024 * 1024;       // 8GB VRAM
    public const long MaxCacheMemory = 512L * 1024 * 1024;            // 512MB

    // UI performance budgets
    public const int MaxFrameTime = 16; // 16ms = 60 FPS
    public const int MaxStartupTime = 5000; // 5 seconds
    public const int MaxModelLoadTime = 30000; // 30 seconds

    // Query performance budgets
    public const int MaxDatabaseQueryTime = 100; // 100ms
    public const int MaxAPIResponseTime = 5000;  // 5 seconds
    public const int MaxEmbeddingTime = 2000;    // 2 seconds

    // Resource discipline thresholds
    public const double MaxCpuUsagePercent = 80.0; // 80% CPU usage
    public const double MaxMemoryUsagePercent = 75.0; // 75% memory usage
    public const double MaxVramUsagePercent = 90.0; // 90% VRAM usage

    /// <summary>
    /// Validates if resource consumption is within budget constraints
    /// </summary>
    public static BudgetValidationResult ValidateResourceConsumption(SystemResourceMetrics metrics)
    {
        var violations = new List<BudgetViolation>();

        if (metrics.ApplicationMemory > MaxApplicationMemory)
        {
            violations.Add(new BudgetViolation
            {
                Type = ViolationType.MemoryBudget,
                Severity = ViolationSeverity.Critical,
                Message = $"Application memory {metrics.ApplicationMemory / (1024.0 * 1024 * 1024):F2}GB exceeds budget {MaxApplicationMemory / (1024.0 * 1024 * 1024):F2}GB",
                CurrentValue = metrics.ApplicationMemory,
                BudgetLimit = MaxApplicationMemory,
                Component = "Application"
            });
        }

        if (metrics.VramUsage > MaxModelMemory)
        {
            violations.Add(new BudgetViolation
            {
                Type = ViolationType.VramBudget,
                Severity = ViolationSeverity.Critical,
                Message = $"VRAM usage {metrics.VramUsage / (1024.0 * 1024 * 1024):F2}GB exceeds budget {MaxModelMemory / (1024.0 * 1024 * 1024):F2}GB",
                CurrentValue = metrics.VramUsage,
                BudgetLimit = MaxModelMemory,
                Component = "GPU"
            });
        }

        if (metrics.FrameTime > MaxFrameTime)
        {
            violations.Add(new BudgetViolation
            {
                Type = ViolationType.FrameBudget,
                Severity = metrics.FrameTime > MaxFrameTime * 2 ? ViolationSeverity.Critical : ViolationSeverity.Warning,
                Message = $"Frame time {metrics.FrameTime}ms exceeds budget {MaxFrameTime}ms",
                CurrentValue = metrics.FrameTime,
                BudgetLimit = MaxFrameTime,
                Component = "UI Rendering"
            });
        }

        if (metrics.CpuUsagePercent > MaxCpuUsagePercent)
        {
            violations.Add(new BudgetViolation
            {
                Type = ViolationType.CpuBudget,
                Severity = metrics.CpuUsagePercent > 95 ? ViolationSeverity.Critical : ViolationSeverity.Warning,
                Message = $"CPU usage {metrics.CpuUsagePercent:F1}% exceeds threshold {MaxCpuUsagePercent:F1}%",
                CurrentValue = metrics.CpuUsagePercent,
                BudgetLimit = MaxCpuUsagePercent,
                Component = "CPU"
            });
        }

        return new BudgetValidationResult
        {
            IsWithinBudget = violations.Count == 0,
            Violations = violations,
            ValidationTime = DateTime.UtcNow,
            OverallHealth = CalculateOverallHealth(violations)
        };
    }

    private static ResourceHealth CalculateOverallHealth(List<BudgetViolation> violations)
    {
        if (!violations.Any()) return ResourceHealth.Excellent;
        
        var criticalViolations = violations.Count(v => v.Severity == ViolationSeverity.Critical);
        var warningViolations = violations.Count(v => v.Severity == ViolationSeverity.Warning);

        if (criticalViolations >= 2) return ResourceHealth.Critical;
        if (criticalViolations >= 1) return ResourceHealth.Poor;
        if (warningViolations >= 3) return ResourceHealth.Fair;
        if (warningViolations >= 1) return ResourceHealth.Good;

        return ResourceHealth.Excellent;
    }
}

/// <summary>
/// System resource metrics for budget validation
/// </summary>
public record SystemResourceMetrics
{
    public long ApplicationMemory { get; init; }
    public long VramUsage { get; init; }
    public double CpuUsagePercent { get; init; }
    public double MemoryUsagePercent { get; init; }
    public double FrameTime { get; init; }
    public long DatabaseQueryTime { get; init; }
    public DateTime MeasurementTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Budget validation result
/// </summary>
public record BudgetValidationResult
{
    public bool IsWithinBudget { get; init; }
    public List<BudgetViolation> Violations { get; init; } = new();
    public DateTime ValidationTime { get; init; }
    public ResourceHealth OverallHealth { get; init; }
}

/// <summary>
/// Resource budget violation
/// </summary>
public record BudgetViolation
{
    public ViolationType Type { get; init; }
    public ViolationSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public double CurrentValue { get; init; }
    public double BudgetLimit { get; init; }
    public string Component { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Types of budget violations
/// </summary>
public enum ViolationType
{
    MemoryBudget,
    VramBudget,
    CpuBudget,
    FrameBudget,
    QueryBudget,
    StartupBudget,
    ApiResponseBudget
}

/// <summary>
/// Severity levels for violations
/// </summary>
public enum ViolationSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Overall resource health assessment
/// </summary>
public enum ResourceHealth
{
    Critical,
    Poor,
    Fair,
    Good,
    Excellent
}