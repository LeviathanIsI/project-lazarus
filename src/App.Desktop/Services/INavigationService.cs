using System.ComponentModel;

namespace Lazarus.App.Desktop.Services;

/// <summary>
/// Interface for navigation service managing view switching and navigation state
/// </summary>
public interface INavigationService : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the current view model being displayed
    /// </summary>
    object? CurrentViewModel { get; }

    /// <summary>
    /// Gets the current navigation section
    /// </summary>
    NavigationSection CurrentSection { get; }

    /// <summary>
    /// Event raised when navigation occurs
    /// </summary>
    event EventHandler<NavigationEventArgs>? NavigationChanged;

    /// <summary>
    /// Navigates to the specified view model type
    /// </summary>
    /// <typeparam name="TViewModel">The view model type to navigate to</typeparam>
    void NavigateToView<TViewModel>() where TViewModel : class;

    /// <summary>
    /// Navigates to the specified navigation section
    /// </summary>
    /// <param name="section">The navigation section to navigate to</param>
    void NavigateToSection(NavigationSection section);

    /// <summary>
    /// Gets whether the specified section is currently selected
    /// </summary>
    /// <param name="section">The section to check</param>
    /// <returns>True if the section is selected; otherwise, false</returns>
    bool IsCurrentSection(NavigationSection section);
    
    /// <summary>
    /// Initializes the navigation service with the default view
    /// This should be called after the DI container is fully constructed
    /// </summary>
    void Initialize();
}

/// <summary>
/// Navigation sections available in the application
/// </summary>
public enum NavigationSection
{
    /// <summary>
    /// Dashboard section (default)
    /// </summary>
    Dashboard,

    /// <summary>
    /// Conversations section
    /// </summary>
    Conversations,

    /// <summary>
    /// Model Configuration section
    /// </summary>
    ModelConfiguration,

    /// <summary>
    /// Runner Manager section
    /// </summary>
    RunnerManager,

    /// <summary>
    /// Jobs section
    /// </summary>
    Jobs,

    /// <summary>
    /// Datasets section
    /// </summary>
    Datasets,

    /// <summary>
    /// Images section
    /// </summary>
    Images,

    /// <summary>
    /// Video section
    /// </summary>
    Video,

    /// <summary>
    /// Voice section
    /// </summary>
    Voice,

    /// <summary>
    /// 3D Models section
    /// </summary>
    ThreeDModels,

    /// <summary>
    /// Entities section
    /// </summary>
    Entities,

    /// <summary>
    /// Training section
    /// </summary>
    Training
}

/// <summary>
/// Event arguments for navigation changes
/// </summary>
public class NavigationEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationEventArgs"/> class
    /// </summary>
    /// <param name="previousSection">The previous navigation section</param>
    /// <param name="currentSection">The current navigation section</param>
    /// <param name="viewModel">The current view model</param>
    public NavigationEventArgs(NavigationSection? previousSection, NavigationSection currentSection, object? viewModel)
    {
        PreviousSection = previousSection;
        CurrentSection = currentSection;
        ViewModel = viewModel;
    }

    /// <summary>
    /// Gets the previous navigation section
    /// </summary>
    public NavigationSection? PreviousSection { get; }

    /// <summary>
    /// Gets the current navigation section
    /// </summary>
    public NavigationSection CurrentSection { get; }

    /// <summary>
    /// Gets the current view model
    /// </summary>
    public object? ViewModel { get; }
}