using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;
using Lazarus.Desktop.Services;
using Timer = System.Timers.Timer;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// Dashboard ViewModel - Central command center and system overview
/// Shows system status, recent activity, and provides quick actions
/// </summary>
public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly INavigationService _navigationService;
    private readonly SystemStateViewModel _systemState;
    private readonly Timer _dashboardUpdateTimer;
    
    private string _welcomeMessage = "Welcome to Project Lazarus";
    private bool _isSystemHealthy = true;
    private string _quickTip = "💡 Start by loading a model in Model Configuration";

    public DashboardViewModel(INavigationService navigationService, SystemStateViewModel systemState)
    {
        _navigationService = navigationService;
        _systemState = systemState;
        
        // Initialize quick action commands
        QuickChatCommand = new RelayCommand(_ => _navigationService.NavigateTo(NavigationTab.Conversations));
        QuickModelCommand = new RelayCommand(_ => _navigationService.NavigateTo(NavigationTab.Models));
        QuickImageCommand = new RelayCommand(_ => _navigationService.NavigateTo(NavigationTab.Images));
        QuickVideoCommand = new RelayCommand(_ => _navigationService.NavigateTo(NavigationTab.Video));
        
        // Initialize recent activity (mock data for now)
        InitializeRecentActivity();
        
        // Start real-time dashboard updates every 3 seconds
        _dashboardUpdateTimer = new Timer(3000);
        _dashboardUpdateTimer.Elapsed += (_, _) => RefreshDashboard();
        _dashboardUpdateTimer.Start();
        
        // Initial refresh
        RefreshDashboard();
        
        Console.WriteLine("[DashboardViewModel] Initialized - Central command ready");
    }

    #region Properties

    /// <summary>
    /// Welcome message for the user
    /// </summary>
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set
        {
            _welcomeMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// System health status
    /// </summary>
    public bool IsSystemHealthy
    {
        get => _isSystemHealthy;
        set
        {
            _isSystemHealthy = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Quick tip for users
    /// </summary>
    public string QuickTip
    {
        get => _quickTip;
        set
        {
            _quickTip = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// System state reference for dashboard display
    /// </summary>
    public SystemStateViewModel SystemState => _systemState;

    // Real-time performance metrics for Dashboard templates
    public string GpuUtilization => "67%"; // TODO: Get from SystemState when available
    public double GpuUtilizationValue => 67.0; // For progress bars
    
    public string VramUsage => _systemState?.VramUsage ?? "0 MB / 0 MB";
    public double VramPercentage => _systemState?.VramPercentage ?? 0;
    
    public string TokensPerSecond => _systemState?.TokensPerSecond ?? "0.0";
    public string TokensPerSecondDisplay => $"{_systemState?.TokensPerSecond ?? "0.0"} t/s";
    
    public string GpuTemperature => "72°C"; // TODO: Integrate with SystemState
    public double GpuTemperatureValue => 72.0;
    
    public string SystemStatus => _systemState?.IsOnline == true ? "Online" : "Offline";
    public string SystemStatusColor => _systemState?.ApiStatusColor ?? "#f87171";
    
    public string CurrentModelDisplay => _systemState?.CurrentModel ?? "No model loaded";
    public string CurrentRunnerDisplay => _systemState?.CurrentRunner ?? "No runner";
    
    public int QueuedJobsCount => _systemState?.QueuedJobs ?? 0;
    public string QueueStatus => QueuedJobsCount > 0 ? $"{QueuedJobsCount} queued" : "No jobs";

    /// <summary>
    /// Recent activities for quick access
    /// </summary>
    public ObservableCollection<RecentActivityItem> RecentActivities { get; } = new();

    /// <summary>
    /// System stats summary
    /// </summary>
    public ObservableCollection<SystemStatItem> SystemStats { get; } = new();

    #endregion

    #region Commands

    public ICommand QuickChatCommand { get; }
    public ICommand QuickModelCommand { get; }
    public ICommand QuickImageCommand { get; }
    public ICommand QuickVideoCommand { get; }

    #endregion

    #region Methods

    private void InitializeRecentActivity()
    {
        // Start with empty, real data will populate later
        RecentActivities.Clear();
        SystemStats.Clear();
    }

    public void RefreshDashboard()
    {
        // Update welcome message based on time
        var hour = DateTime.Now.Hour;
        WelcomeMessage = hour switch
        {
            < 12 => "Good Morning! Ready to create?",
            < 17 => "Good Afternoon! Let's build something amazing",
            _ => "Good Evening! Time for some AI magic"
        };

        // Notify all real-time properties that they may have changed
        OnPropertyChanged(nameof(VramUsage));
        OnPropertyChanged(nameof(VramPercentage));
        OnPropertyChanged(nameof(TokensPerSecond));
        OnPropertyChanged(nameof(TokensPerSecondDisplay));
        OnPropertyChanged(nameof(SystemStatus));
        OnPropertyChanged(nameof(SystemStatusColor));
        OnPropertyChanged(nameof(CurrentModelDisplay));
        OnPropertyChanged(nameof(CurrentRunnerDisplay));
        OnPropertyChanged(nameof(QueuedJobsCount));
        OnPropertyChanged(nameof(QueueStatus));

        // Update quick tip based on system state
        if (string.IsNullOrEmpty(_systemState.CurrentModel) || _systemState.CurrentModel == "No model loaded")
        {
            QuickTip = "💡 Start by loading a model in Model Configuration";
        }
        else if (!_systemState.IsOnline)
        {
            QuickTip = "⚠️ API connection issue - check system status";
        }
        else
        {
            QuickTip = "🚀 System ready! Choose your creative workflow";
        }

        // System health check
        IsSystemHealthy = _systemState.IsOnline && !string.IsNullOrEmpty(_systemState.CurrentModel) && _systemState.CurrentModel != "No model loaded";
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
/// Recent activity item for dashboard display
/// </summary>
public class RecentActivityItem
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public string TimeAgo => GetTimeAgo(Timestamp);

    private static string GetTimeAgo(DateTime timestamp)
    {
        var span = DateTime.Now - timestamp;
        return span switch
        {
            { TotalMinutes: < 1 } => "Just now",
            { TotalMinutes: < 60 } => $"{(int)span.TotalMinutes}m ago",
            { TotalHours: < 24 } => $"{(int)span.TotalHours}h ago",
            _ => timestamp.ToString("MMM dd")
        };
    }
}

/// <summary>
/// System stat item for dashboard display
/// </summary>
public class SystemStatItem
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Trend { get; set; } = "→";
}