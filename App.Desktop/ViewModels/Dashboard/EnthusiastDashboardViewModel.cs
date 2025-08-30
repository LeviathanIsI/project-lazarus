using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using App.Shared.Enums;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.Views.Dashboard.Widgets.Enthusiast;

namespace Lazarus.Desktop.ViewModels.Dashboard
{
    /// <summary>
    /// Enthusiast Dashboard ViewModel - Performance-focused interface with customizable 4x3 grid
    /// Shows detailed metrics, real-time graphs, and provides moderate customization options
    /// </summary>
    public class EnthusiastDashboardViewModel : BaseDashboardViewModel
    {
        #region Fields

        private readonly IDiagnosticsService? _diagnosticsService;
        private WidgetLayoutConfiguration _widgetLayout = new();
        private SystemStatus _currentSystemStatus = new();
        private ModelPerformanceData _activeModelData = new();
        private TrainingQueueStatus _trainingStatus = new();

        #endregion

        #region Constructor

        public EnthusiastDashboardViewModel(
            ISystemMonitor systemMonitor,
            IModelManager modelManager,
            ITrainingService trainingService,
            UserPreferencesService preferencesService,
            IDiagnosticsService? diagnosticsService = null)
            : base(systemMonitor, modelManager, trainingService, preferencesService)
        {
            _diagnosticsService = diagnosticsService;
            CurrentViewMode = ViewMode.Enthusiast;
            
            // Initialize collections
            UserPreferences = new Dictionary<string, object>();
            
            // Load saved layout configuration
            LoadLayoutConfiguration();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Widget layout configuration for the 4x3 customizable grid
        /// </summary>
        public WidgetLayoutConfiguration WidgetLayout
        {
            get => _widgetLayout;
            set => SetProperty(ref _widgetLayout, value);
        }

        // Customization mode removed for MVP

        /// <summary>
        /// Current system status with detailed metrics
        /// </summary>
        public SystemStatus CurrentSystemStatus
        {
            get => _currentSystemStatus;
            set => SetProperty(ref _currentSystemStatus, value);
        }

        /// <summary>
        /// Active model performance data
        /// </summary>
        public ModelPerformanceData ActiveModelData
        {
            get => _activeModelData;
            set => SetProperty(ref _activeModelData, value);
        }

        /// <summary>
        /// Training queue status with detailed job information
        /// </summary>
        public TrainingQueueStatus TrainingStatus
        {
            get => _trainingStatus;
            set => SetProperty(ref _trainingStatus, value);
        }

        /// <summary>
        /// User customization preferences
        /// </summary>
        public Dictionary<string, object> UserPreferences { get; set; }

        // Customization palette removed for MVP

        #endregion

        #region BaseDashboardViewModel Implementation

        protected override void InitializeWidgets()
        {
            Console.WriteLine("[EnthusiastDashboard] Initializing 4x3 customizable widget grid...");
            
            // Clear existing widgets
            Widgets.Clear();
            
            // Customization palette removed for MVP
            
            // Create default layout if none exists
            if (WidgetLayout.WidgetPlacements.Count == 0)
            {
                CreateDefaultLayout();
            }
            
            // Create widgets based on layout configuration
            CreateWidgetsFromLayout();
            
            Console.WriteLine($"[EnthusiastDashboard] Initialized with {Widgets.Count} widgets");
        }

        protected override double GetRefreshInterval()
        {
            // Faster refresh for Enthusiast mode - 2 seconds for performance monitoring
            return 2000;
        }

        protected override void OnViewModeChanged()
        {
            // Only show widgets if we're in Enthusiast mode
            if (CurrentViewMode != ViewMode.Enthusiast)
            {
                foreach (var widget in Widgets)
                {
                    widget.Configuration.IsEnabled = false;
                }
                return;
            }

            // Enable all widgets for Enthusiast mode
            foreach (var widget in Widgets)
            {
                widget.Configuration.IsEnabled = true;
            }

            base.OnViewModeChanged();
        }

        #endregion

        #region Public Methods

        // Customization mode removed for MVP

        // AddWidget removed for MVP

        // RemoveWidget by id removed for MVP

        // MoveWidget removed for MVP

        /// <summary>
        /// Reset to default layout
        /// </summary>
        public void ResetToDefaultLayout()
        {
            // Clear current widgets
            foreach (var widget in Widgets.ToList())
            {
                RemoveWidget(widget);
            }

            // Reset layout and recreate
            WidgetLayout.WidgetPlacements.Clear();
            CreateDefaultLayout();
            CreateWidgetsFromLayout();
            SaveLayoutConfiguration();

            StatusMessage = "Dashboard reset to default layout";
        }

        #endregion

        #region Private Methods

        // Customization palette removed for MVP

        private void CreateDefaultLayout()
        {
            // Default 4x3 layout for Enthusiast Dashboard
            var defaultPlacements = new[]
            {
                // Row 0
                new WidgetPlacement
                {
                    WidgetType = "SystemStatus",
                    Position = new WidgetPosition { Row = 0, Column = 0, RowSpan = 1, ColumnSpan = 1 },
                    Configuration = new WidgetConfiguration { WidgetId = "sys_status", IsEnabled = true, RefreshIntervalSeconds = 2 }
                },
                new WidgetPlacement
                {
                    WidgetType = "ActiveModel",
                    Position = new WidgetPosition { Row = 0, Column = 1, RowSpan = 1, ColumnSpan = 2 },
                    Configuration = new WidgetConfiguration { WidgetId = "active_model", IsEnabled = true, RefreshIntervalSeconds = 3 }
                },
                new WidgetPlacement
                {
                    WidgetType = "RecentActivity",
                    Position = new WidgetPosition { Row = 0, Column = 3, RowSpan = 2, ColumnSpan = 1 },
                    Configuration = new WidgetConfiguration { WidgetId = "recent_activity", IsEnabled = true, RefreshIntervalSeconds = 5 }
                },
                
                // Row 1
                new WidgetPlacement
                {
                    WidgetType = "PerformanceMetrics",
                    Position = new WidgetPosition { Row = 1, Column = 0, RowSpan = 2, ColumnSpan = 2 },
                    Configuration = new WidgetConfiguration { WidgetId = "performance", IsEnabled = true, RefreshIntervalSeconds = 1 }
                },
                new WidgetPlacement
                {
                    WidgetType = "TrainingQueue",
                    Position = new WidgetPosition { Row = 1, Column = 2, RowSpan = 1, ColumnSpan = 1 },
                    Configuration = new WidgetConfiguration { WidgetId = "training_queue", IsEnabled = true, RefreshIntervalSeconds = 2 }
                },
                
                // Row 2
                new WidgetPlacement
                {
                    WidgetType = "ModelLibrary",
                    Position = new WidgetPosition { Row = 2, Column = 2, RowSpan = 1, ColumnSpan = 1 },
                    Configuration = new WidgetConfiguration { WidgetId = "model_library", IsEnabled = true, RefreshIntervalSeconds = 10 }
                }
            };

            foreach (var placement in defaultPlacements)
            {
                WidgetLayout.WidgetPlacements.Add(placement);
            }
        }

        private void CreateWidgetsFromLayout()
        {
            foreach (var placement in WidgetLayout.WidgetPlacements)
            {
                CreateWidgetFromPlacement(placement);
            }
        }

        private void CreateWidgetFromPlacement(WidgetPlacement placement)
        {
            IDashboardWidget? widget = placement.WidgetType switch
            {
                "SystemStatus" => new SystemStatusWidget(),
                "ActiveModel" => new ActiveModelWidget(),
                "PerformanceMetrics" => new PerformanceMetricsWidget(),
                "TrainingQueue" => new TrainingQueueWidget(),
                "RecentActivity" => new RecentActivityWidget(),
                "ModelLibrary" => new ModelLibraryWidget(),
                _ => null
            };

            if (widget != null)
            {
                widget.Position = placement.Position;
                widget.Configuration = placement.Configuration;
                AddWidget(widget);
            }
        }

        private void LoadLayoutConfiguration()
        {
            // TODO: Load from user preferences service
            // For now, use default layout
        }

        private void SaveLayoutConfiguration()
        {
            // TODO: Save to user preferences service
            Console.WriteLine($"[EnthusiastDashboard] Layout saved with {WidgetLayout.WidgetPlacements.Count} widgets");
        }

        #endregion

        #region Event Handlers

        protected override void OnSystemStatusChanged(object? sender, SystemStatusChangedEventArgs e)
        {
            base.OnSystemStatusChanged(sender, e);
            
            CurrentSystemStatus = e.SystemStatus;
            
            // Update active model data
            var modelPerformance = _modelManager.GetModelPerformance();
            ActiveModelData = new ModelPerformanceData
            {
                CurrentModel = _modelManager.GetCurrentModel(),
                Performance = modelPerformance,
                IsOnline = e.SystemStatus.IsOnline
            };
        }

        protected override void OnTrainingJobStatusChanged(object? sender, TrainingJobStatusChangedEventArgs e)
        {
            base.OnTrainingJobStatusChanged(sender, e);
            
            // Update training status
            TrainingStatus = new TrainingQueueStatus
            {
                ActiveJobs = _trainingService.GetActiveJobs(),
                QueuedJobs = _trainingService.GetQueuedJobs(),
                RecentlyCompleted = _trainingService.GetRecentlyCompleted()
            };
        }

        #endregion
    }

    #region Data Models

    /// <summary>
    /// Widget layout configuration for customizable dashboard
    /// </summary>
    public class WidgetLayoutConfiguration
    {
        public ObservableCollection<WidgetPlacement> WidgetPlacements { get; set; } = new();
        public int GridRows { get; set; } = 3;
        public int GridColumns { get; set; } = 4;
        public string LayoutName { get; set; } = "Default";
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Widget placement information
    /// </summary>
    public class WidgetPlacement
    {
        public string WidgetType { get; set; } = "";
        public WidgetPosition Position { get; set; } = new();
        public WidgetConfiguration Configuration { get; set; } = new();
    }

    /// <summary>
    /// Widget type information for customization
    /// </summary>
    public class WidgetTypeInfo
    {
        public string TypeName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public WidgetSize DefaultSize { get; set; } = new();
        public string Icon { get; set; } = "";
    }

    /// <summary>
    /// Widget size information
    /// </summary>
    public class WidgetSize
    {
        public int Width { get; set; } = 1;
        public int Height { get; set; } = 1;
    }

    /// <summary>
    /// Model performance data for Enthusiast dashboard
    /// </summary>
    public class ModelPerformanceData
    {
        public ModelInfo CurrentModel { get; set; } = new();
        public PerformanceMetrics Performance { get; set; } = new();
        public bool IsOnline { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Training queue status for Enthusiast dashboard
    /// </summary>
    public class TrainingQueueStatus
    {
        public ObservableCollection<TrainingJob> ActiveJobs { get; set; } = new();
        public ObservableCollection<TrainingJob> QueuedJobs { get; set; } = new();
        public ObservableCollection<TrainingJob> RecentlyCompleted { get; set; } = new();
        public int TotalJobsToday { get; set; }
        public TimeSpan AverageJobTime { get; set; }
    }

    #endregion
}
