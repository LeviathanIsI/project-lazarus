using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Lazarus.App.Desktop.Converters;

/// <summary>
/// Converter that returns the appropriate navigation style based on whether the current section matches the button's section
/// </summary>
public class NavigationStyleConverter : IMultiValueConverter
{
    /// <summary>
    /// Converts the current section and button section to the appropriate style
    /// </summary>
    /// <param name="values">Array containing [0] CurrentSection (string), [1] ButtonSection (string)</param>
    /// <param name="targetType">The target type</param>
    /// <param name="parameter">The parameter (not used)</param>
    /// <param name="culture">The culture (not used)</param>
    /// <returns>The appropriate navigation style</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string currentSection || values[1] is not string buttonSection)
        {
            return Application.Current.FindResource("NavigationItemStyle");
        }

        // Return selected style if the current section matches the button section
        if (string.Equals(currentSection, buttonSection, StringComparison.OrdinalIgnoreCase))
        {
            return Application.Current.FindResource("NavigationItemSelectedStyle");
        }

        // Return normal style for unselected items
        return Application.Current.FindResource("NavigationItemStyle");
    }

    /// <summary>
    /// Converts back (not implemented)
    /// </summary>
    /// <param name="value">The value</param>
    /// <param name="targetTypes">The target types</param>
    /// <param name="parameter">The parameter</param>
    /// <param name="culture">The culture</param>
    /// <returns>Not implemented</returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}