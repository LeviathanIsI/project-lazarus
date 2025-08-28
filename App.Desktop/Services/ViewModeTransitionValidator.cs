using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using App.Shared.Enums;
using Lazarus.Shared.Models;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Comprehensive validation system for ViewMode transitions to ensure zero data loss
/// Tests parameter preservation, UI state continuity, and work-in-progress protection
/// </summary>
public class ViewModeTransitionValidator
{
    private readonly UserPreferencesService _preferencesService;
    private readonly ViewModeStateManager _stateManager;
    private readonly List<TransitionTestCase> _testCases = new();
    
    public ViewModeTransitionValidator(UserPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
        _stateManager = preferencesService.StateManager;
        InitializeTestCases();
        
        Console.WriteLine($"[TransitionValidator] 🔄 Initialized with {_testCases.Count} transition test cases");
    }
    
    /// <summary>
    /// Run comprehensive validation of all ViewMode transitions
    /// </summary>
    public async Task<ViewModeTransitionReport> RunFullValidationAsync()
    {
        Console.WriteLine($"[TransitionValidator] 🚀 Starting comprehensive ViewMode transition validation...");
        
        var report = new ViewModeTransitionReport
        {
            StartTime = DateTime.Now,
            TestCases = new List<TransitionTestResult>()
        };
        
        try
        {
            
            // Test all possible ViewMode transitions
            var allTransitions = GetAllPossibleTransitions();
            var totalTests = allTransitions.Count * _testCases.Count;
            var completedTests = 0;
            
            Console.WriteLine($"[TransitionValidator] Testing {totalTests} scenarios ({allTransitions.Count} transitions × {_testCases.Count} test cases)");
            
            foreach (var transition in allTransitions)
            {
                Console.WriteLine($"[TransitionValidator] 📋 Testing transition: {transition.From} → {transition.To}");
                
                foreach (var testCase in _testCases)
                {
                    var testResult = await RunTransitionTestAsync(transition, testCase);
                    report.TestCases.Add(testResult);
                    completedTests++;
                    
                    Console.WriteLine($"[TransitionValidator]   {testCase.Name}: {(testResult.Success ? "✅ PASS" : "❌ FAIL")}");
                    
                    if (!testResult.Success)
                    {
                        Console.WriteLine($"[TransitionValidator]     Failure: {testResult.FailureReason}");
                    }
                    
                    // Small delay to prevent UI saturation
                    await Task.Delay(100);
                }
            }
            
            // Generate comprehensive analysis
            report.Analysis = AnalyzeValidationResults(report.TestCases);
            report.EndTime = DateTime.Now;
            report.Duration = report.EndTime - report.StartTime;
            
            Console.WriteLine($"[TransitionValidator] ✅ Validation completed: {report.Analysis.SuccessfulTests}/{totalTests} tests passed");
            
            return report;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TransitionValidator] ❌ Validation failed: {ex.Message}");
            report.ErrorMessage = ex.Message;
            return report;
        }
    }
    
    /// <summary>
    /// Run quick validation with essential tests only
    /// </summary>
    public async Task<ViewModeTransitionReport> RunQuickValidationAsync()
    {
        Console.WriteLine($"[TransitionValidator] ⚡ Running quick ViewMode transition validation...");
        
        var report = new ViewModeTransitionReport
        {
            StartTime = DateTime.Now,
            TestCases = new List<TransitionTestResult>()
        };
        
        try
        {
            // Test critical transitions only
            var criticalTransitions = new[]
            {
                new ViewModeTransition { From = ViewMode.Novice, To = ViewMode.Enthusiast },
                new ViewModeTransition { From = ViewMode.Enthusiast, To = ViewMode.Developer },
                new ViewModeTransition { From = ViewMode.Developer, To = ViewMode.Novice }
            };
            
            // Test essential scenarios only
            var essentialTestCases = _testCases.Where(tc => 
                tc.Priority == TestPriority.Critical || tc.Priority == TestPriority.High).ToList();
            
            foreach (var transition in criticalTransitions)
            {
                foreach (var testCase in essentialTestCases)
                {
                    var testResult = await RunTransitionTestAsync(transition, testCase);
                    report.TestCases.Add(testResult);
                    
                    Console.WriteLine($"[TransitionValidator]   {testCase.Name} ({transition.From}→{transition.To}): {(testResult.Success ? "✅" : "❌")}");
                }
            }
            
            report.Analysis = AnalyzeValidationResults(report.TestCases);
            report.EndTime = DateTime.Now;
            report.Duration = report.EndTime - report.StartTime;
            
            Console.WriteLine($"[TransitionValidator] ⚡ Quick validation completed: {report.Analysis.SuccessfulTests}/{report.TestCases.Count} tests passed");
            
            return report;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TransitionValidator] ❌ Quick validation failed: {ex.Message}");
            report.ErrorMessage = ex.Message;
            return report;
        }
    }
    
    /// <summary>
    /// Test a specific ViewMode transition scenario
    /// </summary>
    private async Task<TransitionTestResult> RunTransitionTestAsync(ViewModeTransition transition, TransitionTestCase testCase)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new TransitionTestResult
        {
            TestCaseName = testCase.Name,
            FromViewMode = transition.From,
            ToViewMode = transition.To,
            StartTime = DateTime.Now
        };
        
        try
        {
            // Step 1: Set up initial ViewMode
            await SwitchToViewModeAsync(transition.From);
            await Task.Delay(200); // Allow UI to settle
            
            // Step 2: Set up test data according to test case
            var testData = await SetupTestDataAsync(testCase);
            result.InitialData = testData;
            
            // Step 3: Preserve state before transition
            await PreserveStateAsync(testData);
            
            // Step 4: Perform ViewMode transition
            var transitionStart = Stopwatch.GetTimestamp();
            await SwitchToViewModeAsync(transition.To);
            var transitionEnd = Stopwatch.GetTimestamp();
            
            result.TransitionDuration = TimeSpan.FromMilliseconds(
                (double)(transitionEnd - transitionStart) / Stopwatch.Frequency * 1000);
            
            await Task.Delay(300); // Allow transition to complete
            
            // Step 5: Validate data preservation
            var validationResult = await ValidateDataPreservationAsync(testCase, testData);
            result.ValidationResults = validationResult;
            
            // Step 6: Test state restoration
            var restorationResult = await ValidateStateRestorationAsync(testCase, testData);
            result.RestorationResults = restorationResult;
            
            // Step 7: Determine overall success
            result.Success = validationResult.All(v => v.Success) && restorationResult.All(r => r.Success);
            
            if (!result.Success)
            {
                var failures = validationResult.Where(v => !v.Success)
                    .Concat(restorationResult.Where(r => !r.Success))
                    .Select(f => f.FailureReason)
                    .ToList();
                result.FailureReason = string.Join("; ", failures);
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.FailureReason = $"Test execution failed: {ex.Message}";
            Console.WriteLine($"[TransitionValidator] ❌ Test failed: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            result.EndTime = DateTime.Now;
            result.TotalDuration = stopwatch.Elapsed;
        }
        
        return result;
    }
    
    /// <summary>
    /// Set up test data based on the test case requirements
    /// </summary>
    private async Task<TestDataState> SetupTestDataAsync(TransitionTestCase testCase)
    {
        var testData = new TestDataState
        {
            TestCaseName = testCase.Name,
            SetupTime = DateTime.Now
        };
        
        try
        {
            // Set up parameter values
            testData.ParameterValues = testCase.TestParameters.ToDictionary(
                p => p.Key, p => GenerateTestValue(p.Value));
            
            // Set up UI state
            testData.UIState = testCase.UIStateSettings.ToDictionary(
                s => s.Key, s => GenerateUIStateValue(s.Value));
            
            // Set up work-in-progress data if specified
            if (testCase.WorkInProgressData.Count > 0)
            {
                testData.WorkInProgressItems = testCase.WorkInProgressData.ToDictionary(
                    w => w.Key, w => new WorkInProgressData
                    {
                        Description = w.Value,
                        Data = GenerateWorkData(w.Key),
                        LastModified = DateTime.Now,
                        IsUnsaved = true,
                        WorkType = w.Key
                    });
            }
            
            Console.WriteLine($"[TransitionValidator] 📋 Test data setup: {testData.ParameterValues.Count} params, {testData.UIState.Count} UI states, {testData.WorkInProgressItems.Count} work items");
            
            return testData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TransitionValidator] ❌ Test data setup failed: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Preserve state using the ViewModeStateManager
    /// </summary>
    private async Task PreserveStateAsync(TestDataState testData)
    {
        await Task.Run(() =>
        {
            // Preserve parameter values
            if (testData.ParameterValues.Count > 0)
            {
                _stateManager.PreserveParameterState(testData.ParameterValues);
            }
            
            // Preserve UI state
            if (testData.UIState.Count > 0)
            {
                _stateManager.PreserveUIState(testData.UIState);
            }
            
            // Preserve work-in-progress data
            foreach (var work in testData.WorkInProgressItems)
            {
                _stateManager.PreserveWorkInProgress(work.Key, work.Value);
            }
        });
        
        Console.WriteLine($"[TransitionValidator] 💾 State preservation completed");
    }
    
    /// <summary>
    /// Validate that data was preserved correctly during transition
    /// </summary>
    private async Task<List<ValidationResult>> ValidateDataPreservationAsync(TransitionTestCase testCase, TestDataState originalData)
    {
        var results = new List<ValidationResult>();
        
        await Task.Run(() =>
        {
            // Validate parameter preservation
            foreach (var originalParam in originalData.ParameterValues)
            {
                var preserved = _stateManager.RestoreParameterState(new[] { originalParam.Key });
                
                if (preserved.ContainsKey(originalParam.Key))
                {
                    if (AreValuesEqual(preserved[originalParam.Key], originalParam.Value))
                    {
                        results.Add(new ValidationResult
                        {
                            TestType = "Parameter Preservation",
                            ItemName = originalParam.Key,
                            Success = true,
                            Details = $"Value preserved: {originalParam.Value}"
                        });
                    }
                    else
                    {
                        results.Add(new ValidationResult
                        {
                            TestType = "Parameter Preservation",
                            ItemName = originalParam.Key,
                            Success = false,
                            FailureReason = $"Value changed: {originalParam.Value} → {preserved[originalParam.Key]}"
                        });
                    }
                }
                else
                {
                    results.Add(new ValidationResult
                    {
                        TestType = "Parameter Preservation",
                        ItemName = originalParam.Key,
                        Success = false,
                        FailureReason = "Parameter not found in restored state"
                    });
                }
            }
            
            // Validate UI state preservation
            var restoredUI = _stateManager.RestoreUIState();
            foreach (var originalUI in originalData.UIState)
            {
                if (restoredUI.ContainsKey(originalUI.Key))
                {
                    if (AreValuesEqual(restoredUI[originalUI.Key], originalUI.Value))
                    {
                        results.Add(new ValidationResult
                        {
                            TestType = "UI State Preservation",
                            ItemName = originalUI.Key,
                            Success = true,
                            Details = $"UI state preserved: {originalUI.Value}"
                        });
                    }
                    else
                    {
                        results.Add(new ValidationResult
                        {
                            TestType = "UI State Preservation",
                            ItemName = originalUI.Key,
                            Success = false,
                            FailureReason = $"UI state changed: {originalUI.Value} → {restoredUI[originalUI.Key]}"
                        });
                    }
                }
                else
                {
                    results.Add(new ValidationResult
                    {
                        TestType = "UI State Preservation",
                        ItemName = originalUI.Key,
                        Success = false,
                        FailureReason = "UI state not found in restored data"
                    });
                }
            }
            
            // Validate work-in-progress preservation
            foreach (var originalWork in originalData.WorkInProgressItems)
            {
                var preserved = _stateManager.GetWorkInProgress(originalWork.Key);
                
                if (preserved != null)
                {
                    results.Add(new ValidationResult
                    {
                        TestType = "Work-in-Progress Preservation",
                        ItemName = originalWork.Key,
                        Success = true,
                        Details = $"Work preserved: {preserved.Description}"
                    });
                }
                else
                {
                    results.Add(new ValidationResult
                    {
                        TestType = "Work-in-Progress Preservation",
                        ItemName = originalWork.Key,
                        Success = false,
                        FailureReason = "Work-in-progress data not found"
                    });
                }
            }
        });
        
        return results;
    }
    
    /// <summary>
    /// Validate that state can be properly restored in the new ViewMode
    /// </summary>
    private async Task<List<ValidationResult>> ValidateStateRestorationAsync(TransitionTestCase testCase, TestDataState originalData)
    {
        var results = new List<ValidationResult>();
        
        await Task.Run(() =>
        {
            try
            {
                // Test parameter restoration with current ViewMode constraints
                var availableParams = GetParametersForCurrentViewMode();
                var restored = _stateManager.RestoreParameterState(availableParams);
                
                foreach (var originalParam in originalData.ParameterValues)
                {
                    if (availableParams.Contains(originalParam.Key))
                    {
                        if (restored.ContainsKey(originalParam.Key))
                        {
                            results.Add(new ValidationResult
                            {
                                TestType = "State Restoration",
                                ItemName = originalParam.Key,
                                Success = true,
                                Details = $"Parameter restored in current ViewMode"
                            });
                        }
                        else
                        {
                            results.Add(new ValidationResult
                            {
                                TestType = "State Restoration",
                                ItemName = originalParam.Key,
                                Success = false,
                                FailureReason = "Parameter available but not restored"
                            });
                        }
                    }
                    else
                    {
                        // This is expected behavior - parameter not available in current ViewMode
                        results.Add(new ValidationResult
                        {
                            TestType = "State Restoration",
                            ItemName = originalParam.Key,
                            Success = true,
                            Details = "Parameter correctly hidden in current ViewMode"
                        });
                    }
                }
                
                // Test UI state restoration
                var restoredUI = _stateManager.RestoreUIState();
                results.Add(new ValidationResult
                {
                    TestType = "UI State Restoration",
                    ItemName = "All UI States",
                    Success = restoredUI.Count > 0,
                    Details = $"Restored {restoredUI.Count} UI state items"
                });
            }
            catch (Exception ex)
            {
                results.Add(new ValidationResult
                {
                    TestType = "State Restoration",
                    ItemName = "General",
                    Success = false,
                    FailureReason = $"Restoration failed: {ex.Message}"
                });
            }
        });
        
        return results;
    }
    
    /// <summary>
    /// Switch to specified ViewMode and wait for transition
    /// </summary>
    private async Task SwitchToViewModeAsync(ViewMode targetViewMode)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _preferencesService.ApplyViewMode(targetViewMode);
        }, DispatcherPriority.Normal);
        
        // Wait for transition to complete
        await Task.Delay(500);
    }
    
    /// <summary>
    /// Initialize comprehensive test cases
    /// </summary>
    private void InitializeTestCases()
    {
        // Critical: Basic parameter preservation
        _testCases.Add(new TransitionTestCase
        {
            Name = "Basic Parameter Preservation",
            Description = "Test preservation of common parameters during ViewMode transitions",
            Priority = TestPriority.Critical,
            TestParameters = new Dictionary<string, Type>
            {
                ["Temperature"] = typeof(double),
                ["TopP"] = typeof(double),
                ["TopK"] = typeof(int),
                ["RepetitionPenalty"] = typeof(double)
            },
            UIStateSettings = new Dictionary<string, Type>
            {
                ["ScrollPosition"] = typeof(double),
                ["ExpandedSection"] = typeof(bool)
            },
            WorkInProgressData = new Dictionary<string, string>(),
            ExpectedBehavior = "All basic parameters should be preserved across any ViewMode transition"
        });
        
        // High: Advanced parameter handling
        _testCases.Add(new TransitionTestCase
        {
            Name = "Advanced Parameter Handling",
            Description = "Test handling of advanced parameters that may not be available in all ViewModes",
            Priority = TestPriority.High,
            TestParameters = new Dictionary<string, Type>
            {
                ["Mirostat"] = typeof(int),
                ["TailFreeSampling"] = typeof(double),
                ["TypicalP"] = typeof(double),
                ["LocallyTypical"] = typeof(double)
            },
            UIStateSettings = new Dictionary<string, Type>
            {
                ["AdvancedSectionExpanded"] = typeof(bool),
                ["ExpertSettingsVisible"] = typeof(bool)
            },
            WorkInProgressData = new Dictionary<string, string>(),
            ExpectedBehavior = "Advanced parameters should be preserved but may not be restored in Novice mode"
        });
        
        // High: Work-in-progress protection
        _testCases.Add(new TransitionTestCase
        {
            Name = "Work-in-Progress Protection",
            Description = "Test protection of unsaved work during ViewMode transitions",
            Priority = TestPriority.High,
            TestParameters = new Dictionary<string, Type>(),
            UIStateSettings = new Dictionary<string, Type>
            {
                ["ChatScrollPosition"] = typeof(double),
                ["SelectedTab"] = typeof(string)
            },
            WorkInProgressData = new Dictionary<string, string>
            {
                ["UnsavedPrompt"] = "User is composing a complex prompt",
                ["PartialConfiguration"] = "Model configuration in progress",
                ["DraftSettings"] = "Image generation settings being adjusted"
            },
            ExpectedBehavior = "All work-in-progress data should be preserved and accessible after transition"
        });
        
        // Medium: LoRA state synchronization
        _testCases.Add(new TransitionTestCase
        {
            Name = "LoRA State Synchronization",
            Description = "Test LoRA configuration preservation during ViewMode changes",
            Priority = TestPriority.Medium,
            TestParameters = new Dictionary<string, Type>
            {
                ["LoRA1_Weight"] = typeof(double),
                ["LoRA2_Weight"] = typeof(double),
                ["LoRA1_Enabled"] = typeof(bool),
                ["LoRA2_Enabled"] = typeof(bool)
            },
            UIStateSettings = new Dictionary<string, Type>
            {
                ["LoRATabExpanded"] = typeof(bool),
                ["LoRAListScrollPosition"] = typeof(double)
            },
            WorkInProgressData = new Dictionary<string, string>(),
            ExpectedBehavior = "LoRA states should be synchronized across ViewMode transitions"
        });
        
        // Medium: Complex UI state management
        _testCases.Add(new TransitionTestCase
        {
            Name = "Complex UI State Management",
            Description = "Test preservation of complex UI states during transitions",
            Priority = TestPriority.Medium,
            TestParameters = new Dictionary<string, Type>(),
            UIStateSettings = new Dictionary<string, Type>
            {
                ["MainScrollPosition"] = typeof(double),
                ["SidebarExpanded"] = typeof(bool),
                ["SelectedMainTab"] = typeof(string),
                ["SelectedSubTab"] = typeof(string),
                ["WindowWidth"] = typeof(double),
                ["WindowHeight"] = typeof(double),
                ["PanelSplitterPosition"] = typeof(double)
            },
            WorkInProgressData = new Dictionary<string, string>(),
            ExpectedBehavior = "All UI layout states should be preserved to maintain user context"
        });
        
        // Low: Error recovery testing
        _testCases.Add(new TransitionTestCase
        {
            Name = "Error Recovery Testing",
            Description = "Test system behavior when state preservation encounters errors",
            Priority = TestPriority.Low,
            TestParameters = new Dictionary<string, Type>
            {
                ["CorruptedParameter"] = typeof(string),
                ["InvalidValue"] = typeof(object)
            },
            UIStateSettings = new Dictionary<string, Type>
            {
                ["InvalidUIState"] = typeof(object)
            },
            WorkInProgressData = new Dictionary<string, string>
            {
                ["CorruptedWork"] = "This work item contains invalid data"
            },
            ExpectedBehavior = "System should handle errors gracefully and preserve what it can"
        });
    }
    
    #region Helper Methods
    
    private List<ViewModeTransition> GetAllPossibleTransitions()
    {
        var transitions = new List<ViewModeTransition>();
        var viewModes = new[] { ViewMode.Novice, ViewMode.Enthusiast, ViewMode.Developer };
        
        foreach (var from in viewModes)
        {
            foreach (var to in viewModes)
            {
                if (from != to)
                {
                    transitions.Add(new ViewModeTransition { From = from, To = to });
                }
            }
        }
        
        return transitions;
    }
    
    private object GenerateTestValue(Type valueType)
    {
        var random = new Random();
        
        return valueType.Name switch
        {
            nameof(Double) => 0.5 + (random.NextDouble() * 1.0), // 0.5 - 1.5
            nameof(Int32) => random.Next(1, 100),
            nameof(Boolean) => random.Next(0, 2) == 1,
            nameof(String) => $"TestValue_{Guid.NewGuid():N}[..8]",
            _ => $"TestObject_{valueType.Name}"
        };
    }
    
    private object GenerateUIStateValue(Type valueType)
    {
        var random = new Random();
        
        return valueType.Name switch
        {
            nameof(Double) => random.NextDouble() * 1000, // Scroll positions, sizes, etc.
            nameof(Boolean) => random.Next(0, 2) == 1,
            nameof(String) => $"Tab_{random.Next(1, 5)}",
            _ => $"UIState_{valueType.Name}"
        };
    }
    
    private object GenerateWorkData(string workType)
    {
        return workType switch
        {
            "UnsavedPrompt" => $"Test prompt content created at {DateTime.Now:HH:mm:ss}",
            "PartialConfiguration" => new { Temperature = 0.7, TopP = 0.9, Modified = DateTime.Now },
            "DraftSettings" => new { Width = 512, Height = 768, Quality = "High", Created = DateTime.Now },
            _ => $"Generic work data for {workType}"
        };
    }
    
    private bool AreValuesEqual(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return true;
        if (value1 == null || value2 == null) return false;
        
        // Handle floating point comparison with tolerance
        if (value1 is double d1 && value2 is double d2)
        {
            return Math.Abs(d1 - d2) < 0.0001;
        }
        
        return value1.Equals(value2);
    }
    
    private List<string> GetParametersForCurrentViewMode()
    {
        var currentViewMode = _preferencesService.CurrentViewMode;
        
        return currentViewMode switch
        {
            ViewMode.Novice => new List<string> { "Temperature", "TopP" },
            ViewMode.Enthusiast => new List<string> { "Temperature", "TopP", "TopK", "RepetitionPenalty" },
            ViewMode.Developer => new List<string> { "Temperature", "TopP", "TopK", "RepetitionPenalty", "Mirostat", "TailFreeSampling", "TypicalP" },
            _ => new List<string>()
        };
    }
    
    private TransitionAnalysis AnalyzeValidationResults(List<TransitionTestResult> testResults)
    {
        var analysis = new TransitionAnalysis
        {
            TotalTests = testResults.Count,
            SuccessfulTests = testResults.Count(r => r.Success),
            FailedTests = testResults.Count(r => !r.Success),
            AverageTransitionTime = testResults.Where(r => r.TransitionDuration != null)
                .Average(r => r.TransitionDuration!.Value.TotalMilliseconds),
            CriticalFailures = testResults.Where(r => !r.Success && 
                _testCases.Any(tc => tc.Name == r.TestCaseName && tc.Priority == TestPriority.Critical))
                .ToList()
        };
        
        // Generate insights
        analysis.Insights = GenerateInsights(analysis, testResults);
        
        return analysis;
    }
    
    private List<string> GenerateInsights(TransitionAnalysis analysis, List<TransitionTestResult> testResults)
    {
        var insights = new List<string>();
        
        // Overall success rate
        var successRate = (double)analysis.SuccessfulTests / analysis.TotalTests;
        if (successRate >= 0.95)
        {
            insights.Add($"Excellent data preservation: {successRate:P1} of transitions successful");
        }
        else if (successRate >= 0.80)
        {
            insights.Add($"Good data preservation: {successRate:P1} success rate, minor improvements needed");
        }
        else
        {
            insights.Add($"Data preservation issues detected: Only {successRate:P1} success rate");
        }
        
        // Critical failure analysis
        if (analysis.CriticalFailures.Count > 0)
        {
            insights.Add($"CRITICAL: {analysis.CriticalFailures.Count} critical test failures require immediate attention");
        }
        
        // Transition speed analysis
        if (analysis.AverageTransitionTime < 500)
        {
            insights.Add($"Fast transitions: Average {analysis.AverageTransitionTime:F0}ms transition time");
        }
        else if (analysis.AverageTransitionTime < 1000)
        {
            insights.Add($"Acceptable transition speed: {analysis.AverageTransitionTime:F0}ms average");
        }
        else
        {
            insights.Add($"Slow transitions detected: {analysis.AverageTransitionTime:F0}ms average - optimization recommended");
        }
        
        // ViewMode-specific analysis
        var transitionFailures = testResults
            .Where(r => !r.Success)
            .GroupBy(r => $"{r.FromViewMode}→{r.ToViewMode}")
            .Where(g => g.Count() > 1)
            .ToList();
        
        foreach (var failure in transitionFailures)
        {
            insights.Add($"Transition issues: {failure.Key} failed {failure.Count()} tests");
        }
        
        return insights;
    }
    
    #endregion
}

#region Data Structures

public class ViewModeTransition
{
    public ViewMode From { get; set; }
    public ViewMode To { get; set; }
    
    public override string ToString() => $"{From} → {To}";
}

public class TransitionTestCase
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public TestPriority Priority { get; set; }
    public Dictionary<string, Type> TestParameters { get; set; } = new();
    public Dictionary<string, Type> UIStateSettings { get; set; } = new();
    public Dictionary<string, string> WorkInProgressData { get; set; } = new();
    public string ExpectedBehavior { get; set; } = "";
}

public enum TestPriority
{
    Critical = 1,
    High = 2,
    Medium = 3,
    Low = 4
}

public class TestDataState
{
    public string TestCaseName { get; set; } = "";
    public DateTime SetupTime { get; set; }
    public Dictionary<string, object> ParameterValues { get; set; } = new();
    public Dictionary<string, object> UIState { get; set; } = new();
    public Dictionary<string, WorkInProgressData> WorkInProgressItems { get; set; } = new();
}

public class TransitionTestResult
{
    public string TestCaseName { get; set; } = "";
    public ViewMode FromViewMode { get; set; }
    public ViewMode ToViewMode { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan? TransitionDuration { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public TestDataState? InitialData { get; set; }
    public List<ValidationResult> ValidationResults { get; set; } = new();
    public List<ValidationResult> RestorationResults { get; set; } = new();
}

public class ValidationResult
{
    public string TestType { get; set; } = "";
    public string ItemName { get; set; } = "";
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? Details { get; set; }
}

public class ViewModeTransitionReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public List<TransitionTestResult> TestCases { get; set; } = new();
    public TransitionAnalysis? Analysis { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TransitionAnalysis
{
    public int TotalTests { get; set; }
    public int SuccessfulTests { get; set; }
    public int FailedTests { get; set; }
    public double AverageTransitionTime { get; set; }
    public List<TransitionTestResult> CriticalFailures { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    
    public double SuccessRate => TotalTests > 0 ? (double)SuccessfulTests / TotalTests : 0;
}

#endregion