using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Controls;
using Lazarus.Shared.Models;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel that adapts to actual model capabilities instead of static forms
/// </summary>
public class DynamicParameterViewModel : INotifyPropertyChanged
{
    private ModelCapabilities? _currentCapabilities;
    private StackPanel? _dynamicUI;
    private string _currentModelName = "";
    private List<AppliedLoRAInfo> _appliedLoRAs = new();
    
    // Dynamic parameter properties - these get created based on model capabilities
    private readonly Dictionary<string, object> _parameterValues = new();
    
    public DynamicParameterViewModel()
    {
        // Subscribe to cross-tab LoRA state changes for real-time synchronization
        LorAsViewModel.LoRAStateChanged += OnLoRAStateChanged;
        
        Console.WriteLine("[DynamicParameterViewModel] Subscribed to cross-tab LoRA state changes");
    }
    
    public ModelCapabilities? CurrentCapabilities
    {
        get => _currentCapabilities;
        private set
        {
            if (_currentCapabilities != value)
            {
                _currentCapabilities = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCapabilities));
                OnPropertyChanged(nameof(ModelSummary));
            }
        }
    }
    
    public StackPanel? DynamicUI
    {
        get => _dynamicUI;
        private set
        {
            if (_dynamicUI != value)
            {
                _dynamicUI = value;
                OnPropertyChanged();
            }
        }
    }
    
    public bool HasCapabilities => CurrentCapabilities != null;
    
    public string ModelSummary => CurrentCapabilities != null 
        ? $"{CurrentCapabilities.ModelName} • {CurrentCapabilities.AvailableParameters.Count} adjustable parameters" + 
          (CurrentCapabilities.HasActiveLoRAs ? $" • {CurrentCapabilities.AppliedLoRAs.Count(l => l.IsEnabled)} active LoRA(s)" : "")
        : "No model introspection data available";
        
    /// <summary>
    /// Currently applied LoRAs that affect model parameters
    /// </summary>
    public List<AppliedLoRAInfo> AppliedLoRAs
    {
        get => _appliedLoRAs;
        set
        {
            if (_appliedLoRAs != value)
            {
                _appliedLoRAs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasActiveLoRAs));
                OnPropertyChanged(nameof(LoRAStatusSummary));
                
                // Refresh capabilities when LoRAs change
                _ = RefreshCapabilitiesAsync();
            }
        }
    }
    
    /// <summary>
    /// Whether any LoRAs are currently active - references live LoRA data for real-time updates
    /// </summary>
    public bool HasActiveLoRAs
    {
        get
        {
            // First try to get live data from the main LoRA tab
            // This ensures real-time synchronization with checkbox changes
            try
            {
                // Get real-time active LoRA count from the main LoRA tab
                var hasActiveFromTab = LorAsViewModel.GetCurrentAppliedActiveCount() > 0;
                if (hasActiveFromTab != (_appliedLoRAs.Any(l => l.IsEnabled)))
                {
                    Console.WriteLine($"[DynamicParameterViewModel] LoRA state mismatch detected! Tab: {hasActiveFromTab}, Local: {_appliedLoRAs.Any(l => l.IsEnabled)}");
                }
                return hasActiveFromTab;
            }
            catch
            {
                // Fallback to local collection if main tab is not accessible
                return _appliedLoRAs.Any(l => l.IsEnabled);
            }
        }
    }
    
    /// <summary>
    /// Summary of LoRA influence on parameters - uses live data for consistency
    /// </summary>
    public string LoRAStatusSummary
    {
        get
        {
            try
            {
                // Get live LoRA data from the main LoRA tab
                var liveLoRAs = LorAsViewModel.GetCurrentAppliedLoRAs();
                if (liveLoRAs == null || liveLoRAs.Count == 0) 
                    return "No LoRAs loaded into model";
                
                var activeLoRAs = liveLoRAs.Where(l => l.IsEnabled).ToList();
                var inactiveLoRAs = liveLoRAs.Where(l => !l.IsEnabled).ToList();
                
                if (activeLoRAs.Count == 0)
                {
                    return inactiveLoRAs.Count == 1 
                        ? "1 LoRA available but inactive - model running base configuration"
                        : $"{inactiveLoRAs.Count} LoRAs available but all inactive - model running base configuration";
                }
                
                // CONSISTENT MESSAGING: If HasActiveLoRAs is true, celebrate it!
                var totalWeight = activeLoRAs.Sum(l => l.Weight);
                
                if (activeLoRAs.Count == 1)
                {
                    return $"Model enhanced with 1 adapter (weight: {totalWeight:F2})";
                }
                else
                {
                    var summary = $"{activeLoRAs.Count} LoRAs loaded into model (total weight: {totalWeight:F2})";
                    
                    if (inactiveLoRAs.Count > 0)
                    {
                        summary += $" • {inactiveLoRAs.Count} inactive";
                    }
                    
                    return summary;
                }
            }
            catch
            {
                // Fallback to local collection if live data is not accessible
                return _appliedLoRAs.Count == 0 
                    ? "No LoRAs loaded into model"
                    : $"LoRA status updating...";
            }
        }
    }

    /// <summary>
    /// Load and introspect a model to discover its actual capabilities
    /// </summary>
    public async Task<bool> LoadModelCapabilitiesAsync(string modelName = "current")
    {
        Console.WriteLine($"[DynamicParameterViewModel] Loading capabilities for: {modelName}");
        
        try
        {
            _currentModelName = modelName;
            
            // CRITICAL: Query orchestrator for currently active LoRAs first
            Console.WriteLine("[DynamicParameterViewModel] Querying orchestrator for active LoRAs...");
            var orchestratorLoRAs = await ApiClient.GetAppliedLoRAsAsync() ?? new List<AppliedLoRAInfo>();
            
            // Sync local LoRA state with orchestrator truth
            if (orchestratorLoRAs.Count > 0)
            {
                Console.WriteLine($"[DynamicParameterViewModel] Found {orchestratorLoRAs.Count} active LoRAs in orchestrator");
                _appliedLoRAs = orchestratorLoRAs.ToList();
                OnPropertyChanged(nameof(AppliedLoRAs));
                OnPropertyChanged(nameof(HasActiveLoRAs));
                OnPropertyChanged(nameof(LoRAStatusSummary));
                
                // Use LoRA-aware capabilities endpoint
                var capabilities = await ApiClient.GetModelCapabilitiesWithLoRAsAsync(modelName, orchestratorLoRAs);
                if (capabilities == null)
                {
                    Console.WriteLine("[DynamicParameterViewModel] LoRA-aware capabilities failed, falling back to basic");
                    capabilities = await ApiClient.GetModelCapabilitiesAsync(modelName);
                }
                else
                {
                    Console.WriteLine($"[DynamicParameterViewModel] ✅ Loaded LoRA-aware capabilities with {capabilities.AppliedLoRAs.Count} LoRA modifications");
                }
                
                CurrentCapabilities = capabilities;
            }
            else
            {
                Console.WriteLine("[DynamicParameterViewModel] No active LoRAs found, using base model capabilities");
                _appliedLoRAs.Clear();
                OnPropertyChanged(nameof(AppliedLoRAs));
                OnPropertyChanged(nameof(HasActiveLoRAs));
                OnPropertyChanged(nameof(LoRAStatusSummary));
                
                // Use basic capabilities endpoint
                var capabilities = await ApiClient.GetModelCapabilitiesAsync(modelName);
                CurrentCapabilities = capabilities;
            }
            
            if (CurrentCapabilities == null)
            {
                Console.WriteLine("[DynamicParameterViewModel] No capabilities returned from API");
                return false;
            }
            
            Console.WriteLine($"[DynamicParameterViewModel] Loaded {CurrentCapabilities.AvailableParameters.Count} parameters for {CurrentCapabilities.ModelName}");
            
            // Initialize parameter values with model-recommended defaults
            InitializeParameterValues();
            
            // Generate new dynamic UI
            await RegenerateUIAsync();
            
            Console.WriteLine($"[DynamicParameterViewModel] ✅ Dynamic UI regenerated successfully with LoRA state synchronized");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DynamicParameterViewModel] LoadModelCapabilities failed: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Refresh capabilities when LoRA configuration changes
    /// </summary>
    public async Task RefreshCapabilitiesAsync()
    {
        if (!string.IsNullOrEmpty(_currentModelName))
        {
            await LoadModelCapabilitiesAsync(_currentModelName);
        }
    }
    
    /// <summary>
    /// Update applied LoRAs from the LoRA management system
    /// </summary>
    public void UpdateAppliedLoRAs(List<AppliedLoRAInfo> appliedLoRAs)
    {
        AppliedLoRAs = appliedLoRAs;
    }

    /// <summary>
    /// Initialize parameter values with model-recommended defaults
    /// </summary>
    private void InitializeParameterValues()
    {
        if (CurrentCapabilities == null) return;
        
        Console.WriteLine("[DynamicParameterViewModel] Initializing parameter values with model-optimized defaults");
        
        _parameterValues.Clear();
        
        foreach (var (paramName, capability) in CurrentCapabilities.AvailableParameters)
        {
            // Use model-recommended default if available, otherwise capability default
            var defaultValue = CurrentCapabilities.RecommendedDefaults.TryGetValue(paramName, out var recommended)
                ? recommended
                : capability.DefaultValue;
            
            if (defaultValue != null)
            {
                _parameterValues[paramName] = defaultValue;
                Console.WriteLine($"[DynamicParameterViewModel] {paramName} = {defaultValue} (model-optimized)");
            }
        }
        
        // Notify UI that all parameter values may have changed
        OnPropertyChanged("");
    }

    /// <summary>
    /// Regenerate the UI based on current model capabilities
    /// </summary>
    private async Task RegenerateUIAsync()
    {
        if (CurrentCapabilities == null)
        {
            DynamicUI = null;
            return;
        }
        
        Console.WriteLine("[DynamicParameterViewModel] Generating dynamic UI...");
        
        // Generate UI on UI thread
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                DynamicUI = DynamicParameterUIGenerator.GenerateParameterControls(CurrentCapabilities, this);
                Console.WriteLine($"[DynamicParameterViewModel] UI generated with {DynamicUI?.Children.Count ?? 0} sections");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DynamicParameterViewModel] UI generation failed: {ex.GetType().Name}: {ex.Message}");
                DynamicUI = null;
            }
        });
    }

    /// <summary>
    /// Get current parameter values for sending to model
    /// </summary>
    public Dictionary<string, object> GetParameterValues()
    {
        return new Dictionary<string, object>(_parameterValues);
    }
    
    /// <summary>
    /// Update a parameter value (called by UI bindings)
    /// </summary>
    public void SetParameterValue(string parameterName, object value)
    {
        if (_parameterValues.TryGetValue(parameterName, out var currentValue) && !currentValue.Equals(value))
        {
            _parameterValues[parameterName] = value;
            Console.WriteLine($"[DynamicParameterViewModel] Parameter updated: {parameterName} = {value}");
            
            // Check for parameter dependencies and update affected parameters
            CheckParameterDependencies(parameterName, value);
            
            OnPropertyChanged(parameterName);
        }
    }
    
    /// <summary>
    /// Get a parameter value (called by UI bindings)
    /// </summary>
    public object? GetParameterValue(string parameterName)
    {
        return _parameterValues.TryGetValue(parameterName, out var value) ? value : null;
    }

    /// <summary>
    /// Check and handle parameter dependencies
    /// </summary>
    private void CheckParameterDependencies(string changedParameter, object newValue)
    {
        if (CurrentCapabilities?.Dependencies == null) return;
        
        foreach (var dependency in CurrentCapabilities.Dependencies)
        {
            if (dependency.TriggerParameter != changedParameter) continue;
            
            // Check if dependency condition is met
            var conditionMet = dependency.Condition switch
            {
                ComparisonType.Equals => newValue.Equals(dependency.TriggerValue),
                ComparisonType.NotEquals => !newValue.Equals(dependency.TriggerValue),
                ComparisonType.GreaterThan => IsGreaterThan(newValue, dependency.TriggerValue),
                ComparisonType.LessThan => IsLessThan(newValue, dependency.TriggerValue),
                ComparisonType.GreaterThanOrEqual => IsGreaterThanOrEqual(newValue, dependency.TriggerValue),
                ComparisonType.LessThanOrEqual => IsLessThanOrEqual(newValue, dependency.TriggerValue),
                _ => false
            };
            
            if (conditionMet)
            {
                Console.WriteLine($"[DynamicParameterViewModel] Dependency triggered: {dependency.TriggerParameter} -> {dependency.AffectedParameter}");
                
                switch (dependency.Action)
                {
                    case DependencyAction.SetValue when dependency.NewValue != null:
                        SetParameterValue(dependency.AffectedParameter, dependency.NewValue);
                        break;
                        
                    case DependencyAction.ShowWarning when !string.IsNullOrEmpty(dependency.Warning):
                        // TODO: Show warning in UI
                        Console.WriteLine($"[DynamicParameterViewModel] Warning: {dependency.Warning}");
                        break;
                        
                    case DependencyAction.Hide:
                        // TODO: Hide parameter in UI
                        Console.WriteLine($"[DynamicParameterViewModel] Should hide parameter: {dependency.AffectedParameter}");
                        break;
                }
            }
        }
    }
    
    #region Comparison Helpers
    
    private static bool IsGreaterThan(object left, object? right)
    {
        return (left, right) switch
        {
            (IComparable<int> l, int r) => l.CompareTo(r) > 0,
            (IComparable<float> l, float r) => l.CompareTo(r) > 0,
            (IComparable<double> l, double r) => l.CompareTo(r) > 0,
            (IComparable l, IComparable r) when l.GetType() == r.GetType() => l.CompareTo(r) > 0,
            _ => false
        };
    }
    
    private static bool IsLessThan(object left, object? right)
    {
        return (left, right) switch
        {
            (IComparable<int> l, int r) => l.CompareTo(r) < 0,
            (IComparable<float> l, float r) => l.CompareTo(r) < 0,
            (IComparable<double> l, double r) => l.CompareTo(r) < 0,
            (IComparable l, IComparable r) when l.GetType() == r.GetType() => l.CompareTo(r) < 0,
            _ => false
        };
    }
    
    private static bool IsGreaterThanOrEqual(object left, object? right)
    {
        return left.Equals(right) || IsGreaterThan(left, right);
    }
    
    private static bool IsLessThanOrEqual(object left, object? right)
    {
        return left.Equals(right) || IsLessThan(left, right);
    }
    
    #endregion
    
    /// <summary>
    /// Handle cross-tab LoRA state changes for real-time synchronization
    /// </summary>
    private void OnLoRAStateChanged(object? sender, EventArgs e)
    {
        try
        {
            Console.WriteLine($"[DynamicParameterViewModel] 🔔 LoRA state changed! Triggering HasActiveLoRAs refresh");
            
            // Immediately notify UI properties that depend on LoRA state
            OnPropertyChanged(nameof(HasActiveLoRAs));
            OnPropertyChanged(nameof(LoRAStatusSummary));
            
            // Optionally refresh capabilities asynchronously (but don't wait for it)
            _ = Task.Run(async () =>
            {
                try
                {
                    await RefreshCapabilitiesAsync();
                    Console.WriteLine($"[DynamicParameterViewModel] ✅ Capabilities refreshed due to LoRA change");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DynamicParameterViewModel] Failed to refresh capabilities: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DynamicParameterViewModel] Error handling LoRA state change: {ex.Message}");
        }
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
    
    /// <summary>
    /// Property indexer for dynamic parameter binding
    /// </summary>
    public object? this[string parameterName]
    {
        get => GetParameterValue(parameterName);
        set 
        { 
            if (value != null) 
                SetParameterValue(parameterName, value); 
        }
    }
}