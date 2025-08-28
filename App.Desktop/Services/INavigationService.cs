using System;
using System.ComponentModel;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Navigation service interface for proper MVVM navigation
/// Eliminates code-behind click handlers and view manipulation
/// </summary>
public interface INavigationService : INotifyPropertyChanged
{
    /// <summary>
    /// Current active tab
    /// </summary>
    NavigationTab CurrentTab { get; }
    
    /// <summary>
    /// Current active subtab (empty string if none)
    /// </summary>
    string CurrentSubtab { get; }
    
    /// <summary>
    /// Navigate to a specific tab
    /// </summary>
    void NavigateTo(NavigationTab tab);
    
    /// <summary>
    /// Navigate to a specific subtab within a main tab
    /// </summary>
    void NavigateToSubtab(NavigationTab tab, string subtab);
    
    /// <summary>
    /// Check if a tab is currently active
    /// </summary>
    bool IsTabActive(NavigationTab tab);
    
    // Tab visibility properties for UI binding
    bool IsDashboardVisible { get; }
    bool IsConversationsVisible { get; }
    bool IsModelsVisible { get; }
    bool IsRunnerManagerVisible { get; }
    bool IsJobsVisible { get; }
    bool IsDatasetsVisible { get; }
    bool IsImagesVisible { get; }
    bool IsVideoVisible { get; }
    bool IsThreeDModelsVisible { get; }
    bool IsVoiceVisible { get; }
    bool IsEntitiesVisible { get; }
    
    // Tab active state properties for button highlighting
    bool IsDashboardActive { get; }
    bool IsConversationsActive { get; }
    bool IsModelsActive { get; }
    bool IsRunnerManagerActive { get; }
    bool IsJobsActive { get; }
    bool IsDatasetsActive { get; }
    bool IsImagesActive { get; }
    bool IsVideoActive { get; }
    bool IsThreeDModelsActive { get; }
    bool IsVoiceActive { get; }
    bool IsEntitiesActive { get; }
}

/// <summary>
/// Available navigation tabs in the application
/// </summary>
public enum NavigationTab
{
    Dashboard,
    Conversations,
    Models,
    RunnerManager,
    Jobs,
    Datasets,
    Images,
    Video,
    ThreeDModels,
    Voice,
    Entities
}