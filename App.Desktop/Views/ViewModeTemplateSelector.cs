using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Lazarus.Shared.Models;
using Lazarus.Desktop.Services;
using App.Shared.Enums;
using System;

namespace Lazarus.Desktop.Views;

/// <summary>
/// Advanced template selector that combines parameter type with ViewMode complexity
/// Provides true progressive disclosure by selecting templates from Novice/Enthusiast/Developer dictionaries
/// </summary>
public class ViewModeTemplateSelector : DataTemplateSelector
{
    private static ViewMode _currentViewMode = ViewMode.Novice;
    
    static ViewModeTemplateSelector()
    {
        // Subscribe to global ViewMode changes
        UserPreferencesService.ViewModeChanged += (sender, newMode) =>
        {
            _currentViewMode = newMode;
            Console.WriteLine($"[ViewModeTemplateSelector] ViewMode changed to: {newMode}");
        };
    }
    
    public ViewModeTemplateSelector()
    {
        // Instance constructor - static event subscription handles ViewMode tracking
    }

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        var currentViewMode = _currentViewMode;
        
        // Handle parameter templates (for BaseModelView)
        if (item is KeyValuePair<string, ParameterMetadata> kvp)
        {
            var paramType = kvp.Value.Type.ToLowerInvariant();
            
            // Generate template key based on ViewMode + parameter type
            var templateKey = GenerateTemplateKey(paramType, kvp.Key, currentViewMode);
            
            // Try to find template in current ViewMode first
            var template = FindTemplate(templateKey, container);
            
            if (template != null)
                return template;
                
            // Fallback strategy: try simpler ViewModes if current template not found
            template = FallbackTemplateSearch(paramType, kvp.Key, currentViewMode, container);
            
            return template ?? CreateFallbackTemplate(paramType);
        }
        
        // Handle ViewModel templates (for Dashboard, Jobs, etc.)
        if (item != null)
        {
            var viewModelType = item.GetType().Name;
            // Special-case Datasets when user sees overlay: be lenient
            if (viewModelType.Contains("Dataset", StringComparison.OrdinalIgnoreCase))
            {
                // Try several known keys in order
                var candidates = new[]
                {
                    $"{currentViewMode}DatasetsViewModelTemplate",
                    $"{currentViewMode}DatasetViewModelTemplate",
                    $"DatasetsViewModelTemplate",
                    $"DatasetViewModelTemplate"
                };
                foreach (var key in candidates)
                {
                    var t = FindTemplate(key, container);
                    if (t != null) return t;
                }
                // Try direct load from the active ViewMode dictionary as a last resort
                foreach (var key in candidates)
                {
                    var t = TryLoadTemplateFromViewMode(currentViewMode, key);
                    if (t != null) return t;
                }
            }
            
            // Generate ViewMode-specific template key
            var templateKey = $"{currentViewMode}{viewModelType}Template";
            
            var template = FindTemplate(templateKey, container);
            if (template != null)
                return template;
            
            // Hard recovery: attempt to load the ViewMode dictionary directly and fetch the template
            Console.WriteLine($"[Selector] Attempting hard recovery for {templateKey}");
            template = TryLoadTemplateFromViewMode(currentViewMode, templateKey);
            if (template != null)
            {
                Console.WriteLine($"[Selector] ✅ Hard recovery successful for {templateKey}");
                return template;
            }
            else
            {
                Console.WriteLine($"[Selector] ❌ Hard recovery failed for {templateKey}");
            }

            // Try fallback chain
            template = FallbackViewModelTemplateSearch(viewModelType, currentViewMode, container);
            if (template != null)
                return template;
            
            // After failing to find a template by key(s):
            Console.WriteLine($"[Selector] ❌ No template found for VM={viewModelType} mode={currentViewMode} key={templateKey}");
            return BuildFallbackTemplate($"{currentViewMode}:{viewModelType} -> {templateKey}");
        }

        return base.SelectTemplate(item, container);
    }
    
    /// <summary>
    /// Generate template key based on ViewMode and parameter characteristics
    /// </summary>
    private string GenerateTemplateKey(string paramType, string paramName, ViewMode viewMode)
    {
        // Special handling for specific parameter names that need custom templates
        var specialTemplates = new Dictionary<string, string>
        {
            { "temperature", "TemperatureTemplate" },
            { "cfg_scale", "CFGTemplate" }, 
            { "steps", "StepsTemplate" },
            { "seed", "SeedTemplate" }
        };
        
        if (specialTemplates.TryGetValue(paramName.ToLowerInvariant(), out var specialTemplate))
        {
            return $"{viewMode}{specialTemplate}";
        }
        
        // Standard template naming: [ViewMode][ParameterType]Template
        return paramType switch
        {
            "float" => $"{viewMode}FloatParameterTemplate",
            "int" => $"{viewMode}IntParameterTemplate", 
            "bool" => $"{viewMode}BoolParameterTemplate",
            "dropdown" => $"{viewMode}DropdownParameterTemplate",
            "string" => $"{viewMode}StringParameterTemplate",
            _ => $"{viewMode}FloatParameterTemplate" // Default fallback
        };
    }

    private static DataTemplate? TryLoadTemplateFromViewMode(ViewMode mode, string key)
    {
        try
        {
            string file = mode switch
            {
                ViewMode.Novice => "NoviceTemplates.xaml",
                ViewMode.Enthusiast => "EnthusiastTemplates.xaml",
                ViewMode.Developer => "DeveloperTemplates.xaml",
                _ => "NoviceTemplates.xaml"
            };
            var uri = new Uri($"pack://application:,,,/Resources/ViewModes/{file}");
            Console.WriteLine($"[Selector] Loading dictionary from {file}");
            var dict = new ResourceDictionary { Source = uri };
            Console.WriteLine($"[Selector] Dictionary loaded, contains {dict.Count} items");
            if (dict.Contains(key))
            {
                Console.WriteLine($"[Selector] ✅ Found {key} in {file}");
                return dict[key] as DataTemplate;
            }
            Console.WriteLine($"[Selector] ❌ {key} not found in {file}");
            // List all keys for debugging
            foreach (var dictKey in dict.Keys)
            {
                if (dictKey.ToString()?.Contains("Dataset") == true)
                {
                    Console.WriteLine($"[Selector]   - Found dataset-related key: {dictKey}");
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Find template in application resources
    /// </summary>
    private DataTemplate? FindTemplate(string templateKey, DependencyObject container)
    {
        try
        {
            // Prefer WPF resource lookup which traverses merged dictionaries
            var fromApp = Application.Current?.TryFindResource(templateKey) as DataTemplate;
            if (fromApp != null)
            {
                return fromApp;
            }

            if (container is FrameworkElement fe)
            {
                var fromElement = fe.TryFindResource(templateKey) as DataTemplate;
                if (fromElement != null)
                {
                    return fromElement;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Fallback search strategy: try simpler ViewModes if current template missing
    /// Developer → Enthusiast → Novice → Generic
    /// </summary>
    private DataTemplate? FallbackTemplateSearch(string paramType, string paramName, ViewMode currentMode, DependencyObject container)
    {
        // Define fallback chain
        var fallbackChain = currentMode switch
        {
            ViewMode.Developer => new[] { ViewMode.Enthusiast, ViewMode.Novice },
            ViewMode.Enthusiast => new[] { ViewMode.Novice },
            ViewMode.Novice => new ViewMode[] { },
            _ => new ViewMode[] { }
        };
        
        // Try each fallback ViewMode
        foreach (var fallbackMode in fallbackChain)
        {
            var fallbackKey = GenerateTemplateKey(paramType, paramName, fallbackMode);
            var template = FindTemplate(fallbackKey, container);
            
            if (template != null)
            {
                Console.WriteLine($"[ViewModeTemplateSelector] Using fallback template: {fallbackKey} for {currentMode} mode");
                return template;
            }
        }
        
        // Try generic templates (no ViewMode prefix)
        var genericKey = paramType switch
        {
            "float" => "FloatParameterTemplate",
            "int" => "IntParameterTemplate",
            "bool" => "BoolParameterTemplate", 
            "dropdown" => "DropdownParameterTemplate",
            "string" => "StringParameterTemplate",
            _ => "FloatParameterTemplate"
        };
        
        return FindTemplate(genericKey, container);
    }
    
    /// <summary>
    /// Fallback search for ViewModel templates - try simpler ViewModes
    /// </summary>
    private DataTemplate? FallbackViewModelTemplateSearch(string viewModelType, ViewMode currentMode, DependencyObject container)
    {
        // Define fallback chain for ViewModels
        var fallbackChain = currentMode switch
        {
            ViewMode.Developer => new[] { ViewMode.Enthusiast, ViewMode.Novice },
            ViewMode.Enthusiast => new[] { ViewMode.Novice },
            ViewMode.Novice => new ViewMode[] { },
            _ => new ViewMode[] { }
        };
        
        // Try each fallback ViewMode
        foreach (var fallbackMode in fallbackChain)
        {
            var fallbackKey = $"{fallbackMode}{viewModelType}Template";
            var template = FindTemplate(fallbackKey, container);
            
            if (template != null)
            {
                Console.WriteLine($"[ViewModeTemplateSelector] Using fallback ViewModel template: {fallbackKey} for {currentMode} mode");
                return template;
            }
        }
        
        // Try generic template (no ViewMode prefix)
        var genericKey = $"{viewModelType}Template";
        return FindTemplate(genericKey, container);
    }
    
    /// <summary>
    /// Create emergency fallback template if all searches fail
    /// </summary>
    private DataTemplate CreateFallbackTemplate(string paramType)
    {
        Console.WriteLine($"[ViewModeTemplateSelector] Creating emergency fallback template for {paramType}");
        
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.BackgroundProperty, Application.Current.Resources["TertiaryDarkBrush"]);
        factory.SetValue(Border.PaddingProperty, new Thickness(8));
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        
        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetValue(TextBlock.TextProperty, $"[Missing {paramType} template]");
        textFactory.SetValue(TextBlock.ForegroundProperty, Application.Current.Resources["ErrorBrush"]);
        textFactory.SetValue(TextBlock.FontStyleProperty, FontStyles.Italic);
        
        factory.AppendChild(textFactory);
        template.VisualTree = factory;
        
        return template;
    }
    
    private static DataTemplate BuildFallbackTemplate(string message)
    {
        var dt = new DataTemplate();
        var fef = new FrameworkElementFactory(typeof(Border));
        fef.SetValue(Border.PaddingProperty, new Thickness(16));
        fef.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        fef.SetValue(Border.BorderBrushProperty, Brushes.OrangeRed);
        var inner = new FrameworkElementFactory(typeof(TextBlock));
        inner.SetValue(TextBlock.TextProperty, $"[MISSING TEMPLATE] {message}");
        inner.SetValue(TextBlock.FontSizeProperty, 16.0);
        inner.SetValue(TextBlock.FontWeightProperty, FontWeights.Bold);
        fef.AppendChild(inner);
        dt.VisualTree = fef;
        return dt;
    }
}