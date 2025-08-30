using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using App.Shared.Enums;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Dashboard.Widgets.Enthusiast
{
    /// <summary>
    /// Performance Metrics Widget for Enthusiast Dashboard
    /// Shows real-time CPU/Memory/GPU graphs with customizable time ranges
    /// </summary>
    public partial class PerformanceMetricsWidget : UserControl, IDashboardWidget, INotifyPropertyChanged
    {
        #region Fields

        private readonly ISystemMonitor? _systemMonitor;
        
        private int _selectedTimeRangeIndex = 0;
        private double _currentCpuUsage = 0;
        private double _currentMemoryUsage = 0;
        private double _currentGpuUsage = 0;
        private double _tokensPerSecond = 0;
        private string _lastUpdateText = "Never";
        private string _dataPointsText = "0 points";
        
        private ObservableCollection<DataPoint> _cpuHistory = new();
        private ObservableCollection<DataPoint> _memoryHistory = new();

        #endregion

        #region Properties

        public ViewMode[] SupportedModes => new[] { ViewMode.Enthusiast };
        public string Title { get; set; } = "Performance Metrics";
        bool IDashboardWidget.IsVisible => SupportedModes.Contains(CurrentViewMode);
        public UserControl WidgetContent => this;
        public WidgetPosition Position { get; set; } = new WidgetPosition();
        public WidgetConfiguration Configuration { get; set; } = new WidgetConfiguration { WidgetId = "performance_metrics_enthusiast" };
        protected ViewMode CurrentViewMode => ((App)App.Current)?.ServiceProvider?.GetService<UserPreferencesService>()?.CurrentViewMode ?? ViewMode.Enthusiast;

        public int SelectedTimeRangeIndex
        {
            get => _selectedTimeRangeIndex;
            set
            {
                if (SetProperty(ref _selectedTimeRangeIndex, value))
                {
                    OnTimeRangeChanged();
                }
            }
        }

        public double CurrentCpuUsage
        {
            get => _currentCpuUsage;
            set => SetProperty(ref _currentCpuUsage, value);
        }

        public double CurrentMemoryUsage
        {
            get => _currentMemoryUsage;
            set => SetProperty(ref _currentMemoryUsage, value);
        }

        public double CurrentGpuUsage
        {
            get => _currentGpuUsage;
            set => SetProperty(ref _currentGpuUsage, value);
        }

        public double TokensPerSecond
        {
            get => _tokensPerSecond;
            set => SetProperty(ref _tokensPerSecond, value);
        }

        public string LastUpdateText
        {
            get => _lastUpdateText;
            set => SetProperty(ref _lastUpdateText, value);
        }

        public string DataPointsText
        {
            get => _dataPointsText;
            set => SetProperty(ref _dataPointsText, value);
        }

        #endregion

        #region Constructor

        public PerformanceMetricsWidget()
        {
            InitializeComponent();
            DataContext = this;
            
            Title = "Performance Metrics";
            Configuration.WidgetId = "performance_metrics_enthusiast";
            Configuration.RefreshIntervalSeconds = 1; // Fast refresh for real-time charts
            
            // Get services from DI container
            _systemMonitor = ((App)App.Current)?.ServiceProvider?.GetService<ISystemMonitor>();
            
            // Subscribe to size changes for chart redrawing
            SizeChanged += OnSizeChanged;
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
                
                // Update current values
                CurrentCpuUsage = systemStatus.CpuUsagePercent;
                CurrentMemoryUsage = memoryUsage.SystemRamPercentage;
                CurrentGpuUsage = systemStatus.GpuUtilizationPercent;
                TokensPerSecond = _systemMonitor.GetTokensPerSecond();
                
                // Update history data
                var timeRange = GetSelectedTimeRange();
                _cpuHistory = _systemMonitor.GetCpuHistory(timeRange);
                _memoryHistory = _systemMonitor.GetMemoryHistory(timeRange);
                
                // Update UI
                LastUpdateText = DateTime.Now.ToString("HH:mm:ss");
                DataPointsText = $"{_cpuHistory.Count} points";
                
                // Redraw charts
                DrawCharts();
                
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
                System.Diagnostics.Debug.WriteLine($"PerformanceMetricsWidget refresh error: {ex.Message}");
                SetErrorState();
            }
        }

        public void Dispose()
        {
            if (_systemMonitor != null)
            {
                _systemMonitor.StatusChanged -= OnSystemStatusChanged;
            }
            
            SizeChanged -= OnSizeChanged;
        }

        public event EventHandler<WidgetDataChangedEventArgs>? DataChanged;
        public event EventHandler<WidgetConfigurationChangedEventArgs>? ConfigurationChanged;

        #endregion

        #region Private Methods

        private TimeSpan GetSelectedTimeRange()
        {
            return SelectedTimeRangeIndex switch
            {
                0 => TimeSpan.FromMinutes(1),
                1 => TimeSpan.FromMinutes(5),
                2 => TimeSpan.FromMinutes(15),
                3 => TimeSpan.FromHours(1),
                _ => TimeSpan.FromMinutes(1)
            };
        }

        private void OnTimeRangeChanged()
        {
            RefreshData();
        }

        private void DrawCharts()
        {
            Dispatcher.Invoke(() =>
            {
                DrawChart(CpuChart, _cpuHistory, Colors.LimeGreen);
                DrawChart(MemoryChart, _memoryHistory, Colors.DodgerBlue);
            });
        }

        private void DrawChart(System.Windows.Controls.Canvas canvas, ObservableCollection<DataPoint> data, Color color)
        {
            if (canvas == null || data.Count < 2) return;
            
            canvas.Children.Clear();
            
            var width = canvas.ActualWidth;
            var height = canvas.ActualHeight;
            
            if (width <= 0 || height <= 0) return;
            
            var points = new PointCollection();
            var maxValue = 100.0; // Percentage scale
            var timeSpan = data.Last().Timestamp - data.First().Timestamp;
            
            // Convert data points to canvas coordinates
            for (int i = 0; i < data.Count; i++)
            {
                var dataPoint = data[i];
                var x = (dataPoint.Timestamp - data.First().Timestamp).TotalMilliseconds / timeSpan.TotalMilliseconds * width;
                var y = height - (dataPoint.Value / maxValue * height);
                points.Add(new Point(x, y));
            }
            
            // Create the polyline
            var polyline = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2,
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(64, color.R, color.G, color.B), 0),
                        new GradientStop(Color.FromArgb(16, color.R, color.G, color.B), 1)
                    }
                }
            };
            
            // Add fill area
            if (points.Count > 0)
            {
                var fillPoints = new PointCollection(points);
                fillPoints.Add(new Point(width, height)); // Bottom right
                fillPoints.Add(new Point(0, height)); // Bottom left
                
                var fillPolygon = new Polygon
                {
                    Points = fillPoints,
                    Fill = polyline.Fill
                };
                
                canvas.Children.Add(fillPolygon);
            }
            
            canvas.Children.Add(polyline);
        }

        private void SetErrorState()
        {
            CurrentCpuUsage = 0;
            CurrentMemoryUsage = 0;
            CurrentGpuUsage = 0;
            TokensPerSecond = 0;
            LastUpdateText = "Error";
            DataPointsText = "No data";
            
            // Clear charts
            CpuChart?.Children.Clear();
            MemoryChart?.Children.Clear();
        }

        private void OnSystemStatusChanged(object? sender, SystemStatusChangedEventArgs e)
        {
            // RefreshData will be called by the timer, no need to call here
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Redraw charts when size changes
            DrawCharts();
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
