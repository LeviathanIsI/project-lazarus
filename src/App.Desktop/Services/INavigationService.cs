using System.ComponentModel;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Service for managing navigation between views in the application.
/// </summary>
public interface INavigationService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the currently active view.
    /// </summary>
    string? CurrentView { get; }

    /// <summary>
    /// Gets a value indicating whether navigation back is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether navigation forward is possible.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Navigates to the specified view.
    /// </summary>
    /// <param name="viewName">The name of the view to navigate to.</param>
    /// <param name="parameter">Optional parameter to pass to the view.</param>
    void NavigateTo(string viewName, object? parameter = null);

    /// <summary>
    /// Navigates back to the previous view.
    /// </summary>
    void GoBack();

    /// <summary>
    /// Navigates forward to the next view.
    /// </summary>
    void GoForward();

    /// <summary>
    /// Clears the navigation history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event EventHandler<NavigationEventArgs>? Navigated;
}

/// <summary>
/// Event arguments for navigation events.
/// </summary>
public sealed class NavigationEventArgs : EventArgs
{
    public NavigationEventArgs(string viewName, object? parameter = null)
    {
        ViewName = viewName;
        Parameter = parameter;
    }

    public string ViewName { get; }
    public object? Parameter { get; }
}