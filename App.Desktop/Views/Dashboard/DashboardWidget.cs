using System;
using System.Windows;
using System.Windows.Controls;
using App.Shared.Enums;
using Lazarus.Desktop.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Dashboard
{
    /// <summary>
    /// Base class for all Dashboard widgets
    /// Provides common functionality and XAML integration
    /// </summary>
    public abstract class DashboardWidget : UserControl, IDashboardWidget
    {
        #region Dependency Properties

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(DashboardWidget),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsWidgetVisibleProperty =
            DependencyProperty.Register(nameof(IsWidgetVisible), typeof(bool), typeof(DashboardWidget),
                new PropertyMetadata(true));

        #endregion

        #region Properties

        /// <summary>
        /// Display title for the widget
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// ViewModes that this widget supports
        /// </summary>
        public abstract ViewMode[] SupportedModes { get; }

        /// <summary>
        /// Whether the widget should be visible in the current ViewMode
        /// </summary>
        public virtual bool IsWidgetVisible
        {
            get => (bool)GetValue(IsWidgetVisibleProperty);
            protected set => SetValue(IsWidgetVisibleProperty, value);
        }

        /// <summary>
        /// IDashboardWidget implementation - maps to IsWidgetVisible
        /// </summary>
        bool IDashboardWidget.IsVisible => IsWidgetVisible;

        /// <summary>
        /// The UI content for this widget (returns this UserControl)
        /// </summary>
        public UserControl WidgetContent => this;

        /// <summary>
        /// Grid position for layout
        /// </summary>
        public WidgetPosition Position { get; set; } = new WidgetPosition();

        /// <summary>
        /// Widget configuration and customization options
        /// </summary>
        public WidgetConfiguration Configuration { get; set; } = new WidgetConfiguration();

        /// <summary>
        /// Current ViewMode from preferences service
        /// </summary>
        protected ViewMode CurrentViewMode => ((App)App.Current)?.ServiceProvider?.GetService<Lazarus.Desktop.Services.UserPreferencesService>()?.CurrentViewMode ?? ViewMode.Novice;

        #endregion

        #region Constructor

        protected DashboardWidget()
        {
            // Set default styling
            Background = System.Windows.Media.Brushes.Transparent;
            
            // Subscribe to ViewMode changes
            Loaded += OnWidgetLoaded;
            Unloaded += OnWidgetUnloaded;
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Refresh the widget's data - implement in derived classes
        /// </summary>
        public abstract void RefreshData();

        /// <summary>
        /// Initialize the widget with required services - implement in derived classes
        /// </summary>
        public abstract void Initialize();

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Handle ViewMode changes - override in derived widgets for mode-specific behavior
        /// </summary>
        protected virtual void OnViewModeChanged(ViewMode newMode)
        {
            // Update visibility based on supported modes
            IsWidgetVisible = SupportedModes.Contains(newMode);
            
            // Refresh data when mode changes
            if (IsWidgetVisible)
            {
                RefreshData();
            }
        }

        /// <summary>
        /// Clean up resources when widget is disposed - override in derived classes
        /// </summary>
        public virtual void Dispose()
        {
            // Base cleanup
            Loaded -= OnWidgetLoaded;
            Unloaded -= OnWidgetUnloaded;
        }

        #endregion

        #region Event Handlers

        private void OnWidgetLoaded(object sender, RoutedEventArgs e)
        {
            // Subscribe to ViewMode changes when loaded
            var preferencesService = ((App)App.Current)?.ServiceProvider?.GetService<Lazarus.Desktop.Services.UserPreferencesService>();
            if (preferencesService != null)
            {
                preferencesService.PropertyChanged += OnPreferencesChanged;
                OnViewModeChanged(preferencesService.CurrentViewMode);
            }
        }

        private void OnWidgetUnloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe from ViewMode changes when unloaded
            var preferencesService = ((App)App.Current)?.ServiceProvider?.GetService<Lazarus.Desktop.Services.UserPreferencesService>();
            if (preferencesService != null)
            {
                preferencesService.PropertyChanged -= OnPreferencesChanged;
            }
        }

        private void OnPreferencesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Lazarus.Desktop.Services.UserPreferencesService.CurrentViewMode))
            {
                var preferencesService = sender as Lazarus.Desktop.Services.UserPreferencesService;
                if (preferencesService != null)
                {
                    Dispatcher.Invoke(() => OnViewModeChanged(preferencesService.CurrentViewMode));
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Event fired when widget data changes
        /// </summary>
        public event EventHandler<WidgetDataChangedEventArgs>? DataChanged;

        /// <summary>
        /// Event fired when widget configuration changes
        /// </summary>
        public event EventHandler<WidgetConfigurationChangedEventArgs>? ConfigurationChanged;

        /// <summary>
        /// Raise the DataChanged event
        /// </summary>
        protected virtual void OnDataChanged(object? oldData, object? newData)
        {
            DataChanged?.Invoke(this, new WidgetDataChangedEventArgs
            {
                WidgetId = Configuration.WidgetId,
                OldData = oldData,
                NewData = newData,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Raise the ConfigurationChanged event
        /// </summary>
        protected virtual void OnConfigurationChanged(WidgetConfiguration oldConfig, WidgetConfiguration newConfig)
        {
            ConfigurationChanged?.Invoke(this, new WidgetConfigurationChangedEventArgs
            {
                WidgetId = Configuration.WidgetId,
                OldConfiguration = oldConfig,
                NewConfiguration = newConfig
            });
        }

        #endregion
    }
}
