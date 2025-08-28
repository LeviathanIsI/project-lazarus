using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using App.Shared.Enums;
using Lazarus.Shared.Models;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Manages state preservation across ViewMode transitions to prevent data loss
/// Ensures users can safely switch between Novice/Enthusiast/Developer without losing work
/// </summary>
public class ViewModeStateManager : INotifyPropertyChanged
{
    #region State Storage

    /// <summary>
    /// Parameter values preserved across ViewMode transitions
    /// Key: Parameter name, Value: Current value
    /// </summary>
    private readonly Dictionary<string, object> _preservedParameterValues = new();

    /// <summary>
    /// UI state preserved across ViewMode transitions
    /// Stores things like scroll positions, expanded states, selected tabs, etc.
    /// </summary>
    private readonly Dictionary<string, object> _preservedUIState = new();

    /// <summary>
    /// Work-in-progress data that must not be lost during transitions
    /// Examples: partial prompts, unsaved configurations, active generations
    /// </summary>
    private readonly Dictionary<string, WorkInProgressData> _workInProgress = new();

    /// <summary>
    /// ViewMode-specific preferences and customizations
    /// </summary>
    private readonly Dictionary<ViewMode, ViewModePreferences> _viewModePreferences = new();

    /// <summary>
    /// Tracks which parameters were visible/modified in each ViewMode
    /// </summary>
    private readonly Dictionary<ViewMode, HashSet<string>> _viewModeParameterHistory = new();

    #endregion

    #region State Preservation Methods

    /// <summary>
    /// Preserve current parameter values before ViewMode transition
    /// </summary>
    /// <param name="parameters">Current parameter values to preserve</param>
    public void PreserveParameterState(Dictionary<string, object> parameters)
    {
        Console.WriteLine($"[ViewModeState] 💾 Preserving {parameters.Count} parameter values...");

        foreach (var (paramName, value) in parameters)
        {
            _preservedParameterValues[paramName] = value;
            Console.WriteLine($"[ViewModeState] Preserved {paramName} = {value}");
        }

        Console.WriteLine($"[ViewModeState] ✅ Parameter state preserved successfully");
    }

    /// <summary>
    /// Restore parameter values after ViewMode transition
    /// </summary>
    /// <param name="availableParameters">Parameters available in the new ViewMode</param>
    /// <returns>Restored parameter values that should be applied</returns>
    public Dictionary<string, object> RestoreParameterState(IEnumerable<string> availableParameters)
    {
        var restored = new Dictionary<string, object>();
        var availableSet = availableParameters.ToHashSet();

        Console.WriteLine($"[ViewModeState] 🔄 Restoring parameter state for {availableSet.Count} available parameters...");

        foreach (var (paramName, value) in _preservedParameterValues)
        {
            if (availableSet.Contains(paramName))
            {
                restored[paramName] = value;
                Console.WriteLine($"[ViewModeState] Restored {paramName} = {value}");
            }
            else
            {
                Console.WriteLine($"[ViewModeState] ⚠️ Parameter {paramName} not available in current ViewMode");
            }
        }

        Console.WriteLine($"[ViewModeState] ✅ Restored {restored.Count} parameter values");
        return restored;
    }

    /// <summary>
    /// Preserve UI state before ViewMode transition
    /// </summary>
    /// <param name="uiState">Current UI state to preserve</param>
    public void PreserveUIState(Dictionary<string, object> uiState)
    {
        Console.WriteLine($"[ViewModeState] 💾 Preserving UI state ({uiState.Count} items)...");

        foreach (var (key, value) in uiState)
        {
            _preservedUIState[key] = value;
            Console.WriteLine($"[ViewModeState] Preserved UI state: {key}");
        }
    }

    /// <summary>
    /// Restore UI state after ViewMode transition
    /// </summary>
    /// <returns>Restored UI state</returns>
    public Dictionary<string, object> RestoreUIState()
    {
        Console.WriteLine($"[ViewModeState] 🔄 Restoring UI state ({_preservedUIState.Count} items)...");
        return new Dictionary<string, object>(_preservedUIState);
    }

    /// <summary>
    /// Preserve work-in-progress data that must not be lost
    /// </summary>
    /// <param name="workType">Type of work being preserved</param>
    /// <param name="data">Work data to preserve</param>
    public void PreserveWorkInProgress(string workType, WorkInProgressData data)
    {
        _workInProgress[workType] = data;
        Console.WriteLine($"[ViewModeState] 💾 Preserved work-in-progress: {workType} ({data.Description})");
    }

    /// <summary>
    /// Get work-in-progress data
    /// </summary>
    /// <param name="workType">Type of work to retrieve</param>
    /// <returns>Work data if available, null otherwise</returns>
    public WorkInProgressData? GetWorkInProgress(string workType)
    {
        return _workInProgress.TryGetValue(workType, out var data) ? data : null;
    }

    /// <summary>
    /// Clear specific work-in-progress data (when work is completed/saved)
    /// </summary>
    /// <param name="workType">Type of work to clear</param>
    public void ClearWorkInProgress(string workType)
    {
        if (_workInProgress.Remove(workType))
        {
            Console.WriteLine($"[ViewModeState] 🗑️ Cleared work-in-progress: {workType}");
        }
    }

    #endregion

    #region ViewMode Preferences Management

    /// <summary>
    /// Save preferences specific to a ViewMode
    /// </summary>
    /// <param name="viewMode">ViewMode to save preferences for</param>
    /// <param name="preferences">Preferences to save</param>
    public void SaveViewModePreferences(ViewMode viewMode, ViewModePreferences preferences)
    {
        _viewModePreferences[viewMode] = preferences;
        Console.WriteLine($"[ViewModeState] 💾 Saved preferences for {viewMode} mode");
    }

    /// <summary>
    /// Get preferences for a specific ViewMode
    /// </summary>
    /// <param name="viewMode">ViewMode to get preferences for</param>
    /// <returns>Preferences if available, default preferences otherwise</returns>
    public ViewModePreferences GetViewModePreferences(ViewMode viewMode)
    {
        if (_viewModePreferences.TryGetValue(viewMode, out var prefs))
        {
            return prefs;
        }

        // Return default preferences for the ViewMode
        var defaultPrefs = CreateDefaultPreferences(viewMode);
        _viewModePreferences[viewMode] = defaultPrefs;
        return defaultPrefs;
    }

    /// <summary>
    /// Track parameter usage history for adaptive UI
    /// </summary>
    /// <param name="viewMode">ViewMode where parameter was used</param>
    /// <param name="parameterName">Parameter that was used</param>
    public void TrackParameterUsage(ViewMode viewMode, string parameterName)
    {
        if (!_viewModeParameterHistory.ContainsKey(viewMode))
        {
            _viewModeParameterHistory[viewMode] = new HashSet<string>();
        }

        _viewModeParameterHistory[viewMode].Add(parameterName);
    }

    /// <summary>
    /// Get parameters that were previously used in a ViewMode
    /// </summary>
    /// <param name="viewMode">ViewMode to check</param>
    /// <returns>Set of parameter names that were used</returns>
    public HashSet<string> GetUsedParameters(ViewMode viewMode)
    {
        return _viewModeParameterHistory.TryGetValue(viewMode, out var used) 
            ? new HashSet<string>(used) 
            : new HashSet<string>();
    }

    #endregion

    #region Transition Safety Checks

    /// <summary>
    /// Check if there's unsaved work that would be lost during transition
    /// </summary>
    /// <returns>List of work items that would be lost</returns>
    public List<string> GetUnsavedWorkWarnings()
    {
        var warnings = new List<string>();

        foreach (var (workType, data) in _workInProgress)
        {
            if (data.IsUnsaved)
            {
                warnings.Add($"{data.Description} ({data.LastModified:HH:mm})");
            }
        }

        return warnings;
    }

    /// <summary>
    /// Check if transition to target ViewMode would hide parameters user is actively using
    /// </summary>
    /// <param name="targetViewMode">ViewMode being switched to</param>
    /// <param name="activeParameters">Parameters currently being used</param>
    /// <returns>List of parameters that would be hidden</returns>
    public List<string> GetParameterVisibilityWarnings(ViewMode targetViewMode, IEnumerable<string> activeParameters)
    {
        var warnings = new List<string>();

        foreach (var paramName in activeParameters)
        {
            var classification = ParameterRiskRegistry.GetClassification(paramName);
            if (classification != null && !ParameterRiskRegistry.ShouldShowInViewMode(paramName, targetViewMode))
            {
                warnings.Add($"{paramName} ({classification.RiskLevel})");
            }
        }

        return warnings;
    }

    #endregion

    #region Smart Transition Suggestions

    /// <summary>
    /// Suggest optimal ViewMode based on current parameter usage
    /// </summary>
    /// <param name="currentParameters">Parameters user is currently using</param>
    /// <returns>Suggested ViewMode and reasoning</returns>
    public ViewModeSuggestion SuggestOptimalViewMode(IEnumerable<string> currentParameters)
    {
        var paramList = currentParameters.ToList();
        var riskLevels = paramList
            .Select(p => ParameterRiskRegistry.GetClassification(p))
            .Where(c => c != null)
            .Select(c => c!.RiskLevel)
            .ToList();

        // Analyze risk level distribution
        var hasExperimental = riskLevels.Contains(ParameterRiskLevel.Experimental);
        var hasCaution = riskLevels.Contains(ParameterRiskLevel.Caution);
        var experimentalCount = riskLevels.Count(r => r == ParameterRiskLevel.Experimental);
        var cautionCount = riskLevels.Count(r => r == ParameterRiskLevel.Caution);

        ViewMode suggestedMode;
        string reasoning;

        if (hasExperimental)
        {
            suggestedMode = ViewMode.Developer;
            reasoning = experimentalCount == 1 
                ? "You're using 1 experimental parameter - Developer mode provides full technical documentation"
                : $"You're using {experimentalCount} experimental parameters - Developer mode is recommended for advanced users";
        }
        else if (hasCaution)
        {
            suggestedMode = ViewMode.Enthusiast;
            reasoning = cautionCount == 1
                ? "You're using 1 advanced parameter - Enthusiast mode provides helpful guidance"
                : $"You're using {cautionCount} advanced parameters - Enthusiast mode offers balanced control";
        }
        else
        {
            suggestedMode = ViewMode.Novice;
            reasoning = "All your parameters are beginner-friendly - Novice mode keeps things simple";
        }

        return new ViewModeSuggestion
        {
            SuggestedMode = suggestedMode,
            Reasoning = reasoning,
            ParameterCount = paramList.Count,
            ExperimentalCount = experimentalCount,
            CautionCount = cautionCount
        };
    }

    /// <summary>
    /// Get transition guidance for user
    /// </summary>
    /// <param name="fromMode">Current ViewMode</param>
    /// <param name="toMode">Target ViewMode</param>
    /// <returns>Guidance information</returns>
    public ViewModeTransitionGuidance GetTransitionGuidance(ViewMode fromMode, ViewMode toMode)
    {
        var guidance = new ViewModeTransitionGuidance
        {
            FromMode = fromMode,
            ToMode = toMode
        };

        if (toMode == ViewMode.Developer && fromMode != ViewMode.Developer)
        {
            guidance.Benefits.Add("Access to all experimental parameters");
            guidance.Benefits.Add("Technical documentation and debugging info");
            guidance.Benefits.Add("Advanced parameter interaction warnings");
            guidance.Warnings.Add("More complex interface with many technical controls");
        }
        else if (toMode == ViewMode.Enthusiast)
        {
            guidance.Benefits.Add("Balanced interface with professional controls");
            guidance.Benefits.Add("Contextual guidance for advanced parameters");
            guidance.Benefits.Add("Collapsible sections to manage complexity");
            
            if (fromMode == ViewMode.Developer)
                guidance.Warnings.Add("Some experimental parameters will be hidden");
        }
        else if (toMode == ViewMode.Novice)
        {
            guidance.Benefits.Add("Simplified interface focuses on essential controls");
            guidance.Benefits.Add("Visual guides and friendly explanations");
            guidance.Benefits.Add("Protected from potentially problematic parameters");
            
            if (fromMode != ViewMode.Novice)
                guidance.Warnings.Add("Advanced and experimental parameters will be hidden");
        }

        return guidance;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create default preferences for a ViewMode
    /// </summary>
    private ViewModePreferences CreateDefaultPreferences(ViewMode viewMode)
    {
        return new ViewModePreferences
        {
            ViewMode = viewMode,
            ShowParameterDescriptions = viewMode != ViewMode.Developer,
            ShowRiskIndicators = true,
            ShowInteractionWarnings = viewMode != ViewMode.Novice,
            CollapseAdvancedSections = viewMode == ViewMode.Novice,
            EnableParameterHistory = true,
            EnableSmartSuggestions = viewMode != ViewMode.Developer
        };
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

/// <summary>
/// Represents work-in-progress data that must be preserved across ViewMode transitions
/// </summary>
public class WorkInProgressData
{
    public string Description { get; set; } = "";
    public object Data { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.Now;
    public bool IsUnsaved { get; set; } = true;
    public string WorkType { get; set; } = "";
}

/// <summary>
/// ViewMode-specific preferences and customizations
/// </summary>
public class ViewModePreferences
{
    public ViewMode ViewMode { get; set; }
    public bool ShowParameterDescriptions { get; set; } = true;
    public bool ShowRiskIndicators { get; set; } = true;
    public bool ShowInteractionWarnings { get; set; } = true;
    public bool CollapseAdvancedSections { get; set; }
    public bool EnableParameterHistory { get; set; } = true;
    public bool EnableSmartSuggestions { get; set; } = true;
    
    // UI Layout preferences
    public Dictionary<string, bool> ExpanderStates { get; set; } = new();
    public Dictionary<string, double> ScrollPositions { get; set; } = new();
    public Dictionary<string, string> SelectedTabs { get; set; } = new();
}

/// <summary>
/// Smart ViewMode suggestion based on parameter usage
/// </summary>
public class ViewModeSuggestion
{
    public ViewMode SuggestedMode { get; set; }
    public string Reasoning { get; set; } = "";
    public int ParameterCount { get; set; }
    public int ExperimentalCount { get; set; }
    public int CautionCount { get; set; }
    public double ConfidenceScore => CalculateConfidence();

    private double CalculateConfidence()
    {
        // Simple confidence calculation based on parameter risk distribution
        if (ExperimentalCount > 0 && SuggestedMode == ViewMode.Developer) return 0.9;
        if (CautionCount > 0 && SuggestedMode == ViewMode.Enthusiast) return 0.8;
        if (ExperimentalCount == 0 && CautionCount == 0 && SuggestedMode == ViewMode.Novice) return 0.7;
        return 0.5;
    }
}

/// <summary>
/// Guidance information for ViewMode transitions
/// </summary>
public class ViewModeTransitionGuidance
{
    public ViewMode FromMode { get; set; }
    public ViewMode ToMode { get; set; }
    public List<string> Benefits { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string Summary => $"Switching from {FromMode} to {ToMode}";
}