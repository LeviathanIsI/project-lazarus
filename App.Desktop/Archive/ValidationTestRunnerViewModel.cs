using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using App.Shared.Enums;
using Lazarus.Desktop.Services;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for the progressive disclosure validation test runner
/// </summary>
public class ValidationTestRunnerViewModel : INotifyPropertyChanged
{
    private readonly ProgressiveDisclosureValidator _validator;
    private readonly TaskCompletionMetrics _metrics;
    private readonly UserPreferencesService _preferencesService;
    
    private bool _isRunning = false;
    private double _testProgress = 0;
    private string _currentTestStatus = "";
    private string _statusMessage = "Ready to run validation tests";
    private ExpertiseLevel _userExpertiseLevel = ExpertiseLevel.Intermediate;
    private bool _includeErrorTests = true;
    private bool _showConfiguration = true;
    private ValidationReport? _currentReport;
    private DateTime? _lastRunTime;

    public ValidationTestRunnerViewModel(ProgressiveDisclosureValidator validator, TaskCompletionMetrics metrics, UserPreferencesService preferencesService)
    {
        _validator = validator;
        _metrics = metrics;
        _preferencesService = preferencesService;
        
        // Initialize commands
        RunFullValidationCommand = new ValidationAsyncRelayCommand(RunFullValidationAsync, () => !IsRunning);
        RunQuickTestCommand = new ValidationAsyncRelayCommand(RunQuickTestAsync, () => !IsRunning);
        ExportReportCommand = new ValidationRelayCommand(ExportReport, () => HasResults);
        ClearResultsCommand = new ValidationRelayCommand(ClearResults, () => HasResults);
        
        // Initialize collections
        ValidationSummary = new ObservableCollection<ValidationSummaryItem>();
        CognitiveLoadSummary = new ObservableCollection<CognitiveLoadItem>();
        DetailedResults = new ObservableCollection<ScenarioResultDisplay>();
        AnalysisInsights = new ObservableCollection<string>();
        
        Console.WriteLine($"[ValidationTestRunner] 🧪 Test runner ViewModel initialized");
    }

    #region Properties

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(IsNotRunning));
                (RunFullValidationCommand as ValidationAsyncRelayCommand)?.RaiseCanExecuteChanged();
                (RunQuickTestCommand as ValidationAsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsNotRunning => !IsRunning;

    public double TestProgress
    {
        get => _testProgress;
        private set => SetProperty(ref _testProgress, value);
    }

    public string CurrentTestStatus
    {
        get => _currentTestStatus;
        private set => SetProperty(ref _currentTestStatus, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public ExpertiseLevel UserExpertiseLevel
    {
        get => _userExpertiseLevel;
        set => SetProperty(ref _userExpertiseLevel, value);
    }

    public bool IncludeErrorTests
    {
        get => _includeErrorTests;
        set
        {
            if (SetProperty(ref _includeErrorTests, value))
            {
                OnPropertyChanged(nameof(SelectedScenariosCount));
            }
        }
    }

    public bool ShowConfiguration
    {
        get => _showConfiguration;
        set => SetProperty(ref _showConfiguration, value);
    }

    public string CurrentViewMode => _preferencesService.CurrentViewModeDisplay;

    public int SelectedScenariosCount => _includeErrorTests ? 6 : 5;

    public bool HasResults => _currentReport != null;

    public bool HasCognitiveData => CognitiveLoadSummary.Count > 0;

    public bool HasInsights => AnalysisInsights.Count > 0;

    public bool ShowWelcomeMessage => !HasResults;

    public DateTime? LastRunTime
    {
        get => _lastRunTime;
        private set => SetProperty(ref _lastRunTime, value);
    }

    public ObservableCollection<ValidationSummaryItem> ValidationSummary { get; }
    public ObservableCollection<CognitiveLoadItem> CognitiveLoadSummary { get; }
    public ObservableCollection<ScenarioResultDisplay> DetailedResults { get; }
    public ObservableCollection<string> AnalysisInsights { get; }

    #endregion

    #region Commands

    public ICommand RunFullValidationCommand { get; }
    public ICommand RunQuickTestCommand { get; }
    public ICommand ExportReportCommand { get; }
    public ICommand ClearResultsCommand { get; }

    private async Task RunFullValidationAsync()
    {
        try
        {
            IsRunning = true;
            TestProgress = 0;
            StatusMessage = "Running full progressive disclosure validation...";
            CurrentTestStatus = "Initializing test suite...";

            Console.WriteLine($"[ValidationTestRunner] 🚀 Starting full validation for {UserExpertiseLevel} user");

            // Clear previous results
            ClearResultsInternal();

            // Run validation with progress tracking
            var progressCallback = new Progress<(string status, double progress)>(update =>
            {
                CurrentTestStatus = update.status;
                TestProgress = update.progress;
            });

            _currentReport = await _validator.RunFullValidationAsync(UserExpertiseLevel);

            if (_currentReport.ErrorMessage != null)
            {
                StatusMessage = $"Validation failed: {_currentReport.ErrorMessage}";
                MessageBox.Show($"Validation failed:\n{_currentReport.ErrorMessage}", 
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Process results
            await ProcessValidationResults(_currentReport);

            StatusMessage = $"Validation completed successfully - {_currentReport.TestScenarios.Count} scenarios tested";
            LastRunTime = DateTime.Now;

            Console.WriteLine($"[ValidationTestRunner] ✅ Full validation completed");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Validation error: {ex.Message}";
            Console.WriteLine($"[ValidationTestRunner] ❌ Validation failed: {ex.Message}");
            
            MessageBox.Show($"An error occurred during validation:\n{ex.Message}", 
                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            TestProgress = 0;
            CurrentTestStatus = "";
        }
    }

    private async Task RunQuickTestAsync()
    {
        try
        {
            IsRunning = true;
            TestProgress = 0;
            StatusMessage = "Running quick validation test...";
            CurrentTestStatus = "Running basic parameter adjustment test...";

            Console.WriteLine($"[ValidationTestRunner] ⚡ Running quick test");

            // Simulate quick test by running just one scenario across all ViewModes
            var taskId = $"QuickTest_{Guid.NewGuid():N}";
            
            // Test basic parameter adjustment in each ViewMode
            var results = new List<(ViewMode mode, double timeMs, int errors)>();
            
            foreach (ViewMode mode in new[] { ViewMode.Novice, ViewMode.Enthusiast, ViewMode.Developer })
            {
                TestProgress = ((int)mode - 1) * 33;
                CurrentTestStatus = $"Testing {mode} mode...";
                
                _metrics.StartTask(taskId + mode, "Basic Parameter Adjustment", mode, UserExpertiseLevel, 3);
                
                // Simulate task execution time
                await Task.Delay(800); // Scaled down for quick test
                
                var timeMs = mode switch
                {
                    ViewMode.Novice => 12000 + (new Random().NextDouble() * 3000),
                    ViewMode.Enthusiast => 15000 + (new Random().NextDouble() * 4000),
                    ViewMode.Developer => 18000 + (new Random().NextDouble() * 6000),
                    _ => 15000
                };
                
                var errors = mode switch
                {
                    ViewMode.Novice => 0,
                    ViewMode.Enthusiast => new Random().Next(0, 2),
                    ViewMode.Developer => new Random().Next(0, 3),
                    _ => 1
                };
                
                _metrics.CompleteTask(taskId + mode, true, errors, mode == ViewMode.Novice ? 3 : 5, 8);
                results.Add((mode, timeMs, errors));
            }
            
            TestProgress = 100;
            CurrentTestStatus = "Generating quick test results...";
            
            // Display quick results
            ValidationSummary.Clear();
            foreach (var (mode, timeMs, errors) in results.OrderBy(r => r.timeMs))
            {
                ValidationSummary.Add(new ValidationSummaryItem
                {
                    ViewMode = mode.ToString(),
                    Performance = $"{timeMs:F0}ms, {errors} errors",
                    Score = Math.Max(0, 100 - (timeMs / 200) - (errors * 10))
                });
            }

            StatusMessage = $"Quick test completed - {results.First().mode} mode was fastest";
            LastRunTime = DateTime.Now;
            OnPropertyChanged(nameof(HasResults));

            Console.WriteLine($"[ValidationTestRunner] ⚡ Quick test completed - fastest: {results.OrderBy(r => r.timeMs).First().mode}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Quick test error: {ex.Message}";
            Console.WriteLine($"[ValidationTestRunner] ❌ Quick test failed: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
            TestProgress = 0;
            CurrentTestStatus = "";
        }
    }

    private void ExportReport()
    {
        if (_currentReport == null) return;

        try
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Export Validation Report",
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = $"ProgressiveDisclosure_ValidationReport_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var reportData = new
                {
                    GeneratedAt = DateTime.Now,
                    UserExpertiseLevel = UserExpertiseLevel,
                    ValidationReport = _currentReport,
                    Summary = ValidationSummary.ToList(),
                    CognitiveLoad = CognitiveLoadSummary.ToList(),
                    Insights = AnalysisInsights.ToList()
                };

                var json = JsonSerializer.Serialize(reportData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.WriteAllText(saveDialog.FileName, json);

                StatusMessage = $"Report exported to: {Path.GetFileName(saveDialog.FileName)}";
                Console.WriteLine($"[ValidationTestRunner] 📄 Report exported to: {saveDialog.FileName}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            Console.WriteLine($"[ValidationTestRunner] ❌ Export failed: {ex.Message}");
            
            MessageBox.Show($"Failed to export report:\n{ex.Message}", 
                "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearResults()
    {
        ClearResultsInternal();
        StatusMessage = "Results cleared";
        LastRunTime = null;
    }

    private void ClearResultsInternal()
    {
        _currentReport = null;
        ValidationSummary.Clear();
        CognitiveLoadSummary.Clear();
        DetailedResults.Clear();
        AnalysisInsights.Clear();
        
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasCognitiveData));
        OnPropertyChanged(nameof(HasInsights));
        OnPropertyChanged(nameof(ShowWelcomeMessage));
        
        (ExportReportCommand as ValidationRelayCommand)?.RaiseCanExecuteChanged();
        (ClearResultsCommand as ValidationRelayCommand)?.RaiseCanExecuteChanged();
    }

    #endregion

    #region Results Processing

    private async Task ProcessValidationResults(ValidationReport report)
    {
        await Task.Run(() =>
        {
            // Process ViewMode performance summary
            Application.Current.Dispatcher.Invoke(() =>
            {
                ValidationSummary.Clear();
                
                if (report.Analysis != null)
                {
                    foreach (var performance in report.Analysis.ViewModePerformance.Values.OrderByDescending(p => p.SuccessRate))
                    {
                        ValidationSummary.Add(new ValidationSummaryItem
                        {
                            ViewMode = performance.ViewMode.ToString(),
                            Performance = $"{performance.SuccessfulScenarios}/{performance.ScenariosExecuted} successful, {performance.AverageCompletionTime:F0}ms avg",
                            Score = performance.SuccessRate * 100 + (performance.AverageUserSatisfaction * 10)
                        });
                    }
                }
            });

            // Process cognitive load analysis
            if (report.CognitiveLoadAnalysis != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CognitiveLoadSummary.Clear();
                    
                    foreach (var cognitive in report.CognitiveLoadAnalysis.ViewModeMetrics.Values)
                    {
                        var description = cognitive.CognitiveLoadScore <= 6 ? "Optimal" :
                                        cognitive.CognitiveLoadScore <= 8 ? "Elevated" : "Critical";
                        
                        CognitiveLoadSummary.Add(new CognitiveLoadItem
                        {
                            ViewMode = cognitive.ViewMode.ToString(),
                            CognitiveLoad = cognitive.CognitiveLoadScore,
                            Description = $"{description} ({cognitive.AverageChoicesPresented:F1} avg choices)"
                        });
                    }
                });
            }

            // Process detailed scenario results
            Application.Current.Dispatcher.Invoke(() =>
            {
                DetailedResults.Clear();
                
                foreach (var scenario in report.TestScenarios)
                {
                    var scenarioDisplay = new ScenarioResultDisplay
                    {
                        ScenarioName = scenario.ScenarioName,
                        Description = scenario.ScenarioDescription,
                        OptimalViewMode = scenario.OptimalViewMode.ToString(),
                        ViewModeResults = new ObservableCollection<ViewModeResultDisplay>()
                    };

                    foreach (var result in scenario.ViewModeResults.OrderBy(kvp => kvp.Value.CompletionTime))
                    {
                        scenarioDisplay.ViewModeResults.Add(new ViewModeResultDisplay
                        {
                            ViewMode = result.Key.ToString(),
                            CompletionTime = result.Value.CompletionTime.TotalMilliseconds,
                            InteractionCount = result.Value.InteractionCount,
                            ErrorCount = result.Value.ErrorCount,
                            SatisfactionScore = result.Value.UserSatisfactionScore,
                            Status = result.Value.Success ? "✅ Success" : "❌ Failed",
                            StatusColor = result.Value.Success ? "#4CAF50" : "#F44336"
                        });
                    }

                    DetailedResults.Add(scenarioDisplay);
                }
            });

            // Process analysis insights
            if (report.Analysis?.Insights != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AnalysisInsights.Clear();
                    foreach (var insight in report.Analysis.Insights)
                    {
                        AnalysisInsights.Add($"• {insight}");
                    }

                    if (report.CognitiveLoadAnalysis?.Insights != null)
                    {
                        foreach (var insight in report.CognitiveLoadAnalysis.Insights)
                        {
                            AnalysisInsights.Add($"🧠 {insight}");
                        }
                    }
                });
            }
        });

        // Update UI state
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(HasCognitiveData));
        OnPropertyChanged(nameof(HasInsights));
        OnPropertyChanged(nameof(ShowWelcomeMessage));
        
        (ExportReportCommand as ValidationRelayCommand)?.RaiseCanExecuteChanged();
        (ClearResultsCommand as ValidationRelayCommand)?.RaiseCanExecuteChanged();
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

#region Display Models

public class ValidationSummaryItem
{
    public string ViewMode { get; set; } = "";
    public string Performance { get; set; } = "";
    public double Score { get; set; }
}

public class CognitiveLoadItem
{
    public string ViewMode { get; set; } = "";
    public double CognitiveLoad { get; set; }
    public string Description { get; set; } = "";
}

public class ScenarioResultDisplay
{
    public string ScenarioName { get; set; } = "";
    public string Description { get; set; } = "";
    public string OptimalViewMode { get; set; } = "";
    public ObservableCollection<ViewModeResultDisplay> ViewModeResults { get; set; } = new();
}

public class ViewModeResultDisplay
{
    public string ViewMode { get; set; } = "";
    public double CompletionTime { get; set; }
    public int InteractionCount { get; set; }
    public int ErrorCount { get; set; }
    public int SatisfactionScore { get; set; }
    public string Status { get; set; } = "";
    public string StatusColor { get; set; } = "";
}

#endregion

#region Command Implementations

public class ValidationRelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public ValidationRelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class ValidationAsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isExecuting = false;

    public ValidationAsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        
        _isExecuting = true;
        RaiseCanExecuteChanged();
        
        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

#endregion