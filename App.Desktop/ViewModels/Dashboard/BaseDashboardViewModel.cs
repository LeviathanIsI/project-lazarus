using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using App.Shared.Enums;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.ViewModels.Dashboard
{
    /// <summary>
    /// Base class for all Dashboard ViewModels
    /// Provides common functionality and widget management
    /// </summary>
    public abstract class BaseDashboardViewModel : INotifyPropertyChanged, IDisposable
    {
        protected readonly ISystemMonitor _systemMonitor;
        protected readonly IModelManager _modelManager;
        protected readonly ITrainingService _trainingService;
        protected readonly UserPreferencesService _preferencesService;
        protected readonly System.Timers.Timer _refreshTimer;
        
        private ViewMode _currentViewMode;
        private bool _isLoading;
        private string _statusMessage = "";
        private DateTime _lastRefresh;

        protected BaseDashboardViewModel(
            ISystemMonitor systemMonitor,
            IModelManager modelManager,
            ITrainingService trainingService,
            UserPreferencesService preferencesService)
        {
            _systemMonitor = systemMonitor ?? throw new ArgumentNullException(nameof(systemMonitor));
            _modelManager = modelManager ?? throw new ArgumentNullException(nameof(modelManager));
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));

            Widgets = new ObservableCollection<IDashboardWidget>();
            
            // Subscribe to ViewMode changes
            _preferencesService.PropertyChanged += OnPreferencesChanged;
            _currentViewMode = _preferencesService.CurrentViewMode;

            // Setup refresh timer - different intervals for different modes (but don't start it yet)
            _refreshTimer = new System.Timers.Timer(GetRefreshInterval());
            _refreshTimer.Elapsed += OnTimerElapsed;
            _refreshTimer.AutoReset = true;
            
            // Initialize immediately and synchronously on the UI thread
            InitializeSynchronously();
        }

        /// <summary>
        /// Synchronous initialization to avoid threading issues
        /// </summary>
        private void InitializeSynchronously()
        {
            try
            {
                // Subscribe to data service events
                _systemMonitor.StatusChanged += OnSystemStatusChanged;
                _modelManager.ModelChanged += OnModelChanged;
                _trainingService.JobStatusChanged += OnTrainingJobStatusChanged;

                // Ensure widget initialization happens on UI thread
                if (!App.Current.Dispatcher.CheckAccess())
                {
                    App.Current.Dispatcher.Invoke(() => InitializeWidgetsOnUIThread());
                }
                else
                {
                    InitializeWidgetsOnUIThread();
                }

                // Start the refresh timer
                _refreshTimer.Start();
                
                StatusMessage = "Dashboard ready";
                IsLoading = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Dashboard initialization failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Dashboard initialization error: {ex}");
                IsLoading = false;
            }
        }

        private void InitializeWidgetsOnUIThread()
        {
            // Initialize widgets synchronously on UI thread
            InitializeWidgets();
            
            // Initialize all widgets
            foreach (var widget in Widgets)
            {
                widget.Initialize();
            }
        }

        #region Properties

        /// <summary>
        /// Current ViewMode for this dashboard
        /// </summary>
        public ViewMode CurrentViewMode
        {
            get => _currentViewMode;
            set
            {
                if (SetProperty(ref _currentViewMode, value))
                {
                    OnViewModeChanged();
                }
            }
        }

        /// <summary>
        /// Collection of widgets for this dashboard
        /// </summary>
        public ObservableCollection<IDashboardWidget> Widgets { get; }

        /// <summary>
        /// Whether the dashboard is currently loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Current status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Last refresh timestamp
        /// </summary>
        public DateTime LastRefresh
        {
            get => _lastRefresh;
            set => SetProperty(ref _lastRefresh, value);
        }

        /// <summary>
        /// Formatted last refresh time for display
        /// </summary>
        public string LastRefreshDisplay => _lastRefresh == default 
            ? "Never" 
            : $"Last updated: {_lastRefresh:HH:mm:ss}";

        #endregion

        #region Abstract Members

        /// <summary>
        /// Initialize widgets specific to this dashboard type
        /// </summary>
        protected abstract void InitializeWidgets();

        /// <summary>
        /// Get the refresh interval for this dashboard type
        /// </summary>
        protected abstract double GetRefreshInterval();

        /// <summary>
        /// Handle ViewMode changes - derived classes can override
        /// </summary>
        protected virtual void OnViewModeChanged()
        {
            // Ensure updates happen on UI thread
            App.Current?.Dispatcher.Invoke(() =>
            {
                foreach (var widget in Widgets)
                {
                    widget.RefreshData();
                }
                // Update refresh interval
                _refreshTimer.Interval = GetRefreshInterval();
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the dashboard - call after construction
        /// </summary>
        public virtual async Task InitializeAsync()
        {
            // Ensure we're on the UI thread
            if (!App.Current.Dispatcher.CheckAccess())
            {
                await App.Current.Dispatcher.InvokeAsync(async () => await InitializeAsync());
                return;
            }

            // Remove loading overlay semantics; initialize silently
            IsLoading = false;
            StatusMessage = string.Empty;

            try
            {
                // Subscribe to data service events on UI thread
                _systemMonitor.StatusChanged += OnSystemStatusChanged;
                _modelManager.ModelChanged += OnModelChanged;
                _trainingService.JobStatusChanged += OnTrainingJobStatusChanged;

                InitializeWidgets();
                
                // Initialize all widgets
                foreach (var widget in Widgets)
                {
                    widget.Initialize();
                }

                await RefreshAllDataAsync();
                
                // Start the refresh timer
                _refreshTimer.Start();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Dashboard initialization failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Dashboard initialization error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Refresh all dashboard data
        /// </summary>
        public virtual async Task RefreshAllDataAsync()
        {
            // Marshal to UI thread; widgets may touch DependencyObjects
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var widget in Widgets.Where(w => w.IsVisible))
                {
                    try
                    {
                        widget.RefreshData();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Widget refresh error: {ex.Message}");
                    }
                }
                LastRefresh = DateTime.Now;
            });
        }

        /// <summary>
        /// Add a widget to the dashboard
        /// </summary>
        public void AddWidget(IDashboardWidget widget)
        {
            if (widget == null) throw new ArgumentNullException(nameof(widget));
            
            Widgets.Add(widget);
            widget.DataChanged += OnWidgetDataChanged;
            widget.ConfigurationChanged += OnWidgetConfigurationChanged;
            
            if (widget.SupportedModes.Contains(CurrentViewMode))
            {
                widget.Initialize();
            }
        }

        /// <summary>
        /// Remove a widget from the dashboard
        /// </summary>
        public void RemoveWidget(IDashboardWidget widget)
        {
            if (widget == null) return;
            
            widget.DataChanged -= OnWidgetDataChanged;
            widget.ConfigurationChanged -= OnWidgetConfigurationChanged;
            widget.Dispose();
            
            Widgets.Remove(widget);
        }

        #endregion

        #region Event Handlers

        private void OnPreferencesChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(UserPreferencesService.CurrentViewMode))
            {
                CurrentViewMode = _preferencesService.CurrentViewMode;
            }
        }

        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            await RefreshAllDataAsync();
        }

        protected virtual void OnSystemStatusChanged(object? sender, SystemStatusChangedEventArgs e)
        {
            // Base implementation - derived classes can override
            App.Current?.Dispatcher.Invoke(() =>
            {
                StatusMessage = e.SystemStatus.IsOnline ? "System Online" : "System Offline";
            });
        }

        protected virtual void OnModelChanged(object? sender, ModelChangedEventArgs e)
        {
            // Base implementation - derived classes can override
            App.Current?.Dispatcher.Invoke(() =>
            {
                StatusMessage = e.NewModel != null 
                    ? $"Model changed to {e.NewModel.Name}" 
                    : "No model loaded";
            });
        }

        protected virtual void OnTrainingJobStatusChanged(object? sender, TrainingJobStatusChangedEventArgs e)
        {
            // Base implementation - derived classes can override
            App.Current?.Dispatcher.Invoke(() =>
            {
                StatusMessage = $"Training job {e.Job.Name}: {e.NewStatus}";
            });
        }

        private void OnWidgetDataChanged(object? sender, WidgetDataChangedEventArgs e)
        {
            // Handle widget data changes if needed
            System.Diagnostics.Debug.WriteLine($"Widget {e.WidgetId} data changed at {e.Timestamp}");
        }

        private void OnWidgetConfigurationChanged(object? sender, WidgetConfigurationChangedEventArgs e)
        {
            // Handle widget configuration changes if needed
            System.Diagnostics.Debug.WriteLine($"Widget {e.WidgetId} configuration changed");
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public virtual void Dispose()
        {
            if (_disposed) return;

            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();

            // Unsubscribe from events
            if (_preferencesService != null)
                _preferencesService.PropertyChanged -= OnPreferencesChanged;
            
            if (_systemMonitor != null)
                _systemMonitor.StatusChanged -= OnSystemStatusChanged;
                
            if (_modelManager != null)
                _modelManager.ModelChanged -= OnModelChanged;
                
            if (_trainingService != null)
                _trainingService.JobStatusChanged -= OnTrainingJobStatusChanged;

            // Dispose all widgets
            foreach (var widget in Widgets)
            {
                widget.DataChanged -= OnWidgetDataChanged;
                widget.ConfigurationChanged -= OnWidgetConfigurationChanged;
                widget.Dispose();
            }

            Widgets.Clear();
            _disposed = true;
        }

        #endregion
    }
}
