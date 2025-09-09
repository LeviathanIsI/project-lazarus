using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace Lazarus.Desktop.Converters;

public sealed class PathToFileNameConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var s = value as string;
        if (string.IsNullOrWhiteSpace(s)) return null;
        return Path.GetFileName(s);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

