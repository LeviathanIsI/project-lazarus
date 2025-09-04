using System.Globalization;
using System.Windows.Data;

namespace Lazarus.App.Desktop.Converters;

/// <summary>
/// Converts boolean values to expand/collapse icons for progressive disclosure
/// </summary>
public class BoolToExpanderIconConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean value to an expand/collapse icon
    /// </summary>
    /// <param name="value">The boolean value indicating expanded state</param>
    /// <param name="targetType">The type of the binding target property</param>
    /// <param name="parameter">The converter parameter (not used)</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>Chevron down icon when collapsed, chevron up when expanded</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isExpanded)
        {
            return isExpanded ? "▲" : "▼"; // Up arrow when expanded, down arrow when collapsed
        }

        return "▼"; // Default to collapsed state
    }

    /// <summary>
    /// Converts back (not implemented as this is one-way binding)
    /// </summary>
    /// <param name="value">The value that is produced by the binding target</param>
    /// <param name="targetType">The type to convert to</param>
    /// <param name="parameter">The converter parameter to use</param>
    /// <param name="culture">The culture to use in the converter</param>
    /// <returns>Not implemented</returns>
    /// <exception cref="NotImplementedException">This converter is one-way only</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("BoolToExpanderIconConverter is a one-way converter");
    }
}