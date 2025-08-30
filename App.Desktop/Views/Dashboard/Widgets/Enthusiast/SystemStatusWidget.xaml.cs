using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using App.Shared.Enums;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Dashboard.Widgets.Enthusiast
{
    /// <summary>
    /// System Status Widget for Enthusiast Dashboard
    /// Shows detailed runner states, memory usage, and API performance metrics
    /// </summary>
    public partial class SystemStatusWidget : UserControl, IDashboardWidget, INotifyPropertyChanged
    {
        #region Fields

        private readonly ISystemMonitor? _systemMonitor;
        
        private ObservableCollection<RunnerStatusInfo> _runnerStates = new();
        private double _memoryPercentage = 0;
        private string _memoryUsageText = "0 / 0 GB";
        private string _apiResponseTimeText = "0ms";
        private string _lastUpdateText = "Never";
        private Brush _statusColor = Brushes.Gray;

        #endregion

        #region Properties

        public ViewMode[] SupportedModes => new[] { ViewMode.Enthusiast };
        public string Title { get; set; } = "System Status";
        bool IDashboardWidget.IsVisible => SupportedModes.Contains(CurrentViewMode);
        public UserControl WidgetContent => this;
        public WidgetPosition Position { get; set; } = new WidgetPosition();
        public WidgetConfiguration Configuration { get; set; } = new WidgetConfiguration { WidgetId = "system_status_enthusiast" };
        protected ViewMode CurrentViewMode => ((App)App.Current)?.ServiceProvider?.GetService<UserPreferencesService>()?.CurrentViewMode ?? ViewMode.Enthusiast;

        public ObservableCollection<RunnerStatusInfo> RunnerStates
        {
            get => _runnerStates;
            set => SetProperty(ref _runnerStates, value);
        }

        public double MemoryPercentage
        {
            get => _memoryPercentage;
            set => SetProperty(ref _memoryPercentage, value);
        }

        public string MemoryUsageText
        {
            get => _memoryUsageText;
            set => SetProperty(ref _memoryUsageText, value);
        }

        public string ApiResponseTimeText
        {
            get => _apiResponseTimeText;
            set => SetProperty(ref _apiResponseTimeText, value);
        }

        public string LastUpdateText
        {
            get => _lastUpdateText;
            set => SetProperty(ref _lastUpdateText, value);
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        #endregion

        #region Constructor

        public SystemStatusWidget()
        {
            InitializeComponent();
            DataContext = this;
            
            Title = "System Status";
            Configuration.WidgetId = "system_status_enthusiast";
            Configuration.RefreshIntervalSeconds = 2;
            
            // Get services from DI container
            _systemMonitor = ((App)App.Current)?.ServiceProvider?.GetService<ISystemMonitor>();
        }

        #endregion

        #region IDashboardWidget Implementation

        public void Initialize()
        {
            if (_systemMonitor != null)
            {
                _systemMonitor.StatusChanged += OnSystemStatusChanged;
            }
            
            RefreshData();
        }

        public void RefreshData()
        {
            if (_systemMonitor == null) return;
            
            try
            {
                var systemStatus = _systemMonitor.GetCurrentStatus();
                var memoryUsage = _systemMonitor.GetMemoryUsage();
                
                // Update runner states
                UpdateRunnerStates(systemStatus.RunnerStates);
                
                // Update memory usage
                MemoryPercentage = memoryUsage.SystemRamPercentage;
                MemoryUsageText = memoryUsage.SystemRamDisplay;
                
                // Update API response time
                ApiResponseTimeText = $"{systemStatus.ApiResponseTime.TotalMilliseconds:F0}ms";
                
                // Update overall status color
                StatusColor = systemStatus.IsOnline 
                    ? new SolidColorBrush(Color.FromRgb(34, 197, 94))  // Green
                    : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                
                // Update timestamp
                LastUpdateText = DateTime.Now.ToString("HH:mm:ss");
                
                DataChanged?.Invoke(this, new WidgetDataChangedEventArgs
                {
                    WidgetId = Configuration.WidgetId,
                    OldData = null,
                    NewData = systemStatus,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SystemStatusWidget refresh error: {ex.Message}");
                SetErrorState();
            }
        }

        public void Dispose()
        {
            if (_systemMonitor != null)
            {
                _systemMonitor.StatusChanged -= OnSystemStatusChanged;
            }
        }

        public event EventHandler<WidgetDataChangedEventArgs>? DataChanged;
        public event EventHandler<WidgetConfigurationChangedEventArgs>? ConfigurationChanged;

        #endregion

        #region Private Methods

        private void UpdateRunnerStates(Dictionary<RunnerType, RunnerStatus> runnerStates)
        {
            RunnerStates.Clear();
            
            foreach (var kvp in runnerStates)
            {
                var runnerInfo = new RunnerStatusInfo
                {
                    RunnerName = GetRunnerDisplayName(kvp.Key),
                    Status = kvp.Value,
                    StatusText = GetStatusDisplayText(kvp.Value),
                    StatusColor = GetStatusColor(kvp.Value)
                };
                
                RunnerStates.Add(runnerInfo);
            }
        }

        private string GetRunnerDisplayName(RunnerType runnerType)
        {
            return runnerType switch
            {
                RunnerType.LlamaCpp => "llama.cpp",
                RunnerType.LlamaServer => "llama-server",
                RunnerType.vLLM => "vLLM",
                RunnerType.ExLlamaV2 => "ExLlamaV2",
                RunnerType.Ollama => "Ollama",
                _ => runnerType.ToString()
            };
        }

        private string GetStatusDisplayText(RunnerStatus status)
        {
            return status switch
            {
                RunnerStatus.Online => "Online",
                RunnerStatus.Offline => "Offline",
                RunnerStatus.Starting => "Starting",
                RunnerStatus.Stopping => "Stopping",
                RunnerStatus.Error => "Error",
                _ => "Unknown"
            };
        }

        private Brush GetStatusColor(RunnerStatus status)
        {
            return status switch
            {
                RunnerStatus.Online => new SolidColorBrush(Color.FromRgb(34, 197, 94)),   // Green
                RunnerStatus.Starting => new SolidColorBrush(Color.FromRgb(249, 115, 22)), // Orange
                RunnerStatus.Stopping => new SolidColorBrush(Color.FromRgb(249, 115, 22)), // Orange
                RunnerStatus.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),     // Red
                _ => new SolidColorBrush(Color.FromRgb(156, 163, 175))                     // Gray
            };
        }

        private void SetErrorState()
        {
            RunnerStates.Clear();
            RunnerStates.Add(new RunnerStatusInfo
            {
                RunnerName = "System",
                Status = RunnerStatus.Error,
                StatusText = "Error",
                StatusColor = new SolidColorBrush(Color.FromRgb(239, 68, 68))
            });
            
            MemoryPercentage = 0;
            MemoryUsageText = "Error";
            ApiResponseTimeText = "Error";
            StatusColor = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            LastUpdateText = "Error";
        }

        private void OnSystemStatusChanged(object? sender, SystemStatusChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RefreshData();
            });
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
    }

    #region Data Models

    /// <summary>
    /// Runner status information for display
    /// </summary>
    public class RunnerStatusInfo
    {
        public string RunnerName { get; set; } = "";
        public RunnerStatus Status { get; set; }
        public string StatusText { get; set; } = "";
        public Brush StatusColor { get; set; } = Brushes.Gray;
    }

    #endregion
}
