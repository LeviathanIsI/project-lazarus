using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using App.Shared.Enums;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Measures task completion times across different ViewModes to validate progressive disclosure effectiveness
/// Provides cognitive load analysis and user efficiency metrics
/// </summary>
public class TaskCompletionMetrics
{
    private readonly Dictionary<string, List<TaskMetric>> _taskMetrics = new();
    private readonly Dictionary<ViewMode, UserEfficiencyStats> _viewModeStats = new();
    
    #region Task Tracking
    
    /// <summary>
    /// Start tracking a specific task for completion time analysis
    /// </summary>
    /// <param name="taskId">Unique identifier for the task</param>
    /// <param name="taskName">Human-readable task name</param>
    /// <param name="viewMode">Current ViewMode when task started</param>
    /// <param name="userExpertiseLevel">User's self-reported expertise level</param>
    /// <param name="expectedComplexity">Estimated cognitive complexity (1-10 scale)</param>
    public void StartTask(string taskId, string taskName, ViewMode viewMode, ExpertiseLevel userExpertiseLevel, int expectedComplexity = 5)
    {
        var metric = new TaskMetric
        {
            TaskId = taskId,
            TaskName = taskName,
            ViewMode = viewMode,
            UserExpertiseLevel = userExpertiseLevel,
            ExpectedComplexity = expectedComplexity,
            StartTime = DateTime.Now,
            StartTimestamp = Stopwatch.GetTimestamp()
        };
        
        if (!_taskMetrics.ContainsKey(taskId))
        {
            _taskMetrics[taskId] = new List<TaskMetric>();
        }
        
        _taskMetrics[taskId].Add(metric);
        
        Console.WriteLine($"[TaskMetrics] 🎯 Started task '{taskName}' in {viewMode} mode (expertise: {userExpertiseLevel}, complexity: {expectedComplexity})");
    }
    
    /// <summary>
    /// Complete a task and record the completion metrics
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="success">Whether task was completed successfully</param>
    /// <param name="errorCount">Number of errors encountered during task</param>
    /// <param name="clickCount">Number of UI interactions required</param>
    /// <param name="satisfactionRating">User satisfaction rating (1-10)</param>
    public void CompleteTask(string taskId, bool success = true, int errorCount = 0, int clickCount = 0, int satisfactionRating = 8)
    {
        if (!_taskMetrics.ContainsKey(taskId) || _taskMetrics[taskId].Count == 0)
        {
            Console.WriteLine($"[TaskMetrics] ⚠️ No active task found for ID: {taskId}");
            return;
        }
        
        var metric = _taskMetrics[taskId].LastOrDefault(m => m.EndTime == null);
        if (metric == null)
        {
            Console.WriteLine($"[TaskMetrics] ⚠️ No incomplete task found for ID: {taskId}");
            return;
        }
        
        metric.EndTime = DateTime.Now;
        metric.EndTimestamp = Stopwatch.GetTimestamp();
        metric.Success = success;
        metric.ErrorCount = errorCount;
        metric.ClickCount = clickCount;
        metric.SatisfactionRating = satisfactionRating;
        
        // Calculate derived metrics
        var frequency = Stopwatch.Frequency;
        metric.CompletionTimeMs = (double)(metric.EndTimestamp - metric.StartTimestamp) / frequency * 1000;
        metric.ErrorRate = errorCount / Math.Max(clickCount, 1.0);
        metric.EfficiencyScore = CalculateEfficiencyScore(metric);
        
        Console.WriteLine($"[TaskMetrics] ✅ Completed task '{metric.TaskName}' in {metric.CompletionTimeMs:F0}ms");
        Console.WriteLine($"[TaskMetrics]    Success: {success}, Errors: {errorCount}, Clicks: {clickCount}, Satisfaction: {satisfactionRating}/10");
        
        // Update ViewMode statistics
        UpdateViewModeStats(metric);
    }
    
    /// <summary>
    /// Abandon a task (user gave up or switched contexts)
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="reason">Reason for abandonment</param>
    public void AbandonTask(string taskId, string reason = "User abandoned")
    {
        if (!_taskMetrics.ContainsKey(taskId) || _taskMetrics[taskId].Count == 0)
        {
            return;
        }
        
        var metric = _taskMetrics[taskId].LastOrDefault(m => m.EndTime == null);
        if (metric == null)
        {
            return;
        }
        
        metric.EndTime = DateTime.Now;
        metric.EndTimestamp = Stopwatch.GetTimestamp();
        metric.Success = false;
        metric.AbandonmentReason = reason;
        
        var frequency = Stopwatch.Frequency;
        metric.CompletionTimeMs = (double)(metric.EndTimestamp - metric.StartTimestamp) / frequency * 1000;
        
        Console.WriteLine($"[TaskMetrics] ❌ Abandoned task '{metric.TaskName}' after {metric.CompletionTimeMs:F0}ms - {reason}");
        
        UpdateViewModeStats(metric);
    }
    
    #endregion
    
    #region Analytics & Reporting
    
    /// <summary>
    /// Generate comprehensive efficiency report across all ViewModes
    /// </summary>
    public ViewModeEfficiencyReport GenerateEfficiencyReport()
    {
        var report = new ViewModeEfficiencyReport
        {
            GeneratedAt = DateTime.Now,
            TotalTasksTracked = _taskMetrics.Values.SelectMany(x => x).Count()
        };
        
        Console.WriteLine($"[TaskMetrics] 📊 Generating efficiency report for {report.TotalTasksTracked} tracked tasks...");
        
        // Analyze each ViewMode
        foreach (ViewMode viewMode in Enum.GetValues<ViewMode>())
        {
            var viewModeTasks = GetTasksForViewMode(viewMode);
            if (viewModeTasks.Count == 0) continue;
            
            var analysis = new ViewModeAnalysis
            {
                ViewMode = viewMode,
                TotalTasks = viewModeTasks.Count,
                CompletedTasks = viewModeTasks.Count(t => t.Success && t.EndTime != null),
                AbandonedTasks = viewModeTasks.Count(t => !t.Success && t.EndTime != null),
                AverageCompletionTime = viewModeTasks.Where(t => t.Success && t.CompletionTimeMs > 0)
                    .Average(t => t.CompletionTimeMs),
                AverageErrorRate = viewModeTasks.Where(t => t.Success)
                    .Average(t => t.ErrorRate),
                AverageSatisfaction = viewModeTasks.Where(t => t.Success)
                    .Average(t => t.SatisfactionRating),
                AverageEfficiencyScore = viewModeTasks.Where(t => t.Success)
                    .Average(t => t.EfficiencyScore)
            };
            
            // Expertise level breakdown
            analysis.ExpertiseLevelBreakdown = viewModeTasks
                .GroupBy(t => t.UserExpertiseLevel)
                .ToDictionary(g => g.Key, g => new ExpertiseLevelStats
                {
                    TaskCount = g.Count(),
                    SuccessRate = g.Count(t => t.Success) / (double)g.Count(),
                    AverageCompletionTime = g.Where(t => t.Success && t.CompletionTimeMs > 0)
                        .DefaultIfEmpty()
                        .Average(t => t?.CompletionTimeMs ?? 0),
                    AverageErrorRate = g.Where(t => t.Success)
                        .DefaultIfEmpty()
                        .Average(t => t?.ErrorRate ?? 0)
                });
            
            report.ViewModeAnalysis[viewMode] = analysis;
            
            Console.WriteLine($"[TaskMetrics] {viewMode}: {analysis.CompletedTasks}/{analysis.TotalTasks} completed, {analysis.AverageCompletionTime:F0}ms avg");
        }
        
        // Generate insights and recommendations
        report.Insights = GenerateInsights(report);
        
        return report;
    }
    
    /// <summary>
    /// Test specific scenarios to validate progressive disclosure effectiveness
    /// </summary>
    public List<ProgressiveDisclosureTest> RunProgressiveDisclosureValidation()
    {
        Console.WriteLine($"[TaskMetrics] 🧪 Running progressive disclosure validation tests...");
        
        var tests = new List<ProgressiveDisclosureTest>();
        
        // Test 1: Basic Parameter Adjustment Task
        tests.Add(new ProgressiveDisclosureTest
        {
            TestName = "Basic Parameter Adjustment",
            TaskDescription = "Adjust temperature and top_p for creative text generation",
            ExpectedResults = new Dictionary<ViewMode, ExpectedMetrics>
            {
                [ViewMode.Novice] = new() { ExpectedTimeMs = 15000, ExpectedClicks = 3, ExpectedErrors = 0 },
                [ViewMode.Enthusiast] = new() { ExpectedTimeMs = 12000, ExpectedClicks = 4, ExpectedErrors = 0 },
                [ViewMode.Developer] = new() { ExpectedTimeMs = 18000, ExpectedClicks = 8, ExpectedErrors = 1 }
            },
            Hypothesis = "Novice mode should be fastest for basic tasks due to reduced cognitive load"
        });
        
        // Test 2: Advanced Configuration Task
        tests.Add(new ProgressiveDisclosureTest
        {
            TestName = "Advanced Configuration",
            TaskDescription = "Configure mirostat, tail-free sampling, and repetition penalty",
            ExpectedResults = new Dictionary<ViewMode, ExpectedMetrics>
            {
                [ViewMode.Novice] = new() { ExpectedTimeMs = -1, ExpectedClicks = -1, ExpectedErrors = -1 }, // Not available
                [ViewMode.Enthusiast] = new() { ExpectedTimeMs = 25000, ExpectedClicks = 8, ExpectedErrors = 1 },
                [ViewMode.Developer] = new() { ExpectedTimeMs = 20000, ExpectedClicks = 6, ExpectedErrors = 0 }
            },
            Hypothesis = "Developer mode should be fastest for advanced tasks due to direct parameter access"
        });
        
        // Test 3: LoRA Management Task
        tests.Add(new ProgressiveDisclosureTest
        {
            TestName = "LoRA Management",
            TaskDescription = "Apply 2 LoRAs with custom weights and resolve conflicts",
            ExpectedResults = new Dictionary<ViewMode, ExpectedMetrics>
            {
                [ViewMode.Novice] = new() { ExpectedTimeMs = 45000, ExpectedClicks = 12, ExpectedErrors = 2 },
                [ViewMode.Enthusiast] = new() { ExpectedTimeMs = 35000, ExpectedClicks = 8, ExpectedErrors = 1 },
                [ViewMode.Developer] = new() { ExpectedTimeMs = 25000, ExpectedClicks = 6, ExpectedErrors = 0 }
            },
            Hypothesis = "Task complexity should favor higher expertise ViewModes"
        });
        
        // Test 4: Image Generation Workflow
        tests.Add(new ProgressiveDisclosureTest
        {
            TestName = "Image Generation Workflow",
            TaskDescription = "Create image with specific style, resolution, and post-processing",
            ExpectedResults = new Dictionary<ViewMode, ExpectedMetrics>
            {
                [ViewMode.Novice] = new() { ExpectedTimeMs = 30000, ExpectedClicks = 6, ExpectedErrors = 0 },
                [ViewMode.Enthusiast] = new() { ExpectedTimeMs = 25000, ExpectedClicks = 8, ExpectedErrors = 1 },
                [ViewMode.Developer] = new() { ExpectedTimeMs = 35000, ExpectedClicks = 12, ExpectedErrors = 2 }
            },
            Hypothesis = "Guided workflows should benefit from progressive disclosure"
        });
        
        Console.WriteLine($"[TaskMetrics] ✅ Created {tests.Count} progressive disclosure validation tests");
        return tests;
    }
    
    /// <summary>
    /// Analyze cognitive load patterns across ViewModes
    /// </summary>
    public CognitiveLoadAnalysis AnalyzeCognitiveLoad()
    {
        Console.WriteLine($"[TaskMetrics] 🧠 Analyzing cognitive load patterns...");
        
        var analysis = new CognitiveLoadAnalysis
        {
            AnalysisDate = DateTime.Now
        };
        
        foreach (ViewMode viewMode in Enum.GetValues<ViewMode>())
        {
            var tasks = GetTasksForViewMode(viewMode).Where(t => t.Success).ToList();
            if (tasks.Count == 0) continue;
            
            var cognitiveMetrics = new CognitiveMetrics
            {
                ViewMode = viewMode,
                TaskCount = tasks.Count,
                
                // Miller's Rule Analysis (7±2 items)
                AverageChoicesPresented = EstimateChoicesPresented(viewMode),
                CognitiveLoadScore = CalculateCognitiveLoadScore(viewMode),
                
                // Error pattern analysis
                ErrorsByComplexity = tasks.GroupBy(t => t.ExpectedComplexity)
                    .ToDictionary(g => g.Key, g => g.Average(t => t.ErrorRate)),
                
                // Time-to-confusion metrics
                AverageTimeToFirstError = tasks.Where(t => t.ErrorCount > 0)
                    .Average(t => t.CompletionTimeMs * 0.3), // Estimate first error at 30% completion
                
                // Decision fatigue indicators
                CompletionTimeByTaskOrder = tasks.OrderBy(t => t.StartTime)
                    .Select((t, i) => new { Order = i, Time = t.CompletionTimeMs })
                    .GroupBy(x => x.Order / 5) // Group by 5s
                    .ToDictionary(g => g.Key, g => g.Average(x => x.Time))
            };
            
            analysis.ViewModeMetrics[viewMode] = cognitiveMetrics;
            
            Console.WriteLine($"[TaskMetrics] {viewMode}: Cognitive load score {cognitiveMetrics.CognitiveLoadScore:F2}, {cognitiveMetrics.AverageChoicesPresented} avg choices");
        }
        
        // Generate cognitive load insights
        analysis.Insights = GenerateCognitiveInsights(analysis);
        
        return analysis;
    }
    
    #endregion
    
    #region Helper Methods
    
    private List<TaskMetric> GetTasksForViewMode(ViewMode viewMode)
    {
        return _taskMetrics.Values
            .SelectMany(x => x)
            .Where(t => t.ViewMode == viewMode)
            .ToList();
    }
    
    private double CalculateEfficiencyScore(TaskMetric metric)
    {
        if (metric.CompletionTimeMs <= 0) return 0;
        
        // Efficiency score: success rate * speed * accuracy * satisfaction
        var timeScore = Math.Max(0, 1.0 - (metric.CompletionTimeMs / 60000.0)); // Penalty after 1 minute
        var accuracyScore = Math.Max(0, 1.0 - metric.ErrorRate);
        var satisfactionScore = metric.SatisfactionRating / 10.0;
        var successScore = metric.Success ? 1.0 : 0.0;
        
        return timeScore * accuracyScore * satisfactionScore * successScore * 100.0;
    }
    
    private void UpdateViewModeStats(TaskMetric metric)
    {
        if (!_viewModeStats.ContainsKey(metric.ViewMode))
        {
            _viewModeStats[metric.ViewMode] = new UserEfficiencyStats();
        }
        
        var stats = _viewModeStats[metric.ViewMode];
        stats.TotalTasks++;
        
        if (metric.Success)
        {
            stats.SuccessfulTasks++;
            stats.TotalCompletionTime += metric.CompletionTimeMs;
            stats.TotalErrors += metric.ErrorCount;
            stats.TotalClicks += metric.ClickCount;
        }
        else
        {
            stats.AbandonedTasks++;
        }
    }
    
    private double EstimateChoicesPresented(ViewMode viewMode)
    {
        // Estimate based on ViewMode complexity
        return viewMode switch
        {
            ViewMode.Novice => 5.2, // Miller's Rule optimal
            ViewMode.Enthusiast => 8.7, // Slightly above optimal
            ViewMode.Developer => 15.3, // Significantly above optimal
            _ => 7.0
        };
    }
    
    private double CalculateCognitiveLoadScore(ViewMode viewMode)
    {
        // Score from 1-10, where 5 is optimal cognitive load
        var choicesPresented = EstimateChoicesPresented(viewMode);
        var millerOptimal = 7.0; // Miller's Rule central value
        
        var deviationFromOptimal = Math.Abs(choicesPresented - millerOptimal);
        var cognitiveLoad = 5.0 + (deviationFromOptimal * 0.5);
        
        return Math.Min(10.0, Math.Max(1.0, cognitiveLoad));
    }
    
    private List<string> GenerateInsights(ViewModeEfficiencyReport report)
    {
        var insights = new List<string>();
        
        // Find most efficient ViewMode
        var mostEfficient = report.ViewModeAnalysis
            .Where(kvp => kvp.Value.CompletedTasks > 0)
            .OrderByDescending(kvp => kvp.Value.AverageEfficiencyScore)
            .FirstOrDefault();
        
        if (mostEfficient.Key != default)
        {
            insights.Add($"Most efficient ViewMode: {mostEfficient.Key} (avg efficiency: {mostEfficient.Value.AverageEfficiencyScore:F1})");
        }
        
        // Analyze expertise level matching
        foreach (var analysis in report.ViewModeAnalysis.Values)
        {
            foreach (var expertise in analysis.ExpertiseLevelBreakdown)
            {
                var match = IsExpertiseLevelWellMatched(analysis.ViewMode, expertise.Key);
                if (!match)
                {
                    insights.Add($"{expertise.Key} users in {analysis.ViewMode} mode show suboptimal performance (success rate: {expertise.Value.SuccessRate:P})");
                }
            }
        }
        
        // Progressive disclosure validation
        var novicePerformance = report.ViewModeAnalysis.GetValueOrDefault(ViewMode.Novice);
        var developerPerformance = report.ViewModeAnalysis.GetValueOrDefault(ViewMode.Developer);
        
        if (novicePerformance != null && developerPerformance != null)
        {
            if (novicePerformance.AverageCompletionTime < developerPerformance.AverageCompletionTime)
            {
                insights.Add("Progressive disclosure validates: Novice mode enables faster task completion for basic operations");
            }
            else
            {
                insights.Add("Progressive disclosure concern: Developer mode outperforming Novice mode suggests UI complexity issues");
            }
        }
        
        return insights;
    }
    
    private List<string> GenerateCognitiveInsights(CognitiveLoadAnalysis analysis)
    {
        var insights = new List<string>();
        
        foreach (var metrics in analysis.ViewModeMetrics.Values)
        {
            // Miller's Rule compliance
            if (metrics.AverageChoicesPresented <= 9)
            {
                insights.Add($"{metrics.ViewMode}: Complies with Miller's Rule (7±2) - cognitive load manageable");
            }
            else
            {
                insights.Add($"{metrics.ViewMode}: Violates Miller's Rule ({metrics.AverageChoicesPresented:F1} choices) - high cognitive load risk");
            }
            
            // Cognitive load assessment
            if (metrics.CognitiveLoadScore <= 6)
            {
                insights.Add($"{metrics.ViewMode}: Optimal cognitive load (score: {metrics.CognitiveLoadScore:F1})");
            }
            else if (metrics.CognitiveLoadScore <= 8)
            {
                insights.Add($"{metrics.ViewMode}: Elevated cognitive load (score: {metrics.CognitiveLoadScore:F1}) - monitor user fatigue");
            }
            else
            {
                insights.Add($"{metrics.ViewMode}: Critical cognitive load (score: {metrics.CognitiveLoadScore:F1}) - immediate UI simplification needed");
            }
        }
        
        return insights;
    }
    
    private bool IsExpertiseLevelWellMatched(ViewMode viewMode, ExpertiseLevel expertiseLevel)
    {
        // Define optimal matches
        return (viewMode, expertiseLevel) switch
        {
            (ViewMode.Novice, ExpertiseLevel.Beginner) => true,
            (ViewMode.Novice, ExpertiseLevel.Intermediate) => true,
            (ViewMode.Enthusiast, ExpertiseLevel.Intermediate) => true,
            (ViewMode.Enthusiast, ExpertiseLevel.Advanced) => true,
            (ViewMode.Developer, ExpertiseLevel.Advanced) => true,
            (ViewMode.Developer, ExpertiseLevel.Expert) => true,
            _ => false
        };
    }
    
    #endregion
}

/// <summary>
/// Represents a single task completion measurement
/// </summary>
public class TaskMetric
{
    public string TaskId { get; set; } = "";
    public string TaskName { get; set; } = "";
    public ViewMode ViewMode { get; set; }
    public ExpertiseLevel UserExpertiseLevel { get; set; }
    public int ExpectedComplexity { get; set; } // 1-10 scale
    
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long StartTimestamp { get; set; }
    public long EndTimestamp { get; set; }
    
    public double CompletionTimeMs { get; set; }
    public bool Success { get; set; }
    public int ErrorCount { get; set; }
    public int ClickCount { get; set; }
    public double ErrorRate { get; set; }
    public int SatisfactionRating { get; set; } // 1-10
    public double EfficiencyScore { get; set; } // 0-100
    public string? AbandonmentReason { get; set; }
}


/// <summary>
/// Comprehensive efficiency report across ViewModes
/// </summary>
public class ViewModeEfficiencyReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalTasksTracked { get; set; }
    public Dictionary<ViewMode, ViewModeAnalysis> ViewModeAnalysis { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

/// <summary>
/// Analysis data for a specific ViewMode
/// </summary>
public class ViewModeAnalysis
{
    public ViewMode ViewMode { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int AbandonedTasks { get; set; }
    public double AverageCompletionTime { get; set; }
    public double AverageErrorRate { get; set; }
    public double AverageSatisfaction { get; set; }
    public double AverageEfficiencyScore { get; set; }
    public Dictionary<ExpertiseLevel, ExpertiseLevelStats> ExpertiseLevelBreakdown { get; set; } = new();
    
    public double SuccessRate => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks : 0;
    public double AbandonmentRate => TotalTasks > 0 ? (double)AbandonedTasks / TotalTasks : 0;
}

/// <summary>
/// Statistics for users at a specific expertise level
/// </summary>
public class ExpertiseLevelStats
{
    public int TaskCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageCompletionTime { get; set; }
    public double AverageErrorRate { get; set; }
}

/// <summary>
/// User efficiency statistics for a ViewMode
/// </summary>
public class UserEfficiencyStats
{
    public int TotalTasks { get; set; }
    public int SuccessfulTasks { get; set; }
    public int AbandonedTasks { get; set; }
    public double TotalCompletionTime { get; set; }
    public int TotalErrors { get; set; }
    public int TotalClicks { get; set; }
    
    public double AverageCompletionTime => SuccessfulTasks > 0 ? TotalCompletionTime / SuccessfulTasks : 0;
    public double SuccessRate => TotalTasks > 0 ? (double)SuccessfulTasks / TotalTasks : 0;
    public double AverageErrorRate => SuccessfulTasks > 0 ? (double)TotalErrors / Math.Max(TotalClicks, 1) : 0;
}

/// <summary>
/// Test specification for progressive disclosure validation
/// </summary>
public class ProgressiveDisclosureTest
{
    public string TestName { get; set; } = "";
    public string TaskDescription { get; set; } = "";
    public Dictionary<ViewMode, ExpectedMetrics> ExpectedResults { get; set; } = new();
    public string Hypothesis { get; set; } = "";
}

/// <summary>
/// Expected metrics for a specific test scenario
/// </summary>
public class ExpectedMetrics
{
    public double ExpectedTimeMs { get; set; }
    public int ExpectedClicks { get; set; }
    public int ExpectedErrors { get; set; }
}

/// <summary>
/// Cognitive load analysis across ViewModes
/// </summary>
public class CognitiveLoadAnalysis
{
    public DateTime AnalysisDate { get; set; }
    public Dictionary<ViewMode, CognitiveMetrics> ViewModeMetrics { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

/// <summary>
/// Cognitive performance metrics for a ViewMode
/// </summary>
public class CognitiveMetrics
{
    public ViewMode ViewMode { get; set; }
    public int TaskCount { get; set; }
    public double AverageChoicesPresented { get; set; }
    public double CognitiveLoadScore { get; set; } // 1-10 scale
    public Dictionary<int, double> ErrorsByComplexity { get; set; } = new();
    public double AverageTimeToFirstError { get; set; }
    public Dictionary<int, double> CompletionTimeByTaskOrder { get; set; } = new();
}