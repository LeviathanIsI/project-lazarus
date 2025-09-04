using Microsoft.Extensions.Logging;
using Lazarus.App.Desktop.ViewModels;
using Lazarus.App.Desktop.Utilities;

namespace Lazarus.App.Desktop.Diagnostics;

/// <summary>
/// Generates comprehensive threading validation reports for all ViewModels
/// </summary>
public class ThreadingValidationReport
{
    private readonly ILogger<ThreadingValidationReport> _logger;

    public ThreadingValidationReport(ILogger<ThreadingValidationReport> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs comprehensive threading validation across all ViewModels
    /// </summary>
    /// <returns>A comprehensive validation report</returns>
    public ValidationReport GenerateReport()
    {
        _logger.LogInformation("Starting comprehensive threading validation analysis...");
        
        var report = new ValidationReport
        {
            GeneratedAt = DateTime.UtcNow,
            ViewModelResults = new List<ViewModelValidationResult>()
        };

        // Test key ViewModels for threading violations
        var viewModelTypes = new[]
        {
            typeof(MainWindowViewModel),
            typeof(ConversationsViewModel),
            typeof(DashboardViewModel),
            typeof(ModelConfigurationViewModel),
            typeof(RunnerManagerViewModel)
        };

        foreach (var viewModelType in viewModelTypes)
        {
            try
            {
                var result = ValidateViewModelType(viewModelType);
                report.ViewModelResults.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate ViewModel type {ViewModelType}", viewModelType.Name);
                report.ViewModelResults.Add(new ViewModelValidationResult
                {
                    ViewModelName = viewModelType.Name,
                    IsThreadSafe = false,
                    Violations = new[] { $"Validation failed: {ex.Message}" },
                    HasProperDisposal = false
                });
            }
        }

        // Generate summary
        report.TotalViewModels = report.ViewModelResults.Count;
        report.ThreadSafeViewModels = report.ViewModelResults.Count(r => r.IsThreadSafe);
        report.ViewModelsWithProperDisposal = report.ViewModelResults.Count(r => r.HasProperDisposal);
        report.OverallScore = report.TotalViewModels > 0 
            ? (double)report.ThreadSafeViewModels / report.TotalViewModels * 100.0 
            : 0.0;

        _logger.LogInformation(
            "Threading validation completed: {ThreadSafe}/{Total} ViewModels are thread-safe ({Score:F1}%)",
            report.ThreadSafeViewModels,
            report.TotalViewModels,
            report.OverallScore);

        return report;
    }

    /// <summary>
    /// Validates a specific ViewModel type for threading violations
    /// </summary>
    /// <param name="viewModelType">The ViewModel type to validate</param>
    /// <returns>Validation results</returns>
    private ViewModelValidationResult ValidateViewModelType(Type viewModelType)
    {
        var result = new ViewModelValidationResult
        {
            ViewModelName = viewModelType.Name,
            Violations = new List<string>()
        };

        // Check for ObservableCollection usage
        var observableCollectionProperties = viewModelType.GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                       p.PropertyType.GetGenericTypeDefinition() == typeof(System.Collections.ObjectModel.ObservableCollection<>))
            .ToList();

        if (observableCollectionProperties.Any())
        {
            foreach (var prop in observableCollectionProperties)
            {
                // Check if it's wrapped with thread safety measures
                var isThreadSafeWrapped = CheckForThreadSafetyMeasures(viewModelType, prop.Name);
                if (!isThreadSafeWrapped)
                {
                    result.Violations.Add($"Property '{prop.Name}' uses raw ObservableCollection without thread safety measures");
                }
            }
        }

        // Check for proper IDisposable implementation
        result.HasProperDisposal = typeof(IDisposable).IsAssignableFrom(viewModelType);
        if (!result.HasProperDisposal)
        {
            result.Violations.Add("ViewModel does not implement IDisposable for proper resource cleanup");
        }

        // Check for DispatcherTimer usage
        var timerFields = viewModelType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(System.Windows.Threading.DispatcherTimer))
            .ToList();

        if (timerFields.Any())
        {
            // Good - using DispatcherTimer instead of regular Timer
            _logger.LogDebug("ViewModel {ViewModelType} correctly uses DispatcherTimer", viewModelType.Name);
        }

        // Check for System.Timer usage (bad)
        var systemTimerFields = viewModelType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(System.Timers.Timer) || f.FieldType == typeof(System.Threading.Timer))
            .ToList();

        foreach (var field in systemTimerFields)
        {
            result.Violations.Add($"Field '{field.Name}' uses {field.FieldType.Name} instead of DispatcherTimer");
        }

        // Check for BaseViewModel inheritance (good pattern)
        var inheritsFromBaseViewModel = typeof(BaseViewModel).IsAssignableFrom(viewModelType);
        if (inheritsFromBaseViewModel)
        {
            _logger.LogDebug("ViewModel {ViewModelType} correctly inherits from BaseViewModel", viewModelType.Name);
        }
        else
        {
            result.Violations.Add("ViewModel does not inherit from thread-safe BaseViewModel");
        }

        result.IsThreadSafe = result.Violations.Count == 0;
        
        return result;
    }

    /// <summary>
    /// Checks if a property has thread safety measures in place
    /// </summary>
    /// <param name="type">The type containing the property</param>
    /// <param name="propertyName">The property name</param>
    /// <returns>True if thread safety measures are detected</returns>
    private bool CheckForThreadSafetyMeasures(Type type, string propertyName)
    {
        // Look for usage patterns that indicate thread safety
        // This is a simplified check - in reality, we'd need more sophisticated analysis
        
        // Check if the type name suggests thread safety
        var property = type.GetProperty(propertyName);
        if (property == null) return false;

        // Check if it's our ThreadSafeObservableCollection
        if (property.PropertyType.Name.Contains("ThreadSafe"))
        {
            return true;
        }

        // Check for UI thread enforcement in methods that might modify the collection
        var methods = type.GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var hasUIThreadEnforcement = methods.Any(m => 
            m.Name.Contains("ExecuteOnUIThread") || 
            m.Name.Contains("Dispatcher"));

        return hasUIThreadEnforcement;
    }
}

/// <summary>
/// Comprehensive validation report for threading safety
/// </summary>
public class ValidationReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalViewModels { get; set; }
    public int ThreadSafeViewModels { get; set; }
    public int ViewModelsWithProperDisposal { get; set; }
    public double OverallScore { get; set; }
    public List<ViewModelValidationResult> ViewModelResults { get; set; } = new();

    /// <summary>
    /// Gets a summary of the validation results
    /// </summary>
    public string GetSummary()
    {
        return $"Threading Validation Report\n" +
               $"Generated: {GeneratedAt:yyyy-MM-dd HH:mm:ss}\n" +
               $"Overall Score: {OverallScore:F1}%\n" +
               $"Thread-Safe ViewModels: {ThreadSafeViewModels}/{TotalViewModels}\n" +
               $"Proper Disposal: {ViewModelsWithProperDisposal}/{TotalViewModels}\n\n" +
               $"Detailed Results:\n" +
               string.Join("\n", ViewModelResults.Select(r => 
                   $"- {r.ViewModelName}: {(r.IsThreadSafe ? "✓" : "✗")} " +
                   $"({r.Violations.Count} violations)"));
    }
}

/// <summary>
/// Validation results for a specific ViewModel
/// </summary>
public class ViewModelValidationResult
{
    public string ViewModelName { get; set; } = string.Empty;
    public bool IsThreadSafe { get; set; }
    public bool HasProperDisposal { get; set; }
    public ICollection<string> Violations { get; set; } = new List<string>();
}