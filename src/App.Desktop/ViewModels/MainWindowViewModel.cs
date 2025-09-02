using CommunityToolkit.Mvvm.Input;
using Lazarus.App.Desktop.Services;
using Lazarus.App.Shared.Contracts;
using Lazarus.App.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the main window
/// </summary>
public partial class MainWindowViewModel : BaseViewModel
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly DispatcherTimer _timer;

    private TrainingSession? _selectedSession;
    private string _selectedStatusFilter = "All";
    private DateTime _currentTime = DateTime.Now;
    private Brush _apiStatusBrush = Brushes.Gray;
    private ThemeOption? _selectedTheme;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class
    /// </summary>
    /// <param name="trainingService">The training service</param>
    /// <param name="logger">The logger</param>
    /// <param name="userPreferencesService">The user preferences service</param>
    public MainWindowViewModel(ITrainingService trainingService, ILogger<MainWindowViewModel> logger, IUserPreferencesService userPreferencesService)
    {
        _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userPreferencesService = userPreferencesService ?? throw new ArgumentNullException(nameof(userPreferencesService));

        // Initialize collections
        TrainingSessions = new ObservableCollection<TrainingSession>();
        StatusFilter = new ObservableCollection<string> { "All", "Pending", "Running", "Completed", "Failed", "Cancelled" };
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

        // Set initial selected theme
        _selectedTheme = AvailableThemes.FirstOrDefault(t => t.Theme == _userPreferencesService.CurrentTheme);

        // Initialize commands
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        CreateSessionCommand = new AsyncRelayCommand(CreateSessionAsync);
        StartSessionCommand = new AsyncRelayCommand<TrainingSession>(StartSessionAsync);
        StopSessionCommand = new AsyncRelayCommand<TrainingSession>(StopSessionAsync);
        DeleteSessionCommand = new AsyncRelayCommand<TrainingSession>(DeleteSessionAsync);

        // Initialize timer for current time updates
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        // Set initial status
        StatusMessage = "Ready";
    }

    /// <summary>
    /// Gets the window title
    /// </summary>
    public string WindowTitle => "Lazarus Training Platform";

    /// <summary>
    /// Gets the collection of training sessions
    /// </summary>
    public ObservableCollection<TrainingSession> TrainingSessions { get; }

    /// <summary>
    /// Gets the collection of status filter options
    /// </summary>
    public ObservableCollection<string> StatusFilter { get; }

    /// <summary>
    /// Gets the collection of available themes
    /// </summary>
    public ObservableCollection<ThemeOption> AvailableThemes { get; }

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
                _logger.LogInformation("Theme selection changed to {ThemeName}", value.DisplayName);
                _userPreferencesService.CurrentTheme = value.Theme;
                _userPreferencesService.ApplyThemePreference();
            }
        }
    }

    /// <summary>
    /// Gets or sets the selected training session
    /// </summary>
    public TrainingSession? SelectedSession
    {
        get => _selectedSession;
        set => SetProperty(ref _selectedSession, value);
    }

    /// <summary>
    /// Gets or sets the selected status filter
    /// </summary>
    public string SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if (SetProperty(ref _selectedStatusFilter, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    /// <summary>
    /// Gets the current time
    /// </summary>
    public DateTime CurrentTime
    {
        get => _currentTime;
        private set => SetProperty(ref _currentTime, value);
    }

    /// <summary>
    /// Gets the API status brush
    /// </summary>
    public Brush ApiStatusBrush
    {
        get => _apiStatusBrush;
        private set => SetProperty(ref _apiStatusBrush, value);
    }

    /// <summary>
    /// Gets the count of active sessions
    /// </summary>
    public int ActiveSessionsCount => TrainingSessions.Count(s => s.Status == TrainingStatus.Running);

    /// <summary>
    /// Gets the count of completed sessions
    /// </summary>
    public int CompletedSessionsCount => TrainingSessions.Count(s => s.Status == TrainingStatus.Completed);

    /// <summary>
    /// Gets the count of total sessions
    /// </summary>
    public int TotalSessionsCount => TrainingSessions.Count;

    /// <summary>
    /// Gets the refresh command
    /// </summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the create session command
    /// </summary>
    public IAsyncRelayCommand CreateSessionCommand { get; }

    /// <summary>
    /// Gets the start session command
    /// </summary>
    public IAsyncRelayCommand<TrainingSession> StartSessionCommand { get; }

    /// <summary>
    /// Gets the stop session command
    /// </summary>
    public IAsyncRelayCommand<TrainingSession> StopSessionCommand { get; }

    /// <summary>
    /// Gets the delete session command
    /// </summary>
    public IAsyncRelayCommand<TrainingSession> DeleteSessionCommand { get; }

    /// <summary>
    /// Initializes the view model asynchronously
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Initializing main window view model");
            await RefreshAsync();
            await CheckApiStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing main window view model");
            StatusMessage = "Failed to initialize application";
        }
    }

    /// <summary>
    /// Refreshes the training sessions
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task RefreshAsync()
    {
        if (IsBusy) return;

        try
        {
            SetBusyState(true, "Loading training sessions...");
            _logger.LogInformation("Refreshing training sessions");

            var sessions = await _trainingService.GetAllSessionsAsync();
            
            // Apply status filter
            if (SelectedStatusFilter != "All" && Enum.TryParse<TrainingStatus>(SelectedStatusFilter, out var statusFilter))
            {
                sessions = sessions.Where(s => s.Status == statusFilter);
            }

            // Update the collection on the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                TrainingSessions.Clear();
                foreach (var session in sessions.OrderByDescending(s => s.CreatedAt))
                {
                    TrainingSessions.Add(session);
                }
            });

            // Update dashboard counts
            OnPropertyChanged(nameof(ActiveSessionsCount));
            OnPropertyChanged(nameof(CompletedSessionsCount));
            OnPropertyChanged(nameof(TotalSessionsCount));

            SetBusyState(false, $"Loaded {TrainingSessions.Count} training sessions");
            ApiStatusBrush = Brushes.Green;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing training sessions");
            SetBusyState(false, "Failed to load training sessions");
            ApiStatusBrush = Brushes.Red;
        }
    }

    /// <summary>
    /// Creates a new training session
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task CreateSessionAsync()
    {
        try
        {
            SetBusyState(true, "Creating new training session...");
            _logger.LogInformation("Creating new training session");

            // Create a sample session for demonstration
            var newSession = new TrainingSession
            {
                Name = $"Training Session {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Description = "Automatically created training session",
                Status = TrainingStatus.Pending,
                Progress = 0
            };

            var createdSession = await _trainingService.CreateSessionAsync(newSession);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                TrainingSessions.Insert(0, createdSession);
            });

            OnPropertyChanged(nameof(TotalSessionsCount));
            SetBusyState(false, "Training session created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training session");
            SetBusyState(false, "Failed to create training session");
        }
    }

    /// <summary>
    /// Starts a training session
    /// </summary>
    /// <param name="session">The session to start</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task StartSessionAsync(TrainingSession? session)
    {
        if (session == null) return;

        try
        {
            SetBusyState(true, $"Starting session {session.Name}...");
            _logger.LogInformation("Starting training session {SessionId}", session.Id);

            var success = await _trainingService.StartSessionAsync(session.Id);
            
            if (success)
            {
                await RefreshAsync();
                SetBusyState(false, "Training session started successfully");
            }
            else
            {
                SetBusyState(false, "Failed to start training session");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training session {SessionId}", session.Id);
            SetBusyState(false, "Failed to start training session");
        }
    }

    /// <summary>
    /// Stops a training session
    /// </summary>
    /// <param name="session">The session to stop</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task StopSessionAsync(TrainingSession? session)
    {
        if (session == null) return;

        try
        {
            SetBusyState(true, $"Stopping session {session.Name}...");
            _logger.LogInformation("Stopping training session {SessionId}", session.Id);

            var success = await _trainingService.StopSessionAsync(session.Id);
            
            if (success)
            {
                await RefreshAsync();
                SetBusyState(false, "Training session stopped successfully");
            }
            else
            {
                SetBusyState(false, "Failed to stop training session");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping training session {SessionId}", session.Id);
            SetBusyState(false, "Failed to stop training session");
        }
    }

    /// <summary>
    /// Deletes a training session
    /// </summary>
    /// <param name="session">The session to delete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task DeleteSessionAsync(TrainingSession? session)
    {
        if (session == null) return;

        // Confirm deletion
        var result = MessageBox.Show(
            $"Are you sure you want to delete the training session '{session.Name}'?",
            "Confirm Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            SetBusyState(true, $"Deleting session {session.Name}...");
            _logger.LogInformation("Deleting training session {SessionId}", session.Id);

            var success = await _trainingService.DeleteSessionAsync(session.Id);
            
            if (success)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TrainingSessions.Remove(session);
                });

                OnPropertyChanged(nameof(TotalSessionsCount));
                SetBusyState(false, "Training session deleted successfully");
            }
            else
            {
                SetBusyState(false, "Failed to delete training session");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training session {SessionId}", session.Id);
            SetBusyState(false, "Failed to delete training session");
        }
    }

    /// <summary>
    /// Checks the API status
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task CheckApiStatusAsync()
    {
        try
        {
            // Simple health check by trying to get sessions
            await _trainingService.GetAllSessionsAsync();
            ApiStatusBrush = Brushes.Green;
        }
        catch (Exception)
        {
            ApiStatusBrush = Brushes.Red;
        }
    }

    /// <summary>
    /// Handles the timer tick event
    /// </summary>
    /// <param name="sender">The sender</param>
    /// <param name="e">The event arguments</param>
    private void Timer_Tick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now;
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