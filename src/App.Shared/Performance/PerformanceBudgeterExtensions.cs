using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Extension methods for registering Performance Budgeter services
/// </summary>
public static class PerformanceBudgeterExtensions
{
    /// <summary>
    /// Add Performance Budgeter services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddPerformanceBudgeter(this IServiceCollection services, 
        long totalVRAM = 8L * 1024 * 1024 * 1024) // Default 8GB VRAM
    {
        // Register core performance services
        services.AddSingleton<PerformanceCollector>();
        services.AddSingleton<VRAMBudgetManager>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<VRAMBudgetManager>>();
            return new VRAMBudgetManager(logger, totalVRAM);
        });
        
        // Register main performance budgeter
        services.AddSingleton<PerformanceBudgeter>();
        
        // Register startup performance tracking
        services.AddTransient<StartupBudgetEnforcer>();

        return services;
    }

    /// <summary>
    /// Configure performance budgeter with custom settings
    /// </summary>
    public static IServiceCollection ConfigurePerformanceBudgeter(this IServiceCollection services,
        Action<PerformanceBudgeterOptions> configure)
    {
        var options = new PerformanceBudgeterOptions();
        configure(options);

        services.AddSingleton(options);
        return services;
    }
}

/// <summary>
/// Configuration options for Performance Budgeter
/// </summary>
public class PerformanceBudgeterOptions
{
    /// <summary>
    /// Total VRAM available for allocation management
    /// </summary>
    public long TotalVRAM { get; set; } = 8L * 1024 * 1024 * 1024; // 8GB default

    /// <summary>
    /// Budget enforcement interval
    /// </summary>
    public TimeSpan BudgetEnforcementInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Performance metrics collection interval
    /// </summary>
    public TimeSpan MetricsCollectionInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximum number of performance metrics to keep in history
    /// </summary>
    public int MaxMetricsHistoryCount { get; set; } = 300; // 10 minutes at 2-second intervals

    /// <summary>
    /// Enable automatic garbage collection for critical memory violations
    /// </summary>
    public bool EnableAutoGCRemediation { get; set; } = true;

    /// <summary>
    /// Enable detailed LOH monitoring
    /// </summary>
    public bool EnableLOHMonitoring { get; set; } = true;

    /// <summary>
    /// Enable UI performance tracking
    /// </summary>
    public bool EnableUIPerformanceTracking { get; set; } = true;

    /// <summary>
    /// Memory growth threshold for warnings (bytes per minute)
    /// </summary>
    public long MemoryGrowthWarningThreshold { get; set; } = 100 * 1024 * 1024; // 100MB/minute

    /// <summary>
    /// Frame time budget for UI performance (milliseconds)
    /// </summary>
    public double FrameTimeBudget { get; set; } = ResourceBudgets.MaxFrameTime;

    /// <summary>
    /// Database query time budget (milliseconds)
    /// </summary>
    public double QueryTimeBudget { get; set; } = ResourceBudgets.MaxDatabaseQueryTime;

    /// <summary>
    /// Startup time budget (milliseconds)
    /// </summary>
    public long StartupTimeBudget { get; set; } = ResourceBudgets.MaxStartupTime;
}