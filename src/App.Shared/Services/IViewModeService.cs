using System.ComponentModel;

namespace Lazarus.App.Shared.Services;

/// <summary>
/// Service for managing user interface complexity levels and view mode preferences
/// </summary>
public interface IViewModeService : INotifyPropertyChanged
{
    /// <summary>
    /// Event raised when view mode changes
    /// </summary>
    event EventHandler<ViewModeChangedEventArgs>? ViewModeChanged;

    /// <summary>
    /// Gets the current view mode
    /// </summary>
    ViewMode CurrentViewMode { get; }

    /// <summary>
    /// Sets the view mode and persists the preference
    /// </summary>
    /// <param name="viewMode">The view mode to set</param>
    Task SetViewModeAsync(ViewMode viewMode);

    /// <summary>
    /// Gets whether a feature should be visible for the current view mode
    /// </summary>
    /// <param name="requiredLevel">The minimum view mode level required</param>
    /// <returns>True if the feature should be visible</returns>
    bool IsFeatureVisible(ViewMode requiredLevel);

    /// <summary>
    /// Gets whether advanced features should be shown
    /// </summary>
    bool ShowAdvancedFeatures { get; }

    /// <summary>
    /// Gets whether developer features should be shown
    /// </summary>
    bool ShowDeveloperFeatures { get; }

    /// <summary>
    /// Loads view mode preference from storage
    /// </summary>
    Task LoadViewModeAsync();

    /// <summary>
    /// Saves current view mode preference to storage
    /// </summary>
    Task SaveViewModeAsync();
}

/// <summary>
/// User interface complexity levels for the Lazarus application
/// </summary>
public enum ViewMode
{
    /// <summary>
    /// Novice mode - Simple interface with essential features only
    /// </summary>
    Novice = 1,

    /// <summary>
    /// Enthusiast mode - Moderate complexity with additional configuration options
    /// </summary>
    Enthusiast = 2,

    /// <summary>
    /// Developer mode - Full interface with all advanced features and debugging tools
    /// </summary>
    Developer = 3
}

/// <summary>
/// Event arguments for view mode change notifications
/// </summary>
public class ViewModeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModeChangedEventArgs"/> class
    /// </summary>
    /// <param name="previousMode">The previous view mode</param>
    /// <param name="newMode">The new view mode</param>
    public ViewModeChangedEventArgs(ViewMode previousMode, ViewMode newMode)
    {
        PreviousMode = previousMode;
        NewMode = newMode;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the previous view mode
    /// </summary>
    public ViewMode PreviousMode { get; }

    /// <summary>
    /// Gets the new view mode
    /// </summary>
    public ViewMode NewMode { get; }

    /// <summary>
    /// Gets the timestamp when the change occurred
    /// </summary>
    public DateTime Timestamp { get; }
}