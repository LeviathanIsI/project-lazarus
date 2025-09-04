using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace Lazarus.App.Shared.Performance;

/// <summary>
/// Performance Analysis Service - Generates comprehensive resource consumption analysis and optimization recommendations
/// </summary>
public class PerformanceAnalysisService : IDisposable
{
    private readonly ILogger<PerformanceAnalysisService> _logger;
    private readonly PerformanceBudgeter _budgeter;
    private readonly Queue<PerformanceSnapshot> _performanceHistory = new();
    private readonly object _historyLock = new();
    private bool _disposed = false;

    public PerformanceAnalysisService(
        ILogger<PerformanceAnalysisService> logger,
        PerformanceBudgeter budgeter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _budgeter = budgeter ?? throw new ArgumentNullException(nameof(budgeter));
        
        // Subscribe to budget violations for real-time analysis
        _budgeter.BudgetViolation += OnBudgetViolation;
        _budgeter.OptimizationRecommendation += OnOptimizationRecommendation;
    }

    /// <summary>
    /// Generate comprehensive resource consumption analysis
    /// </summary>
    public async Task<ResourceConsumptionAnalysis> AnalyzeResourceConsumptionAsync(TimeSpan analysisWindow)
    {
        _logger.LogInformation("Starting comprehensive resource consumption analysis over {Window}", analysisWindow);

        try
        {
            // Generate current performance report
            var currentReport = await _budgeter.GeneratePerformanceReportAsync();
            
            // Analyze historical trends
            var historicalData = GetPerformanceHistory(analysisWindow);
            var trendAnalysis = AnalyzeTrends(historicalData);
            
            // Memory allocation pattern analysis
            var memoryPatterns = AnalyzeMemoryPatterns(historicalData, currentReport);
            
            // VRAM utilization assessment
            var vramAnalysis = AnalyzeVRAMUtilization(currentReport.VRAMAllocationStats);
            
            // UI responsiveness analysis
            var uiAnalysis = AnalyzeUIResponsiveness(currentReport.UIPerformanceMetrics, historicalData);
            
            // Database query performance analysis
            var queryAnalysis = AnalyzeDatabasePerformance(currentReport.DatabasePerformanceMetrics);
            
            // Generate optimization recommendations
            var optimizations = await GenerateOptimizationRecommendationsAsync(currentReport, trendAnalysis);

            var analysis = new ResourceConsumptionAnalysis
            {
                AnalysisWindow = analysisWindow,
                GeneratedAt = DateTime.UtcNow,
                CurrentPerformanceReport = currentReport,
                TrendAnalysis = trendAnalysis,
                MemoryPatterns = memoryPatterns,
                VRAMAnalysis = vramAnalysis,
                UIAnalysis = uiAnalysis,
                DatabaseAnalysis = queryAnalysis,
                OptimizationRecommendations = optimizations,
                OverallHealthScore = CalculateOverallHealthScore(currentReport, trendAnalysis),
                RiskAssessment = AssessPerformanceRisks(currentReport, trendAnalysis)
            };

            _logger.LogInformation("Resource consumption analysis completed - Health Score: {Score}/100, Risk Level: {Risk}",
                analysis.OverallHealthScore, analysis.RiskAssessment.OverallRisk);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource consumption analysis");
            throw;
        }
    }

    /// <summary>
    /// Perform memory leak detection analysis
    /// </summary>
    public Task<MemoryLeakAnalysis> DetectMemoryLeaksAsync(TimeSpan monitoringPeriod)
    {
        _logger.LogInformation("Starting memory leak detection over {Period}", monitoringPeriod);

        var analysis = new MemoryLeakAnalysis
        {
            MonitoringPeriod = monitoringPeriod,
            AnalysisStartTime = DateTime.UtcNow
        };

        try
        {
            var historicalData = GetPerformanceHistory(monitoringPeriod);
            
            if (historicalData.Length < 10) // Need at least 10 data points
            {
                analysis.Confidence = ConfidenceLevel.Low;
                analysis.Conclusion = "Insufficient historical data for reliable leak detection";
                return Task.FromResult(analysis);
            }

            // Analyze memory growth patterns
            var memoryValues = historicalData.Select(h => (double)h.ApplicationMemory).ToArray();
            var timeValues = historicalData.Select(h => (h.Timestamp - historicalData[0].Timestamp).TotalMinutes).ToArray();
            
            // Linear regression to detect consistent growth
            var (slope, correlation) = CalculateLinearRegression(timeValues, memoryValues);
            
            // Calculate growth rate in MB per hour
            var growthRateMBPerHour = slope * 60 / (1024 * 1024);
            
            analysis.MemoryGrowthRate = growthRateMBPerHour;
            analysis.CorrelationCoefficient = correlation;
            
            // Assess leak likelihood
            if (Math.Abs(correlation) > 0.8 && growthRateMBPerHour > 10) // 10MB/hour consistent growth
            {
                analysis.LeakLikelihood = LeakLikelihood.High;
                analysis.Confidence = ConfidenceLevel.High;
                analysis.Conclusion = $"Strong indication of memory leak: {growthRateMBPerHour:F1}MB/hour consistent growth";
            }
            else if (Math.Abs(correlation) > 0.6 && growthRateMBPerHour > 5) // 5MB/hour growth
            {
                analysis.LeakLikelihood = LeakLikelihood.Moderate;
                analysis.Confidence = ConfidenceLevel.Medium;
                analysis.Conclusion = $"Possible memory leak: {growthRateMBPerHour:F1}MB/hour growth pattern";
            }
            else
            {
                analysis.LeakLikelihood = LeakLikelihood.Low;
                analysis.Confidence = ConfidenceLevel.High;
                analysis.Conclusion = "No significant memory leak detected";
            }

            // Analyze GC effectiveness
            var gcData = historicalData.Where(h => h.GCPressure > 0).ToArray();
            if (gcData.Any())
            {
                var avgGCPressure = gcData.Average(h => (double)h.GCPressure);
                var gcTrend = gcData.Length > 1 ? CalculateGCTrend(gcData) : 0;
                
                analysis.GCEffectivenessScore = CalculateGCEffectivenessScore(avgGCPressure, gcTrend);
            }

            // Generate specific recommendations
            analysis.Recommendations = GenerateMemoryLeakRecommendations(analysis);

            _logger.LogInformation("Memory leak analysis completed - Likelihood: {Likelihood}, Growth Rate: {Rate:F1}MB/h",
                analysis.LeakLikelihood, analysis.MemoryGrowthRate);

            return Task.FromResult(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory leak detection");
            analysis.Conclusion = $"Error during analysis: {ex.Message}";
            analysis.Confidence = ConfidenceLevel.Low;
            return Task.FromResult(analysis);
        }
    }

    /// <summary>
    /// Export comprehensive performance report
    /// </summary>
    public Task<string> ExportPerformanceReportAsync(ResourceConsumptionAnalysis analysis, ExportFormat format = ExportFormat.Json)
    {
        try
        {
            var result = format switch
            {
                ExportFormat.Json => JsonSerializer.Serialize(analysis, new JsonSerializerOptions { WriteIndented = true }),
                ExportFormat.Csv => ExportToCsv(analysis),
                ExportFormat.Text => ExportToText(analysis),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting performance report");
            throw;
        }
    }

    private PerformanceSnapshot[] GetPerformanceHistory(TimeSpan window)
    {
        lock (_historyLock)
        {
            var cutoff = DateTime.UtcNow - window;
            return _performanceHistory
                .Where(s => s.Timestamp >= cutoff)
                .OrderBy(s => s.Timestamp)
                .ToArray();
        }
    }

    private TrendAnalysisResult AnalyzeTrends(PerformanceSnapshot[] history)
    {
        if (history.Length < 2)
        {
            return new TrendAnalysisResult
            {
                DataPoints = history.Length,
                Reliability = TrendReliability.Insufficient
            };
        }

        var memoryTrend = CalculateTrendDirection(history.Select(h => (double)h.ApplicationMemory).ToArray());
        var cpuTrend = CalculateTrendDirection(history.Select(h => h.CpuUsage).ToArray());
        var frameTrend = CalculateTrendDirection(history.Select(h => h.AverageFrameTime).ToArray());

        return new TrendAnalysisResult
        {
            DataPoints = history.Length,
            MemoryTrend = memoryTrend,
            CpuTrend = cpuTrend,
            FrameTimeTrend = frameTrend,
            Reliability = history.Length > 50 ? TrendReliability.High : 
                         history.Length > 20 ? TrendReliability.Medium : TrendReliability.Low
        };
    }

    private MemoryPatternAnalysis AnalyzeMemoryPatterns(PerformanceSnapshot[] history, PerformanceReport current)
    {
        var currentMemory = current.SystemMetrics.ApplicationMemory;
        var vramUsage = current.VRAMAllocationStats.AllocatedVRAM;
        var gcPressure = current.SystemMetrics.GCPressure;

        var patterns = new List<string>();

        // Check for memory growth patterns
        if (history.Length > 10)
        {
            var recentMemory = history.TakeLast(5).Average(h => (double)h.ApplicationMemory);
            var earlierMemory = history.Take(5).Average(h => (double)h.ApplicationMemory);
            
            if (recentMemory > earlierMemory * 1.1) // 10% growth
            {
                patterns.Add("Consistent memory growth detected");
            }
        }

        // Check for high GC pressure
        if (gcPressure > 500 * 1024 * 1024) // 500MB
        {
            patterns.Add("High garbage collection pressure");
        }

        // Check VRAM to RAM ratio
        var vramToRamRatio = vramUsage > 0 && currentMemory > 0 ? (double)vramUsage / currentMemory : 0;
        if (vramToRamRatio > 2.0)
        {
            patterns.Add("VRAM usage significantly higher than system memory");
        }

        return new MemoryPatternAnalysis
        {
            CurrentMemoryMB = currentMemory / (1024 * 1024),
            GCPressureMB = gcPressure / (1024 * 1024),
            VRAMToRAMRatio = vramToRamRatio,
            IdentifiedPatterns = patterns
        };
    }

    private VRAMUtilizationAnalysis AnalyzeVRAMUtilization(VRAMAllocationStats vramStats)
    {
        var utilizationPercent = vramStats.TotalVRAM > 0 ? (double)vramStats.AllocatedVRAM / vramStats.TotalVRAM * 100 : 0;
        
        // Determine utilization level and recommendations
        var utilizationLevel = utilizationPercent > 90 ? UtilizationLevel.Critical :
                              utilizationPercent > 75 ? UtilizationLevel.High :
                              utilizationPercent > 50 ? UtilizationLevel.Moderate :
                              UtilizationLevel.Low;

        var recommendations = new List<string>();
        if (utilizationPercent > 90)
        {
            recommendations.Add("VRAM usage critical - consider model optimization or hardware upgrade");
        }
        else if (utilizationPercent > 75)
        {
            recommendations.Add("High VRAM usage - monitor for potential allocation failures");
        }
        else if (utilizationPercent > 50)
        {
            recommendations.Add("Moderate VRAM usage - room for additional models");
        }
        else
        {
            recommendations.Add("Low VRAM usage - capacity available for larger models");
        }

        return new VRAMUtilizationAnalysis
        {
            TotalVRAMGB = vramStats.TotalVRAM / (1024.0 * 1024 * 1024),
            AllocatedVRAMGB = vramStats.AllocatedVRAM / (1024.0 * 1024 * 1024),
            UtilizationPercent = utilizationPercent,
            AllocationCount = vramStats.AllocationCount,
            LargestAllocationMB = vramStats.LargestAllocation / (1024 * 1024),
            UtilizationLevel = utilizationLevel,
            Recommendations = recommendations
        };
    }

    private UIResponsivenessAnalysis AnalyzeUIResponsiveness(UIPerformanceMetrics uiMetrics, PerformanceSnapshot[] history)
    {
        // Assess responsiveness level
        var responsivenessLevel = uiMetrics.AverageFrameTime <= ResourceBudgets.MaxFrameTime ? ResponsivenessLevel.Excellent :
                                 uiMetrics.AverageFrameTime <= ResourceBudgets.MaxFrameTime * 1.5 ? ResponsivenessLevel.Good :
                                 uiMetrics.AverageFrameTime <= ResourceBudgets.MaxFrameTime * 2 ? ResponsivenessLevel.Fair :
                                 ResponsivenessLevel.Poor;

        // Generate recommendations
        var recommendations = new List<string>();
        if (!uiMetrics.IsWithinBudget)
        {
            recommendations.Add("UI frame time exceeds budget - optimize rendering or reduce visual complexity");
        }

        if (uiMetrics.FrameTimeVariance > 100) // High variance
        {
            recommendations.Add("High frame time variance - investigate inconsistent performance");
        }

        return new UIResponsivenessAnalysis
        {
            AverageFrameTimeMs = uiMetrics.AverageFrameTime,
            MaxFrameTimeMs = uiMetrics.MaxFrameTime,
            CurrentFPS = uiMetrics.AverageFrameTime > 0 ? 1000.0 / uiMetrics.AverageFrameTime : 0,
            IsWithinBudget = uiMetrics.IsWithinBudget,
            ResponsivenessLevel = responsivenessLevel,
            Recommendations = recommendations
        };
    }

    private DatabasePerformanceAnalysis AnalyzeDatabasePerformance(DatabasePerformanceMetrics dbMetrics)
    {
        var slowQueryRatio = dbMetrics.QueriesExecuted > 0 ? (double)dbMetrics.SlowQueries / dbMetrics.QueriesExecuted * 100 : 0;

        // Assess database performance
        var performanceLevel = (dbMetrics.IsWithinBudget && slowQueryRatio < 5) ? DatabasePerformanceLevel.Excellent :
                              dbMetrics.AverageQueryTime <= ResourceBudgets.MaxDatabaseQueryTime * 1.5 ? DatabasePerformanceLevel.Good :
                              dbMetrics.AverageQueryTime <= ResourceBudgets.MaxDatabaseQueryTime * 2 ? DatabasePerformanceLevel.Fair :
                              DatabasePerformanceLevel.Poor;

        // Generate recommendations
        var recommendations = new List<string>();
        if (slowQueryRatio > 10)
        {
            recommendations.Add("High percentage of slow queries - review query optimization and indexing");
        }

        if (dbMetrics.MaxQueryTime > ResourceBudgets.MaxDatabaseQueryTime * 5)
        {
            recommendations.Add("Very slow queries detected - investigate query plans and database design");
        }

        return new DatabasePerformanceAnalysis
        {
            AverageQueryTimeMs = dbMetrics.AverageQueryTime,
            MaxQueryTimeMs = dbMetrics.MaxQueryTime,
            TotalQueries = dbMetrics.QueriesExecuted,
            SlowQueries = dbMetrics.SlowQueries,
            SlowQueryRatio = slowQueryRatio,
            PerformanceLevel = performanceLevel,
            Recommendations = recommendations,
            IsWithinBudget = dbMetrics.IsWithinBudget
        };
    }

    private Task<List<OptimizationRecommendation>> GenerateOptimizationRecommendationsAsync(
        PerformanceReport report, TrendAnalysisResult trends)
    {
        var recommendations = new List<OptimizationRecommendation>();

        // Memory optimization recommendations
        if (report.SystemMetrics.ApplicationMemory > ResourceBudgets.MaxApplicationMemory * 0.8)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = OptimizationCategory.Memory,
                Priority = OptimizationPriority.High,
                Title = "High memory usage detected",
                Description = "Application memory usage approaching budget limits",
                Actions = new[]
                {
                    "Implement object pooling for frequently allocated objects",
                    "Review cache sizes and implement LRU eviction",
                    "Consider using memory-mapped files for large datasets",
                    "Profile memory allocation patterns to identify hotspots"
                },
                ExpectedImpact = "20-40% reduction in memory usage"
            });
        }

        // VRAM optimization recommendations
        if (report.VRAMAllocationStats.UsagePercent > 75)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = OptimizationCategory.VRAM,
                Priority = OptimizationPriority.High,
                Title = "High VRAM utilization",
                Description = "GPU memory usage may limit model loading capabilities",
                Actions = new[]
                {
                    "Consider model quantization to reduce VRAM footprint",
                    "Implement model swapping for inactive models",
                    "Use gradient checkpointing during training",
                    "Optimize batch sizes for inference"
                },
                ExpectedImpact = "30-50% reduction in VRAM usage"
            });
        }

        // UI performance recommendations
        if (!report.UIPerformanceMetrics.IsWithinBudget)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = OptimizationCategory.UI,
                Priority = OptimizationPriority.Medium,
                Title = "UI responsiveness issues",
                Description = "Frame rendering times exceed budget constraints",
                Actions = new[]
                {
                    "Implement UI virtualization for large data sets",
                    "Optimize data binding and reduce unnecessary updates",
                    "Use background threads for heavy computations",
                    "Consider reducing visual effects complexity"
                },
                ExpectedImpact = "40-60% improvement in frame times"
            });
        }

        // Database performance recommendations
        if (!report.DatabasePerformanceMetrics.IsWithinBudget)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = OptimizationCategory.Database,
                Priority = OptimizationPriority.Medium,
                Title = "Database query performance",
                Description = "Query execution times exceed performance budgets",
                Actions = new[]
                {
                    "Add appropriate database indices for frequently queried columns",
                    "Implement query result caching",
                    "Consider database query optimization",
                    "Use connection pooling to reduce overhead"
                },
                ExpectedImpact = "50-70% improvement in query times"
            });
        }

        // Trend-based recommendations
        if (trends.MemoryTrend == TrendDirection.Increasing)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Category = OptimizationCategory.Memory,
                Priority = OptimizationPriority.Medium,
                Title = "Memory growth trend detected",
                Description = "Consistent memory usage increase may indicate potential leaks",
                Actions = new[]
                {
                    "Run detailed memory profiling to identify leak sources",
                    "Review object lifetime management",
                    "Implement more aggressive garbage collection",
                    "Monitor for unregistered event handlers"
                },
                ExpectedImpact = "Stabilize memory usage growth"
            });
        }

        return Task.FromResult(recommendations);
    }

    private int CalculateOverallHealthScore(PerformanceReport report, TrendAnalysisResult trends)
    {
        var score = 100;

        // Budget violations impact
        score -= report.BudgetCompliance.Violations.Count(v => v.Severity == ViolationSeverity.Critical) * 20;
        score -= report.BudgetCompliance.Violations.Count(v => v.Severity == ViolationSeverity.Warning) * 10;

        // Resource usage impact
        var memoryUsagePercent = (double)report.SystemMetrics.ApplicationMemory / ResourceBudgets.MaxApplicationMemory * 100;
        if (memoryUsagePercent > 90) score -= 15;
        else if (memoryUsagePercent > 75) score -= 10;
        else if (memoryUsagePercent > 60) score -= 5;

        var vramUsagePercent = report.VRAMAllocationStats.UsagePercent;
        if (vramUsagePercent > 90) score -= 15;
        else if (vramUsagePercent > 75) score -= 10;
        else if (vramUsagePercent > 60) score -= 5;

        // Performance metrics impact
        if (!report.UIPerformanceMetrics.IsWithinBudget) score -= 10;
        if (!report.DatabasePerformanceMetrics.IsWithinBudget) score -= 10;

        // Trend impact
        if (trends.MemoryTrend == TrendDirection.Increasing) score -= 5;
        if (trends.CpuTrend == TrendDirection.Increasing) score -= 5;

        return Math.Max(0, Math.Min(100, score));
    }

    private PerformanceRiskAssessment AssessPerformanceRisks(PerformanceReport report, TrendAnalysisResult trends)
    {
        var risks = new List<PerformanceRisk>();

        // Memory risks
        var memoryUsagePercent = (double)report.SystemMetrics.ApplicationMemory / ResourceBudgets.MaxApplicationMemory * 100;
        if (memoryUsagePercent > 85)
        {
            risks.Add(new PerformanceRisk
            {
                Type = RiskType.Memory,
                Severity = memoryUsagePercent > 95 ? RiskSeverity.High : RiskSeverity.Medium,
                Description = $"Memory usage at {memoryUsagePercent:F1}% of budget",
                Probability = 0.8,
                Impact = "Application may run out of memory, causing crashes or severe performance degradation"
            });
        }

        // VRAM risks
        if (report.VRAMAllocationStats.UsagePercent > 85)
        {
            risks.Add(new PerformanceRisk
            {
                Type = RiskType.VRAM,
                Severity = report.VRAMAllocationStats.UsagePercent > 95 ? RiskSeverity.High : RiskSeverity.Medium,
                Description = $"VRAM usage at {report.VRAMAllocationStats.UsagePercent:F1}%",
                Probability = 0.7,
                Impact = "Model loading may fail or cause GPU memory allocation errors"
            });
        }

        // Performance trend risks
        if (trends.MemoryTrend == TrendDirection.Increasing && trends.Reliability != TrendReliability.Insufficient)
        {
            risks.Add(new PerformanceRisk
            {
                Type = RiskType.Trend,
                Severity = RiskSeverity.Medium,
                Description = "Consistent memory growth trend detected",
                Probability = 0.6,
                Impact = "Progressive memory exhaustion may lead to long-term stability issues"
            });
        }

        var overallRisk = risks.Any() ? risks.Max(r => r.Severity) : RiskSeverity.Low;

        return new PerformanceRiskAssessment
        {
            OverallRisk = overallRisk,
            IdentifiedRisks = risks,
            RiskScore = CalculateRiskScore(risks),
            AssessmentTime = DateTime.UtcNow
        };
    }

    private (double slope, double correlation) CalculateLinearRegression(double[] x, double[] y)
    {
        var n = x.Length;
        if (n != y.Length || n < 2) return (0, 0);

        var meanX = x.Average();
        var meanY = y.Average();

        var numerator = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
        var denominatorX = x.Sum(xi => Math.Pow(xi - meanX, 2));
        var denominatorY = y.Sum(yi => Math.Pow(yi - meanY, 2));

        if (denominatorX == 0) return (0, 0);

        var slope = numerator / denominatorX;
        var correlation = denominatorY == 0 ? 0 : numerator / Math.Sqrt(denominatorX * denominatorY);

        return (slope, correlation);
    }

    private TrendDirection CalculateTrendDirection(double[] values)
    {
        if (values.Length < 2) return TrendDirection.Stable;

        var x = Enumerable.Range(0, values.Length).Select(i => (double)i).ToArray();
        var (slope, _) = CalculateLinearRegression(x, values);

        var threshold = values.Average() * 0.01; // 1% threshold

        if (slope > threshold) return TrendDirection.Increasing;
        if (slope < -threshold) return TrendDirection.Decreasing;
        return TrendDirection.Stable;
    }

    private double CalculateGCTrend(PerformanceSnapshot[] gcData)
    {
        if (gcData.Length < 2) return 0;

        var gcPressureValues = gcData.Select(d => (double)d.GCPressure).ToArray();
        var timeValues = gcData.Select(d => (d.Timestamp - gcData[0].Timestamp).TotalMinutes).ToArray();

        var (slope, _) = CalculateLinearRegression(timeValues, gcPressureValues);
        return slope;
    }

    private double CalculateGCEffectivenessScore(double avgGCPressure, double gcTrend)
    {
        var baseScore = 100.0;

        // Penalize high average GC pressure
        if (avgGCPressure > 1024 * 1024 * 1024) // 1GB
        {
            baseScore -= 40;
        }
        else if (avgGCPressure > 512 * 1024 * 1024) // 512MB
        {
            baseScore -= 20;
        }

        // Penalize increasing GC pressure trend
        if (gcTrend > 0)
        {
            baseScore -= Math.Min(30, gcTrend / (1024 * 1024) * 10); // Scale based on MB/minute growth
        }

        return Math.Max(0, Math.Min(100, baseScore));
    }

    private List<string> GenerateMemoryLeakRecommendations(MemoryLeakAnalysis analysis)
    {
        var recommendations = new List<string>();

        if (analysis.LeakLikelihood == LeakLikelihood.High)
        {
            recommendations.Add("Use memory profiler to identify specific allocation sources");
            recommendations.Add("Review event handler registration/unregistration patterns");
            recommendations.Add("Audit disposal patterns for IDisposable objects");
            recommendations.Add("Check for circular references preventing garbage collection");
        }
        else if (analysis.LeakLikelihood == LeakLikelihood.Moderate)
        {
            recommendations.Add("Monitor memory usage for longer periods to confirm trends");
            recommendations.Add("Review cache implementations for proper size limits");
            recommendations.Add("Implement more frequent garbage collection triggers");
        }

        if (analysis.GCEffectivenessScore < 60)
        {
            recommendations.Add("Investigate objects with long lifetimes affecting GC efficiency");
            recommendations.Add("Consider generation-specific GC tuning");
        }

        return recommendations;
    }

    private int CalculateRiskScore(List<PerformanceRisk> risks)
    {
        if (!risks.Any()) return 0;

        var weightedScore = risks.Sum(r => r.Probability * (int)r.Severity * 10);
        return (int)Math.Min(100, weightedScore);
    }

    private string ExportToCsv(ResourceConsumptionAnalysis analysis)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Metric,Value,Unit");
        csv.AppendLine($"Health Score,{analysis.OverallHealthScore},Score (0-100)");
        csv.AppendLine($"Memory Usage,{analysis.CurrentPerformanceReport.SystemMetrics.ApplicationMemory / (1024 * 1024)},MB");
        csv.AppendLine($"VRAM Usage,{analysis.VRAMAnalysis.AllocatedVRAMGB:F2},GB");
        csv.AppendLine($"CPU Usage,{analysis.CurrentPerformanceReport.SystemMetrics.CpuUsage:F1},%");
        csv.AppendLine($"Average Frame Time,{analysis.UIAnalysis.AverageFrameTimeMs:F2},ms");
        csv.AppendLine($"Average Query Time,{analysis.DatabaseAnalysis.AverageQueryTimeMs:F2},ms");
        return csv.ToString();
    }

    private string ExportToText(ResourceConsumptionAnalysis analysis)
    {
        var text = new StringBuilder();
        text.AppendLine("LAZARUS PERFORMANCE ANALYSIS REPORT");
        text.AppendLine($"Generated: {analysis.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
        text.AppendLine($"Analysis Window: {analysis.AnalysisWindow}");
        text.AppendLine();
        
        text.AppendLine($"OVERALL HEALTH SCORE: {analysis.OverallHealthScore}/100");
        text.AppendLine($"RISK LEVEL: {analysis.RiskAssessment.OverallRisk}");
        text.AppendLine();
        
        text.AppendLine("RESOURCE UTILIZATION:");
        text.AppendLine($"  Memory: {analysis.CurrentPerformanceReport.SystemMetrics.ApplicationMemory / (1024 * 1024)} MB");
        text.AppendLine($"  VRAM: {analysis.VRAMAnalysis.AllocatedVRAMGB:F2} GB ({analysis.VRAMAnalysis.UtilizationPercent:F1}%)");
        text.AppendLine($"  CPU: {analysis.CurrentPerformanceReport.SystemMetrics.CpuUsage:F1}%");
        text.AppendLine();
        
        text.AppendLine("PERFORMANCE METRICS:");
        text.AppendLine($"  UI Responsiveness: {analysis.UIAnalysis.ResponsivenessLevel}");
        text.AppendLine($"  Frame Time: {analysis.UIAnalysis.AverageFrameTimeMs:F2}ms");
        text.AppendLine($"  Database Performance: {analysis.DatabaseAnalysis.PerformanceLevel}");
        text.AppendLine($"  Query Time: {analysis.DatabaseAnalysis.AverageQueryTimeMs:F2}ms");
        text.AppendLine();
        
        if (analysis.OptimizationRecommendations.Any())
        {
            text.AppendLine("OPTIMIZATION RECOMMENDATIONS:");
            foreach (var rec in analysis.OptimizationRecommendations.Take(5))
            {
                text.AppendLine($"  • {rec.Title} ({rec.Priority})");
                text.AppendLine($"    {rec.Description}");
            }
        }
        
        return text.ToString();
    }

    private void OnBudgetViolation(object? sender, BudgetViolationEvent e)
    {
        // Store violation for historical analysis
        RecordPerformanceSnapshot();
    }

    private void OnOptimizationRecommendation(object? sender, PerformanceOptimizationEvent e)
    {
        _logger.LogInformation("Performance optimization recommended: {Recommendations}", 
            string.Join("; ", e.Recommendations));
    }

    private void RecordPerformanceSnapshot()
    {
        Task.Run(async () =>
        {
            try
            {
                var report = await _budgeter.GeneratePerformanceReportAsync();
                var snapshot = new PerformanceSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    ApplicationMemory = report.SystemMetrics.ApplicationMemory,
                    CpuUsage = report.SystemMetrics.CpuUsage,
                    AverageFrameTime = report.UIPerformanceMetrics.AverageFrameTime,
                    GCPressure = report.SystemMetrics.GCPressure
                };

                lock (_historyLock)
                {
                    _performanceHistory.Enqueue(snapshot);
                    if (_performanceHistory.Count > 1000) // Keep last 1000 snapshots
                    {
                        _performanceHistory.Dequeue();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording performance snapshot");
            }
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _budgeter.BudgetViolation -= OnBudgetViolation;
            _budgeter.OptimizationRecommendation -= OnOptimizationRecommendation;
            
            lock (_historyLock)
            {
                _performanceHistory.Clear();
            }
        }
    }
}

// Supporting data structures and enums for the analysis service...
// [Previous records and enums continue here with their full definitions]

public record ResourceConsumptionAnalysis
{
    public TimeSpan AnalysisWindow { get; init; }
    public DateTime GeneratedAt { get; init; }
    public PerformanceReport CurrentPerformanceReport { get; init; } = new();
    public TrendAnalysisResult TrendAnalysis { get; init; } = new();
    public MemoryPatternAnalysis MemoryPatterns { get; init; } = new();
    public VRAMUtilizationAnalysis VRAMAnalysis { get; init; } = new();
    public UIResponsivenessAnalysis UIAnalysis { get; init; } = new();
    public DatabasePerformanceAnalysis DatabaseAnalysis { get; init; } = new();
    public List<OptimizationRecommendation> OptimizationRecommendations { get; init; } = new();
    public int OverallHealthScore { get; init; }
    public PerformanceRiskAssessment RiskAssessment { get; init; } = new();
}

public record MemoryLeakAnalysis
{
    public TimeSpan MonitoringPeriod { get; init; }
    public DateTime AnalysisStartTime { get; init; }
    public double MemoryGrowthRate { get; set; } // MB per hour
    public double CorrelationCoefficient { get; set; }
    public LeakLikelihood LeakLikelihood { get; set; }
    public ConfidenceLevel Confidence { get; set; }
    public string Conclusion { get; set; } = string.Empty;
    public double GCEffectivenessScore { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

public record PerformanceSnapshot
{
    public DateTime Timestamp { get; init; }
    public long ApplicationMemory { get; init; }
    public double CpuUsage { get; init; }
    public double AverageFrameTime { get; init; }
    public long GCPressure { get; init; }
}

// Enums and additional records...
public enum ExportFormat { Json, Csv, Text }
public enum LeakLikelihood { Low, Moderate, High }
public enum ConfidenceLevel { Low, Medium, High }
public enum TrendReliability { Insufficient, Low, Medium, High }
public enum UtilizationLevel { Low, Moderate, High, Critical }
public enum ResponsivenessLevel { Poor, Fair, Good, Excellent }
public enum DatabasePerformanceLevel { Poor, Fair, Good, Excellent }
public enum OptimizationCategory { Memory, VRAM, CPU, UI, Database, Network }
public enum OptimizationPriority { Low, Medium, High, Critical }
public enum RiskType { Memory, VRAM, CPU, UI, Database, Trend }
public enum RiskSeverity { Low, Medium, High }

// Additional record definitions...
public record TrendAnalysisResult
{
    public int DataPoints { get; init; }
    public TrendDirection MemoryTrend { get; init; }
    public TrendDirection CpuTrend { get; init; }
    public TrendDirection FrameTimeTrend { get; init; }
    public TrendReliability Reliability { get; init; }
}

public record MemoryPatternAnalysis
{
    public long CurrentMemoryMB { get; init; }
    public long GCPressureMB { get; init; }
    public double VRAMToRAMRatio { get; init; }
    public List<string> IdentifiedPatterns { get; init; } = new();
}

public record VRAMUtilizationAnalysis
{
    public double TotalVRAMGB { get; init; }
    public double AllocatedVRAMGB { get; init; }
    public double UtilizationPercent { get; init; }
    public int AllocationCount { get; init; }
    public long LargestAllocationMB { get; init; }
    public UtilizationLevel UtilizationLevel { get; init; }
    public List<string> Recommendations { get; init; } = new();
    public bool AllocationFeasibility { get; init; }
    public double AvailableForAllocation { get; init; }
    public double GPUUtilization { get; init; }
}

public record UIResponsivenessAnalysis
{
    public double AverageFrameTimeMs { get; init; }
    public double MaxFrameTimeMs { get; init; }
    public double CurrentFPS { get; init; }
    public bool IsWithinBudget { get; init; }
    public ResponsivenessLevel ResponsivenessLevel { get; init; }
    public List<string> Recommendations { get; init; } = new();
}

public record DatabasePerformanceAnalysis
{
    public double AverageQueryTimeMs { get; init; }
    public double MaxQueryTimeMs { get; init; }
    public int TotalQueries { get; init; }
    public int SlowQueries { get; init; }
    public double SlowQueryRatio { get; init; }
    public DatabasePerformanceLevel PerformanceLevel { get; init; }
    public List<string> Recommendations { get; init; } = new();
    public bool IsWithinBudget { get; init; }
}

public record OptimizationRecommendation
{
    public OptimizationCategory Category { get; init; }
    public OptimizationPriority Priority { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string[] Actions { get; init; } = Array.Empty<string>();
    public string ExpectedImpact { get; init; } = string.Empty;
}

public record PerformanceRiskAssessment
{
    public RiskSeverity OverallRisk { get; init; }
    public List<PerformanceRisk> IdentifiedRisks { get; init; } = new();
    public int RiskScore { get; init; }
    public DateTime AssessmentTime { get; init; }
}

public record PerformanceRisk
{
    public RiskType Type { get; init; }
    public RiskSeverity Severity { get; init; }
    public string Description { get; init; } = string.Empty;
    public double Probability { get; init; }
    public string Impact { get; init; } = string.Empty;
}