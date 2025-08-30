using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Lazarus.Desktop.Converters
{
    public sealed class RoleToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as string) switch
            {
                "user" => new SolidColorBrush(Color.FromRgb(139, 92, 246)),      // purple
                "assistant" => new SolidColorBrush(Color.FromRgb(34, 197, 94)), // green
                "system" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),    // red
                _ => new SolidColorBrush(Color.FromRgb(75, 85, 99))             // gray
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class RoleToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as string) switch
            {
                "user" => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Left
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }


    public sealed class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !string.IsNullOrWhiteSpace(value as string);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class BoolToTextConverter : IValueConverter
    {
        public string TrueText { get; set; } = "True";
        public string FalseText { get; set; } = "False";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? TrueText : FalseText;
            return FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class StringEqualsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string parameterValue)
                return string.Equals(stringValue, parameterValue, StringComparison.OrdinalIgnoreCase) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class NullToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Returns true when value != null, false otherwise (for IsEnabled bindings)
    public sealed class NullToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class BooleanToAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            return HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
                return isUser 
                    ? new SolidColorBrush(Color.FromRgb(196, 69, 54))   // AccentRedBrush equivalent
                    : new SolidColorBrush(Color.FromRgb(30, 30, 30));   // TertiaryDarkBrush equivalent
            return new SolidColorBrush(Color.FromRgb(30, 30, 30));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Missing converters that cause LoRAs tab crashes
    public sealed class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class InverseStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class BoolToApplyTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isLoaded && isLoaded ? "✓ Applied" : "Apply";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class BoolToLoRAStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool hasActiveLoRAs && hasActiveLoRAs 
                ? "Model Modified by LoRAs" 
                : "Base Model Configuration";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Additional converters needed for ViewMode templates
    public sealed class BoolToActiveStatusConverter : IValueConverter
    {
        public static readonly BoolToActiveStatusConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isActive && isActive ? "ACTIVE" : "IDLE";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class BoolToAppliedStatusConverter : IValueConverter
    {
        public static readonly BoolToAppliedStatusConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isApplied && isApplied ? "APPLIED" : "READY";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class BoolToAccentBrushConverter : IValueConverter
    {
        public static readonly BoolToAccentBrushConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isActive && isActive 
                ? Application.Current.FindResource("AccentGreenBrush") 
                : Application.Current.FindResource("TextMutedBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Maps status strings (e.g., "OK", "Warning", "Error") to brushes for dark theme
    public sealed class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (value as string)?.ToLowerInvariant() ?? string.Empty;

            // Choose brush keys by severity; fall back to muted text for unknown
            string brushKey = status switch
            {
                var s when s.Contains("ok") || s.Contains("valid") || s.Contains("pass") => "AccentGreenBrush",
                var s when s.Contains("warn") || s.Contains("caution") => "AccentYellowBrush",
                var s when s.Contains("error") || s.Contains("fail") || s.Contains("invalid") => "ErrorBrush",
                _ => "TextMutedBrush"
            };

            return Application.Current.FindResource(brushKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class RoleToDisplayNameConverter : IValueConverter
    {
        public static readonly RoleToDisplayNameConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as string) switch
            {
                "user" => "USER",
                "assistant" => "ASSISTANT", 
                "system" => "SYSTEM",
                _ => "UNKNOWN"
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class RoleToBackgroundBrushConverter : IValueConverter
    {
        public static readonly RoleToBackgroundBrushConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value as string) switch
            {
                "user" => Application.Current.FindResource("TertiaryDarkBrush"),
                "assistant" => Application.Current.FindResource("SecondaryDarkBrush"),
                "system" => Application.Current.FindResource("PrimaryDarkBrush"),
                _ => Application.Current.FindResource("SecondaryDarkBrush")
            };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Update existing InvertBoolConverter to have Instance property
    public sealed class InvertBoolConverter : IValueConverter
    {
        public static readonly InvertBoolConverter Instance = new();
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : true;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : false;
    }

    /// <summary>
    /// Converts boolean to "Selected" tag for button states
    /// Used for MVVM navigation button styling
    /// </summary>
    public sealed class BoolToSelectedTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
                return "Selected";
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Forensic visibility converter to track binding changes during navigation debugging
    /// </summary>
    public sealed class ForensicVisibilityConverter : IValueConverter
    {
        public ForensicVisibilityConverter()
        {
            Console.WriteLine("[ForensicVisibilityConverter] FORENSICS: Constructor called - Converter initialized");
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterName = parameter?.ToString() ?? "UNKNOWN";
            var boolValue = value is bool b && b;
            var visibility = boolValue ? Visibility.Visible : Visibility.Collapsed;
            
            Console.WriteLine($"[ForensicVisibilityConverter] FORENSICS: {parameterName} - Input: {value} ({value?.GetType().Name}) -> Boolean: {boolValue} -> Visibility: {visibility}");
            
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class SpeakingToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isSpeaking = value is bool b && b;
            return isSpeaking ? "Stop Talking" : "Start Talking";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class SpeakingToBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isSpeaking = value is bool b && b;
            var resourceKey = isSpeaking ? "ErrorBrush" : "AccentRedBrush";
            return Application.Current.FindResource(resourceKey);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public sealed class StringToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string hex)
                {
                    return (Color)ColorConverter.ConvertFromString(hex);
                }
            }
            catch { }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}