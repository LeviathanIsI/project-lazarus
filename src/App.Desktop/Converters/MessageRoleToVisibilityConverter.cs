using Lazarus.App.Shared.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lazarus.App.Desktop.Converters;

/// <summary>
/// Converts MessageRole to Visibility, hiding system messages
/// </summary>
public class MessageRoleToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role == MessageRole.System 
                ? Visibility.Collapsed 
                : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}