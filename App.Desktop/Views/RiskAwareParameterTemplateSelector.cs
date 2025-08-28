using System.Windows;
using System.Windows.Controls;
using Lazarus.Shared.Models;

namespace Lazarus.Desktop.Views;

/// <summary>
/// Template selector that incorporates risk classification for parameter display
/// </summary>
public class RiskAwareParameterTemplateSelector : DataTemplateSelector
{
    // Standard parameter templates
    public DataTemplate SafeFloatTemplate { get; set; } = null!;
    public DataTemplate CautionFloatTemplate { get; set; } = null!;
    public DataTemplate ExperimentalFloatTemplate { get; set; } = null!;
    
    public DataTemplate SafeIntTemplate { get; set; } = null!;
    public DataTemplate CautionIntTemplate { get; set; } = null!;
    public DataTemplate ExperimentalIntTemplate { get; set; } = null!;
    
    public DataTemplate SafeBoolTemplate { get; set; } = null!;
    public DataTemplate CautionBoolTemplate { get; set; } = null!;
    public DataTemplate ExperimentalBoolTemplate { get; set; } = null!;
    
    public DataTemplate SafeDropdownTemplate { get; set; } = null!;
    public DataTemplate CautionDropdownTemplate { get; set; } = null!;
    public DataTemplate ExperimentalDropdownTemplate { get; set; } = null!;
    
    public DataTemplate SafeStringTemplate { get; set; } = null!;
    public DataTemplate CautionStringTemplate { get; set; } = null!;
    public DataTemplate ExperimentalStringTemplate { get; set; } = null!;
    
    // Fallback templates
    public DataTemplate DefaultSafeTemplate { get; set; } = null!;
    public DataTemplate DefaultCautionTemplate { get; set; } = null!;
    public DataTemplate DefaultExperimentalTemplate { get; set; } = null!;

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is not KeyValuePair<string, ParameterMetadata> kvp)
            return base.SelectTemplate(item, container);

        var parameter = kvp.Value;
        var paramType = parameter.Type.ToLowerInvariant();
        var riskLevel = parameter.RiskLevel;

        // Select template based on type and risk level
        return (paramType, riskLevel) switch
        {
            // Float templates
            ("float", ParameterRiskLevel.Safe) => SafeFloatTemplate ?? DefaultSafeTemplate,
            ("float", ParameterRiskLevel.Caution) => CautionFloatTemplate ?? DefaultCautionTemplate,
            ("float", ParameterRiskLevel.Experimental) => ExperimentalFloatTemplate ?? DefaultExperimentalTemplate,
            
            // Integer templates
            ("int", ParameterRiskLevel.Safe) => SafeIntTemplate ?? DefaultSafeTemplate,
            ("int", ParameterRiskLevel.Caution) => CautionIntTemplate ?? DefaultCautionTemplate,
            ("int", ParameterRiskLevel.Experimental) => ExperimentalIntTemplate ?? DefaultExperimentalTemplate,
            
            // Boolean templates
            ("bool", ParameterRiskLevel.Safe) => SafeBoolTemplate ?? DefaultSafeTemplate,
            ("bool", ParameterRiskLevel.Caution) => CautionBoolTemplate ?? DefaultCautionTemplate,
            ("bool", ParameterRiskLevel.Experimental) => ExperimentalBoolTemplate ?? DefaultExperimentalTemplate,
            
            // Dropdown templates
            ("dropdown", ParameterRiskLevel.Safe) when parameter.AllowedValues?.Any() == true => SafeDropdownTemplate ?? DefaultSafeTemplate,
            ("dropdown", ParameterRiskLevel.Caution) when parameter.AllowedValues?.Any() == true => CautionDropdownTemplate ?? DefaultCautionTemplate,
            ("dropdown", ParameterRiskLevel.Experimental) when parameter.AllowedValues?.Any() == true => ExperimentalDropdownTemplate ?? DefaultExperimentalTemplate,
            
            // String templates
            ("string", ParameterRiskLevel.Safe) => SafeStringTemplate ?? DefaultSafeTemplate,
            ("string", ParameterRiskLevel.Caution) => CautionStringTemplate ?? DefaultCautionTemplate,
            ("string", ParameterRiskLevel.Experimental) => ExperimentalStringTemplate ?? DefaultExperimentalTemplate,
            
            // Default fallbacks
            (_, ParameterRiskLevel.Safe) => DefaultSafeTemplate,
            (_, ParameterRiskLevel.Caution) => DefaultCautionTemplate,
            (_, ParameterRiskLevel.Experimental) => DefaultExperimentalTemplate,
            
            // Ultimate fallback
            _ => DefaultSafeTemplate ?? base.SelectTemplate(item, container)
        };
    }
}

/// <summary>
/// Value converter to get risk classification info for binding
/// </summary>
public class ParameterRiskConverter : System.Windows.Data.IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is not KeyValuePair<string, ParameterMetadata> kvp)
            return null;

        var parameterName = kvp.Key;
        var conversionTarget = parameter?.ToString()?.ToLowerInvariant();

        var classification = ParameterRiskRegistry.GetClassification(parameterName);
        if (classification == null) return null;

        return conversionTarget switch
        {
            "risklevel" => classification.RiskLevel,
            "category" => classification.Category,
            "riskcolor" => classification.GetRiskColor(),
            "riskicon" => classification.GetRiskIcon(),
            "description" => classification.Description,
            "safeguide" => classification.SafeUsageGuideline,
            "riskexplanation" => classification.RiskExplanation,
            "commonmistakes" => classification.CommonMistakes,
            "interactswith" => classification.InteractsWith,
            "recommendedfor" => classification.RecommendedFor,
            "avoidwhen" => classification.AvoidWhen,
            "requiresexpertise" => classification.RequiresExpertise,
            "canbreakmodel" => classification.CanBreakModel,
            "affectsquality" => classification.AffectsOutputQuality,
            "minimumlevel" => classification.MinimumExperienceLevel,
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}