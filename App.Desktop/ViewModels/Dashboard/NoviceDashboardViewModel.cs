using System;
using System.Threading.Tasks;
using App.Shared.Enums;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.Views.Dashboard.Widgets;

namespace Lazarus.Desktop.ViewModels.Dashboard
{
    /// <summary>
    /// Novice Dashboard ViewModel - Simple, friendly interface
    /// Shows 6 core widgets in a 3x2 grid with large, easy-to-understand status indicators
    /// </summary>
    public class NoviceDashboardViewModel : BaseDashboardViewModel
    {
        #region Constructor

        public NoviceDashboardViewModel(
            ISystemMonitor systemMonitor,
            IModelManager modelManager,
            ITrainingService trainingService,
            UserPreferencesService preferencesService)
            : base(systemMonitor, modelManager, trainingService, preferencesService)
        {
            // Set ViewMode to Novice
            CurrentViewMode = ViewMode.Novice;
        }

        #endregion

        #region BaseDashboardViewModel Implementation

        protected override void InitializeWidgets()
        {
            // Clear any existing widgets
            Widgets.Clear();

            // Row 1: Chat Status, Model Status, Training Status
            var chatStatusWidget = new ChatStatusWidget();
            chatStatusWidget.Position = new WidgetPosition { Row = 0, Column = 0 };
            AddWidget(chatStatusWidget);

            var modelStatusWidget = new ModelStatusWidget();
            modelStatusWidget.Position = new WidgetPosition { Row = 0, Column = 1 };
            AddWidget(modelStatusWidget);

            var trainingStatusWidget = new TrainingStatusWidget();
            trainingStatusWidget.Position = new WidgetPosition { Row = 0, Column = 2 };
            AddWidget(trainingStatusWidget);

            // Row 2: Voice Features, Quick Actions, Help & Tutorials
            var voiceFeaturesWidget = new VoiceFeaturesWidget();
            voiceFeaturesWidget.Position = new WidgetPosition { Row = 1, Column = 0 };
            AddWidget(voiceFeaturesWidget);

            var quickActionsWidget = new QuickActionsWidget();
            quickActionsWidget.Position = new WidgetPosition { Row = 1, Column = 1 };
            AddWidget(quickActionsWidget);

            var helpTutorialsWidget = new HelpTutorialsWidget();
            helpTutorialsWidget.Position = new WidgetPosition { Row = 1, Column = 2 };
            AddWidget(helpTutorialsWidget);

            Console.WriteLine("[NoviceDashboard] Initialized with 6 widgets in 3x2 grid");
        }

        protected override double GetRefreshInterval()
        {
            // Slower refresh for Novice mode - 5 seconds
            return 5000;
        }

        protected override void OnViewModeChanged()
        {
            // Only show widgets if we're in Novice mode
            if (CurrentViewMode != ViewMode.Novice)
            {
                foreach (var widget in Widgets)
                {
                    widget.Configuration.IsEnabled = false;
                }
                return;
            }

            // Enable all widgets for Novice mode
            foreach (var widget in Widgets)
            {
                widget.Configuration.IsEnabled = true;
            }

            base.OnViewModeChanged();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Simple status indicators for Novice mode
        /// </summary>
        public bool IsChatReady => _systemMonitor.GetCurrentStatus().IsOnline;

        public int AvailableModelsCount => _modelManager.GetAvailableModels().Count;

        public bool IsTrainingActive => _trainingService.GetActiveJobs().Count > 0;

        public int TrainingProgress
        {
            get
            {
                var activeJobs = _trainingService.GetActiveJobs();
                return activeJobs.Count > 0 ? activeJobs[0].ProgressPercent : 0;
            }
        }

        public bool IsVoiceEnabled => true; // TODO: Get from actual voice service

        public string WelcomeMessage
        {
            get
            {
                var hour = DateTime.Now.Hour;
                var greeting = hour switch
                {
                    < 12 => "Good Morning!",
                    < 17 => "Good Afternoon!",
                    _ => "Good Evening!"
                };

                return $"{greeting} Ready to create something amazing?";
            }
        }

        public string SystemStatusSummary
        {
            get
            {
                var status = _systemMonitor.GetCurrentStatus();
                var model = _modelManager.GetCurrentModel();

                if (!status.IsOnline)
                {
                    return "⚠️ System is offline - please check your setup";
                }

                if (string.IsNullOrEmpty(model.Name) || model.Name == "No model loaded")
                {
                    return "📦 Ready to go! Load a model to get started";
                }

                return "✅ Everything looks good! Your AI assistant is ready";
            }
        }

        #endregion

        #region Event Handlers

        protected override void OnSystemStatusChanged(object? sender, SystemStatusChangedEventArgs e)
        {
            base.OnSystemStatusChanged(sender, e);
            
            // Update simple status properties
            OnPropertyChanged(nameof(IsChatReady));
            OnPropertyChanged(nameof(SystemStatusSummary));
        }

        protected override void OnModelChanged(object? sender, ModelChangedEventArgs e)
        {
            base.OnModelChanged(sender, e);
            
            // Update model-related properties
            OnPropertyChanged(nameof(AvailableModelsCount));
            OnPropertyChanged(nameof(SystemStatusSummary));
        }

        protected override void OnTrainingJobStatusChanged(object? sender, TrainingJobStatusChangedEventArgs e)
        {
            base.OnTrainingJobStatusChanged(sender, e);
            
            // Update training-related properties
            OnPropertyChanged(nameof(IsTrainingActive));
            OnPropertyChanged(nameof(TrainingProgress));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get a simple status summary for the entire system
        /// </summary>
        public async Task<string> GetSystemHealthSummaryAsync()
        {
            await Task.Delay(100); // Simulate async check
            
            var issues = 0;
            var warnings = new System.Collections.Generic.List<string>();
            
            // Check system status
            var status = _systemMonitor.GetCurrentStatus();
            if (!status.IsOnline)
            {
                issues++;
                warnings.Add("System offline");
            }
            
            // Check model status
            var model = _modelManager.GetCurrentModel();
            if (string.IsNullOrEmpty(model.Name) || model.Name == "No model loaded")
            {
                warnings.Add("No model loaded");
            }
            
            // Check memory usage
            var memory = _systemMonitor.GetMemoryUsage();
            if (memory.SystemRamPercentage > 90)
            {
                issues++;
                warnings.Add("High memory usage");
            }
            
            if (issues == 0 && warnings.Count == 0)
            {
                return "🎉 Perfect! Everything is running smoothly.";
            }
            
            if (issues == 0)
            {
                return $"✅ System healthy. {warnings.Count} minor item(s) to note.";
            }
            
            return $"⚠️ {issues} issue(s) need attention. Check system status.";
        }

        #endregion
    }
}
