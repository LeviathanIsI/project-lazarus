using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lazarus.Shared.Models;

/// <summary>
/// Dynamic model capability detection - what parameters can actually be modified
/// </summary>
public class ModelCapabilities
{
    public string ModelName { get; set; } = "";
    public string ModelFamily { get; set; } = ""; // "qwen", "llama", "mistral", "gemma", etc.
    public string Architecture { get; set; } = ""; // "transformer", "mamba", "retnet", etc.
    public ModelClass Class { get; set; } = ModelClass.Unknown;
    public long ParameterCount { get; set; }
    public int ContextLength { get; set; }
    public string Quantization { get; set; } = "";
    
    /// <summary>
    /// Parameters that can actually be modified for this model
    /// </summary>
    public Dictionary<string, ParameterCapability> AvailableParameters { get; set; } = new();
    
    /// <summary>
    /// Parameter interdependencies - when one parameter affects another
    /// </summary>
    public List<ParameterDependency> Dependencies { get; set; } = new();
    
    /// <summary>
    /// Model-recommended defaults (better than hardcoded values)
    /// </summary>
    public Dictionary<string, object> RecommendedDefaults { get; set; } = new();
    
    /// <summary>
    /// Parameters that should be hidden for this model
    /// </summary>
    public HashSet<string> UnsupportedParameters { get; set; } = new();
    
    /// <summary>
    /// Model-specific warnings and limitations
    /// </summary>
    public List<string> ModelWarnings { get; set; } = new();
    
    /// <summary>
    /// Last time capabilities were detected
    /// </summary>
    public DateTime DetectionTimestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// LoRA adapters currently applied to this model
    /// </summary>
    public List<AppliedLoRAInfo> AppliedLoRAs { get; set; } = new();
    
    /// <summary>
    /// LoRA-specific parameter modifications
    /// </summary>
    public Dictionary<string, LoRAParameterModification> LoRAModifications { get; set; } = new();
    
    /// <summary>
    /// Whether LoRAs are currently active and affecting inference
    /// </summary>
    public bool HasActiveLoRAs => AppliedLoRAs.Any(l => l.IsEnabled);
    
    /// <summary>
    /// Total LoRA weight impact on the model
    /// </summary>
    public float TotalLoRAWeight => AppliedLoRAs.Where(l => l.IsEnabled).Sum(l => l.Weight);
}

/// <summary>
/// What we can actually do with a specific parameter on this model
/// </summary>
public class ParameterCapability
{
    public string Name { get; set; } = "";
    public ParameterType Type { get; set; } = ParameterType.Float;
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public object? DefaultValue { get; set; }
    public double? StepSize { get; set; } // For sliders
    public List<object>? AllowedValues { get; set; } // For enums
    
    public bool IsModifiable { get; set; } = true;
    public bool IsExperimental { get; set; } = false;
    public bool IsRecommended { get; set; } = true;
    
    /// <summary>
    /// Model-specific description for this parameter
    /// </summary>
    public string ModelSpecificDescription { get; set; } = "";
    
    /// <summary>
    /// Validation rules specific to this model
    /// </summary>
    public List<ValidationRule> ValidationRules { get; set; } = new();
}

/// <summary>
/// Parameter interdependencies - "if X then Y"
/// </summary>
public class ParameterDependency
{
    public string TriggerParameter { get; set; } = "";
    public ComparisonType Condition { get; set; } = ComparisonType.Equals;
    public object? TriggerValue { get; set; }
    
    public string AffectedParameter { get; set; } = "";
    public DependencyAction Action { get; set; } = DependencyAction.Hide;
    public object? NewValue { get; set; }
    public string? Warning { get; set; }
}

/// <summary>
/// Model validation rules
/// </summary>
public class ValidationRule
{
    public string RuleName { get; set; } = "";
    public ComparisonType Comparison { get; set; } = ComparisonType.GreaterThan;
    public object? CompareValue { get; set; }
    public string ErrorMessage { get; set; } = "";
    public ValidationType Severity { get; set; } = ValidationType.Warning;
}

// Enums for type safety
public enum ModelClass
{
    Unknown,
    Basic,      // Simple models, minimal parameters
    Standard,   // Most models, standard parameter set
    Advanced,   // Sophisticated models, full parameter suite
    Experimental // Bleeding edge, may have unique parameters
}

public enum ParameterType
{
    Float,
    Integer,
    Boolean,
    String,
    Enum,
    Array
}

public enum ComparisonType
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    In,
    Range
}

public enum DependencyAction
{
    Hide,           // Hide the affected parameter
    Disable,        // Show but disable
    SetValue,       // Change to specific value
    SetRange,       // Change min/max range
    ShowWarning     // Display warning message
}

public enum ValidationType
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Model family-specific parameter logic
/// </summary>
public static class ModelFamilyProfiles
{
    public static readonly Dictionary<string, ModelProfile> Profiles = new()
    {
        ["qwen"] = new ModelProfile
        {
            Family = "qwen",
            PreferredParameters = ["Temperature", "TopP", "TopK", "RepetitionPenalty"],
            ExcellentParameters = ["MinP", "TypicalP"], // Qwen responds well to these
            ProblematicParameters = ["MirostatMode"], // May cause issues
            DefaultTemperature = 0.7f,
            DefaultTopP = 0.8f, // Qwen likes slightly lower top-p
            SpecialBehaviors = ["Responds well to MinP over TopP", "Sensitive to repetition penalty"]
        },
        
        ["llama"] = new ModelProfile
        {
            Family = "llama", 
            PreferredParameters = ["Temperature", "TopP", "TopK", "FrequencyPenalty"],
            ExcellentParameters = ["MirostatMode", "MirostatTau"], // LLaMA invented Mirostat
            ProblematicParameters = ["DryMultiplier"], // Too new for older LLaMA
            DefaultTemperature = 0.8f, // LLaMA handles higher temp well
            DefaultTopP = 0.9f,
            SpecialBehaviors = ["Excellent Mirostat support", "Handles higher temperatures"]
        },
        
        ["mistral"] = new ModelProfile
        {
            Family = "mistral",
            PreferredParameters = ["Temperature", "TopP", "TopK"],
            ExcellentParameters = ["TfsZ"], // Mistral works well with TFS
            ProblematicParameters = ["EtaCutoff", "EpsilonCutoff"], // Experimental features
            DefaultTemperature = 0.6f, // Mistral prefers lower temp
            DefaultTopP = 0.95f,
            SpecialBehaviors = ["Conservative temperature recommended", "TFS works well"]
        }
    };
}

public class ModelProfile
{
    public string Family { get; set; } = "";
    public List<string> PreferredParameters { get; set; } = new();
    public List<string> ExcellentParameters { get; set; } = new();
    public List<string> ProblematicParameters { get; set; } = new();
    public float DefaultTemperature { get; set; } = 0.7f;
    public float DefaultTopP { get; set; } = 0.9f;
    public List<string> SpecialBehaviors { get; set; } = new();
}

/// <summary>
/// Information about a LoRA adapter applied to the model
/// </summary>
public class AppliedLoRAInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string AdapterType { get; set; } = "";
    public float Weight { get; set; } = 0.8f;
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
    
    // LoRA technical specs
    public int Rank { get; set; } = 16;
    public int Alpha { get; set; } = 16;
    public List<string> TargetModules { get; set; } = new();
    public string BaseModel { get; set; } = "";
    
    // Impact on inference
    public List<string> AffectedParameters { get; set; } = new();
    public Dictionary<string, float> ParameterInfluence { get; set; } = new();
    
    // Metadata
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = "";
}

/// <summary>
/// How a LoRA modifies a specific parameter's behavior
/// </summary>
public class LoRAParameterModification
{
    public string ParameterName { get; set; } = "";
    public ModificationType Type { get; set; } = ModificationType.Range;
    
    // Range modifications
    public object? NewMinValue { get; set; }
    public object? NewMaxValue { get; set; }
    public object? NewDefaultValue { get; set; }
    
    // Behavioral changes
    public float SensitivityMultiplier { get; set; } = 1.0f;
    public string ModificationDescription { get; set; } = "";
    
    // Which LoRAs cause this modification
    public List<string> CausingLoRAs { get; set; } = new();
}

public enum ModificationType
{
    Range,          // Changes min/max/default values
    Sensitivity,    // Changes how sensitive the parameter is
    Behavior,       // Completely changes parameter behavior
    NewParameter,   // Adds a new parameter
    Disable         // Disables a parameter
}