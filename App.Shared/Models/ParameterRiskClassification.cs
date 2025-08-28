using System;
using System.Collections.Generic;
using System.Linq;
using App.Shared.Enums;

namespace Lazarus.Shared.Models;

/// <summary>
/// Risk levels for AI parameters based on potential impact and complexity
/// </summary>
public enum ParameterRiskLevel
{
    /// <summary>
    /// Safe for beginners - minimal risk of breaking functionality or producing unwanted output
    /// </summary>
    Safe = 1,

    /// <summary>
    /// Requires caution - may affect output quality or model behavior
    /// </summary>
    Caution = 2,

    /// <summary>
    /// Experimental/Advanced - high risk of instability, requires expertise
    /// </summary>
    Experimental = 3
}

/// <summary>
/// Categories for parameter functionality
/// </summary>
public enum ParameterCategory
{
    // Core sampling parameters
    CoreSampling,
    
    // Repetition and pattern control
    RepetitionControl,
    
    // Advanced sampling methods
    AdvancedSampling,
    
    // Context and memory
    ContextManagement,
    
    // Performance and optimization
    Performance,
    
    // Experimental features
    Experimental,
    
    // Output control
    OutputControl,
    
    // Model behavior
    ModelBehavior
}

/// <summary>
/// Comprehensive risk classification and metadata for AI parameters
/// </summary>
public class ParameterRiskClassification
{
    public string ParameterName { get; set; } = "";
    public ParameterRiskLevel RiskLevel { get; set; }
    public ParameterCategory Category { get; set; }
    public string Description { get; set; } = "";
    public string SafeUsageGuideline { get; set; } = "";
    public string RiskExplanation { get; set; } = "";
    public List<string> CommonMistakes { get; set; } = new();
    public bool RequiresExpertise { get; set; }
    public bool CanBreakModel { get; set; }
    public bool AffectsOutputQuality { get; set; }
    public int MinimumExperienceLevel { get; set; } = 1; // 1=Novice, 2=Enthusiast, 3=Developer
    public List<string> InteractsWith { get; set; } = new(); // Parameters this interacts with
    public string RecommendedFor { get; set; } = "";
    public string AvoidWhen { get; set; } = "";

    /// <summary>
    /// Get the display color for this risk level
    /// </summary>
    public string GetRiskColor()
    {
        return RiskLevel switch
        {
            ParameterRiskLevel.Safe => "#4CAF50", // Green
            ParameterRiskLevel.Caution => "#FF9800", // Orange
            ParameterRiskLevel.Experimental => "#F44336", // Red
            _ => "#757575" // Gray
        };
    }

    /// <summary>
    /// Get the icon for this risk level
    /// </summary>
    public string GetRiskIcon()
    {
        return RiskLevel switch
        {
            ParameterRiskLevel.Safe => "✅",
            ParameterRiskLevel.Caution => "⚠️",
            ParameterRiskLevel.Experimental => "⚠️",
            _ => "❓"
        };
    }

    /// <summary>
    /// Generate a comprehensive tooltip for this parameter
    /// </summary>
    public string GetRiskTooltip()
    {
        var lines = new List<string>
        {
            $"{GetRiskIcon()} {RiskLevel} Parameter",
            Description
        };

        if (!string.IsNullOrEmpty(SafeUsageGuideline))
            lines.Add($"💡 Safe Usage: {SafeUsageGuideline}");

        if (!string.IsNullOrEmpty(RiskExplanation))
            lines.Add($"⚠️ Risk: {RiskExplanation}");

        if (CommonMistakes.Any())
            lines.Add($"🚫 Common Mistakes: {string.Join(", ", CommonMistakes)}");

        if (InteractsWith.Any())
            lines.Add($"🔗 Interacts with: {string.Join(", ", InteractsWith)}");

        if (!string.IsNullOrEmpty(RecommendedFor))
            lines.Add($"✨ Best for: {RecommendedFor}");

        if (!string.IsNullOrEmpty(AvoidWhen))
            lines.Add($"❌ Avoid when: {AvoidWhen}");

        return string.Join("\n\n", lines);
    }
}

/// <summary>
/// Central registry for parameter risk classifications
/// </summary>
public static class ParameterRiskRegistry
{
    /// <summary>
    /// Comprehensive risk classifications for all AI parameters
    /// </summary>
    public static readonly Dictionary<string, ParameterRiskClassification> Classifications = new()
    {
        {
            nameof(SamplingParameters.Temperature),
            new ParameterRiskClassification
            {
                ParameterName = "Temperature",
                RiskLevel = ParameterRiskLevel.Safe,
                Category = ParameterCategory.CoreSampling,
                Description = "Controls creativity vs coherence in responses",
                SafeUsageGuideline = "Keep between 0.3-1.2 for reliable results",
                RiskExplanation = "Low values may cause repetitive loops, high values cause incoherence",
                CommonMistakes = { "Setting too high (>1.5)", "Using 0.0 for creative tasks" },
                RequiresExpertise = false,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 1,
                InteractsWith = { "TopP", "TopK", "MirostatMode" },
                RecommendedFor = "All users - primary creativity control",
                AvoidWhen = "Never - always safe to adjust within reasonable range"
            }
        },
        {
            nameof(SamplingParameters.TopP),
            new ParameterRiskClassification
            {
                ParameterName = "TopP",
                RiskLevel = ParameterRiskLevel.Safe,
                Category = ParameterCategory.CoreSampling,
                Description = "Nucleus sampling - limits vocabulary to top probability mass",
                SafeUsageGuideline = "Keep between 0.5-0.95 for balanced vocabulary",
                RiskExplanation = "Very low values limit vocabulary severely, 1.0 disables filtering",
                CommonMistakes = { "Setting below 0.3", "Using 1.0 without understanding", "Combining with very low TopK" },
                RequiresExpertise = false,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 1,
                InteractsWith = { "Temperature", "TopK", "MinP" },
                RecommendedFor = "Users who want vocabulary control",
                AvoidWhen = "Using TopK or MinP unless you understand the interaction"
            }
        },
        {
            nameof(SamplingParameters.TopK),
            new ParameterRiskClassification
            {
                ParameterName = "TopK",
                RiskLevel = ParameterRiskLevel.Safe,
                Category = ParameterCategory.CoreSampling,
                Description = "Limits vocabulary to K most likely tokens",
                SafeUsageGuideline = "Use 20-100 for focused responses, higher for creativity",
                RiskExplanation = "Very low values severely limit word choices",
                CommonMistakes = { "Setting below 10", "Using with TopP without understanding interaction" },
                RequiresExpertise = false,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 1,
                InteractsWith = { "TopP", "Temperature", "MinP" },
                RecommendedFor = "Users who want simple vocabulary control",
                AvoidWhen = "Already using TopP or MinP effectively"
            }
        },
        {
            nameof(SamplingParameters.MinP),
            new ParameterRiskClassification
            {
                ParameterName = "MinP",
                RiskLevel = ParameterRiskLevel.Caution,
                Category = ParameterCategory.AdvancedSampling,
                Description = "Dynamic threshold sampling based on highest probability token",
                SafeUsageGuideline = "Use 0.01-0.1 as alternative to TopP",
                RiskExplanation = "Can interact unpredictably with TopP/TopK, newer parameter with less testing",
                CommonMistakes = { "Using with TopP simultaneously", "Setting too high (>0.2)", "Not understanding the algorithm" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 2,
                InteractsWith = { "TopP", "TopK", "Temperature" },
                RecommendedFor = "Advanced users wanting modern sampling",
                AvoidWhen = "Already satisfied with TopP/TopK combination"
            }
        },
        {
            nameof(SamplingParameters.RepetitionPenalty),
            new ParameterRiskClassification
            {
                ParameterName = "RepetitionPenalty",
                RiskLevel = ParameterRiskLevel.Safe,
                Category = ParameterCategory.RepetitionControl,
                Description = "Penalizes repeated tokens to reduce loops",
                SafeUsageGuideline = "Use 1.05-1.15 for subtle anti-repetition",
                RiskExplanation = "High values can break grammar by avoiding necessary words",
                CommonMistakes = { "Setting too high (>1.3)", "Using below 1.0", "Not adjusting range parameter" },
                RequiresExpertise = false,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 1,
                InteractsWith = { "FrequencyPenalty", "PresencePenalty", "RepetitionPenaltyRange" },
                RecommendedFor = "Users experiencing repetitive output",
                AvoidWhen = "Model already produces varied output"
            }
        },
        {
            nameof(SamplingParameters.FrequencyPenalty),
            new ParameterRiskClassification
            {
                ParameterName = "FrequencyPenalty",
                RiskLevel = ParameterRiskLevel.Caution,
                Category = ParameterCategory.RepetitionControl,
                Description = "Penalizes tokens based on their frequency in output",
                SafeUsageGuideline = "Use 0.1-0.5 for gentle repetition reduction",
                RiskExplanation = "Can make output sound unnatural by avoiding common words",
                CommonMistakes = { "Setting too high (>1.0)", "Using negative values", "Combining with high repetition penalty" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 2,
                InteractsWith = { "PresencePenalty", "RepetitionPenalty" },
                RecommendedFor = "Users fine-tuning repetition behavior",
                AvoidWhen = "Repetition penalty already working well"
            }
        },
        {
            nameof(SamplingParameters.PresencePenalty),
            new ParameterRiskClassification
            {
                ParameterName = "PresencePenalty",
                RiskLevel = ParameterRiskLevel.Caution,
                Category = ParameterCategory.RepetitionControl,
                Description = "Penalizes any token that has appeared before",
                SafeUsageGuideline = "Use 0.1-0.5 for topic diversity",
                RiskExplanation = "Can avoid necessary words like articles and pronouns",
                CommonMistakes = { "Setting too high (>1.0)", "Not understanding difference from frequency penalty" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 2,
                InteractsWith = { "FrequencyPenalty", "RepetitionPenalty" },
                RecommendedFor = "Users wanting topic diversity",
                AvoidWhen = "Output already sufficiently varied"
            }
        },
        {
            nameof(SamplingParameters.MirostatMode),
            new ParameterRiskClassification
            {
                ParameterName = "MirostatMode",
                RiskLevel = ParameterRiskLevel.Experimental,
                Category = ParameterCategory.AdvancedSampling,
                Description = "Dynamic sampling targeting specific entropy levels",
                SafeUsageGuideline = "Use Mode 2 with Tau 4-6 for consistent perplexity",
                RiskExplanation = "Overrides Temperature completely, complex parameter interactions",
                CommonMistakes = { "Using Mode 1 instead of 2", "Not adjusting Tau/Eta", "Enabling without understanding entropy" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 3,
                InteractsWith = { "Temperature", "MirostatTau", "MirostatEta" },
                RecommendedFor = "Experts wanting consistent text perplexity",
                AvoidWhen = "Satisfied with temperature-based sampling"
            }
        },
        {
            nameof(SamplingParameters.MirostatTau),
            new ParameterRiskClassification
            {
                ParameterName = "MirostatTau",
                RiskLevel = ParameterRiskLevel.Experimental,
                Category = ParameterCategory.AdvancedSampling,
                Description = "Target entropy level for Mirostat sampling",
                SafeUsageGuideline = "Use 4-6 for balanced creativity/coherence",
                RiskExplanation = "Requires understanding of information theory and entropy",
                CommonMistakes = { "Setting too high (>8)", "Using without enabling Mirostat mode" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 3,
                InteractsWith = { "MirostatMode", "MirostatEta" },
                RecommendedFor = "Experts fine-tuning Mirostat behavior",
                AvoidWhen = "Mirostat mode disabled"
            }
        },
        {
            nameof(SamplingParameters.MirostatEta),
            new ParameterRiskClassification
            {
                ParameterName = "MirostatEta",
                RiskLevel = ParameterRiskLevel.Experimental,
                Category = ParameterCategory.AdvancedSampling,
                Description = "Learning rate for Mirostat entropy adaptation",
                SafeUsageGuideline = "Use 0.1 as starting point, adjust slowly",
                RiskExplanation = "High values can cause sampling instability",
                CommonMistakes = { "Setting too high (>0.3)", "Frequent adjustments", "Using without understanding learning rates" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 3,
                InteractsWith = { "MirostatMode", "MirostatTau" },
                RecommendedFor = "Experts fine-tuning Mirostat convergence",
                AvoidWhen = "Mirostat mode disabled or unstable"
            }
        },
        {
            nameof(SamplingParameters.TfsZ),
            new ParameterRiskClassification
            {
                ParameterName = "TfsZ",
                RiskLevel = ParameterRiskLevel.Experimental,
                Category = ParameterCategory.AdvancedSampling,
                Description = "Tail Free Sampling - removes low probability derivative tokens",
                SafeUsageGuideline = "Use 0.95-0.99 for subtle tail removal",
                RiskExplanation = "Poorly understood parameter, may interact unpredictably with others",
                CommonMistakes = { "Setting too low (<0.9)", "Using without understanding derivatives", "Combining with multiple other filters" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 3,
                InteractsWith = { "TopP", "TopK", "MinP" },
                RecommendedFor = "Researchers experimenting with sampling methods",
                AvoidWhen = "Other sampling parameters working satisfactorily"
            }
        },
        {
            nameof(SamplingParameters.EtaCutoff),
            new ParameterRiskClassification
            {
                ParameterName = "EtaCutoff",
                RiskLevel = ParameterRiskLevel.Experimental,
                Category = ParameterCategory.AdvancedSampling,
                Description = "Eta sampling - dynamic probability threshold",
                SafeUsageGuideline = "Start with 0.1-1.0 if experimenting",
                RiskExplanation = "Experimental parameter with limited documentation and testing",
                CommonMistakes = { "Setting too high", "Not understanding the algorithm", "Using with other cutoff parameters" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 3,
                InteractsWith = { "EpsilonCutoff", "Temperature" },
                RecommendedFor = "Advanced researchers only",
                AvoidWhen = "Seeking stable, predictable output"
            }
        },
        {
            nameof(SamplingParameters.EpsilonCutoff),
            new ParameterRiskClassification
            {
                ParameterName = "EpsilonCutoff",
                RiskLevel = ParameterRiskLevel.Experimental,
                Category = ParameterCategory.AdvancedSampling,
                Description = "Epsilon sampling - removes tokens below threshold",
                SafeUsageGuideline = "Use very small values (0.0001-0.001) if needed",
                RiskExplanation = "Can severely restrict vocabulary, experimental implementation",
                CommonMistakes = { "Setting too high (>0.01)", "Not understanding probability thresholds" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 3,
                InteractsWith = { "EtaCutoff", "TopP" },
                RecommendedFor = "Advanced researchers only",
                AvoidWhen = "Seeking reliable output quality"
            }
        },
        {
            nameof(SamplingParameters.DryMultiplier),
            new ParameterRiskClassification
            {
                ParameterName = "DryMultiplier",
                RiskLevel = ParameterRiskLevel.Experimental,
                Category = ParameterCategory.Experimental,
                Description = "Don't Repeat Yourself sampling - penalizes sequence repetition",
                SafeUsageGuideline = "Use 0.1-0.5 for subtle sequence anti-repetition",
                RiskExplanation = "Newest experimental parameter, behavior not fully understood",
                CommonMistakes = { "Setting too high (>1.0)", "Not understanding sequence vs token repetition", "Combining with other repetition penalties" },
                RequiresExpertise = true,
                CanBreakModel = false,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 3,
                InteractsWith = { "RepetitionPenalty", "DryBase", "DryAllowedLength" },
                RecommendedFor = "Cutting-edge researchers and experimenters",
                AvoidWhen = "Need reliable, tested behavior"
            }
        },
        {
            nameof(SamplingParameters.MaxTokens),
            new ParameterRiskClassification
            {
                ParameterName = "MaxTokens",
                RiskLevel = ParameterRiskLevel.Safe,
                Category = ParameterCategory.OutputControl,
                Description = "Limits maximum response length",
                SafeUsageGuideline = "Set based on desired response length and context limits",
                RiskExplanation = "Very high values may exceed context window",
                CommonMistakes = { "Setting too low for task", "Exceeding model context limit", "Not accounting for prompt length" },
                RequiresExpertise = false,
                CanBreakModel = false,
                AffectsOutputQuality = false,
                MinimumExperienceLevel = 1,
                InteractsWith = { "ContextLength" },
                RecommendedFor = "All users - essential output control",
                AvoidWhen = "Never - always safe to adjust"
            }
        },
        {
            nameof(SamplingParameters.Seed),
            new ParameterRiskClassification
            {
                ParameterName = "Seed",
                RiskLevel = ParameterRiskLevel.Safe,
                Category = ParameterCategory.ModelBehavior,
                Description = "Controls randomization for reproducible output",
                SafeUsageGuideline = "Use -1 for random, fixed values for reproducible testing",
                RiskExplanation = "Fixed seeds make output predictable and less varied",
                CommonMistakes = { "Using same seed for production", "Not understanding reproducibility implications" },
                RequiresExpertise = false,
                CanBreakModel = false,
                AffectsOutputQuality = false,
                MinimumExperienceLevel = 1,
                InteractsWith = { "Temperature", "TopP" },
                RecommendedFor = "Testing and debugging scenarios",
                AvoidWhen = "Want varied, unpredictable responses"
            }
        },
        {
            nameof(SamplingParameters.ContextLength),
            new ParameterRiskClassification
            {
                ParameterName = "ContextLength",
                RiskLevel = ParameterRiskLevel.Caution,
                Category = ParameterCategory.ContextManagement,
                Description = "Maximum context window size for the model",
                SafeUsageGuideline = "Use model's maximum supported context or lower for performance",
                RiskExplanation = "Exceeding model limits causes errors, high values increase memory usage",
                CommonMistakes = { "Exceeding model capability", "Not accounting for memory limitations", "Setting too low for task" },
                RequiresExpertise = true,
                CanBreakModel = true,
                AffectsOutputQuality = true,
                MinimumExperienceLevel = 2,
                InteractsWith = { "MaxTokens", "BatchSize" },
                RecommendedFor = "Users needing long context for complex tasks",
                AvoidWhen = "Memory or performance constrained"
            }
        },
        {
            nameof(SamplingParameters.BatchSize),
            new ParameterRiskClassification
            {
                ParameterName = "BatchSize",
                RiskLevel = ParameterRiskLevel.Caution,
                Category = ParameterCategory.Performance,
                Description = "Number of tokens processed in parallel",
                SafeUsageGuideline = "Adjust based on available VRAM and model size",
                RiskExplanation = "Too high values can cause out-of-memory errors",
                CommonMistakes = { "Setting too high for available memory", "Not considering model size", "Using with CPU-only setups" },
                RequiresExpertise = true,
                CanBreakModel = true,
                AffectsOutputQuality = false,
                MinimumExperienceLevel = 2,
                InteractsWith = { "ContextLength", "ThreadCount" },
                RecommendedFor = "Users optimizing performance with adequate hardware",
                AvoidWhen = "Limited VRAM or unstable performance"
            }
        },
        {
            nameof(SamplingParameters.ThreadCount),
            new ParameterRiskClassification
            {
                ParameterName = "ThreadCount",
                RiskLevel = ParameterRiskLevel.Safe,
                Category = ParameterCategory.Performance,
                Description = "Number of CPU threads for processing",
                SafeUsageGuideline = "Use number of CPU cores or slightly less",
                RiskExplanation = "Too many threads can reduce performance due to context switching",
                CommonMistakes = { "Setting higher than CPU cores", "Using maximum on shared systems" },
                RequiresExpertise = false,
                CanBreakModel = false,
                AffectsOutputQuality = false,
                MinimumExperienceLevel = 1,
                InteractsWith = { "BatchSize" },
                RecommendedFor = "Users wanting to optimize CPU usage",
                AvoidWhen = "System resources are constrained"
            }
        }
    };

    /// <summary>
    /// Get risk classification for a parameter
    /// </summary>
    public static ParameterRiskClassification? GetClassification(string parameterName)
    {
        Classifications.TryGetValue(parameterName, out var classification);
        return classification;
    }

    /// <summary>
    /// Get all parameters for a specific risk level
    /// </summary>
    public static List<ParameterRiskClassification> GetByRiskLevel(ParameterRiskLevel riskLevel)
    {
        return Classifications.Values.Where(c => c.RiskLevel == riskLevel).ToList();
    }

    /// <summary>
    /// Get all parameters for a specific category
    /// </summary>
    public static List<ParameterRiskClassification> GetByCategory(ParameterCategory category)
    {
        return Classifications.Values.Where(c => c.Category == category).ToList();
    }

    /// <summary>
    /// Get parameters appropriate for a specific experience level
    /// </summary>
    public static List<ParameterRiskClassification> GetByExperienceLevel(int experienceLevel)
    {
        return Classifications.Values.Where(c => c.MinimumExperienceLevel <= experienceLevel).ToList();
    }

    /// <summary>
    /// Check if a parameter should be shown for a given ViewMode
    /// </summary>
    public static bool ShouldShowInViewMode(string parameterName, ViewMode viewMode)
    {
        var classification = GetClassification(parameterName);
        if (classification == null) return true; // Show unknown parameters by default

        return viewMode switch
        {
            ViewMode.Novice => classification.MinimumExperienceLevel <= 1 && classification.RiskLevel == ParameterRiskLevel.Safe,
            ViewMode.Enthusiast => classification.MinimumExperienceLevel <= 2,
            ViewMode.Developer => true, // Developers see everything
            _ => true
        };
    }

    /// <summary>
    /// Get warning message for parameter at current experience level
    /// </summary>
    public static string? GetWarningForLevel(string parameterName, ViewMode viewMode)
    {
        var classification = GetClassification(parameterName);
        if (classification == null) return null;

        var currentLevel = viewMode switch
        {
            ViewMode.Novice => 1,
            ViewMode.Enthusiast => 2,
            ViewMode.Developer => 3,
            _ => 1
        };

        if (classification.MinimumExperienceLevel > currentLevel)
        {
            return $"⚠️ This {classification.RiskLevel} parameter is recommended for {(ViewMode)classification.MinimumExperienceLevel} level and above.";
        }

        if (classification.RiskLevel == ParameterRiskLevel.Experimental && currentLevel < 3)
        {
            return "🧪 Experimental parameter - may behave unpredictably. Consider switching to Developer mode.";
        }

        return null;
    }
}