using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using App.Shared.Enums;
using Lazarus.Desktop.Helpers;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Dashboard.Widgets
{
    /// <summary>
    /// Model Status Widget for Novice Dashboard
    /// Shows simple model count and current model status
    /// </summary>
    public partial class ModelStatusWidget : UserControl, IDashboardWidget, INotifyPropertyChanged
    {
        #region Fields

        private readonly IModelManager? _modelManager;
        private readonly INavigationService? _navigationService;
        
        private string _modelCountText = "0 Models";
        private string _currentModelText = "No model loaded";
        private string _statusText = "Load a model to get started";
        private string _actionButtonText = "Browse Models";

        #endregion

        #region Properties

        public ViewMode[] SupportedModes => new[] { ViewMode.Novice };
        public string Title { get; set; } = "Model Status";
        bool IDashboardWidget.IsVisible => SupportedModes.Contains(CurrentViewMode);
        public UserControl WidgetContent => this;
        public WidgetPosition Position { get; set; } = new WidgetPosition();
        public WidgetConfiguration Configuration { get; set; } = new WidgetConfiguration { WidgetId = "model_status_novice" };
        protected ViewMode CurrentViewMode => ((App)App.Current)?.ServiceProvider?.GetService<UserPreferencesService>()?.CurrentViewMode ?? ViewMode.Novice;

        public string ModelCountText
        {
            get => _modelCountText;
            set => SetProperty(ref _modelCountText, value);
        }

        public string CurrentModelText
        {
            get => _currentModelText;
            set => SetProperty(ref _currentModelText, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ActionButtonText
        {
            get => _actionButtonText;
            set => SetProperty(ref _actionButtonText, value);
        }

        public ICommand ActionCommand { get; }

        #endregion

        #region Constructor

        public ModelStatusWidget()
        {
            InitializeComponent();
            DataContext = this;
            
            Title = "Model Status";
            Configuration.WidgetId = "model_status_novice";
            
            // Get services from DI container
            _modelManager = ((App)App.Current)?.ServiceProvider?.GetService<IModelManager>();
            _navigationService = ((App)App.Current)?.ServiceProvider?.GetService<INavigationService>();
            
            // Initialize command
            ActionCommand = new RelayCommand(_ => ExecuteAction());
        }

        #endregion

        #region IDashboardWidget Implementation

        public void Initialize()
        {
            if (_modelManager != null)
            {
                _modelManager.ModelChanged += OnModelChanged;
            }
            
            RefreshData();
        }

        public void RefreshData()
        {
            if (_modelManager == null) return;
            
            try
            {
                var availableModels = _modelManager.GetAvailableModels();
                var currentModel = _modelManager.GetCurrentModel();
                
                // Update model count
                var count = availableModels.Count;
                ModelCountText = count switch
                {
                    0 => "No Models",
                    1 => "1 Model",
                    _ => $"{count} Models"
                };
                
                // Update current model
                if (string.IsNullOrEmpty(currentModel.Name) || currentModel.Name == "No model loaded")
                {
                    CurrentModelText = "No model loaded";
                    StatusText = count > 0 ? "Choose a model to activate" : "Add models to get started";
                    ActionButtonText = count > 0 ? "Load Model" : "Add Models";
                }
                else
                {
                    CurrentModelText = currentModel.Name;
                    StatusText = "Ready for AI tasks";
                    ActionButtonText = "Switch Model";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ModelStatusWidget refresh error: {ex.Message}");
                SetErrorState();
            }
        }

        public void Dispose()
        {
            if (_modelManager != null)
            {
                _modelManager.ModelChanged -= OnModelChanged;
            }
        }

        public event EventHandler<WidgetDataChangedEventArgs>? DataChanged;
        public event EventHandler<WidgetConfigurationChangedEventArgs>? ConfigurationChanged;

        #endregion

        #region Private Methods

        private void SetErrorState()
        {
            ModelCountText = "Error";
            CurrentModelText = "Unable to check models";
            StatusText = "Check system status";
            ActionButtonText = "Retry";
        }

        private void ExecuteAction()
        {
            _navigationService?.NavigateTo(NavigationTab.Models);
        }

        private void OnModelChanged(object? sender, ModelChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                RefreshData();
            });
        }

        #endregion

        #region INotifyPropertyChanged

        public new event PropertyChangedEventHandler? PropertyChanged;

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
