using System.Windows;
using System.Windows.Controls;
using Lazarus.Shared.Models;

namespace Lazarus.Desktop.Views;

/// <summary>
/// Selects the appropriate data template based on parameter type
/// Because we're not fucking around with static UI anymore
/// </summary>
public class ParameterTemplateSelector : DataTemplateSelector
{
    public DataTemplate FloatTemplate { get; set; } = null!;
    public DataTemplate IntTemplate { get; set; } = null!;
    public DataTemplate BoolTemplate { get; set; } = null!;
    public DataTemplate DropdownTemplate { get; set; } = null!;
    public DataTemplate StringTemplate { get; set; } = null!;

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (item is KeyValuePair<string, ParameterMetadata> kvp)
        {
            var paramType = kvp.Value.Type.ToLowerInvariant();

            return paramType switch
            {
                "float" => FloatTemplate,
                "int" => IntTemplate,
                "bool" => BoolTemplate,
                "dropdown" when kvp.Value.AllowedValues?.Any() == true => DropdownTemplate,
                "string" => StringTemplate,
                _ => FloatTemplate // Default fallback for unknown types
            };
        }

        return base.SelectTemplate(item, container);
    }
}