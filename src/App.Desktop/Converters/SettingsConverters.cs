using System;
using System.Globalization;
using System.Windows.Data;

namespace Lazarus.Desktop.Converters;

/// <summary>
/// Converts string values to boolean for RadioButton bindings
/// </summary>
public class StringToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;

        return value.ToString() == parameter.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue && boolValue)
            return parameter?.ToString() ?? string.Empty;

        return Binding.DoNothing;
    }
}

/// <summary>
/// Maps the selected modality to the left panel header text.
/// </summary>
public class ModalityToLeftHeaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var modality = value?.ToString() ?? string.Empty;
        return modality switch
        {
            "Conversations" => "Conversation Models",
            "Voice" => "Voice Models",
            "Images" => "Image Models",
            "ThreeD" => "3D Models",
            "Entities" => "Entities",
            "Videos" => "Video Files",
            "DesignProgress" => "Mission Control",
            _ => "Models"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Maps the selected modality to the right panel header text.
/// </summary>
public class ModalityToRightHeaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var modality = value?.ToString() ?? string.Empty;
        return modality switch
        {
            "Conversations" => "Conversation Details",
            "Voice" => "Voice Details",
            "Images" => "Image Details",
            "ThreeD" => "3D Model Details",
            "Entities" => "Entity Details",
            "Videos" => "Video Details",
            "DesignProgress" => "Job Details",
            _ => "Details"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}

/// <summary>
/// Converts milliseconds to seconds for display
/// </summary>
public class MillisecondsToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int milliseconds)
            return milliseconds / 1000.0;
        
        if (value is double ms)
            return ms / 1000.0;

        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double seconds)
            return (int)(seconds * 1000);

        if (value is string str && double.TryParse(str, out var sec))
            return (int)(sec * 1000);

        return 0;
    }
}

/// <summary>
/// Generic equality converter: returns true when value.ToString() == parameter.ToString().
/// </summary>
public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null) return false;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.Ordinal);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            return parameter?.ToString() ?? string.Empty;
        }
        return Binding.DoNothing;
    }
}