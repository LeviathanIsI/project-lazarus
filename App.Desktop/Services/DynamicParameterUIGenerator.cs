using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using Lazarus.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Generates dynamic parameter UI based on actual model capabilities
/// NO MORE STATIC FORMS PRETENDING ALL MODELS ARE IDENTICAL!
/// </summary>
public static class DynamicParameterUIGenerator
{
    private static readonly ILogger Logger = LoggerFactory.Create(builder =>
        builder.AddConsole().SetMinimumLevel(LogLevel.Debug)
    ).CreateLogger("DynamicUI");

    /// <summary>
    /// Generates a complete parameter control panel for the model's actual capabilities
    /// </summary>
    public static StackPanel GenerateParameterControls(ModelCapabilities capabilities, object dataContext)
    {
        Logger.LogInformation($"🎨 Generating dynamic UI for {capabilities.ModelName} ({capabilities.AvailableParameters.Count} parameters)");
        
        var mainPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(10),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1a1a1a"))
        };

        // Model info header
        AddModelInfoHeader(mainPanel, capabilities);
        
        // Generate parameter sections based on model class
        switch (capabilities.Class)
        {
            case ModelClass.Basic:
                AddBasicParameterSection(mainPanel, capabilities, dataContext);
                break;
                
            case ModelClass.Standard:
                AddStandardParameterSection(mainPanel, capabilities, dataContext);
                break;
                
            case ModelClass.Advanced:
                AddAdvancedParameterSection(mainPanel, capabilities, dataContext);
                break;
                
            case ModelClass.Experimental:
                AddExperimentalParameterSection(mainPanel, capabilities, dataContext);
                break;
        }
        
        // Add model-specific warnings
        if (capabilities.ModelWarnings.Any())
        {
            AddWarningsSection(mainPanel, capabilities);
        }
        
        Logger.LogDebug($"✅ Generated {mainPanel.Children.Count} UI sections");
        return mainPanel;
    }

    /// <summary>
    /// Model information header with key stats
    /// </summary>
    private static void AddModelInfoHeader(StackPanel parent, ModelCapabilities capabilities)
    {
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 0, 0, 20),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2a2a2a")),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        // Model name
        var nameLabel = new TextBlock
        {
            Text = capabilities.ModelName,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")),
            Margin = new Thickness(10, 10, 10, 5)
        };
        headerPanel.Children.Add(nameLabel);
        
        // Model stats
        var statsText = $"{capabilities.ModelFamily.ToUpper()} • {capabilities.ParameterCount / 1_000_000_000.0:F1}B params • {capabilities.ContextLength:N0} context";
        if (!string.IsNullOrEmpty(capabilities.Quantization))
            statsText += $" • {capabilities.Quantization}";
            
        var statsLabel = new TextBlock
        {
            Text = statsText,
            FontSize = 11,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
            Margin = new Thickness(10, 0, 10, 10)
        };
        headerPanel.Children.Add(statsLabel);
        
        // Parameter count indicator
        var paramCountLabel = new TextBlock
        {
            Text = $"{capabilities.AvailableParameters.Count} adjustable parameters discovered",
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
            Margin = new Thickness(10, 0, 10, 10)
        };
        headerPanel.Children.Add(paramCountLabel);
        
        parent.Children.Add(headerPanel);
    }

    /// <summary>
    /// Basic models - minimal, focused controls
    /// </summary>
    private static void AddBasicParameterSection(StackPanel parent, ModelCapabilities capabilities, object dataContext)
    {
        Logger.LogDebug("Generating basic parameter section");
        
        AddSectionHeader(parent, "🎯 Core Parameters", "Essential controls for this model");
        
        var parametersToShow = new[] { "Temperature", "MaxTokens" };
        
        foreach (var paramName in parametersToShow)
        {
            if (capabilities.AvailableParameters.TryGetValue(paramName, out var capability))
            {
                AddParameterControl(parent, capability, dataContext);
            }
        }
    }

    /// <summary>
    /// Standard models - balanced parameter set
    /// </summary>
    private static void AddStandardParameterSection(StackPanel parent, ModelCapabilities capabilities, object dataContext)
    {
        Logger.LogDebug("Generating standard parameter section");
        
        // Core sampling parameters
        AddSectionHeader(parent, "🎯 Core Sampling", "Primary creativity and focus controls");
        foreach (var paramName in new[] { "Temperature", "TopP", "TopK" })
        {
            if (capabilities.AvailableParameters.TryGetValue(paramName, out var capability))
            {
                AddParameterControl(parent, capability, dataContext);
            }
        }
        
        // Repetition control
        var antiRepetitionParams = capabilities.AvailableParameters.Where(p => 
            p.Key.Contains("Penalty") && p.Value.IsRecommended).ToList();
        
        if (antiRepetitionParams.Any())
        {
            AddSectionHeader(parent, "🔄 Anti-Repetition", "Prevent loops and repetitive output");
            foreach (var (name, capability) in antiRepetitionParams)
            {
                AddParameterControl(parent, capability, dataContext);
            }
        }
        
        // Generation control
        AddSectionHeader(parent, "📝 Generation Control", "Output length and randomization");
        foreach (var paramName in new[] { "MaxTokens", "Seed" })
        {
            if (capabilities.AvailableParameters.TryGetValue(paramName, out var capability))
            {
                AddParameterControl(parent, capability, dataContext);
            }
        }
    }

    /// <summary>
    /// Advanced models - comprehensive parameter suite
    /// </summary>
    private static void AddAdvancedParameterSection(StackPanel parent, ModelCapabilities capabilities, object dataContext)
    {
        Logger.LogDebug("Generating advanced parameter section");
        
        // Group parameters by functionality
        var coreParams = new[] { "Temperature", "TopP", "TopK", "MinP", "TypicalP" };
        var repetitionParams = capabilities.AvailableParameters.Where(p => 
            p.Key.Contains("Penalty") || p.Key.Contains("Repetition")).Select(p => p.Key).ToArray();
        var advancedSamplingParams = new[] { "TfsZ", "EtaCutoff", "EpsilonCutoff", "DryMultiplier" };
        var mirostatParams = new[] { "MirostatMode", "MirostatTau", "MirostatEta" };
        
        // Core sampling
        AddParameterSection(parent, capabilities, dataContext, "🎯 Core Sampling", 
            "Primary creativity and focus controls", coreParams);
        
        // Advanced sampling (only if supported)
        var supportedAdvanced = advancedSamplingParams.Where(p => 
            capabilities.AvailableParameters.ContainsKey(p) && 
            capabilities.AvailableParameters[p].IsRecommended).ToArray();
        
        if (supportedAdvanced.Any())
        {
            AddParameterSection(parent, capabilities, dataContext, "⚡ Advanced Sampling", 
                "Experimental sampling techniques", supportedAdvanced);
        }
        
        // Mirostat section (if supported)
        if (capabilities.AvailableParameters.ContainsKey("MirostatMode"))
        {
            AddParameterSection(parent, capabilities, dataContext, "🧠 Mirostat Control", 
                "Dynamic entropy targeting", mirostatParams);
        }
        
        // Anti-repetition
        if (repetitionParams.Any())
        {
            AddParameterSection(parent, capabilities, dataContext, "🔄 Anti-Repetition", 
                "Prevent loops and repetitive output", repetitionParams);
        }
        
        // Generation control
        AddParameterSection(parent, capabilities, dataContext, "📝 Generation Control", 
            "Output length and randomization", new[] { "MaxTokens", "Seed" });
    }

    /// <summary>
    /// Experimental models - bleeding edge parameters
    /// </summary>
    private static void AddExperimentalParameterSection(StackPanel parent, ModelCapabilities capabilities, object dataContext)
    {
        Logger.LogDebug("Generating experimental parameter section");
        
        // Show all available parameters grouped by recommendation level
        var recommendedParams = capabilities.AvailableParameters
            .Where(p => p.Value.IsRecommended && !p.Value.IsExperimental)
            .Select(p => p.Key).ToArray();
            
        var experimentalParams = capabilities.AvailableParameters
            .Where(p => p.Value.IsExperimental)
            .Select(p => p.Key).ToArray();
        
        if (recommendedParams.Any())
        {
            AddParameterSection(parent, capabilities, dataContext, "✅ Recommended Parameters", 
                "Safe parameters for this experimental model", recommendedParams);
        }
        
        if (experimentalParams.Any())
        {
            AddParameterSection(parent, capabilities, dataContext, "🧪 Experimental Parameters", 
                "Bleeding edge - use with caution!", experimentalParams);
        }
    }

    /// <summary>
    /// Add a parameter section with header
    /// </summary>
    private static void AddParameterSection(StackPanel parent, ModelCapabilities capabilities, object dataContext, 
        string title, string description, string[] parameterNames)
    {
        var hasAnyParams = parameterNames.Any(name => capabilities.AvailableParameters.ContainsKey(name));
        if (!hasAnyParams) return;
        
        AddSectionHeader(parent, title, description);
        
        foreach (var paramName in parameterNames)
        {
            if (capabilities.AvailableParameters.TryGetValue(paramName, out var capability))
            {
                AddParameterControl(parent, capability, dataContext);
            }
        }
    }

    /// <summary>
    /// Add a section header with title and description
    /// </summary>
    private static void AddSectionHeader(StackPanel parent, string title, string description)
    {
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 15, 0, 10)
        };
        
        var titleLabel = new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7fafc")),
            Margin = new Thickness(0, 0, 0, 2)
        };
        headerPanel.Children.Add(titleLabel);
        
        var descLabel = new TextBlock
        {
            Text = description,
            FontSize = 10,
            FontStyle = FontStyles.Italic,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
            Margin = new Thickness(0, 0, 0, 8)
        };
        headerPanel.Children.Add(descLabel);
        
        parent.Children.Add(headerPanel);
    }

    /// <summary>
    /// Generate appropriate control for parameter type
    /// </summary>
    private static void AddParameterControl(StackPanel parent, ParameterCapability capability, object dataContext)
    {
        var controlPanel = new Grid
        {
            Margin = new Thickness(0, 8, 0, 8)
        };
        
        controlPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        controlPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        
        // Parameter label with description
        var labelPanel = new StackPanel { Orientation = Orientation.Vertical };
        
        var nameLabel = new TextBlock
        {
            Text = capability.Name,
            FontSize = 12,
            FontWeight = FontWeights.Medium,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e2e8f0"))
        };
        labelPanel.Children.Add(nameLabel);
        
        if (!string.IsNullOrEmpty(capability.ModelSpecificDescription))
        {
            var descLabel = new TextBlock
            {
                Text = capability.ModelSpecificDescription,
                FontSize = 9,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(capability.IsRecommended 
                    ? (Color)ColorConverter.ConvertFromString("#10b981")  // Green for recommended
                    : (Color)ColorConverter.ConvertFromString("#f59e0b")), // Yellow for not recommended
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0)
            };
            labelPanel.Children.Add(descLabel);
        }
        
        Grid.SetColumn(labelPanel, 0);
        controlPanel.Children.Add(labelPanel);
        
        // Generate appropriate input control
        FrameworkElement inputControl = capability.Type switch
        {
            ParameterType.Float => CreateFloatSlider(capability, dataContext),
            ParameterType.Integer when capability.AllowedValues != null => CreateEnumComboBox(capability, dataContext),
            ParameterType.Integer => CreateIntegerSlider(capability, dataContext),
            ParameterType.Boolean => CreateBooleanCheckBox(capability, dataContext),
            ParameterType.Enum => CreateEnumComboBox(capability, dataContext),
            _ => CreateTextBox(capability, dataContext)
        };
        
        // Add experimental indicator
        if (capability.IsExperimental)
        {
            inputControl.ToolTip = "🧪 Experimental parameter - may cause unexpected behavior";
        }
        
        Grid.SetColumn(inputControl, 1);
        controlPanel.Children.Add(inputControl);
        
        parent.Children.Add(controlPanel);
    }

    /// <summary>
    /// Create float slider with value display
    /// </summary>
    private static FrameworkElement CreateFloatSlider(ParameterCapability capability, object dataContext)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        
        var slider = new Slider
        {
            Minimum = Convert.ToDouble(capability.MinValue),
            Maximum = Convert.ToDouble(capability.MaxValue),
            Value = Convert.ToDouble(capability.DefaultValue),
            Width = 70,
            Margin = new Thickness(0, 0, 5, 0),
            SmallChange = capability.StepSize ?? 0.01,
            LargeChange = capability.StepSize ?? 0.1
        };
        
        var valueBox = new TextBox
        {
            Width = 45,
            Height = 20,
            FontSize = 10,
            Text = capability.DefaultValue?.ToString() ?? "0",
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e1e1e")),
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7fafc")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"))
        };
        
        // Bind slider and textbox together
        var binding = new Binding(capability.Name)
        {
            Source = dataContext,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        
        slider.SetBinding(Slider.ValueProperty, binding);
        valueBox.SetBinding(TextBox.TextProperty, new Binding(capability.Name)
        {
            Source = dataContext,
            Mode = BindingMode.TwoWay,
            StringFormat = "F3",
            Converter = new FloatStringConverter()
        });
        
        panel.Children.Add(slider);
        panel.Children.Add(valueBox);
        
        return panel;
    }

    /// <summary>
    /// Create integer slider with value display
    /// </summary>
    private static FrameworkElement CreateIntegerSlider(ParameterCapability capability, object dataContext)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        
        var slider = new Slider
        {
            Minimum = Convert.ToDouble(capability.MinValue),
            Maximum = Convert.ToDouble(capability.MaxValue),
            Value = Convert.ToDouble(capability.DefaultValue),
            Width = 70,
            Margin = new Thickness(0, 0, 5, 0),
            SmallChange = 1,
            LargeChange = 10
        };
        
        var valueBox = new TextBox
        {
            Width = 45,
            Height = 20,
            FontSize = 10,
            Text = capability.DefaultValue?.ToString() ?? "0",
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e1e1e")),
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7fafc")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"))
        };
        
        var binding = new Binding(capability.Name)
        {
            Source = dataContext,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        
        slider.SetBinding(Slider.ValueProperty, binding);
        valueBox.SetBinding(TextBox.TextProperty, new Binding(capability.Name)
        {
            Source = dataContext,
            Mode = BindingMode.TwoWay,
            StringFormat = "F0",
            Converter = new IntegerStringConverter()
        });
        
        panel.Children.Add(slider);
        panel.Children.Add(valueBox);
        
        return panel;
    }

    /// <summary>
    /// Create enum/dropdown control
    /// </summary>
    private static FrameworkElement CreateEnumComboBox(ParameterCapability capability, object dataContext)
    {
        var comboBox = new ComboBox
        {
            Width = 100,
            Height = 22,
            FontSize = 10,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e1e1e")),
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7fafc")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"))
        };
        
        if (capability.AllowedValues != null)
        {
            foreach (var value in capability.AllowedValues)
            {
                comboBox.Items.Add(value);
            }
        }
        
        comboBox.SelectedItem = capability.DefaultValue;
        
        var binding = new Binding(capability.Name)
        {
            Source = dataContext,
            Mode = BindingMode.TwoWay
        };
        comboBox.SetBinding(ComboBox.SelectedItemProperty, binding);
        
        return comboBox;
    }

    /// <summary>
    /// Create boolean checkbox
    /// </summary>
    private static FrameworkElement CreateBooleanCheckBox(ParameterCapability capability, object dataContext)
    {
        var checkBox = new CheckBox
        {
            IsChecked = (bool?)capability.DefaultValue ?? false,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7fafc"))
        };
        
        var binding = new Binding(capability.Name)
        {
            Source = dataContext,
            Mode = BindingMode.TwoWay
        };
        checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
        
        return checkBox;
    }

    /// <summary>
    /// Create text input box
    /// </summary>
    private static FrameworkElement CreateTextBox(ParameterCapability capability, object dataContext)
    {
        var textBox = new TextBox
        {
            Width = 100,
            Height = 20,
            FontSize = 10,
            Text = capability.DefaultValue?.ToString() ?? "",
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e1e1e")),
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7fafc")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"))
        };
        
        var binding = new Binding(capability.Name)
        {
            Source = dataContext,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        textBox.SetBinding(TextBox.TextProperty, binding);
        
        return textBox;
    }

    /// <summary>
    /// Add model-specific warnings section
    /// </summary>
    private static void AddWarningsSection(StackPanel parent, ModelCapabilities capabilities)
    {
        var warningsPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 20, 0, 0),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c2d12")), // Dark red
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var headerLabel = new TextBlock
        {
            Text = "⚠️ Model-Specific Notes",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbbf24")), // Yellow
            Margin = new Thickness(10, 10, 10, 5)
        };
        warningsPanel.Children.Add(headerLabel);
        
        foreach (var warning in capabilities.ModelWarnings)
        {
            var warningLabel = new TextBlock
            {
                Text = $"• {warning}",
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fed7aa")), // Light orange
                Margin = new Thickness(10, 2, 10, 2),
                TextWrapping = TextWrapping.Wrap
            };
            warningsPanel.Children.Add(warningLabel);
        }
        
        var bottomMargin = new Border { Height = 10 };
        warningsPanel.Children.Add(bottomMargin);
        
        parent.Children.Add(warningsPanel);
    }
}

/// <summary>
/// Converter for float slider/textbox binding
/// </summary>
public class FloatStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f) return f.ToString("F3");
        if (value is double d) return d.ToString("F3");
        return value?.ToString() ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && float.TryParse(s, out var result))
            return result;
        return 0f;
    }
}

/// <summary>
/// Converter for integer slider/textbox binding
/// </summary>
public class IntegerStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && int.TryParse(s, out var result))
            return result;
        return 0;
    }
}