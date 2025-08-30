using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using App.Shared.Enums;
using Lazarus.Desktop.Helpers;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Dashboard.Widgets
{
    /// <summary>
    /// Chat Status Widget for Novice Dashboard
    /// Shows simple chat connection status and provides quick access to chat
    /// </summary>
    public partial class ChatStatusWidget : UserControl, IDashboardWidget, INotifyPropertyChanged
    {
        #region Fields

        private readonly ISystemMonitor? _systemMonitor;
        private readonly INavigationService? _navigationService;
        
        private bool _isChatReady = false;
        private bool _isConnecting = false;
        private string _statusText = "Setting up...";
        private string _subtitleText = "Initializing chat system";
        private string _actionButtonText = "Open Chat";
        private bool _isActionEnabled = false;
        private Brush _statusColor = Brushes.Orange;
        private Color _statusGlowColor = Colors.Orange;

        #endregion

        #region Properties

        public ViewMode[] SupportedModes => new[] { ViewMode.Novice };
        
        public string Title { get; set; } = "Chat Status";
        bool IDashboardWidget.IsVisible => SupportedModes.Contains(CurrentViewMode);
        public UserControl WidgetContent => this;
        public WidgetPosition Position { get; set; } = new WidgetPosition();
        public WidgetConfiguration Configuration { get; set; } = new WidgetConfiguration { WidgetId = "chat_status_novice" };
        
        protected ViewMode CurrentViewMode => ((App)App.Current)?.ServiceProvider?.GetService<UserPreferencesService>()?.CurrentViewMode ?? ViewMode.Novice;

        public bool IsChatReady
        {
            get => _isChatReady;
            set => SetProperty(ref _isChatReady, value);
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set => SetProperty(ref _isConnecting, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string SubtitleText
        {
            get => _subtitleText;
            set => SetProperty(ref _subtitleText, value);
        }

        public string ActionButtonText
        {
            get => _actionButtonText;
            set => SetProperty(ref _actionButtonText, value);
        }

        public bool IsActionEnabled
        {
            get => _isActionEnabled;
            set => SetProperty(ref _isActionEnabled, value);
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        public Color StatusGlowColor
        {
            get => _statusGlowColor;
            set => SetProperty(ref _statusGlowColor, value);
        }

        public ICommand ActionCommand { get; }

        #endregion

        #region Constructor

        public ChatStatusWidget()
        {
            InitializeComponent();
            DataContext = this;
            
            // Get services from DI container
            _systemMonitor = ((App)App.Current)?.ServiceProvider?.GetService<ISystemMonitor>();
            _navigationService = ((App)App.Current)?.ServiceProvider?.GetService<INavigationService>();
            
            // Initialize command
            ActionCommand = new RelayCommand(_ => ExecuteAction(), _ => IsActionEnabled);
            
            // Set initial state
            UpdateStatus();
        }

        #endregion

        #region IDashboardWidget Implementation

        public void Initialize()
        {
            // Subscribe to system monitor events
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
                var performance = ((App)App.Current)?.ServiceProvider?.GetService<IModelManager>()?.GetModelPerformance();
                
                // Update chat readiness based on system status
                var wasReady = IsChatReady;
                IsChatReady = systemStatus.IsOnline && !string.IsNullOrEmpty(systemStatus.CurrentRunner);
                
                // Update UI state
                UpdateStatus();
                
                // Notify if status changed
                if (wasReady != IsChatReady)
                {
                    DataChanged?.Invoke(this, new WidgetDataChangedEventArgs
                    {
                        WidgetId = Configuration.WidgetId,
                        OldData = wasReady,
                        NewData = IsChatReady,
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ChatStatusWidget refresh error: {ex.Message}");
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

        private void UpdateStatus()
        {
            if (IsChatReady)
            {
                StatusText = "Chat Ready ✓";
                SubtitleText = "Connected and ready for conversations";
                ActionButtonText = "Open Chat";
                IsActionEnabled = true;
                IsConnecting = false;
                StatusColor = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Green
                StatusGlowColor = Color.FromRgb(34, 197, 94);
            }
            else if (IsConnecting)
            {
                StatusText = "Connecting...";
                SubtitleText = "Setting up chat connection";
                ActionButtonText = "Please Wait";
                IsActionEnabled = false;
                StatusColor = new SolidColorBrush(Color.FromRgb(249, 115, 22)); // Orange
                StatusGlowColor = Color.FromRgb(249, 115, 22);
            }
            else
            {
                StatusText = "Not Ready";
                SubtitleText = "Load a model to enable chat";
                ActionButtonText = "Load Model";
                IsActionEnabled = true;
                IsConnecting = false;
                StatusColor = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // Gray
                StatusGlowColor = Color.FromRgb(156, 163, 175);
            }
        }

        private void SetErrorState()
        {
            StatusText = "Error";
            SubtitleText = "Unable to check chat status";
            ActionButtonText = "Retry";
            IsActionEnabled = true;
            IsConnecting = false;
            StatusColor = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
            StatusGlowColor = Color.FromRgb(239, 68, 68);
        }

        private void ExecuteAction()
        {
            if (_navigationService == null) return;
            
            if (IsChatReady)
            {
                // Navigate to chat
                _navigationService.NavigateTo(NavigationTab.Conversations);
            }
            else
            {
                // Navigate to model configuration
                _navigationService.NavigateTo(NavigationTab.Models);
            }
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
}
