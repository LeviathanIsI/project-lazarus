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
    public partial class HelpTutorialsWidget : UserControl, IDashboardWidget, INotifyPropertyChanged
    {
        public ViewMode[] SupportedModes => new[] { ViewMode.Novice };
        public string Title { get; set; } = "Help & Tutorials";
        bool IDashboardWidget.IsVisible => SupportedModes.Contains(CurrentViewMode);
        public UserControl WidgetContent => this;
        public WidgetPosition Position { get; set; } = new WidgetPosition();
        public WidgetConfiguration Configuration { get; set; } = new WidgetConfiguration { WidgetId = "help_tutorials_novice" };
        protected ViewMode CurrentViewMode => ((App)App.Current)?.ServiceProvider?.GetService<UserPreferencesService>()?.CurrentViewMode ?? ViewMode.Novice;
        
        public HelpTutorialsWidget() 
        { 
            Title = "Help & Tutorials";
            Configuration.WidgetId = "help_tutorials_novice";
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
