using System;
using System.Globalization;
using System.Windows.Data;

namespace Lazarus.App.Desktop.Converters;

/// <summary>
/// Converts a file size in bytes to a human-readable string representation
/// </summary>
public class FileSizeConverter : IValueConverter
{
    private static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB" };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            return FormatBytes(bytes);
        }
        
        if (value is int intBytes)
        {
            return FormatBytes(intBytes);
        }

        if (value is double doubleBytes)
        {
            return FormatBytes((long)doubleBytes);
        }

        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("FileSizeConverter does not support converting back from string to bytes.");
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0)
            return "0 B";

        if (bytes < 0)
            return "-" + FormatBytes(-bytes);

        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < SizeSuffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {SizeSuffixes[suffixIndex]}";
    }
}