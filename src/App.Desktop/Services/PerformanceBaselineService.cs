using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Lazarus.App.Shared.Performance;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Performance baseline service that establishes resource consumption baselines and validates budget compliance
/// </summary>
public class PerformanceBaselineService : BackgroundService
{
    private readonly ILogger<PerformanceBaselineService> _logger;
    private readonly PerformanceBudgeter _performanceBudgeter;
    private readonly StartupBudgetEnforcer _startupEnforcer;
    private PerformanceReport? _baselineReport;

    public event EventHandler<PerformanceReport>? BaselineEstablished;
    public event EventHandler<BudgetViolationEvent>? BaselineBudgetViolation;

    public PerformanceBaselineService(
        ILogger<PerformanceBaselineService> logger,
        PerformanceBudgeter performanceBudgeter,
        StartupBudgetEnforcer startupEnforcer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _performanceBudgeter = performanceBudgeter ?? throw new ArgumentNullException(nameof(performanceBudgeter));
        _startupEnforcer = startupEnforcer ?? throw new ArgumentNullException(nameof(startupEnforcer));

        // Subscribe to budget violations
        _performanceBudgeter.BudgetViolation += OnBudgetViolation;
    }

    /// <summary>
    /// Get the current performance baseline (null if not yet established)
    /// </summary>
    public PerformanceReport? GetBaseline() => _baselineReport;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performance Baseline Service starting - establishing resource consumption baselines");

        try
        {
            // Wait a bit for application to stabilize after startup
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            // Complete startup validation
            var startupReport = await _startupEnforcer.CompleteStartupValidationAsync();
            LogStartupResults(startupReport);

            // Start UI performance tracking
            _performanceBudgeter.StartUIPerformanceTracking();

            // Wait additional time for UI to stabilize
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            // Establish performance baseline
            await EstablishPerformanceBaselineAsync();

            // Continue monitoring and validation
            await RunContinuousMonitoringAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Performance baseline service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in performance baseline service");
        }
    }

    private async Task EstablishPerformanceBaselineAsync()
    {
        try
        {
            _logger.LogInformation("Establishing performance baseline - collecting initial measurements");

            // Force garbage collection to establish clean baseline
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced);

            // Wait a moment for GC to settle
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Generate comprehensive baseline report
            _baselineReport = await _performanceBudgeter.GeneratePerformanceReportAsync();

            _logger.LogInformation("Performance baseline established - Grade: {Grade}, Memory: {Memory}MB, VRAM: {VRAM}MB",
                _baselineReport.OverallGrade,
                _baselineReport.SystemMetrics.ApplicationMemory / (1024 * 1024),
                _baselineReport.VRAMAllocationStats.AllocatedVRAM / (1024 * 1024));

            // Log budget compliance
            if (!_baselineReport.BudgetCompliance.IsWithinBudget)
            {
                _logger.LogWarning("Baseline budget violations detected: {Violations}",
                    string.Join(", ", _baselineReport.BudgetCompliance.Violations.Select(v => v.Type.ToString())));
            }

            // Log performance trends
            if (_baselineReport.PerformanceTrends.Trends.Any())
            {
                var trendSummary = _baselineReport.PerformanceTrends.Trends
                    .Select(t => $"{t.Key}: {t.Value}")
                    .ToArray();
                _logger.LogInformation("Performance trends: {Trends}", string.Join(", ", trendSummary));
            }

            // Emit baseline established event
            BaselineEstablished?.Invoke(this, _baselineReport);

            // Log recommendations if any
            if (_baselineReport.Recommendations.Any())
            {
                _logger.LogInformation("Performance recommendations: {Recommendations}",
                    string.Join("; ", _baselineReport.Recommendations));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish performance baseline");
        }
    }

    private async Task RunContinuousMonitoringAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting continuous performance monitoring");

        var monitoringInterval = TimeSpan.FromMinutes(5); // Generate report every 5 minutes
        var lastReportTime = DateTime.UtcNow;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(monitoringInterval, stoppingToken);

                // Generate periodic performance report
                var currentReport = await _performanceBudgeter.GeneratePerformanceReportAsync();

                // Compare with baseline if available
                if (_baselineReport != null)
                {
                    var comparison = CompareWithBaseline(currentReport, _baselineReport);
                    LogPerformanceComparison(comparison);
                }

                // Log current status
                _logger.LogInformation("Performance monitoring - Grade: {Grade}, CPU: {CPU:F1}%, Memory: {Memory}MB, Frame: {Frame:F1}ms",
                    currentReport.OverallGrade,
                    currentReport.SystemMetrics.CpuUsage,
                    currentReport.SystemMetrics.ApplicationMemory / (1024 * 1024),
                    currentReport.UIPerformanceMetrics.AverageFrameTime);

                lastReportTime = DateTime.UtcNow;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during continuous performance monitoring");
            }
        }
    }

    private void LogStartupResults(StartupReport startupReport)
    {
        _logger.LogInformation("Startup Performance Report:");
        _logger.LogInformation("  Total Time: {TotalTime}ms (Budget: {Budget}ms) - Grade: {Grade}",
            startupReport.TotalStartupTime, ResourceBudgets.MaxStartupTime, startupReport.StartupGrade);

        if (startupReport.Milestones.Any())
        {
            _logger.LogInformation("  Milestones:");
            foreach (var milestone in startupReport.Milestones)
            {
                _logger.LogInformation("    {Milestone}: {Time}ms", milestone.Key, milestone.Value);
            }
        }

        if (startupReport.Phases.Any())
        {
            _logger.LogInformation("  Phases:");
            foreach (var phase in startupReport.Phases)
            {
                var budgetInfo = phase.BudgetMs > 0 ? $" (Budget: {phase.BudgetMs}ms)" : "";
                _logger.LogInformation("    {Phase}: {Time}ms{Budget}", phase.Name, phase.ElapsedMs, budgetInfo);
            }
        }

        if (startupReport.BudgetViolations.Any())
        {
            _logger.LogWarning("  Budget Violations:");
            foreach (var violation in startupReport.BudgetViolations)
            {
                _logger.LogWarning("    {Severity}: {Message}", violation.Severity, violation.Message);
            }
        }
    }

    private PerformanceComparison CompareWithBaseline(PerformanceReport current, PerformanceReport baseline)
    {
        var memoryDelta = current.SystemMetrics.ApplicationMemory - baseline.SystemMetrics.ApplicationMemory;
        var cpuDelta = current.SystemMetrics.CpuUsage - baseline.SystemMetrics.CpuUsage;
        var frameDelta = current.UIPerformanceMetrics.AverageFrameTime - baseline.UIPerformanceMetrics.AverageFrameTime;
        var vramDelta = current.VRAMAllocationStats.AllocatedVRAM - baseline.VRAMAllocationStats.AllocatedVRAM;

        return new PerformanceComparison
        {
            MemoryDeltaBytes = memoryDelta,
            CpuDeltaPercent = cpuDelta,
            FrameTimeDeltaMs = frameDelta,
            VRAMDeltaBytes = vramDelta,
            GradeImprovement = (PerformanceGrade)((int)current.OverallGrade - (int)baseline.OverallGrade),
            ComparisonTime = DateTime.UtcNow
        };
    }

    private void LogPerformanceComparison(PerformanceComparison comparison)
    {
        var memoryChange = comparison.MemoryDeltaBytes / (1024.0 * 1024);
        var vramChange = comparison.VRAMDeltaBytes / (1024.0 * 1024);

        _logger.LogInformation("Performance vs Baseline - Memory: {Memory:+#;-#;0}MB, CPU: {CPU:+#.#;-#.#;0}%, Frame: {Frame:+#.#;-#.#;0}ms, VRAM: {VRAM:+#;-#;0}MB",
            memoryChange, comparison.CpuDeltaPercent, comparison.FrameTimeDeltaMs, vramChange);

        if (Math.Abs(memoryChange) > 100) // 100MB change
        {
            var direction = memoryChange > 0 ? "increased" : "decreased";
            _logger.LogInformation("Significant memory change detected: {Change:F1}MB {Direction} from baseline",
                Math.Abs(memoryChange), direction);
        }

        if (Math.Abs(comparison.FrameTimeDeltaMs) > 5) // 5ms frame time change
        {
            var direction = comparison.FrameTimeDeltaMs > 0 ? "slower" : "faster";
            _logger.LogInformation("UI performance change: {Change:F1}ms {Direction} than baseline",
                Math.Abs(comparison.FrameTimeDeltaMs), direction);
        }
    }

    private void OnBudgetViolation(object? sender, BudgetViolationEvent e)
    {
        BaselineBudgetViolation?.Invoke(this, e);
        
        _logger.LogWarning("Budget violation during operation: {Type} - {Message} (Current: {Current}, Limit: {Limit})",
            e.Violation.Type, e.Violation.Message, e.Violation.CurrentValue, e.Violation.BudgetLimit);
    }

    public override void Dispose()
    {
        _performanceBudgeter.StopUIPerformanceTracking();
        _performanceBudgeter.BudgetViolation -= OnBudgetViolation;
        base.Dispose();
    }
}

/// <summary>
/// Performance comparison with baseline
/// </summary>
public record PerformanceComparison
{
    public long MemoryDeltaBytes { get; init; }
    public double CpuDeltaPercent { get; init; }
    public double FrameTimeDeltaMs { get; init; }
    public long VRAMDeltaBytes { get; init; }
    public PerformanceGrade GradeImprovement { get; init; }
    public DateTime ComparisonTime { get; init; }
}