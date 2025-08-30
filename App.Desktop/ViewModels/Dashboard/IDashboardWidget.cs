using System;
using System.Windows.Controls;
using App.Shared.Enums;

namespace Lazarus.Desktop.ViewModels.Dashboard
{
    /// <summary>
    /// Interface for Dashboard widgets - modular components that display specific data
    /// Each widget can support different ViewModes and provide its own UI
    /// </summary>
    public interface IDashboardWidget
    {
        /// <summary>
        /// Display title for the widget
        /// </summary>
        string Title { get; }

        /// <summary>
        /// ViewModes that this widget supports
        /// </summary>
        ViewMode[] SupportedModes { get; }

        /// <summary>
        /// Whether the widget should be visible in the current ViewMode
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// The UI content for this widget
        /// </summary>
        UserControl WidgetContent { get; }

        /// <summary>
        /// Grid position for layout (Row, Column, RowSpan, ColumnSpan)
        /// </summary>
        WidgetPosition Position { get; set; }

        /// <summary>
        /// Widget configuration and customization options
        /// </summary>
        WidgetConfiguration Configuration { get; set; }

        /// <summary>
        /// Refresh the widget's data
        /// </summary>
        void RefreshData();

        /// <summary>
        /// Initialize the widget with required services
        /// </summary>
        void Initialize();

        /// <summary>
        /// Clean up resources when widget is disposed
        /// </summary>
        void Dispose();

        /// <summary>
        /// Event fired when widget data changes
        /// </summary>
        event EventHandler<WidgetDataChangedEventArgs> DataChanged;

        /// <summary>
        /// Event fired when widget configuration changes
        /// </summary>
        event EventHandler<WidgetConfigurationChangedEventArgs> ConfigurationChanged;
    }

    /// <summary>
    /// Widget position in the dashboard grid
    /// </summary>
    public class WidgetPosition
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int RowSpan { get; set; } = 1;
        public int ColumnSpan { get; set; } = 1;
    }

    /// <summary>
    /// Widget configuration and settings
    /// </summary>
    public class WidgetConfiguration
    {
        public string WidgetId { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public int RefreshIntervalSeconds { get; set; } = 5;
        public bool ShowTitle { get; set; } = true;
        public bool AllowResize { get; set; } = true;
        public bool AllowMove { get; set; } = true;
        public System.Collections.Generic.Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    /// <summary>
    /// Event args for widget data changes
    /// </summary>
    public class WidgetDataChangedEventArgs : EventArgs
    {
        public string WidgetId { get; set; } = "";
        public object? OldData { get; set; }
        public object? NewData { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Event args for widget configuration changes
    /// </summary>
    public class WidgetConfigurationChangedEventArgs : EventArgs
    {
        public string WidgetId { get; set; } = "";
        public WidgetConfiguration OldConfiguration { get; set; } = new();
        public WidgetConfiguration NewConfiguration { get; set; } = new();
    }
}
