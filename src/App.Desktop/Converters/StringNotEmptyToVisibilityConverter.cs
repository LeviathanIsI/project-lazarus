using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lazarus.Desktop.Converters
{
    public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public bool Collapse { get; set; } = true;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = value as string;
            var visible = !string.IsNullOrEmpty(s);
            if (visible) return Visibility.Visible;
            return Collapse ? Visibility.Collapsed : Visibility.Hidden;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}

