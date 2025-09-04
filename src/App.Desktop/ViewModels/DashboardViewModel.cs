using CommunityToolkit.Mvvm.Input;
using Lazarus.App.Desktop.Services;
using Lazarus.App.Desktop.Services.Models;
using Lazarus.App.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Lazarus.App.Desktop.ViewModels;

/// <summary>
/// View model for the Dashboard section - central hub for system status and quick actions
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly ILogger<DashboardViewModel> _logger;
    private readonly INavigationService _navigationService;
    private readonly ISystemStatusService _systemStatusService;
    private readonly IViewModeService _viewModeService;
    private readonly HardwareInventoryService _hardwareInventoryService;
    private readonly DispatcherTimer _refreshTimer;
    
    private int _activeProjects = 12;
    private int _totalModels = 47;
    private int _trainingHours = 342;
    private double _systemLoad = 67.3;
    private double _memoryUsage = 45.2;
    private double _gpuUtilization = 23.8;
    private SystemStatus _systemStatus = SystemStatus.Ready;
    private string _systemStatusText = "Ready";
    private DateTime _lastUpdateTime = DateTime.Now;

    // Expandable card states for progressive disclosure
    private bool _isCpuCardExpanded = false;
    private bool _isMemoryCardExpanded = false;
    private bool _isGpuCardExpanded = false;
    private bool _isStorageCardExpanded = false;

    // Comprehensive hardware inventory
    private CpuSpecification _cpuSpecification = new();
    private MemorySpecification _memorySpecification = new();
    private ObservableCollection<GpuSpecification> _gpuSpecifications = new();
    private ObservableCollection<StorageDevice> _storageDevices = new();
    private HardwareRollupSummary _hardwareRollupSummary = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardViewModel"/> class
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="navigationService">The navigation service</param>
    /// <param name="systemStatusService">The system status service</param>
    /// <param name="viewModeService">The view mode service</param>
    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        INavigationService navigationService,
        ISystemStatusService systemStatusService,
        IViewModeService viewModeService,
        HardwareInventoryService hardwareInventoryService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
        _viewModeService = viewModeService ?? throw new ArgumentNullException(nameof(viewModeService));
        _hardwareInventoryService = hardwareInventoryService ?? throw new ArgumentNullException(nameof(hardwareInventoryService));
        
        Title = "Dashboard";
        StatusMessage = "Dashboard loaded successfully";
        
        // Initialize collections
        RecentActivities = new ObservableCollection<ActivityItem>();
        QuickActions = new ObservableCollection<QuickActionItem>();
        PerformanceMetrics = new ObservableCollection<PerformanceMetric>();
        
        // Initialize commands
        RefreshDashboardCommand = new AsyncRelayCommand(RefreshDashboardAsync);
        StartNewChatCommand = new AsyncRelayCommand(StartNewChatAsync);
        LoadModelCommand = new AsyncRelayCommand(LoadModelAsync);
        StartTrainingCommand = new AsyncRelayCommand(StartTrainingAsync);
        OpenSystemSettingsCommand = new AsyncRelayCommand(OpenSystemSettingsAsync);
        NavigateToJobsCommand = new RelayCommand(() => _navigationService.NavigateToSection(NavigationSection.Jobs));
        NavigateToModelsCommand = new RelayCommand(() => _navigationService.NavigateToSection(NavigationSection.ModelConfiguration));
        NavigateToTrainingCommand = new RelayCommand(() => _navigationService.NavigateToSection(NavigationSection.Training));
        
        // Expandable card toggle commands
        ToggleCpuCardCommand = new RelayCommand(() => IsCpuCardExpanded = !IsCpuCardExpanded);
        ToggleMemoryCardCommand = new RelayCommand(() => IsMemoryCardExpanded = !IsMemoryCardExpanded);
        ToggleGpuCardCommand = new RelayCommand(() => IsGpuCardExpanded = !IsGpuCardExpanded);
        ToggleStorageCardCommand = new RelayCommand(() => IsStorageCardExpanded = !IsStorageCardExpanded);
        
        // Initialize refresh timer
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5) // Update every 5 seconds
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
        
        // Subscribe to service events
        _systemStatusService.StatusChanged += OnSystemStatusChanged;
        _viewModeService.ViewModeChanged += OnViewModeChanged;
        _hardwareInventoryService.InventoryUpdated += OnHardwareInventoryUpdated;
        
        // Load initial data
        LoadDashboardData();
        InitializePerformanceMetrics();
        
        // Start real-time updates immediately (hardware data will populate via events)
        _refreshTimer.Start();
        
        // Initialize comprehensive hardware data asynchronously (don't block UI)
        _ = Task.Run(InitializeHardwareDataAsync);
        
        _logger.LogInformation("Dashboard view model initialized with real-time monitoring");
    }

    /// <summary>
    /// Gets the title of the view
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets or sets the number of active projects
    /// </summary>
    public int ActiveProjects
    {
        get => _activeProjects;
        set => SetProperty(ref _activeProjects, value);
    }

    /// <summary>
    /// Gets or sets the total number of models
    /// </summary>
    public int TotalModels
    {
        get => _totalModels;
        set => SetProperty(ref _totalModels, value);
    }

    /// <summary>
    /// Gets or sets the total training hours
    /// </summary>
    public int TrainingHours
    {
        get => _trainingHours;
        set => SetProperty(ref _trainingHours, value);
    }

    /// <summary>
    /// Gets or sets the current system load percentage
    /// </summary>
    public double SystemLoad
    {
        get => _systemLoad;
        set => SetProperty(ref _systemLoad, value);
    }
    
    /// <summary>
    /// Gets or sets the current memory usage percentage
    /// </summary>
    public double MemoryUsage
    {
        get => _memoryUsage;
        set => SetProperty(ref _memoryUsage, value);
    }
    
    /// <summary>
    /// Gets or sets the current GPU utilization percentage
    /// </summary>
    public double GpuUtilization
    {
        get => _gpuUtilization;
        set => SetProperty(ref _gpuUtilization, value);
    }
    
    /// <summary>
    /// Gets or sets the current system status
    /// </summary>
    public SystemStatus SystemStatus
    {
        get => _systemStatus;
        set => SetProperty(ref _systemStatus, value);
    }
    
    /// <summary>
    /// Gets or sets the system status text
    /// </summary>
    public string SystemStatusText
    {
        get => _systemStatusText;
        set => SetProperty(ref _systemStatusText, value);
    }
    
    /// <summary>
    /// Gets or sets the last update time
    /// </summary>
    public DateTime LastUpdateTime
    {
        get => _lastUpdateTime;
        set => SetProperty(ref _lastUpdateTime, value);
    }

    /// <summary>
    /// Gets the collection of recent activities
    /// </summary>
    public ObservableCollection<ActivityItem> RecentActivities { get; }

    /// <summary>
    /// Gets the collection of quick actions
    /// </summary>
    public ObservableCollection<QuickActionItem> QuickActions { get; }
    
    /// <summary>
    /// Gets the collection of performance metrics
    /// </summary>
    public ObservableCollection<PerformanceMetric> PerformanceMetrics { get; }

    #region Comprehensive Hardware Inventory Properties

    /// <summary>
    /// Gets the detailed CPU specifications and real-time metrics
    /// </summary>
    public CpuSpecification CpuSpecification
    {
        get => _cpuSpecification;
        private set => SetProperty(ref _cpuSpecification, value);
    }

    /// <summary>
    /// Gets the comprehensive memory specifications and usage
    /// </summary>
    public MemorySpecification MemorySpecification
    {
        get => _memorySpecification;
        private set => SetProperty(ref _memorySpecification, value);
    }

    /// <summary>
    /// Gets the collection of GPU specifications and metrics
    /// </summary>
    public ObservableCollection<GpuSpecification> GpuSpecifications
    {
        get => _gpuSpecifications;
        private set => SetProperty(ref _gpuSpecifications, value);
    }

    /// <summary>
    /// Gets the collection of storage devices with health monitoring
    /// </summary>
    public ObservableCollection<StorageDevice> StorageDevices
    {
        get => _storageDevices;
        private set => SetProperty(ref _storageDevices, value);
    }

    /// <summary>
    /// Gets the hardware rollup summary with system-wide metrics
    /// </summary>
    public HardwareRollupSummary HardwareRollupSummary
    {
        get => _hardwareRollupSummary;
        private set => SetProperty(ref _hardwareRollupSummary, value);
    }

    #endregion

    #region Expandable Card Properties

    /// <summary>
    /// Gets or sets whether the CPU card is expanded
    /// </summary>
    public bool IsCpuCardExpanded
    {
        get => _isCpuCardExpanded;
        set => SetProperty(ref _isCpuCardExpanded, value);
    }

    /// <summary>
    /// Gets or sets whether the Memory card is expanded
    /// </summary>
    public bool IsMemoryCardExpanded
    {
        get => _isMemoryCardExpanded;
        set => SetProperty(ref _isMemoryCardExpanded, value);
    }

    /// <summary>
    /// Gets or sets whether the GPU card is expanded
    /// </summary>
    public bool IsGpuCardExpanded
    {
        get => _isGpuCardExpanded;
        set => SetProperty(ref _isGpuCardExpanded, value);
    }

    /// <summary>
    /// Gets or sets whether the Storage card is expanded
    /// </summary>
    public bool IsStorageCardExpanded
    {
        get => _isStorageCardExpanded;
        set => SetProperty(ref _isStorageCardExpanded, value);
    }

    #endregion

    /// <summary>
    /// Gets the refresh dashboard command
    /// </summary>
    public IAsyncRelayCommand RefreshDashboardCommand { get; }
    
    /// <summary>
    /// Gets the start new chat command
    /// </summary>
    public IAsyncRelayCommand StartNewChatCommand { get; }
    
    /// <summary>
    /// Gets the load model command
    /// </summary>
    public IAsyncRelayCommand LoadModelCommand { get; }
    
    /// <summary>
    /// Gets the start training command
    /// </summary>
    public IAsyncRelayCommand StartTrainingCommand { get; }
    
    /// <summary>
    /// Gets the open system settings command
    /// </summary>
    public IAsyncRelayCommand OpenSystemSettingsCommand { get; }
    
    /// <summary>
    /// Gets the navigate to jobs command
    /// </summary>
    public IRelayCommand NavigateToJobsCommand { get; }
    
    /// <summary>
    /// Gets the navigate to models command
    /// </summary>
    public IRelayCommand NavigateToModelsCommand { get; }
    
    /// <summary>
    /// Gets the navigate to training command
    /// </summary>
    public IRelayCommand NavigateToTrainingCommand { get; }

    /// <summary>
    /// Gets the toggle CPU card expansion command
    /// </summary>
    public IRelayCommand ToggleCpuCardCommand { get; }

    /// <summary>
    /// Gets the toggle Memory card expansion command
    /// </summary>
    public IRelayCommand ToggleMemoryCardCommand { get; }

    /// <summary>
    /// Gets the toggle GPU card expansion command
    /// </summary>
    public IRelayCommand ToggleGpuCardCommand { get; }

    /// <summary>
    /// Gets the toggle Storage card expansion command
    /// </summary>
    public IRelayCommand ToggleStorageCardCommand { get; }
    
    /// <summary>
    /// Gets whether advanced features should be visible based on view mode
    /// </summary>
    public bool ShowAdvancedFeatures => _viewModeService.ShowAdvancedFeatures;
    
    /// <summary>
    /// Gets whether developer features should be visible based on view mode
    /// </summary>
    public bool ShowDeveloperFeatures => _viewModeService.ShowDeveloperFeatures;
    
    /// <summary>
    /// Gets the current view mode
    /// </summary>
    public ViewMode CurrentViewMode => _viewModeService.CurrentViewMode;

    /// <summary>
    /// Refreshes the dashboard data
    /// </summary>
    /// <returns>A task that represents the asynchronous operation</returns>
    private async Task RefreshDashboardAsync()
    {
        try
        {
            SetBusyState(true, "Refreshing dashboard data...");
            _logger.LogInformation("Refreshing dashboard data");

            // Simulate data loading
            await Task.Delay(1000);

            // Update metrics with simulated data
            ActiveProjects = Random.Shared.Next(10, 20);
            TotalModels = Random.Shared.Next(40, 60);
            TrainingHours = Random.Shared.Next(300, 400);
            SystemLoad = Random.Shared.NextDouble() * 100;

            LoadDashboardData();
            
            // Update performance metrics
            InitializePerformanceMetrics();
            
            SetBusyState(false, "Dashboard refreshed successfully");
            _logger.LogInformation("Dashboard data refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing dashboard data");
            SetBusyState(false, "Failed to refresh dashboard data");
        }
    }

    /// <summary>
    /// Loads the dashboard data
    /// </summary>
    private void LoadDashboardData()
    {
        // Load recent activities
        RecentActivities.Clear();
        RecentActivities.Add(new ActivityItem("Training Started", "GPT-4 Fine-tuning", DateTime.Now.AddMinutes(-15)));
        RecentActivities.Add(new ActivityItem("Model Deployed", "Vision Transformer", DateTime.Now.AddHours(-2)));
        RecentActivities.Add(new ActivityItem("Dataset Uploaded", "Image Classification Dataset", DateTime.Now.AddHours(-4)));
        RecentActivities.Add(new ActivityItem("Training Completed", "BERT Language Model", DateTime.Now.AddHours(-6)));

        // Load quick actions
        QuickActions.Clear();
        QuickActions.Add(new QuickActionItem("New Chat", "Start a conversation with AI", "💬", StartNewChatCommand));
        QuickActions.Add(new QuickActionItem("Load Model", "Load and configure AI model", "🤖", LoadModelCommand));
        QuickActions.Add(new QuickActionItem("Start Training", "Begin model training session", "🚀", StartTrainingCommand));
        QuickActions.Add(new QuickActionItem("System Settings", "Configure system preferences", "⚙️", OpenSystemSettingsCommand));
        
        // Add advanced actions based on view mode
        if (ShowAdvancedFeatures)
        {
            QuickActions.Add(new QuickActionItem("View Jobs", "Monitor running tasks", "📊", NavigateToJobsCommand));
            QuickActions.Add(new QuickActionItem("Manage Models", "Configure AI models", "🔧", NavigateToModelsCommand));
        }
        
        if (ShowDeveloperFeatures)
        {
            QuickActions.Add(new QuickActionItem("Training Console", "Advanced training controls", "🛠️", NavigateToTrainingCommand));
        }
    }
    
    /// <summary>
    /// Initializes performance metrics collection
    /// </summary>
    private void InitializePerformanceMetrics()
    {
        PerformanceMetrics.Clear();
        PerformanceMetrics.Add(new PerformanceMetric("CPU", SystemLoad, "%", GetStatusColor(SystemLoad)));
        PerformanceMetrics.Add(new PerformanceMetric("Memory", MemoryUsage, "%", GetStatusColor(MemoryUsage)));
        PerformanceMetrics.Add(new PerformanceMetric("GPU", GpuUtilization, "%", GetStatusColor(GpuUtilization)));
    }
    
    /// <summary>
    /// Gets status color based on utilization percentage
    /// </summary>
    /// <param name="percentage">The utilization percentage</param>
    /// <returns>Color string for the status</returns>
    private string GetStatusColor(double percentage)
    {
        return percentage switch
        {
            > 80 => "#F44336", // Red - High
            > 60 => "#FF9800", // Orange - Medium
            _ => "#4CAF50"       // Green - Normal
        };
    }
    
    /// <summary>
    /// Handles refresh timer tick
    /// </summary>
    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            // Update metrics from system status service
            SystemLoad = _systemStatusService.SystemLoadPercentage;
            MemoryUsage = _systemStatusService.MemoryUsage;
            GpuUtilization = _systemStatusService.GpuUtilization;
            ActiveProjects = _systemStatusService.ActiveModels;
            TotalModels = _systemStatusService.RunningJobs;
            TrainingHours = _systemStatusService.TotalTrainingHours;
            SystemStatus = _systemStatusService.CurrentStatus;
            SystemStatusText = GetStatusText(SystemStatus);
            LastUpdateTime = DateTime.Now;
            
            // Update comprehensive performance metrics with detailed hardware data
            UpdateComprehensivePerformanceMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dashboard auto-refresh");
        }
    }
    
    /// <summary>
    /// Initializes hardware data from the inventory service
    /// </summary>
    private async Task InitializeHardwareDataAsync()
    {
        try
        {
            // Wait for hardware inventory service to complete discovery with retry logic
            int attempts = 0;
            int maxAttempts = 10;
            
            while (attempts < maxAttempts)
            {
                await Task.Delay(1000); // Wait 1 second between checks
                attempts++;
                
                // Check if hardware discovery is complete
                if (!string.IsNullOrEmpty(_hardwareInventoryService.CpuSpecification.ProcessorName) &&
                    _hardwareInventoryService.MemorySpecification.TotalPhysicalGB > 0)
                {
                    // Ensure UI updates happen on the UI thread
                    System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
                    {
                        // Load current hardware data from the inventory service
                        CpuSpecification = _hardwareInventoryService.CpuSpecification;
                        MemorySpecification = _hardwareInventoryService.MemorySpecification;
                        
                        // Update ObservableCollection properties with detected hardware
                        GpuSpecifications.Clear();
                        foreach (var gpu in _hardwareInventoryService.GpuSpecifications)
                        {
                            GpuSpecifications.Add(gpu);
                        }
                        
                        StorageDevices.Clear();
                        foreach (var device in _hardwareInventoryService.StorageDevices)
                        {
                            StorageDevices.Add(device);
                        }
                        
                        HardwareRollupSummary = _hardwareInventoryService.RollupSummary;
                        
                        // Update comprehensive performance metrics with real hardware data
                        UpdateComprehensivePerformanceMetrics();
                        
                        _logger.LogInformation("Hardware data initialized successfully: {CpuName}, {MemoryGB:F1}GB, {GpuCount} GPUs, {DriveCount} drives",
                            CpuSpecification.ProcessorName, MemorySpecification.TotalPhysicalGB, 
                            GpuSpecifications.Count, StorageDevices.Count);
                    });
                    return;
                }
            }
            
            _logger.LogWarning("Hardware data initialization timeout after {Attempts} attempts - will rely on InventoryUpdated events", maxAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing hardware data - will rely on InventoryUpdated events");
        }
    }
    
    /// <summary>
    /// Updates performance metrics in the collection
    /// </summary>
    private void UpdatePerformanceMetrics()
    {
        foreach (var metric in PerformanceMetrics)
        {
            switch (metric.Name)
            {
                case "CPU":
                    metric.Value = SystemLoad;
                    metric.Color = GetStatusColor(SystemLoad);
                    break;
                case "Memory":
                    metric.Value = MemoryUsage;
                    metric.Color = GetStatusColor(MemoryUsage);
                    break;
                case "GPU":
                    metric.Value = GpuUtilization;
                    metric.Color = GetStatusColor(GpuUtilization);
                    break;
            }
        }
    }

    /// <summary>
    /// Updates comprehensive performance metrics with detailed hardware consciousness
    /// </summary>
    private void UpdateComprehensivePerformanceMetrics()
    {
        PerformanceMetrics.Clear();

        // CPU Comprehensive Metrics - Add descriptive label
        PerformanceMetrics.Add(new PerformanceMetric(
            $"CPU (Average Core Utilization): {CpuSpecification.ProcessorName}", 
            CpuSpecification.CurrentUsage, 
            "%", 
            GetStatusColor(CpuSpecification.CurrentUsage)));

        // Individual CPU Core Metrics (if advanced features enabled)
        if (ShowAdvancedFeatures)
        {
            foreach (var core in CpuSpecification.CoreMetrics.Take(8)) // Show first 8 cores to avoid clutter
            {
                PerformanceMetrics.Add(new PerformanceMetric(
                    $"  {core.CoreName}", 
                    core.Usage, 
                    "%", 
                    GetStatusColor(core.Usage)));
            }
        }

        // Memory Comprehensive Metrics - Show actual usage instead of just percentage
        PerformanceMetrics.Add(new PerformanceMetric(
            $"Memory (Physical RAM Usage): {MemorySpecification.UsageShortDisplayText}", 
            MemorySpecification.UsagePercentage, 
            "%", 
            GetStatusColor(MemorySpecification.UsagePercentage)));

        // Individual RAM Module Metrics (if developer features enabled)
        if (ShowDeveloperFeatures)
        {
            foreach (var module in MemorySpecification.MemoryModules.Take(4)) // Limit display
            {
                PerformanceMetrics.Add(new PerformanceMetric(
                    $"  {module.BankLabel}: {module.CapacityGB:F0}GB @ {module.SpeedMhz}MHz", 
                    0, // No individual module usage available
                    "", 
                    "#4CAF50"));
            }
        }

        // GPU Comprehensive Metrics
        foreach (var gpu in GpuSpecifications.Take(3)) // Limit to 3 GPUs for display
        {
            PerformanceMetrics.Add(new PerformanceMetric(
                $"GPU (Utilization): {gpu.Name}", 
                gpu.CurrentUsage, 
                "%", 
                GetStatusColor(gpu.CurrentUsage)));

            if (ShowAdvancedFeatures)
            {
                PerformanceMetrics.Add(new PerformanceMetric(
                    $"  VRAM: {gpu.AdapterRamGB:F1}GB", 
                    gpu.MemoryUsagePercentage, 
                    "%", 
                    GetStatusColor(gpu.MemoryUsagePercentage)));
            }
        }

        // Storage Comprehensive Metrics - Show capacity instead of percentage
        foreach (var drive in StorageDevices.Where(d => !string.IsNullOrEmpty(d.DriveLetter)).Take(4))
        {
            PerformanceMetrics.Add(new PerformanceMetric(
                $"Storage (Used/Available) {drive.DriveLetter}: {drive.UsageShortDisplayText}", 
                drive.UsagePercentage, 
                "%", 
                GetStorageHealthColor(drive.HealthStatus)));
        }

        // System Temperature Metrics (if advanced features enabled)
        if (ShowAdvancedFeatures)
        {
            PerformanceMetrics.Add(new PerformanceMetric(
                "CPU Temp", 
                CpuSpecification.CurrentTemperature, 
                "°C", 
                GetTemperatureColor(CpuSpecification.CurrentTemperature)));

            foreach (var gpu in GpuSpecifications.Take(2))
            {
                PerformanceMetrics.Add(new PerformanceMetric(
                    $"GPU Temp", 
                    gpu.CurrentTemperature, 
                    "°C", 
                    GetTemperatureColor(gpu.CurrentTemperature)));
            }
        }

        // System Health Summary
        PerformanceMetrics.Add(new PerformanceMetric(
            $"Overall Health: {HardwareRollupSummary.OverallHealth}", 
            (double)HardwareRollupSummary.OverallHealth * 20, // Convert enum to percentage
            "", 
            GetHealthStatusColor(HardwareRollupSummary.OverallHealth)));
    }

    /// <summary>
    /// Gets color for storage device health status
    /// </summary>
    private string GetStorageHealthColor(DriveHealth health)
    {
        return health switch
        {
            DriveHealth.Excellent => "#4CAF50",
            DriveHealth.Good => "#8BC34A",
            DriveHealth.Fair => "#FFC107",
            DriveHealth.Poor => "#FF9800",
            DriveHealth.Critical => "#F44336",
            DriveHealth.Failing => "#D32F2F",
            _ => "#9E9E9E"
        };
    }

    /// <summary>
    /// Gets color for temperature readings
    /// </summary>
    private string GetTemperatureColor(double temperature)
    {
        return temperature switch
        {
            > 80 => "#F44336", // Red - Very Hot
            > 70 => "#FF9800", // Orange - Hot
            > 60 => "#FFC107", // Yellow - Warm
            _ => "#4CAF50"      // Green - Cool
        };
    }

    /// <summary>
    /// Gets color for overall system health status
    /// </summary>
    private string GetHealthStatusColor(SystemHealthStatus health)
    {
        return health switch
        {
            SystemHealthStatus.Excellent => "#4CAF50",
            SystemHealthStatus.Good => "#8BC34A",
            SystemHealthStatus.Fair => "#FFC107",
            SystemHealthStatus.Poor => "#FF9800",
            SystemHealthStatus.Critical => "#F44336",
            _ => "#9E9E9E"
        };
    }
    
    /// <summary>
    /// Gets human-readable status text from system status enum
    /// </summary>
    /// <param name="status">The system status</param>
    /// <returns>Human-readable status text</returns>
    private string GetStatusText(SystemStatus status)
    {
        return status switch
        {
            SystemStatus.Starting => "Starting up...",
            SystemStatus.Ready => "Ready",
            SystemStatus.Busy => "Processing",
            SystemStatus.Training => "Training in progress",
            SystemStatus.Warning => "Warning - Check logs",
            SystemStatus.Error => "Error detected",
            SystemStatus.Shutdown => "Shutting down...",
            _ => "Unknown"
        };
    }
    
    /// <summary>
    /// Handles system status change events
    /// </summary>
    private void OnSystemStatusChanged(object? sender, SystemStatusChangedEventArgs e)
    {
        SystemStatus = e.NewStatus;
        SystemStatusText = GetStatusText(e.NewStatus);
        
        if (!string.IsNullOrEmpty(e.Message))
        {
            StatusMessage = e.Message;
        }
        
        _logger.LogInformation("System status changed from {PreviousStatus} to {NewStatus}", 
            e.PreviousStatus, e.NewStatus);
    }
    
    /// <summary>
    /// Handles view mode change events
    /// </summary>
    private void OnViewModeChanged(object? sender, ViewModeChangedEventArgs e)
    {
        // Refresh UI visibility based on new view mode
        OnPropertyChanged(nameof(ShowAdvancedFeatures));
        OnPropertyChanged(nameof(ShowDeveloperFeatures));
        OnPropertyChanged(nameof(CurrentViewMode));
        
        // Reload quick actions to reflect view mode changes
        LoadDashboardData();
        
        _logger.LogInformation("View mode changed from {PreviousMode} to {NewMode}", 
            e.PreviousMode, e.NewMode);
    }

    /// <summary>
    /// Handles hardware inventory update events with comprehensive system penetration
    /// </summary>
    private void OnHardwareInventoryUpdated(object? sender, HardwareInventoryEventArgs e)
    {
        try
        {
            // Ensure UI updates happen on the UI thread
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                // Update comprehensive hardware specifications
                CpuSpecification = e.CpuSpecification;
                MemorySpecification = e.MemorySpecification;
                
                // Update ObservableCollection properties with latest hardware data
                GpuSpecifications.Clear();
                foreach (var gpu in e.GpuSpecifications)
                {
                    GpuSpecifications.Add(gpu);
                }
                
                StorageDevices.Clear();
                foreach (var device in e.StorageDevices)
                {
                    StorageDevices.Add(device);
                }
                
                HardwareRollupSummary = e.RollupSummary;

                // Update legacy metrics for compatibility
                SystemLoad = e.RollupSummary.TotalCpuUsage;
                MemoryUsage = e.RollupSummary.TotalMemoryUsage;
                GpuUtilization = e.RollupSummary.TotalGpuUsage;

                // Update detailed performance metrics collection
                UpdateComprehensivePerformanceMetrics();

                LastUpdateTime = e.Timestamp;
            });

            _logger.LogDebug("Hardware inventory updated - {CpuCores}C CPU @ {CpuUsage:F1}%, {MemoryGB:F1}GB RAM @ {MemoryUsage:F1}%, {GpuCount} GPUs @ {GpuUsage:F1}%, {DriveCount} drives",
                e.CpuSpecification.CoreCount,
                e.CpuSpecification.CurrentUsage,
                e.MemorySpecification.TotalPhysicalGB,
                e.MemorySpecification.UsagePercentage,
                e.GpuSpecifications.Count,
                e.RollupSummary.TotalGpuUsage,
                e.StorageDevices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing hardware inventory update");
        }
    }
    
    /// <summary>
    /// Starts a new chat session
    /// </summary>
    private Task StartNewChatAsync()
    {
        try
        {
            SetBusyState(true, "Starting new chat session...");
            
            // Navigate to conversations
            _navigationService.NavigateToSection(NavigationSection.Conversations);
            
            // Add recent activity
            RecentActivities.Insert(0, new ActivityItem("Chat Started", "New conversation session", DateTime.Now));
            
            SetBusyState(false, "Chat session started");
            _logger.LogInformation("New chat session started");
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting new chat session");
            SetBusyState(false, "Failed to start chat session");
            return Task.FromException(ex);
        }
    }
    
    /// <summary>
    /// Loads a model
    /// </summary>
    private async Task LoadModelAsync()
    {
        try
        {
            SetBusyState(true, "Loading model...");
            
            // Simulate model loading delay
            await Task.Delay(2000);
            
            // Navigate to model configuration
            _navigationService.NavigateToSection(NavigationSection.ModelConfiguration);
            
            // Add recent activity
            RecentActivities.Insert(0, new ActivityItem("Model Loaded", "AI model configuration updated", DateTime.Now));
            
            SetBusyState(false, "Model loaded successfully");
            _logger.LogInformation("Model loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading model");
            SetBusyState(false, "Failed to load model");
        }
    }
    
    /// <summary>
    /// Starts a training session
    /// </summary>
    private async Task StartTrainingAsync()
    {
        try
        {
            SetBusyState(true, "Preparing training session...");
            
            // Simulate training preparation delay
            await Task.Delay(1500);
            
            // Navigate to training section
            _navigationService.NavigateToSection(NavigationSection.Training);
            
            // Add recent activity
            RecentActivities.Insert(0, new ActivityItem("Training Started", "New model training session", DateTime.Now));
            
            SetBusyState(false, "Training session started");
            _logger.LogInformation("Training session started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training session");
            SetBusyState(false, "Failed to start training");
        }
    }
    
    /// <summary>
    /// Opens system settings
    /// </summary>
    private async Task OpenSystemSettingsAsync()
    {
        try
        {
            SetBusyState(true, "Opening system settings...");
            
            // Simulate settings loading
            await Task.Delay(500);
            
            // TODO: Navigate to settings when available
            // For now, just update status
            
            SetBusyState(false, "System settings opened");
            _logger.LogInformation("System settings accessed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening system settings");
            SetBusyState(false, "Failed to open settings");
        }
    }
    
    /// <summary>
    /// Disposes of resources used by the DashboardViewModel
    /// </summary>
    protected override void DisposeResources()
    {
        // All UI-related cleanup must happen on the UI thread
        ExecuteOnUIThread(() =>
        {
            // Stop timer properly (DispatcherTimer doesn't implement IDisposable)
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= RefreshTimer_Tick;
            }
            
            // Clear collections to break reference chains
            RecentActivities.Clear();
            QuickActions.Clear();
            PerformanceMetrics.Clear();
            GpuSpecifications.Clear();
            StorageDevices.Clear();
        });
        
        // Unsubscribe from service events to prevent memory leaks
        if (_systemStatusService != null)
        {
            _systemStatusService.StatusChanged -= OnSystemStatusChanged;
        }
        
        if (_viewModeService != null)
        {
            _viewModeService.ViewModeChanged -= OnViewModeChanged;
        }

        if (_hardwareInventoryService != null)
        {
            _hardwareInventoryService.InventoryUpdated -= OnHardwareInventoryUpdated;
        }

        _logger.LogDebug("DashboardViewModel resources disposed successfully");
        
        base.DisposeResources();
    }
}

/// <summary>
/// Represents an activity item in the dashboard
/// </summary>
public class ActivityItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityItem"/> class
    /// </summary>
    /// <param name="action">The action performed</param>
    /// <param name="target">The target of the action</param>
    /// <param name="timestamp">When the action occurred</param>
    public ActivityItem(string action, string target, DateTime timestamp)
    {
        Action = action;
        Target = target;
        Timestamp = timestamp;
    }

    /// <summary>
    /// Gets the action performed
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Gets the target of the action
    /// </summary>
    public string Target { get; }

    /// <summary>
    /// Gets when the action occurred
    /// </summary>
    public DateTime Timestamp { get; }
}

/// <summary>
/// Represents a quick action item in the dashboard
/// </summary>
public class QuickActionItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuickActionItem"/> class
    /// </summary>
    /// <param name="title">The action title</param>
    /// <param name="description">The action description</param>
    /// <param name="icon">The action icon</param>
    /// <param name="command">The command to execute</param>
    public QuickActionItem(string title, string description, string icon, IRelayCommand? command = null)
    {
        Title = title;
        Description = description;
        Icon = icon;
        Command = command;
    }

    /// <summary>
    /// Gets the action title
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the action description
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the action icon
    /// </summary>
    public string Icon { get; }
    
    /// <summary>
    /// Gets the command to execute when action is triggered
    /// </summary>
    public IRelayCommand? Command { get; }
}

/// <summary>
/// Represents a performance metric in the dashboard
/// </summary>
public class PerformanceMetric
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMetric"/> class
    /// </summary>
    /// <param name="name">The metric name</param>
    /// <param name="value">The metric value</param>
    /// <param name="unit">The unit of measurement</param>
    /// <param name="color">The color for visualization</param>
    public PerformanceMetric(string name, double value, string unit, string color)
    {
        Name = name;
        Value = value;
        Unit = unit;
        Color = color;
    }

    /// <summary>
    /// Gets the metric name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the metric value
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets the unit of measurement
    /// </summary>
    public string Unit { get; }
    
    /// <summary>
    /// Gets or sets the color for visualization
    /// </summary>
    public string Color { get; set; }
}