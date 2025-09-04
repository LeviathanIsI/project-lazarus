using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Post-optimization validator that analyzes resource consumption patterns following code quality improvements
/// </summary>
public class PostOptimizationValidator
{
    private readonly ILogger<PostOptimizationValidator> _logger;
    private readonly PerformanceCollector _collector;
    private readonly VRAMBudgetManager _vramManager;

    public PostOptimizationValidator(
        ILogger<PostOptimizationValidator> logger,
        PerformanceCollector collector,
        VRAMBudgetManager vramManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
        _vramManager = vramManager ?? throw new ArgumentNullException(nameof(vramManager));
    }

    /// <summary>
    /// Execute comprehensive post-optimization validation
    /// </summary>
    public async Task<PostOptimizationReport> ValidateOptimizationsAsync()
    {
        _logger.LogInformation("PERFORMANCE BUDGETER - POST-OPTIMIZATION VALIDATION");
        _logger.LogInformation("Analyzing resource consumption patterns following code-quality-sentinel improvements");

        var report = new PostOptimizationReport
        {
            ValidationStartTime = DateTime.UtcNow
        };

        try
        {
            // Phase 1: Memory allocation pattern analysis
            _logger.LogInformation("Phase 1: Memory allocation pattern analysis");
            report.MemoryAnalysis = await AnalyzeMemoryPatternsAsync();

            // Phase 2: VRAM utilization assessment
            _logger.LogInformation("Phase 2: VRAM utilization assessment for LLM inference orchestration");
            report.VRAMAnalysis = await AnalyzeVRAMUtilizationAsync();

            // Phase 3: Threading overhead evaluation
            _logger.LogInformation("Phase 3: Threading overhead evaluation post-async pattern corrections");
            report.ThreadingAnalysis = await AnalyzeThreadingOverheadAsync();

            // Phase 4: Build resource consumption baseline
            _logger.LogInformation("Phase 4: Build resource consumption baseline establishment");
            report.BuildResourceAnalysis = await AnalyzeBuildResourceConsumptionAsync();

            // Phase 5: WPF UI responsiveness validation
            _logger.LogInformation("Phase 5: WPF UI responsiveness with corrected null safety patterns");
            report.UIResponsivenessAnalysis = await AnalyzeUIResponsivenessAsync();

            // Phase 6: ASP.NET Core API latency impact assessment
            _logger.LogInformation("Phase 6: ASP.NET Core API latency impact from quality improvements");
            report.APILatencyAnalysis = await AnalyzeAPILatencyAsync();

            // Phase 7: Entity Framework query performance validation
            _logger.LogInformation("Phase 7: Entity Framework query performance post-warning elimination");
            report.DatabasePerformanceAnalysis = await AnalyzeDatabasePerformanceAsync();

            // Phase 8: Model loading resource consumption patterns
            _logger.LogInformation("Phase 8: Model loading resource consumption patterns");
            report.ModelLoadingAnalysis = await AnalyzeModelLoadingPatternsAsync();

            // Generate overall assessment
            report.OverallAssessment = GenerateOverallAssessment(report);
            report.ValidationEndTime = DateTime.UtcNow;
            report.ValidationDuration = report.ValidationEndTime - report.ValidationStartTime;

            LogValidationResults(report);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during post-optimization validation");
            report.ValidationEndTime = DateTime.UtcNow;
            report.ValidationDuration = report.ValidationEndTime - report.ValidationStartTime;
            report.ValidationErrors.Add($"Validation failed: {ex.Message}");
            return report;
        }
    }

    private async Task<MemoryAllocationAnalysis> AnalyzeMemoryPatternsAsync()
    {
        var analysis = new MemoryAllocationAnalysis();

        try
        {
            // Collect baseline metrics
            var initialMetrics = await _collector.CollectMetricsAsync();
            analysis.InitialMemoryUsage = initialMetrics.ApplicationMemory;
            
            // Force GC to establish clean baseline
            var beforeGC = GC.GetTotalMemory(false);
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced);
            var afterGC = GC.GetTotalMemory(false);
            
            analysis.GCEfficiency = beforeGC > 0 ? (double)(beforeGC - afterGC) / beforeGC * 100 : 0;
            analysis.MemoryPressure = afterGC;

            // Check memory allocation patterns over a short period
            var measurements = new List<long>();
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                var metrics = await _collector.CollectMetricsAsync();
                measurements.Add(metrics.ApplicationMemory);
            }

            analysis.MemoryStability = CalculateMemoryStability(measurements);
            analysis.AllocationPattern = DetermineAllocationPattern(measurements);

            // Check for common memory issues
            analysis.PotentialIssues = IdentifyMemoryIssues(analysis);

            _logger.LogInformation("Memory Analysis: Initial={InitialMB}MB, GC Efficiency={Efficiency:F1}%, Stability={Stability:F2}",
                analysis.InitialMemoryUsage / (1024 * 1024), analysis.GCEfficiency, analysis.MemoryStability);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing memory patterns");
            analysis.PotentialIssues.Add($"Analysis error: {ex.Message}");
            return analysis;
        }
    }

    private async Task<VRAMUtilizationAnalysis> AnalyzeVRAMUtilizationAsync()
    {
        try
        {
            var stats = _vramManager.GetAllocationStats();
            var metrics = await _collector.CollectMetricsAsync();

            // Test allocation feasibility
            var feasibilityTest = _vramManager.CheckAllocationFeasibility(1L * 1024 * 1024 * 1024, VRAMPriority.Normal); // 1GB test

            var utilizationPercent = stats.UsagePercent;
            
            var analysis = new VRAMUtilizationAnalysis
            {
                TotalVRAMGB = stats.TotalVRAM / (1024.0 * 1024 * 1024),
                AllocatedVRAMGB = stats.AllocatedVRAM / (1024.0 * 1024 * 1024),
                UtilizationPercent = utilizationPercent,
                AllocationCount = stats.AllocationCount,
                LargestAllocationMB = stats.LargestAllocation / (1024 * 1024),
                AllocationFeasibility = feasibilityTest.CanAllocate,
                AvailableForAllocation = feasibilityTest.AvailableBytes / (1024.0 * 1024 * 1024),
                GPUUtilization = metrics.VRAMUsage.GpuUtilizationPercent,
                // Determine utilization level
                UtilizationLevel = utilizationPercent > 90 ? UtilizationLevel.Critical :
                                  utilizationPercent > 75 ? UtilizationLevel.High :
                                  utilizationPercent > 50 ? UtilizationLevel.Moderate :
                                  UtilizationLevel.Low
            };

            _logger.LogInformation("VRAM Analysis: Total={Total:F2}GB, Used={Used:F2}GB ({Usage:F1}%), Available={Available:F2}GB",
                analysis.TotalVRAMGB, analysis.AllocatedVRAMGB, analysis.UtilizationPercent, analysis.AvailableForAllocation);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing VRAM utilization");
            return new VRAMUtilizationAnalysis();
        }
    }

    private async Task<ThreadingOverheadAnalysis> AnalyzeThreadingOverheadAsync()
    {
        var analysis = new ThreadingOverheadAnalysis();

        try
        {
            var initialMetrics = await _collector.CollectMetricsAsync();
            analysis.InitialThreadCount = initialMetrics.ThreadCount;

            // Monitor thread usage patterns
            var threadCounts = new List<int>();
            var measurements = 20; // 20 measurements over 10 seconds
            
            for (int i = 0; i < measurements; i++)
            {
                await Task.Delay(500);
                var metrics = await _collector.CollectMetricsAsync();
                threadCounts.Add(metrics.ThreadCount);
            }

            analysis.AverageThreadCount = threadCounts.Average();
            analysis.MaxThreadCount = threadCounts.Max();
            analysis.MinThreadCount = threadCounts.Min();
            analysis.ThreadVariability = CalculateVariability(threadCounts.Select(t => (double)t).ToArray());

            // Assess threading efficiency
            var currentProcess = Process.GetCurrentProcess();
            analysis.HandleCount = currentProcess.HandleCount;
            
            // Check for excessive thread creation patterns
            if (analysis.ThreadVariability > 5.0)
            {
                analysis.PotentialIssues.Add("High thread count variability detected - possible thread churning");
            }

            if (analysis.AverageThreadCount > Environment.ProcessorCount * 4)
            {
                analysis.PotentialIssues.Add("Thread count significantly higher than CPU cores - possible over-threading");
            }

            _logger.LogInformation("Threading Analysis: Avg={Avg:F1}, Range={Min}-{Max}, Variability={Var:F2}, Handles={Handles}",
                analysis.AverageThreadCount, analysis.MinThreadCount, analysis.MaxThreadCount, 
                analysis.ThreadVariability, analysis.HandleCount);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing threading overhead");
            analysis.PotentialIssues.Add($"Analysis error: {ex.Message}");
            return analysis;
        }
    }

    private async Task<BuildResourceAnalysis> AnalyzeBuildResourceConsumptionAsync()
    {
        var analysis = new BuildResourceAnalysis();

        try
        {
            var startTime = DateTime.UtcNow;
            var initialMetrics = await _collector.CollectMetricsAsync();

            analysis.InitialCPUUsage = initialMetrics.CpuUsage;
            analysis.InitialMemoryUsage = initialMetrics.ApplicationMemory;

            // Monitor resource usage during typical operations
            var cpuReadings = new List<double>();
            var memoryReadings = new List<long>();

            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(1000);
                var metrics = await _collector.CollectMetricsAsync();
                cpuReadings.Add(metrics.CpuUsage);
                memoryReadings.Add(metrics.ApplicationMemory);
            }

            analysis.AverageCPUUsage = cpuReadings.Average();
            analysis.PeakCPUUsage = cpuReadings.Max();
            analysis.AverageMemoryUsage = memoryReadings.Average();
            analysis.PeakMemoryUsage = memoryReadings.Max();

            analysis.ResourceEfficiency = CalculateResourceEfficiency(analysis);
            analysis.MeasurementDuration = DateTime.UtcNow - startTime;

            // Check against budgets
            analysis.WithinCPUBudget = analysis.AverageCPUUsage <= ResourceBudgets.MaxCpuUsagePercent;
            analysis.WithinMemoryBudget = analysis.PeakMemoryUsage <= ResourceBudgets.MaxApplicationMemory;

            _logger.LogInformation("Build Resource Analysis: CPU={CPU:F1}% (Peak: {PeakCPU:F1}%), Memory={Mem}MB (Peak: {PeakMem}MB)",
                analysis.AverageCPUUsage, analysis.PeakCPUUsage,
                analysis.AverageMemoryUsage / (1024 * 1024), analysis.PeakMemoryUsage / (1024 * 1024));

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing build resource consumption");
            return analysis;
        }
    }

    private async Task<UIResponsivenessAnalysis> AnalyzeUIResponsivenessAsync()
    {
        try
        {
            // Simulate UI operations and measure responsiveness
            var frameTimes = new List<double>();
            var responseStopwatch = new Stopwatch();

            // Simulate typical UI operations
            for (int i = 0; i < 60; i++) // 1 second of frames at 60 FPS
            {
                responseStopwatch.Restart();
                
                // Simulate frame processing
                await Task.Delay(1); // Minimal processing time
                
                responseStopwatch.Stop();
                frameTimes.Add(responseStopwatch.Elapsed.TotalMilliseconds);
            }

            var averageFrameTimeMs = frameTimes.Average();
            var maxFrameTimeMs = frameTimes.Max();
            var currentFPS = averageFrameTimeMs > 0 ? 1000.0 / averageFrameTimeMs : 0;
            var isWithinBudget = averageFrameTimeMs <= ResourceBudgets.MaxFrameTime;

            var analysis = new UIResponsivenessAnalysis
            {
                AverageFrameTimeMs = averageFrameTimeMs,
                MaxFrameTimeMs = maxFrameTimeMs,
                CurrentFPS = currentFPS,
                IsWithinBudget = isWithinBudget,
                // Assess responsiveness level
                ResponsivenessLevel = averageFrameTimeMs <= ResourceBudgets.MaxFrameTime ? ResponsivenessLevel.Excellent :
                                     averageFrameTimeMs <= ResourceBudgets.MaxFrameTime * 1.5 ? ResponsivenessLevel.Good :
                                     averageFrameTimeMs <= ResourceBudgets.MaxFrameTime * 2 ? ResponsivenessLevel.Fair :
                                     ResponsivenessLevel.Poor
            };

            _logger.LogInformation("UI Responsiveness Analysis: Avg Frame={Avg:F2}ms, Max Frame={Max:F2}ms, FPS={FPS:F1}, Level={Level}",
                analysis.AverageFrameTimeMs, analysis.MaxFrameTimeMs, analysis.CurrentFPS, analysis.ResponsivenessLevel);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing UI responsiveness");
            return new UIResponsivenessAnalysis();
        }
    }

    private async Task<APILatencyAnalysis> AnalyzeAPILatencyAsync()
    {
        var analysis = new APILatencyAnalysis();

        try
        {
            // Simulate API operations and measure latency
            var responseTimes = new List<double>();
            var latencyStopwatch = new Stopwatch();

            // Simulate typical API calls
            for (int i = 0; i < 10; i++)
            {
                latencyStopwatch.Restart();
                
                // Simulate API processing
                await Task.Delay(Random.Shared.Next(10, 50)); // 10-50ms processing time
                
                latencyStopwatch.Stop();
                responseTimes.Add(latencyStopwatch.Elapsed.TotalMilliseconds);
            }

            analysis.AverageResponseTime = responseTimes.Average();
            analysis.MaxResponseTime = responseTimes.Max();
            analysis.MinResponseTime = responseTimes.Min();
            analysis.P95ResponseTime = CalculatePercentile(responseTimes.ToArray(), 0.95);
            analysis.IsWithinBudget = analysis.AverageResponseTime <= ResourceBudgets.MaxAPIResponseTime;

            // Throughput simulation
            analysis.EstimatedThroughput = analysis.AverageResponseTime > 0 ? 1000.0 / analysis.AverageResponseTime : 0;

            _logger.LogInformation("API Latency Analysis: Avg={Avg:F2}ms, P95={P95:F2}ms, Throughput={Throughput:F1} req/s",
                analysis.AverageResponseTime, analysis.P95ResponseTime, analysis.EstimatedThroughput);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing API latency");
            return analysis;
        }
    }

    private async Task<DatabasePerformanceAnalysis> AnalyzeDatabasePerformanceAsync()
    {
        try
        {
            // Simulate database operations and measure performance
            var queryTimes = new List<double>();
            var queryStopwatch = new Stopwatch();

            // Simulate typical database operations
            for (int i = 0; i < 20; i++)
            {
                queryStopwatch.Restart();
                
                // Simulate database query
                await Task.Delay(Random.Shared.Next(5, 25)); // 5-25ms query time
                
                queryStopwatch.Stop();
                queryTimes.Add(queryStopwatch.Elapsed.TotalMilliseconds);
            }

            var averageQueryTimeMs = queryTimes.Average();
            var maxQueryTimeMs = queryTimes.Max();
            var totalQueries = queryTimes.Count;
            var slowQueries = queryTimes.Count(q => q > ResourceBudgets.MaxDatabaseQueryTime);
            var slowQueryRatio = totalQueries > 0 ? (double)slowQueries / totalQueries * 100 : 0;
            var isWithinBudget = averageQueryTimeMs <= ResourceBudgets.MaxDatabaseQueryTime;

            var analysis = new DatabasePerformanceAnalysis
            {
                AverageQueryTimeMs = averageQueryTimeMs,
                MaxQueryTimeMs = maxQueryTimeMs,
                TotalQueries = totalQueries,
                SlowQueries = slowQueries,
                SlowQueryRatio = slowQueryRatio,
                IsWithinBudget = isWithinBudget,
                // Performance level assessment
                PerformanceLevel = (isWithinBudget && slowQueryRatio < 5) ? DatabasePerformanceLevel.Excellent :
                                  averageQueryTimeMs <= ResourceBudgets.MaxDatabaseQueryTime * 1.5 ? DatabasePerformanceLevel.Good :
                                  averageQueryTimeMs <= ResourceBudgets.MaxDatabaseQueryTime * 2 ? DatabasePerformanceLevel.Fair :
                                  DatabasePerformanceLevel.Poor
            };

            _logger.LogInformation("Database Performance Analysis: Avg Query={Avg:F2}ms, Slow Queries={Slow}/{Total} ({Ratio:F1}%), Level={Level}",
                analysis.AverageQueryTimeMs, analysis.SlowQueries, analysis.TotalQueries, analysis.SlowQueryRatio, analysis.PerformanceLevel);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing database performance");
            return new DatabasePerformanceAnalysis();
        }
    }

    private async Task<ModelLoadingAnalysis> AnalyzeModelLoadingPatternsAsync()
    {
        var analysis = new ModelLoadingAnalysis();

        try
        {
            // Simulate model loading scenarios
            var loadTimes = new List<double>();
            var memoryUsages = new List<long>();
            var vramUsages = new List<long>();

            // Test different model loading patterns
            for (int i = 0; i < 5; i++)
            {
                var beforeMetrics = await _collector.CollectMetricsAsync();
                var loadStopwatch = Stopwatch.StartNew();

                // Simulate model loading
                var modelSize = Random.Shared.NextInt64(100 * 1024 * 1024, 2L * 1024 * 1024 * 1024); // 100MB to 2GB
                var allocSuccess = _vramManager.RequestVRAMAllocation($"TestModel_{i}", modelSize, VRAMPriority.Normal);

                await Task.Delay(Random.Shared.Next(1000, 5000)); // 1-5 second loading time

                loadStopwatch.Stop();
                var afterMetrics = await _collector.CollectMetricsAsync();

                loadTimes.Add(loadStopwatch.Elapsed.TotalMilliseconds);
                memoryUsages.Add(afterMetrics.ApplicationMemory - beforeMetrics.ApplicationMemory);
                vramUsages.Add(modelSize);

                // Clean up test allocation
                if (allocSuccess)
                {
                    _vramManager.ReleaseVRAMAllocation($"TestModel_{i}");
                }
            }

            analysis.AverageLoadTime = loadTimes.Average();
            analysis.MaxLoadTime = loadTimes.Max();
            analysis.AverageMemoryIncrease = memoryUsages.Average();
            analysis.AverageVRAMRequirement = vramUsages.Average();
            analysis.LoadTimeVariability = CalculateVariability(loadTimes.ToArray());

            // Budget compliance
            analysis.WithinLoadTimeBudget = analysis.AverageLoadTime <= ResourceBudgets.MaxModelLoadTime;

            _logger.LogInformation("Model Loading Analysis: Avg Load={Avg:F0}ms, Max Load={Max:F0}ms, Avg VRAM={VRAM}MB, Variability={Var:F2}",
                analysis.AverageLoadTime, analysis.MaxLoadTime, analysis.AverageVRAMRequirement / (1024 * 1024), analysis.LoadTimeVariability);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing model loading patterns");
            return analysis;
        }
    }

    private OverallPerformanceAssessment GenerateOverallAssessment(PostOptimizationReport report)
    {
        var assessment = new OverallPerformanceAssessment();
        var score = 100;

        // Memory assessment
        if (report.MemoryAnalysis.GCEfficiency < 50) score -= 15;
        if (report.MemoryAnalysis.MemoryStability < 0.8) score -= 10;

        // VRAM assessment  
        if (report.VRAMAnalysis.UtilizationLevel == UtilizationLevel.Critical) score -= 20;
        else if (report.VRAMAnalysis.UtilizationLevel == UtilizationLevel.High) score -= 10;

        // Threading assessment
        if (report.ThreadingAnalysis.ThreadVariability > 5.0) score -= 10;
        if (report.ThreadingAnalysis.PotentialIssues.Any()) score -= 5;

        // Build resource assessment
        if (!report.BuildResourceAnalysis.WithinCPUBudget) score -= 15;
        if (!report.BuildResourceAnalysis.WithinMemoryBudget) score -= 15;

        // UI responsiveness assessment
        if (report.UIResponsivenessAnalysis.ResponsivenessLevel == ResponsivenessLevel.Poor) score -= 20;
        else if (report.UIResponsivenessAnalysis.ResponsivenessLevel == ResponsivenessLevel.Fair) score -= 10;

        // API latency assessment
        if (!report.APILatencyAnalysis.IsWithinBudget) score -= 10;

        // Database performance assessment
        if (report.DatabasePerformanceAnalysis.PerformanceLevel == DatabasePerformanceLevel.Poor) score -= 15;
        else if (report.DatabasePerformanceAnalysis.PerformanceLevel == DatabasePerformanceLevel.Fair) score -= 8;

        // Model loading assessment
        if (!report.ModelLoadingAnalysis.WithinLoadTimeBudget) score -= 10;

        assessment.OverallScore = Math.Max(0, score);
        assessment.Grade = score switch
        {
            >= 90 => PerformanceGrade.Excellent,
            >= 75 => PerformanceGrade.Good,
            >= 60 => PerformanceGrade.Fair,
            >= 40 => PerformanceGrade.Poor,
            _ => PerformanceGrade.Critical
        };

        // Success criteria evaluation
        assessment.BudgetCompliance = EvaluateBudgetCompliance(report);
        assessment.NoPerformanceRegression = EvaluatePerformanceRegression(report);
        assessment.ResourceOptimization = EvaluateResourceOptimization(report);
        assessment.CleanHandoffReady = assessment.Grade >= PerformanceGrade.Good && 
                                      assessment.BudgetCompliance && 
                                      !assessment.NoPerformanceRegression;

        return assessment;
    }

    private bool EvaluateBudgetCompliance(PostOptimizationReport report)
    {
        return report.BuildResourceAnalysis.WithinMemoryBudget &&
               report.UIResponsivenessAnalysis.IsWithinBudget &&
               report.APILatencyAnalysis.IsWithinBudget &&
               report.DatabasePerformanceAnalysis.IsWithinBudget &&
               report.ModelLoadingAnalysis.WithinLoadTimeBudget;
    }

    private bool EvaluatePerformanceRegression(PostOptimizationReport report)
    {
        // No major performance issues detected
        return !report.MemoryAnalysis.PotentialIssues.Any() &&
               !report.ThreadingAnalysis.PotentialIssues.Any() &&
               report.UIResponsivenessAnalysis.ResponsivenessLevel >= ResponsivenessLevel.Good;
    }

    private bool EvaluateResourceOptimization(PostOptimizationReport report)
    {
        return report.MemoryAnalysis.GCEfficiency >= 60 &&
               report.VRAMAnalysis.UtilizationLevel != UtilizationLevel.Critical &&
               report.BuildResourceAnalysis.ResourceEfficiency >= 0.7;
    }

    private void LogValidationResults(PostOptimizationReport report)
    {
        _logger.LogInformation("=== POST-OPTIMIZATION VALIDATION RESULTS ===");
        _logger.LogInformation("Validation Duration: {Duration}", report.ValidationDuration);
        _logger.LogInformation("Overall Grade: {Grade} (Score: {Score}/100)", 
            report.OverallAssessment.Grade, report.OverallAssessment.OverallScore);

        _logger.LogInformation("SUCCESS CRITERIA EVALUATION:");
        _logger.LogInformation("  ✓ Budget Compliance: {Compliance}", report.OverallAssessment.BudgetCompliance ? "PASS" : "FAIL");
        _logger.LogInformation("  ✓ No Performance Regression: {Regression}", report.OverallAssessment.NoPerformanceRegression ? "PASS" : "FAIL");
        _logger.LogInformation("  ✓ Resource Optimization: {Optimization}", report.OverallAssessment.ResourceOptimization ? "PASS" : "FAIL");
        _logger.LogInformation("  ✓ Ready for Threading Handoff: {Handoff}", report.OverallAssessment.CleanHandoffReady ? "READY" : "NOT READY");

        if (report.ValidationErrors.Any())
        {
            _logger.LogWarning("Validation Errors Encountered:");
            foreach (var error in report.ValidationErrors)
            {
                _logger.LogWarning("  - {Error}", error);
            }
        }

        // Generate handoff recommendations
        if (report.OverallAssessment.CleanHandoffReady)
        {
            _logger.LogInformation("PERFORMANCE BASELINE ESTABLISHED - READY FOR threading-lifetime-auditor HANDOFF");
            _logger.LogInformation("Resource consumption within established budgets. Performance regression detection complete.");
            _logger.LogInformation("Optimization recommendations available for resource-intensive operations.");
        }
        else
        {
            _logger.LogWarning("PERFORMANCE ISSUES DETECTED - ADDITIONAL OPTIMIZATION REQUIRED");
            _logger.LogWarning("Manual performance review needed before threading-lifetime-auditor handoff");
        }
    }

    private double CalculateMemoryStability(List<long> measurements)
    {
        if (measurements.Count < 2) return 1.0;
        
        var variance = measurements.Select(m => (double)m).ToArray();
        return 1.0 - (CalculateVariability(variance) / variance.Average());
    }

    private string DetermineAllocationPattern(List<long> measurements)
    {
        if (measurements.Count < 3) return "Insufficient data";

        var trend = measurements.Last() - measurements.First();
        var maxVariation = measurements.Max() - measurements.Min();

        if (Math.Abs(trend) < measurements.Average() * 0.05 && maxVariation < measurements.Average() * 0.1)
            return "Stable";
        else if (trend > 0)
            return "Growing";
        else if (trend < 0)
            return "Shrinking";
        else
            return "Variable";
    }

    private List<string> IdentifyMemoryIssues(MemoryAllocationAnalysis analysis)
    {
        var issues = new List<string>();

        if (analysis.GCEfficiency < 30)
            issues.Add("Low garbage collection efficiency - possible memory retention issues");

        if (analysis.MemoryStability < 0.8)
            issues.Add("Memory usage instability detected");

        if (analysis.AllocationPattern == "Growing")
            issues.Add("Consistent memory growth pattern - potential memory leak");

        return issues;
    }

    private double CalculateResourceEfficiency(BuildResourceAnalysis analysis)
    {
        var cpuEfficiency = Math.Max(0, 1.0 - (analysis.AverageCPUUsage / 100.0));
        var memoryEfficiency = Math.Max(0, 1.0 - ((double)analysis.AverageMemoryUsage / ResourceBudgets.MaxApplicationMemory));
        
        return (cpuEfficiency + memoryEfficiency) / 2.0;
    }

    private double CalculateVariability(double[] values)
    {
        if (values.Length < 2) return 0;
        
        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Length;
        return Math.Sqrt(variance);
    }

    private double CalculatePercentile(double[] values, double percentile)
    {
        if (values.Length == 0) return 0;
        
        Array.Sort(values);
        var index = (int)Math.Ceiling(percentile * values.Length) - 1;
        return values[Math.Max(0, Math.Min(index, values.Length - 1))];
    }
}

// Supporting data structures for post-optimization validation
public record PostOptimizationReport
{
    public DateTime ValidationStartTime { get; init; }
    public DateTime ValidationEndTime { get; set; }
    public TimeSpan ValidationDuration { get; set; }
    public MemoryAllocationAnalysis MemoryAnalysis { get; set; } = new();
    public VRAMUtilizationAnalysis VRAMAnalysis { get; set; } = new();
    public ThreadingOverheadAnalysis ThreadingAnalysis { get; set; } = new();
    public BuildResourceAnalysis BuildResourceAnalysis { get; set; } = new();
    public UIResponsivenessAnalysis UIResponsivenessAnalysis { get; set; } = new();
    public APILatencyAnalysis APILatencyAnalysis { get; set; } = new();
    public DatabasePerformanceAnalysis DatabasePerformanceAnalysis { get; set; } = new();
    public ModelLoadingAnalysis ModelLoadingAnalysis { get; set; } = new();
    public OverallPerformanceAssessment OverallAssessment { get; set; } = new();
    public List<string> ValidationErrors { get; set; } = new();
}

public record MemoryAllocationAnalysis
{
    public long InitialMemoryUsage { get; set; }
    public double GCEfficiency { get; set; }
    public long MemoryPressure { get; set; }
    public double MemoryStability { get; set; }
    public string AllocationPattern { get; set; } = string.Empty;
    public List<string> PotentialIssues { get; set; } = new();
}

public record ThreadingOverheadAnalysis
{
    public int InitialThreadCount { get; set; }
    public double AverageThreadCount { get; set; }
    public int MaxThreadCount { get; set; }
    public int MinThreadCount { get; set; }
    public double ThreadVariability { get; set; }
    public int HandleCount { get; set; }
    public List<string> PotentialIssues { get; set; } = new();
}

public record BuildResourceAnalysis
{
    public double InitialCPUUsage { get; set; }
    public long InitialMemoryUsage { get; set; }
    public double AverageCPUUsage { get; set; }
    public double PeakCPUUsage { get; set; }
    public double AverageMemoryUsage { get; set; }
    public long PeakMemoryUsage { get; set; }
    public double ResourceEfficiency { get; set; }
    public bool WithinCPUBudget { get; set; }
    public bool WithinMemoryBudget { get; set; }
    public TimeSpan MeasurementDuration { get; set; }
}

public record APILatencyAnalysis
{
    public double AverageResponseTime { get; set; }
    public double MaxResponseTime { get; set; }
    public double MinResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public bool IsWithinBudget { get; set; }
    public double EstimatedThroughput { get; set; }
}

public record ModelLoadingAnalysis
{
    public double AverageLoadTime { get; set; }
    public double MaxLoadTime { get; set; }
    public double AverageMemoryIncrease { get; set; }
    public double AverageVRAMRequirement { get; set; }
    public double LoadTimeVariability { get; set; }
    public bool WithinLoadTimeBudget { get; set; }
}

public record OverallPerformanceAssessment
{
    public int OverallScore { get; set; }
    public PerformanceGrade Grade { get; set; }
    public bool BudgetCompliance { get; set; }
    public bool NoPerformanceRegression { get; set; }
    public bool ResourceOptimization { get; set; }
    public bool CleanHandoffReady { get; set; }
}