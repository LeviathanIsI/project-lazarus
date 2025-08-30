using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using App.Shared.Enums;
using Lazarus.Desktop.Services;
using Lazarus.Desktop.ViewModels.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace Lazarus.Desktop.Views.Dashboard.Widgets.Enthusiast
{
    public partial class TrainingQueueWidget : UserControl, IDashboardWidget, INotifyPropertyChanged
    {
        public ViewMode[] SupportedModes => new[] { ViewMode.Enthusiast };
        public string Title { get; set; } = "Training Queue";
        bool IDashboardWidget.IsVisible => SupportedModes.Contains(CurrentViewMode);
        public UserControl WidgetContent => this;
        public WidgetPosition Position { get; set; } = new WidgetPosition();
        public WidgetConfiguration Configuration { get; set; } = new WidgetConfiguration { WidgetId = "training_queue_enthusiast" };
        protected ViewMode CurrentViewMode => ((App)App.Current)?.ServiceProvider?.GetService<UserPreferencesService>()?.CurrentViewMode ?? ViewMode.Enthusiast;
        
        public TrainingQueueWidget() 
        { 
            Title = "Training Queue";
            Configuration.WidgetId = "training_queue_enthusiast";
        }
        
        public void Initialize() { }
        public void RefreshData() { }
        public void Dispose() { }
        public event EventHandler<WidgetDataChangedEventArgs>? DataChanged;
        public event EventHandler<WidgetConfigurationChangedEventArgs>? ConfigurationChanged;
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value; OnPropertyChanged(propertyName); return true;
        }
    }
}
