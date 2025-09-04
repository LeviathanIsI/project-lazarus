using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lazarus.App.Desktop.Converters;

/// <summary>
/// Converts null values to Visibility
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return Visibility.Collapsed;
            
        if (value is string str && string.IsNullOrWhiteSpace(str))
            return Visibility.Collapsed;
            
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}