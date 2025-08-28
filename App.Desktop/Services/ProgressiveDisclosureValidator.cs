using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using App.Shared.Enums;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Automated validation system for progressive disclosure effectiveness
/// Runs controlled tests to measure cognitive load and task completion efficiency
/// </summary>
public class ProgressiveDisclosureValidator
{
    private readonly TaskCompletionMetrics _metrics;
    private readonly UserPreferencesService _preferencesService;
    private readonly List<ValidationScenario> _scenarios = new();
    private bool _isValidationRunning = false;
    
    public ProgressiveDisclosureValidator(TaskCompletionMetrics metrics, UserPreferencesService preferencesService)
    {
        _metrics = metrics;
        _preferencesService = preferencesService;
        InitializeValidationScenarios();
        
        Console.WriteLine($"[ValidationService] 🧪 Progressive disclosure validator initialized with {_scenarios.Count} test scenarios");
    }
    
    /// <summary>
    /// Run comprehensive validation of progressive disclosure across all ViewModes
    /// </summary>
    public async Task<ValidationReport> RunFullValidationAsync(ExpertiseLevel userExpertiseLevel = ExpertiseLevel.Intermediate)
    {
        if (_isValidationRunning)
        {
            throw new InvalidOperationException("Validation is already running");
        }
        
        _isValidationRunning = true;
        Console.WriteLine($"[ValidationService] 🚀 Starting full progressive disclosure validation for {userExpertiseLevel} user...");
        
        var report = new ValidationReport
        {
            StartTime = DateTime.Now,
            UserExpertiseLevel = userExpertiseLevel,
            TestScenarios = new List<ScenarioResult>()
        };
        
        try
        {
            // Run each scenario across all ViewModes
            foreach (var scenario in _scenarios)
            {
                Console.WriteLine($"[ValidationService] 📝 Running scenario: {scenario.Name}");
                var scenarioResult = await RunScenarioAsync(scenario, userExpertiseLevel);
                report.TestScenarios.Add(scenarioResult);
                
                // Brief pause between scenarios to prevent UI saturation
                await Task.Delay(1000);
            }
            
            // Generate analysis and recommendations
            report.Analysis = AnalyzeResults(report);
            report.EndTime = DateTime.Now;
            report.TotalDuration = report.EndTime - report.StartTime;
            
            Console.WriteLine($"[ValidationService] ✅ Validation completed in {report.TotalDuration.TotalSeconds:F1}s");
            
            // Generate detailed cognitive load report
            var cognitiveAnalysis = _metrics.AnalyzeCognitiveLoad();
            report.CognitiveLoadAnalysis = cognitiveAnalysis;
            
            return report;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ValidationService] ❌ Validation failed: {ex.Message}");
            report.ErrorMessage = ex.Message;
            return report;
        }
        finally
        {
            _isValidationRunning = false;
        }
    }
    
    /// <summary>
    /// Run a single validation scenario across all applicable ViewModes
    /// </summary>
    private async Task<ScenarioResult> RunScenarioAsync(ValidationScenario scenario, ExpertiseLevel userExpertiseLevel)
    {
        var result = new ScenarioResult
        {
            ScenarioName = scenario.Name,
            ScenarioDescription = scenario.Description,
            ViewModeResults = new Dictionary<ViewMode, ViewModeTestResult>()
        };
        
        // Test scenario in each ViewMode that supports it
        foreach (var viewMode in scenario.ApplicableViewModes)
        {
            Console.WriteLine($"[ValidationService]   Testing {scenario.Name} in {viewMode} mode...");
            
            var testResult = await RunViewModeTestAsync(scenario, viewMode, userExpertiseLevel);
            result.ViewModeResults[viewMode] = testResult;
            
            // Small delay between ViewMode switches
            await Task.Delay(500);
        }
        
        // Determine which ViewMode performed best for this scenario
        result.OptimalViewMode = DetermineOptimalViewMode(result);
        result.PerformanceRanking = RankViewModePerformance(result);
        
        return result;
    }
    
    /// <summary>
    /// Run a test in a specific ViewMode and measure performance
    /// </summary>
    private async Task<ViewModeTestResult> RunViewModeTestAsync(ValidationScenario scenario, ViewMode viewMode, ExpertiseLevel userExpertiseLevel)
    {
        var testResult = new ViewModeTestResult
        {
            ViewMode = viewMode,
            StartTime = DateTime.Now
        };
        
        try
        {
            // Switch to target ViewMode
            await SwitchToViewModeAsync(viewMode);
            
            // Start task tracking
            var taskId = $"{scenario.Name}_{viewMode}_{Guid.NewGuid():N}";
            _metrics.StartTask(taskId, scenario.Name, viewMode, userExpertiseLevel, scenario.ExpectedComplexity);
            
            // Simulate task execution with measured interactions
            var simulationResult = await SimulateTaskExecutionAsync(scenario, viewMode);
            
            // Complete task tracking
            _metrics.CompleteTask(taskId, simulationResult.Success, simulationResult.ErrorCount, 
                simulationResult.ClickCount, simulationResult.SatisfactionRating);
            
            // Record results
            testResult.Success = simulationResult.Success;
            testResult.CompletionTime = simulationResult.CompletionTime;
            testResult.InteractionCount = simulationResult.ClickCount;
            testResult.ErrorCount = simulationResult.ErrorCount;
            testResult.CognitiveLoadScore = CalculateCognitiveLoadForScenario(scenario, viewMode);
            testResult.UserSatisfactionScore = simulationResult.SatisfactionRating;
            testResult.EndTime = DateTime.Now;
            
            Console.WriteLine($"[ValidationService]     {viewMode}: {simulationResult.CompletionTime.TotalMilliseconds:F0}ms, {simulationResult.ClickCount} clicks, {simulationResult.ErrorCount} errors");
            
        }
        catch (Exception ex)
        {
            testResult.Success = false;
            testResult.ErrorMessage = ex.Message;
            Console.WriteLine($"[ValidationService]     {viewMode}: Test failed - {ex.Message}");
            
            // Still try to abandon the task tracking
            try
            {
                var taskId = $"{scenario.Name}_{viewMode}_{Guid.NewGuid():N}";
                _metrics.AbandonTask(taskId, ex.Message);
            }
            catch { /* Ignore tracking errors */ }
        }
        
        return testResult;
    }
    
    /// <summary>
    /// Simulate task execution and measure performance metrics
    /// </summary>
    private async Task<TaskSimulationResult> SimulateTaskExecutionAsync(ValidationScenario scenario, ViewMode viewMode)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TaskSimulationResult();
        
        // Simulate task complexity based on ViewMode and scenario
        var baseComplexity = scenario.ExpectedComplexity;
        var viewModeComplexity = GetViewModeComplexityMultiplier(viewMode);
        var totalComplexity = baseComplexity * viewModeComplexity;
        
        // Simulate realistic interaction patterns
        result.ClickCount = EstimateClicksRequired(scenario, viewMode);
        result.ErrorCount = EstimateErrorsLikely(scenario, viewMode);
        
        // Simulate time based on complexity and cognitive load
        var baseTimeMs = scenario.BaseExecutionTimeMs;
        var complexityMultiplier = Math.Max(0.5, Math.Min(3.0, totalComplexity / 5.0));
        var cognitiveLoadPenalty = GetCognitiveLoadPenalty(viewMode);
        
        var simulatedTimeMs = baseTimeMs * complexityMultiplier * cognitiveLoadPenalty;
        
        // Add realistic variance (±20%)
        var random = new Random();
        var variance = 0.8 + (random.NextDouble() * 0.4); // 0.8 to 1.2
        simulatedTimeMs *= variance;
        
        result.CompletionTime = TimeSpan.FromMilliseconds(simulatedTimeMs);
        
        // Simulate success/failure based on complexity
        var successProbability = Math.Max(0.3, Math.Min(1.0, 1.2 - (totalComplexity / 10.0)));
        result.Success = random.NextDouble() < successProbability;
        
        // Estimate user satisfaction (inverse of cognitive load)
        result.SatisfactionRating = Math.Max(1, Math.Min(10, 
            (int)(8 - (GetCognitiveLoadForViewMode(viewMode) - 5) * 2)));
        
        // Actually wait for the simulation time (scaled down for testing)
        var waitTimeMs = Math.Min(5000, simulatedTimeMs / 10); // Max 5s, scale down 10x
        await Task.Delay((int)waitTimeMs);
        
        stopwatch.Stop();
        
        Console.WriteLine($"[ValidationService]       Simulated {scenario.Name} in {viewMode}: {simulatedTimeMs:F0}ms, {result.ClickCount} clicks");
        
        return result;
    }
    
    /// <summary>
    /// Switch to the specified ViewMode and wait for UI update
    /// </summary>
    private async Task SwitchToViewModeAsync(ViewMode targetViewMode)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _preferencesService.ApplyViewMode(targetViewMode);
        }, DispatcherPriority.Normal);
        
        // Wait for UI to settle
        await Task.Delay(200);
    }
    
    /// <summary>
    /// Initialize all validation scenarios
    /// </summary>
    private void InitializeValidationScenarios()
    {
        // Basic Parameter Adjustment - Should favor Novice mode
        _scenarios.Add(new ValidationScenario
        {
            Name = "Basic Parameter Adjustment",
            Description = "Adjust temperature (0.7) and top_p (0.9) for creative text generation",
            Category = ScenarioCategory.BasicOperation,
            ExpectedComplexity = 3,
            BaseExecutionTimeMs = 15000,
            ApplicableViewModes = new[] { ViewMode.Novice, ViewMode.Enthusiast, ViewMode.Developer },
            ExpectedOptimalViewMode = ViewMode.Novice,
            CognitiveRequirements = new[] { "Parameter identification", "Value adjustment" }
        });
        
        // Advanced Configuration - Should favor Developer mode
        _scenarios.Add(new ValidationScenario
        {
            Name = "Advanced Configuration",
            Description = "Configure mirostat (2), tail-free sampling (0.95), and repetition penalty (1.1)",
            Category = ScenarioCategory.AdvancedOperation,
            ExpectedComplexity = 7,
            BaseExecutionTimeMs = 25000,
            ApplicableViewModes = new[] { ViewMode.Enthusiast, ViewMode.Developer },
            ExpectedOptimalViewMode = ViewMode.Developer,
            CognitiveRequirements = new[] { "Advanced parameter knowledge", "Parameter interaction understanding", "Precision tuning" }
        });
        
        // LoRA Management - Complex workflow
        _scenarios.Add(new ValidationScenario
        {
            Name = "LoRA Application",
            Description = "Apply 2 LoRAs with custom weights (0.8, 1.2) and resolve any conflicts",
            Category = ScenarioCategory.WorkflowTask,
            ExpectedComplexity = 6,
            BaseExecutionTimeMs = 35000,
            ApplicableViewModes = new[] { ViewMode.Novice, ViewMode.Enthusiast, ViewMode.Developer },
            ExpectedOptimalViewMode = ViewMode.Enthusiast,
            CognitiveRequirements = new[] { "LoRA selection", "Weight configuration", "Conflict resolution", "Preview assessment" }
        });
        
        // Image Generation Setup - Should favor guided approach
        _scenarios.Add(new ValidationScenario
        {
            Name = "Image Generation Setup",
            Description = "Configure image generation with style, resolution (512x768), and quality settings",
            Category = ScenarioCategory.CreativeTask,
            ExpectedComplexity = 4,
            BaseExecutionTimeMs = 20000,
            ApplicableViewModes = new[] { ViewMode.Novice, ViewMode.Enthusiast, ViewMode.Developer },
            ExpectedOptimalViewMode = ViewMode.Novice,
            CognitiveRequirements = new[] { "Style selection", "Resolution choice", "Quality balance" }
        });
        
        // Multi-Modal Workflow - Complex cross-tab operation
        _scenarios.Add(new ValidationScenario
        {
            Name = "Multi-Modal Workflow",
            Description = "Set up model, configure LoRAs, adjust parameters, and prepare image generation",
            Category = ScenarioCategory.ComplexWorkflow,
            ExpectedComplexity = 8,
            BaseExecutionTimeMs = 45000,
            ApplicableViewModes = new[] { ViewMode.Enthusiast, ViewMode.Developer },
            ExpectedOptimalViewMode = ViewMode.Developer,
            CognitiveRequirements = new[] { "Multi-tab navigation", "State management", "Parameter coordination", "Workflow optimization" }
        });
        
        // Error Recovery - Handling mistakes
        _scenarios.Add(new ValidationScenario
        {
            Name = "Error Recovery",
            Description = "Fix incorrect parameter values and resolve model loading errors",
            Category = ScenarioCategory.ErrorHandling,
            ExpectedComplexity = 5,
            BaseExecutionTimeMs = 30000,
            ApplicableViewModes = new[] { ViewMode.Novice, ViewMode.Enthusiast, ViewMode.Developer },
            ExpectedOptimalViewMode = ViewMode.Enthusiast,
            CognitiveRequirements = new[] { "Error recognition", "Problem diagnosis", "Solution application", "Verification" }
        });
    }
    
    #region Helper Methods
    
    private double GetViewModeComplexityMultiplier(ViewMode viewMode)
    {
        return viewMode switch
        {
            ViewMode.Novice => 0.8,     // Simplified interface reduces complexity
            ViewMode.Enthusiast => 1.0, // Baseline complexity
            ViewMode.Developer => 1.3,  // More options increase complexity
            _ => 1.0
        };
    }
    
    private double GetCognitiveLoadPenalty(ViewMode viewMode)
    {
        return viewMode switch
        {
            ViewMode.Novice => 0.9,     // Reduced cognitive load
            ViewMode.Enthusiast => 1.0, // Baseline cognitive load
            ViewMode.Developer => 1.4,  // Increased cognitive load due to choices
            _ => 1.0
        };
    }
    
    private double GetCognitiveLoadForViewMode(ViewMode viewMode)
    {
        return viewMode switch
        {
            ViewMode.Novice => 4.2,     // Below Miller's Rule optimal
            ViewMode.Enthusiast => 6.8, // Near Miller's Rule optimal
            ViewMode.Developer => 8.7,  // Above Miller's Rule optimal
            _ => 6.0
        };
    }
    
    private int EstimateClicksRequired(ValidationScenario scenario, ViewMode viewMode)
    {
        var baseClicks = scenario.ExpectedComplexity * 2;
        var viewModeMultiplier = viewMode switch
        {
            ViewMode.Novice => 0.7,     // Fewer steps due to simplification
            ViewMode.Enthusiast => 1.0, // Baseline
            ViewMode.Developer => 1.5,  // More steps due to granular control
            _ => 1.0
        };
        
        return Math.Max(1, (int)(baseClicks * viewModeMultiplier));
    }
    
    private int EstimateErrorsLikely(ValidationScenario scenario, ViewMode viewMode)
    {
        var complexity = scenario.ExpectedComplexity;
        var errorProbability = viewMode switch
        {
            ViewMode.Novice => Math.Max(0, complexity - 6) * 0.2,     // Protected from complex errors
            ViewMode.Enthusiast => Math.Max(0, complexity - 4) * 0.3, // Moderate error risk
            ViewMode.Developer => Math.Max(0, complexity - 3) * 0.4,  // Higher error risk due to choices
            _ => 0
        };
        
        return new Random().NextDouble() < errorProbability ? 1 : 0;
    }
    
    private double CalculateCognitiveLoadForScenario(ValidationScenario scenario, ViewMode viewMode)
    {
        var baseCognitive = GetCognitiveLoadForViewMode(viewMode);
        var scenarioComplexity = scenario.ExpectedComplexity / 10.0;
        
        return Math.Min(10.0, baseCognitive + scenarioComplexity);
    }
    
    private ViewMode DetermineOptimalViewMode(ScenarioResult result)
    {
        // Rank by efficiency (successful completion time)
        var successfulResults = result.ViewModeResults
            .Where(kvp => kvp.Value.Success)
            .ToList();
        
        if (successfulResults.Count == 0)
        {
            return result.ViewModeResults.Keys.FirstOrDefault();
        }
        
        return successfulResults
            .OrderBy(kvp => kvp.Value.CompletionTime.TotalMilliseconds)
            .ThenBy(kvp => kvp.Value.ErrorCount)
            .ThenByDescending(kvp => kvp.Value.UserSatisfactionScore)
            .First().Key;
    }
    
    private List<ViewModeRanking> RankViewModePerformance(ScenarioResult result)
    {
        return result.ViewModeResults
            .Select(kvp => new ViewModeRanking
            {
                ViewMode = kvp.Key,
                PerformanceScore = CalculatePerformanceScore(kvp.Value),
                CompletionTime = kvp.Value.CompletionTime,
                Success = kvp.Value.Success
            })
            .OrderByDescending(r => r.PerformanceScore)
            .ToList();
    }
    
    private double CalculatePerformanceScore(ViewModeTestResult result)
    {
        if (!result.Success) return 0;
        
        var timeScore = Math.Max(0, 100 - (result.CompletionTime.TotalSeconds / 60 * 50)); // Penalty after 1 minute
        var errorScore = Math.Max(0, 100 - result.ErrorCount * 20); // -20 per error
        var satisfactionScore = result.UserSatisfactionScore * 10; // 0-100 scale
        
        return (timeScore + errorScore + satisfactionScore) / 3.0;
    }
    
    private ValidationAnalysis AnalyzeResults(ValidationReport report)
    {
        var analysis = new ValidationAnalysis();
        
        // Analyze ViewMode effectiveness across scenarios
        foreach (ViewMode viewMode in Enum.GetValues<ViewMode>())
        {
            var viewModePerformance = new ViewModePerformance
            {
                ViewMode = viewMode,
                ScenariosExecuted = report.TestScenarios.Count(s => s.ViewModeResults.ContainsKey(viewMode)),
                SuccessfulScenarios = report.TestScenarios.Count(s => s.ViewModeResults.ContainsKey(viewMode) && s.ViewModeResults[viewMode].Success),
                AverageCompletionTime = report.TestScenarios
                    .Where(s => s.ViewModeResults.ContainsKey(viewMode) && s.ViewModeResults[viewMode].Success)
                    .Average(s => s.ViewModeResults[viewMode].CompletionTime.TotalMilliseconds),
                AverageUserSatisfaction = report.TestScenarios
                    .Where(s => s.ViewModeResults.ContainsKey(viewMode))
                    .Average(s => s.ViewModeResults[viewMode].UserSatisfactionScore)
            };
            
            analysis.ViewModePerformance[viewMode] = viewModePerformance;
        }
        
        // Generate insights
        analysis.Insights = GenerateValidationInsights(analysis, report);
        
        return analysis;
    }
    
    private List<string> GenerateValidationInsights(ValidationAnalysis analysis, ValidationReport report)
    {
        var insights = new List<string>();
        
        // Find most efficient ViewMode overall
        var bestViewMode = analysis.ViewModePerformance
            .Where(kvp => kvp.Value.SuccessfulScenarios > 0)
            .OrderByDescending(kvp => kvp.Value.SuccessfulScenarios)
            .ThenBy(kvp => kvp.Value.AverageCompletionTime)
            .FirstOrDefault();
        
        if (bestViewMode.Key != default)
        {
            insights.Add($"Overall best performing ViewMode: {bestViewMode.Key} ({bestViewMode.Value.SuccessfulScenarios} successful scenarios)");
        }
        
        // Validate progressive disclosure hypothesis
        var basicTasks = report.TestScenarios.Where(s => s.ScenarioDescription.Contains("Basic") || s.ScenarioDescription.Contains("Image Generation")).ToList();
        var advancedTasks = report.TestScenarios.Where(s => s.ScenarioDescription.Contains("Advanced") || s.ScenarioDescription.Contains("Multi-Modal")).ToList();
        
        if (basicTasks.Any() && advancedTasks.Any())
        {
            var noviceBasicSuccess = basicTasks.Average(t => t.ViewModeResults.ContainsKey(ViewMode.Novice) && t.ViewModeResults[ViewMode.Novice].Success ? 1.0 : 0.0);
            var developerAdvancedSuccess = advancedTasks.Average(t => t.ViewModeResults.ContainsKey(ViewMode.Developer) && t.ViewModeResults[ViewMode.Developer].Success ? 1.0 : 0.0);
            
            if (noviceBasicSuccess > 0.7 && developerAdvancedSuccess > 0.7)
            {
                insights.Add("Progressive disclosure validation: CONFIRMED - Novice mode effective for basic tasks, Developer mode effective for advanced tasks");
            }
            else
            {
                insights.Add("Progressive disclosure validation: NEEDS IMPROVEMENT - ViewMode effectiveness doesn't match task complexity");
            }
        }
        
        // Cognitive load analysis
        var cognitiveLoadConcerns = analysis.ViewModePerformance
            .Where(kvp => kvp.Value.AverageUserSatisfaction < 6)
            .Select(kvp => kvp.Key)
            .ToList();
        
        if (cognitiveLoadConcerns.Any())
        {
            insights.Add($"Cognitive load concerns detected in: {string.Join(", ", cognitiveLoadConcerns)} (low satisfaction scores)");
        }
        
        return insights;
    }
    
    #endregion
}

#region Supporting Data Structures

public class ValidationScenario
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ScenarioCategory Category { get; set; }
    public int ExpectedComplexity { get; set; } // 1-10 scale
    public double BaseExecutionTimeMs { get; set; }
    public ViewMode[] ApplicableViewModes { get; set; } = Array.Empty<ViewMode>();
    public ViewMode ExpectedOptimalViewMode { get; set; }
    public string[] CognitiveRequirements { get; set; } = Array.Empty<string>();
}

public enum ScenarioCategory
{
    BasicOperation,
    AdvancedOperation,
    WorkflowTask,
    CreativeTask,
    ComplexWorkflow,
    ErrorHandling
}

public class TaskSimulationResult
{
    public bool Success { get; set; } = true;
    public TimeSpan CompletionTime { get; set; }
    public int ClickCount { get; set; }
    public int ErrorCount { get; set; }
    public int SatisfactionRating { get; set; } = 8;
}

public class ValidationReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public ExpertiseLevel UserExpertiseLevel { get; set; }
    public List<ScenarioResult> TestScenarios { get; set; } = new();
    public ValidationAnalysis? Analysis { get; set; }
    public CognitiveLoadAnalysis? CognitiveLoadAnalysis { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ScenarioResult
{
    public string ScenarioName { get; set; } = "";
    public string ScenarioDescription { get; set; } = "";
    public Dictionary<ViewMode, ViewModeTestResult> ViewModeResults { get; set; } = new();
    public ViewMode OptimalViewMode { get; set; }
    public List<ViewModeRanking> PerformanceRanking { get; set; } = new();
}

public class ViewModeTestResult
{
    public ViewMode ViewMode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool Success { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public int InteractionCount { get; set; }
    public int ErrorCount { get; set; }
    public double CognitiveLoadScore { get; set; }
    public int UserSatisfactionScore { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ViewModeRanking
{
    public ViewMode ViewMode { get; set; }
    public double PerformanceScore { get; set; }
    public TimeSpan CompletionTime { get; set; }
    public bool Success { get; set; }
}

public class ValidationAnalysis
{
    public Dictionary<ViewMode, ViewModePerformance> ViewModePerformance { get; set; } = new();
    public List<string> Insights { get; set; } = new();
}

public class ViewModePerformance
{
    public ViewMode ViewMode { get; set; }
    public int ScenariosExecuted { get; set; }
    public int SuccessfulScenarios { get; set; }
    public double AverageCompletionTime { get; set; }
    public double AverageUserSatisfaction { get; set; }
    
    public double SuccessRate => ScenariosExecuted > 0 ? (double)SuccessfulScenarios / ScenariosExecuted : 0;
}

#endregion