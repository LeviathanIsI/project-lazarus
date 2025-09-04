using Lazarus.App.Shared.Services;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Lazarus.App.Desktop.Converters;

/// <summary>
/// Converts MessageRole to background brush for message bubbles
/// </summary>
public class MessageRoleToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role == MessageRole.User 
                ? new SolidColorBrush(Color.FromRgb(0, 123, 255)) // Blue for user
                : new SolidColorBrush(Color.FromRgb(240, 240, 240)); // Light gray for assistant
        }
        return new SolidColorBrush(Color.FromRgb(240, 240, 240));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}