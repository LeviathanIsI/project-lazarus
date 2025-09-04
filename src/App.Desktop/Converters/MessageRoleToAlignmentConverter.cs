using Lazarus.App.Shared.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lazarus.App.Desktop.Converters;

/// <summary>
/// Converts MessageRole to HorizontalAlignment for message bubbles
/// </summary>
public class MessageRoleToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MessageRole role)
        {
            return role == MessageRole.User 
                ? HorizontalAlignment.Right 
                : HorizontalAlignment.Left;
        }
        return HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}