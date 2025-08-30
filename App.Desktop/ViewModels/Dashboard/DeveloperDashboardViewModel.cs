using System;
using System.Linq;
using App.Shared.Enums;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.Services.Dashboard;
using Lazarus.Desktop.Views.Dashboard.Widgets;
using Lazarus.Desktop.Views.Dashboard.Widgets.Enthusiast;

namespace Lazarus.Desktop.ViewModels.Dashboard
{
    /// <summary>
    /// Developer-specific Dashboard ViewModel with advanced widgets and detailed system information
    /// </summary>
    public class DeveloperDashboardViewModel : BaseDashboardViewModel
    {
        public DeveloperDashboardViewModel(
            ISystemMonitor systemMonitor,
            IModelManager modelManager,
            ITrainingService trainingService,
            UserPreferencesService preferencesService)
            : base(systemMonitor, modelManager, trainingService, preferencesService)
        {
        }

        protected override void InitializeWidgets()
        {
            // Developer mode: All widgets with advanced details
            Widgets.Clear();

            // For now, use existing widgets until developer-specific ones are created
            // Row 1: System Status and Performance
            Widgets.Add(new SystemStatusWidget
            {
                Position = new WidgetPosition { Row = 0, Column = 0, RowSpan = 1, ColumnSpan = 2 }
            });
            
            Widgets.Add(new PerformanceMetricsWidget
            {
                Position = new WidgetPosition { Row = 0, Column = 2, RowSpan = 1, ColumnSpan = 2 }
            });

            // Row 2: Model Status and Active Model
            Widgets.Add(new ModelStatusWidget
            {
                Position = new WidgetPosition { Row = 1, Column = 0, RowSpan = 1, ColumnSpan = 2 }
            });
            
            Widgets.Add(new ActiveModelWidget
            {
                Position = new WidgetPosition { Row = 1, Column = 2, RowSpan = 1, ColumnSpan = 2 }
            });

            // Row 3: Training Queue and Model Library
            Widgets.Add(new TrainingQueueWidget
            {
                Position = new WidgetPosition { Row = 2, Column = 0, RowSpan = 1, ColumnSpan = 2 }
            });
            
            Widgets.Add(new ModelLibraryWidget
            {
                Position = new WidgetPosition { Row = 2, Column = 2, RowSpan = 1, ColumnSpan = 2 }
            });
            
            // Row 4: Recent Activity
            Widgets.Add(new RecentActivityWidget
            {
                Position = new WidgetPosition { Row = 3, Column = 0, RowSpan = 1, ColumnSpan = 4 }
            });

            // TODO: Add developer-specific widgets when implemented:
            // - SystemPerformanceWidget (detailed performance metrics)
            // - DebugConsoleWidget (live debug output)
            // - ModelArchitectureWidget (model graph visualization)
            // - GpuDetailsWidget (CUDA cores, memory bandwidth, etc.)
            // - TrainingPipelineWidget (detailed pipeline view)
            // - LogViewerWidget (application logs)
            // - ApiMonitorWidget (API call tracking)
            // - ResourceUsageWidget (detailed resource monitoring)
        }

        protected override double GetRefreshInterval()
        {
            // Developer mode: Fast refresh (1 second)
            return 1000;
        }

        protected override void OnViewModeChanged()
        {
            // Developer mode specific handling
            StatusMessage = "Developer Dashboard: Full system access enabled";
            
            // Enable advanced monitoring
            if (_systemMonitor is SystemMonitorService monitor)
            {
                // Could enable additional monitoring features here
            }
        }


    }
}
