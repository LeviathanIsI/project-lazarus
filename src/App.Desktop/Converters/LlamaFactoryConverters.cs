using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Lazarus.Desktop.ViewModels.Training;

namespace Lazarus.Desktop.Converters
{
    public sealed class LfStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value is ConversationsDesignerViewModel.LfStatus s ? s : ConversationsDesignerViewModel.LfStatus.None;
            // Green, Yellow, Red
            return status switch
            {
                ConversationsDesignerViewModel.LfStatus.Valid => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF10B981")),
                ConversationsDesignerViewModel.LfStatus.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF59E0B")),
                ConversationsDesignerViewModel.LfStatus.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9CA3AF"))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    public sealed class LfStatusToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value is ConversationsDesignerViewModel.LfStatus s ? s : ConversationsDesignerViewModel.LfStatus.None;
            return status switch
            {
                ConversationsDesignerViewModel.LfStatus.Valid => "✔",
                ConversationsDesignerViewModel.LfStatus.Warning => "⚠",
                ConversationsDesignerViewModel.LfStatus.Error => "✖",
                _ => ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}

