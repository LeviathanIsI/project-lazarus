using CommunityToolkit.Mvvm.Input;
using Lazarus.App.Desktop.Services;
using Lazarus.App.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the main window
/// </summary>
public partial class MainWindowViewModel : BaseViewModel
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly ISystemStatusService _systemStatusService;
    private readonly DispatcherTimer _timer;
    private DateTime _currentTime = DateTime.Now;
    private ThemeOption? _selectedTheme;
    private string _currentSection = "Dashboard";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="navigationService">The navigation service</param>
    /// <param name="systemStatusService">The system status service</param>
    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        INavigationService navigationService,
        ISystemStatusService systemStatusService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
        
        // Initialize collections
        AvailableThemes = new ObservableCollection<ThemeOption>();
        
        // Initialize available themes
        foreach (var theme in ThemeManager.GetAvailableThemes())
        {
            AvailableThemes.Add(new ThemeOption
            {
                Theme = theme,
                DisplayName = ThemeManager.GetThemeDisplayName(theme)
            });
        }

        // Set initial selected theme to Dark
        _selectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == Theme.Dark);

        // Initialize timer for current time updates
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        // Set initial status
        StatusMessage = "Ready";
        
        // Use injected navigation service
        NavigationService = _navigationService;
        
        // Initialize navigation commands using proper navigation sections
        NavigateToDashboardCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Dashboard); CurrentSection = "Dashboard"; });
        NavigateToConversationsCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Conversations); CurrentSection = "Conversations"; });
        NavigateToModelConfigurationCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.ModelConfiguration); CurrentSection = "Model Configuration"; });
        NavigateToRunnerManagerCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.RunnerManager); CurrentSection = "Runner Manager"; });
        NavigateToJobsCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Jobs); CurrentSection = "Jobs"; });
        NavigateToDatasetsCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Datasets); CurrentSection = "Datasets"; });
        NavigateToImagesCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Images); CurrentSection = "Images"; });
        NavigateToVideoCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Video); CurrentSection = "Video"; });
        NavigateToVoiceCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Voice); CurrentSection = "Voice"; });
        NavigateToThreeDModelsCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.ThreeDModels); CurrentSection = "3D Models"; });
        NavigateToEntitiesCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Entities); CurrentSection = "Entities"; });
        NavigateToTrainingCommand = new RelayCommand(() => { _navigationService.NavigateToSection(NavigationSection.Training); CurrentSection = "Training"; });
    }

    /// <summary>
    /// Gets the window title
    /// </summary>
    public string WindowTitle => "Lazarus Training Platform";


    /// <summary>
    /// Gets the collection of available themes
    /// </summary>
    public ObservableCollection<ThemeOption> AvailableThemes { get; }

    /// <summary>
    /// Gets the navigation service
    /// </summary>
    public INavigationService NavigationService { get; }
    
    /// <summary>
    /// Gets the current system status brush for the status indicator
    /// </summary>
    public System.Windows.Media.SolidColorBrush ApiStatusBrush
    {
        get
        {
            return _systemStatusService.CurrentStatus switch
            {
                SystemStatus.Ready => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green),
                SystemStatus.Busy or SystemStatus.Training => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange),
                SystemStatus.Warning => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Yellow),
                SystemStatus.Error => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red),
                _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray)
            };
        }
    }

    /// <summary>
    /// Gets or sets the selected theme
    /// </summary>
    public ThemeOption? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value) && value != null)
            {
                // Apply theme changes on UI thread to prevent resource dictionary violations
                ExecuteOnUIThread(() =>
                {
                    try
                    {
                        ThemeManager.ApplyTheme(value.Theme);
                        _logger.LogInformation("Theme applied successfully: {Theme}", value.Theme);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to apply theme: {Theme}", value.Theme);
                        StatusMessage = $"Failed to apply theme: {ex.Message}";
                    }
                });
            }
        }
    }

    /// <summary>
    /// Gets or sets the current navigation section
    /// </summary>
    public string CurrentSection
    {
        get => _currentSection;
        set => SetProperty(ref _currentSection, value);
    }


    /// <summary>
    /// Gets the current time
    /// </summary>
    public DateTime CurrentTime
    {
        get => _currentTime;
        private set => SetProperty(ref _currentTime, value);
    }

    // Navigation Commands
    public IRelayCommand NavigateToDashboardCommand { get; }
    public IRelayCommand NavigateToConversationsCommand { get; }
    public IRelayCommand NavigateToModelConfigurationCommand { get; }
    public IRelayCommand NavigateToRunnerManagerCommand { get; }
    public IRelayCommand NavigateToJobsCommand { get; }
    public IRelayCommand NavigateToDatasetsCommand { get; }
    public IRelayCommand NavigateToImagesCommand { get; }
    public IRelayCommand NavigateToVideoCommand { get; }
    public IRelayCommand NavigateToVoiceCommand { get; }
    public IRelayCommand NavigateToThreeDModelsCommand { get; }
    public IRelayCommand NavigateToEntitiesCommand { get; }
    public IRelayCommand NavigateToTrainingCommand { get; }

    /// <summary>
    /// Handles the timer tick event
    /// </summary>
    /// <param name="sender">The sender</param>
    /// <param name="e">The event arguments</param>
    private void Timer_Tick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now;
    }

    /// <summary>
    /// Disposes of resources used by the MainWindowViewModel
    /// </summary>
    protected override void DisposeResources()
    {
        // Properly dispose of timer and event handlers to prevent memory leaks
        ExecuteOnUIThread(() =>
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                // DispatcherTimer doesn't implement IDisposable, so we just null it
                _logger.LogDebug("Timer disposed successfully in MainWindowViewModel");
            }
        });
        
        base.DisposeResources();
    }
}

/// <summary>
/// Represents a theme option for the UI
/// </summary>
public class ThemeOption
{
    /// <summary>
    /// Gets or sets the theme
    /// </summary>
    public Theme Theme { get; set; }

    /// <summary>
    /// Gets or sets the display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
}