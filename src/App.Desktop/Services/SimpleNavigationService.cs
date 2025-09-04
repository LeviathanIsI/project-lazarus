using CommunityToolkit.Mvvm.ComponentModel;
using Lazarus.App.Desktop.ViewModels;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Simple navigation service without dependency injection requirements
/// </summary>
public class SimpleNavigationService : ObservableObject
{
    private object? _currentViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleNavigationService"/> class
    /// </summary>
    public SimpleNavigationService()
    {
        // Initialize with a simple placeholder
        _currentViewModel = new PlaceholderViewModel { Title = "Dashboard" };
    }

    /// <summary>
    /// Gets the current view model
    /// </summary>
    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    /// <summary>
    /// Navigates to a placeholder view
    /// </summary>
    /// <param name="title">The title of the view</param>
    public void NavigateToView(string title)
    {
        CurrentViewModel = new PlaceholderViewModel { Title = title };
    }
}

/// <summary>
/// Simple placeholder view model for navigation
/// </summary>
public class PlaceholderViewModel
{
    /// <summary>
    /// Gets or sets the title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets a placeholder message
    /// </summary>
    public string Message => $"{Title} view coming soon!";
}