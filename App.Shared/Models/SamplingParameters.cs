using System.Collections.Generic;
using App.Shared.Enums;

namespace Lazarus.Shared.Models;

/// <summary>
/// The complete grimoire of LLM sampling parameters - every knob, lever, and blood ritual
/// </summary>
public class SamplingParameters
{
    // Core Chaos Controllers
    public float Temperature { get; set; } = 0.7f;        // 0.0-2.0+ (creativity vs coherence)
    public float TopP { get; set; } = 0.9f;               // 0.0-1.0 (nucleus sampling)
    public int TopK { get; set; } = 40;                   // 1-∞ (vocabulary restriction)
    public float MinP { get; set; } = 0.05f;              // 0.0-1.0 (alternative to top-p)
    public float TypicalP { get; set; } = 1.0f;           // 0.0-1.0 (targets typical probability mass)

    // Repetition & Pattern Control
    public float RepetitionPenalty { get; set; } = 1.1f;  // 0.8-1.3 (anti-repetition)
    public float FrequencyPenalty { get; set; } = 0.0f;   // -2.0-2.0 (penalize frequent tokens)
    public float PresencePenalty { get; set; } = 0.0f;    // -2.0-2.0 (penalize any repeated token)
    public int RepetitionPenaltyRange { get; set; } = 1024; // tokens to consider for repetition

    // Advanced Sampling Sorcery
    public float TfsZ { get; set; } = 1.0f;               // 0.0-1.0 (tail free sampling)
    public float EtaCutoff { get; set; } = 0.0f;          // 0.0-∞ (eta sampling)
    public float EpsilonCutoff { get; set; } = 0.0f;      // 0.0-∞ (epsilon sampling)

    // Mirostat (Dynamic Sampling Madness)
    public int MirostatMode { get; set; } = 0;            // 0=off, 1=v1, 2=v2
    public float MirostatTau { get; set; } = 5.0f;        // target entropy
    public float MirostatEta { get; set; } = 0.1f;        // learning rate

    // Token Generation Control
    public int MaxTokens { get; set; } = 1024;
    public int MinTokens { get; set; } = 0;
    public List<string> StopSequences { get; set; } = new();
    public Dictionary<int, float> LogitBias { get; set; } = new(); // token_id -> bias

    // Context & Memory Management
    public int ContextLength { get; set; } = 4096;
    public float RopeFreqBase { get; set; } = 10000.0f;   // RoPE frequency base
    public float RopeFreqScale { get; set; } = 1.0f;      // RoPE frequency scaling
    public int RopeScalingType { get; set; } = 0;         // 0=none, 1=linear, 2=dynamic

    // Performance & Precision
    public bool UseFlashAttention { get; set; } = true;
    public int BatchSize { get; set; } = 512;
    public int ThreadCount { get; set; } = Environment.ProcessorCount;
    public bool UseMmap { get; set; } = true;             // memory mapping
    public bool UseMlock { get; set; } = false;           // lock in RAM

    // Guidance & Control
    public string Grammar { get; set; } = "";             // BNFF grammar for structured output
    public float GuidanceScale { get; set; } = 1.0f;     // classifier-free guidance
    public int Seed { get; set; } = -1;                   // randomization seed

    // Experimental/Bleeding Edge
    public float DryMultiplier { get; set; } = 0.0f;     // DRY (Don't Repeat Yourself) sampling
    public int DryBase { get; set; } = 1;
    public int DryAllowedLength { get; set; } = 2;
    public float DryPenaltyLastN { get; set; } = 0.0f;

    /// <summary>
    /// Static metadata for generating tooltips and UI controls
    /// </summary>
    public static Dictionary<string, ParameterTooltip> GetParameterTooltips()
    {
        return new Dictionary<string, ParameterTooltip>
        {
            {
                nameof(Temperature),
                new ParameterTooltip
                {
                    Title = "Temperature",
                    Description = "Controls randomness and creativity. Lower values = more focused and deterministic responses. Higher values = more creative and unpredictable output.",
                    SafeRange = "0.1 - 1.2",
                    TypicalValue = "0.7",
                    LowerWarning = "Below 0.3: Very repetitive, may get stuck in loops",
                    HigherWarning = "Above 1.5: Increasingly incoherent, may produce nonsense",
                    ExtremeWarning = "Above 2.0: Expect digital chaos and word salad"
                }
            },
            {
                nameof(TopP),
                new ParameterTooltip
                {
                    Title = "Top-P (Nucleus Sampling)",
                    Description = "Only considers tokens that make up the top P% of probability mass. Lower values = more focused vocabulary selection.",
                    SafeRange = "0.5 - 1.0",
                    TypicalValue = "0.9",
                    LowerWarning = "Below 0.5: Very limited vocabulary, may sound repetitive",
                    HigherWarning = "Above 0.95: Includes more low-probability tokens",
                    ExtremeWarning = "1.0: Disables filtering entirely"
                }
            },
            {
                nameof(TopK),
                new ParameterTooltip
                {
                    Title = "Top-K",
                    Description = "Limits vocabulary to the K most likely tokens. Lower values = more focused, higher = more diverse vocabulary.",
                    SafeRange = "10 - 100",
                    TypicalValue = "40",
                    LowerWarning = "Below 10: Very limited word choices",
                    HigherWarning = "Above 100: May include inappropriate low-probability words",
                    ExtremeWarning = "Above 500: Effectively unlimited vocabulary"
                }
            },
            {
                nameof(MinP),
                new ParameterTooltip
                {
                    Title = "Min-P",
                    Description = "Alternative to Top-P. Only considers tokens with probability >= MinP * highest_probability_token.",
                    SafeRange = "0.01 - 0.2",
                    TypicalValue = "0.05",
                    LowerWarning = "Below 0.01: May include very unlikely tokens",
                    HigherWarning = "Above 0.2: Very restrictive, may limit creativity",
                    ExtremeWarning = "Above 0.5: Extremely limited vocabulary"
                }
            },
            {
                nameof(TypicalP),
                new ParameterTooltip
                {
                    Title = "Typical-P",
                    Description = "Targets tokens with 'typical' probability - not too common, not too rare. 1.0 disables this sampling.",
                    SafeRange = "0.2 - 1.0",
                    TypicalValue = "1.0 (disabled)",
                    LowerWarning = "Below 0.2: Very restrictive, targets only 'typical' words",
                    HigherWarning = "Above 0.9: Minimal effect",
                    ExtremeWarning = "Experimental parameter - effects vary by model"
                }
            },
            {
                nameof(RepetitionPenalty),
                new ParameterTooltip
                {
                    Title = "Repetition Penalty",
                    Description = "Penalizes repeated tokens to reduce loops and repetitive text. 1.0 = no penalty, >1.0 = penalize repetition.",
                    SafeRange = "1.0 - 1.2",
                    TypicalValue = "1.1",
                    LowerWarning = "Below 1.0: Encourages repetition (usually undesired)",
                    HigherWarning = "Above 1.3: May avoid natural repetition (articles, pronouns)",
                    ExtremeWarning = "Above 1.5: Can break grammar and sentence structure"
                }
            },
            {
                nameof(FrequencyPenalty),
                new ParameterTooltip
                {
                    Title = "Frequency Penalty",
                    Description = "Penalizes tokens based on their frequency in the generated text. Positive values discourage repetition.",
                    SafeRange = "-0.5 - 2.0",
                    TypicalValue = "0.0",
                    LowerWarning = "Negative values: Encourages repetition",
                    HigherWarning = "Above 1.0: Strong anti-repetition, may affect natural language flow",
                    ExtremeWarning = "Above 2.0: May produce unnatural vocabulary choices"
                }
            },
            {
                nameof(PresencePenalty),
                new ParameterTooltip
                {
                    Title = "Presence Penalty",
                    Description = "Penalizes tokens that have already appeared, regardless of frequency. Encourages topic diversity.",
                    SafeRange = "-0.5 - 2.0",
                    TypicalValue = "0.0",
                    LowerWarning = "Negative values: Encourages reusing previous tokens",
                    HigherWarning = "Above 1.0: Strong pressure to avoid any repeated words",
                    ExtremeWarning = "Above 2.0: May avoid necessary words like 'the', 'and'"
                }
            },
            {
                nameof(RepetitionPenaltyRange),
                new ParameterTooltip
                {
                    Title = "Repetition Penalty Range",
                    Description = "Number of recent tokens to consider for repetition penalty. Larger values = longer memory of repetition.",
                    SafeRange = "64 - 2048",
                    TypicalValue = "1024",
                    LowerWarning = "Below 64: Very short repetition memory",
                    HigherWarning = "Above 2048: May penalize natural long-range patterns",
                    ExtremeWarning = "Very high values increase computation time"
                }
            },
            {
                nameof(TfsZ),
                new ParameterTooltip
                {
                    Title = "Tail Free Sampling (TFS-Z)",
                    Description = "Removes tokens with low probability derivatives. Lower values = more aggressive filtering.",
                    SafeRange = "0.9 - 1.0",
                    TypicalValue = "1.0 (disabled)",
                    LowerWarning = "Below 0.9: Very aggressive filtering, may limit vocabulary",
                    HigherWarning = "Values close to 1.0 have minimal effect",
                    ExtremeWarning = "Experimental parameter - behavior varies by model"
                }
            },
            {
                nameof(EtaCutoff),
                new ParameterTooltip
                {
                    Title = "Eta Cutoff",
                    Description = "Eta sampling - dynamic probability threshold. Higher values = more restrictive sampling.",
                    SafeRange = "0.0 - 10.0",
                    TypicalValue = "0.0 (disabled)",
                    LowerWarning = "0.0: Parameter disabled",
                    HigherWarning = "Above 5.0: Very restrictive sampling",
                    ExtremeWarning = "Experimental - may interact unpredictably with other parameters"
                }
            },
            {
                nameof(EpsilonCutoff),
                new ParameterTooltip
                {
                    Title = "Epsilon Cutoff",
                    Description = "Epsilon sampling - removes tokens below probability threshold. Higher = more restrictive.",
                    SafeRange = "0.0 - 0.01",
                    TypicalValue = "0.0 (disabled)",
                    LowerWarning = "0.0: Parameter disabled",
                    HigherWarning = "Above 0.001: May significantly limit vocabulary",
                    ExtremeWarning = "Experimental - use with caution"
                }
            },
            {
                nameof(MirostatMode),
                new ParameterTooltip
                {
                    Title = "Mirostat Mode",
                    Description = "Dynamic sampling that targets specific entropy levels. 0=disabled, 1=Mirostat v1, 2=Mirostat v2 (recommended).",
                    SafeRange = "0 - 2",
                    TypicalValue = "0 (disabled)",
                    LowerWarning = "Mode 1: Less stable than v2",
                    HigherWarning = "Mode 2: More consistent perplexity control",
                    ExtremeWarning = "Overrides Temperature when enabled"
                }
            },
            {
                nameof(MirostatTau),
                new ParameterTooltip
                {
                    Title = "Mirostat Tau (Target Entropy)",
                    Description = "Target entropy level for Mirostat. Controls the 'surprisingness' of output. Higher = more surprising/creative.",
                    SafeRange = "3.0 - 8.0",
                    TypicalValue = "5.0",
                    LowerWarning = "Below 3.0: Very predictable, may be repetitive",
                    HigherWarning = "Above 8.0: Highly unpredictable output",
                    ExtremeWarning = "Only active when Mirostat Mode > 0"
                }
            },
            {
                nameof(MirostatEta),
                new ParameterTooltip
                {
                    Title = "Mirostat Eta (Learning Rate)",
                    Description = "How quickly Mirostat adapts to maintain target entropy. Higher values = faster adaptation.",
                    SafeRange = "0.05 - 0.2",
                    TypicalValue = "0.1",
                    LowerWarning = "Below 0.05: Very slow adaptation to entropy changes",
                    HigherWarning = "Above 0.2: May cause instability in sampling",
                    ExtremeWarning = "Only active when Mirostat Mode > 0"
                }
            },
            {
                nameof(MaxTokens),
                new ParameterTooltip
                {
                    Title = "Max Tokens",
                    Description = "Maximum number of tokens to generate. Limits response length.",
                    SafeRange = "50 - 4096",
                    TypicalValue = "1024",
                    LowerWarning = "Below 50: Very short responses, may cut off mid-sentence",
                    HigherWarning = "Above 2048: Long responses, higher memory usage",
                    ExtremeWarning = "Very high values may exceed model context limits"
                }
            },
            {
                nameof(Seed),
                new ParameterTooltip
                {
                    Title = "Random Seed",
                    Description = "Controls randomization. Same seed + parameters = identical output. -1 for random seed each time.",
                    SafeRange = "-1 to 2147483647",
                    TypicalValue = "-1 (random)",
                    LowerWarning = "-1: New random seed each generation",
                    HigherWarning = "Fixed seeds: Reproducible but predictable output",
                    ExtremeWarning = "Use fixed seeds for testing, random for production"
                }
            },
            {
                nameof(DryMultiplier),
                new ParameterTooltip
                {
                    Title = "DRY Multiplier",
                    Description = "Don't Repeat Yourself sampling - penalizes sequences that have appeared before. 0.0 = disabled.",
                    SafeRange = "0.0 - 2.0",
                    TypicalValue = "0.0 (disabled)",
                    LowerWarning = "0.0: DRY sampling disabled",
                    HigherWarning = "Above 1.0: Strong anti-repetition, may affect natural patterns",
                    ExtremeWarning = "Experimental feature - may interact unpredictably"
                }
            }
        };
    }
}

/// <summary>
/// Metadata for dynamic parameter UI generation with risk classification
/// </summary>
public class ParameterMetadata
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = ""; // "float", "int", "bool", "string", "list"
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public object? DefaultValue { get; set; }
    public List<object>? AllowedValues { get; set; } // For enums/dropdowns
    public bool IsSupported { get; set; } = true;
    public bool IsExperimental { get; set; } = false;

    /// <summary>
    /// Gets the risk classification for this parameter
    /// </summary>
    public ParameterRiskClassification? RiskClassification => ParameterRiskRegistry.GetClassification(Name);

    /// <summary>
    /// Gets the risk level for this parameter
    /// </summary>
    public ParameterRiskLevel RiskLevel => RiskClassification?.RiskLevel ?? ParameterRiskLevel.Safe;

    /// <summary>
    /// Gets the parameter category
    /// </summary>
    public ParameterCategory Category => RiskClassification?.Category ?? ParameterCategory.CoreSampling;

    /// <summary>
    /// Determines if this parameter should be shown in the specified ViewMode
    /// </summary>
    public bool ShouldShowInViewMode(ViewMode viewMode)
    {
        return ParameterRiskRegistry.ShouldShowInViewMode(Name, viewMode);
    }

    /// <summary>
    /// Gets warning message for this parameter at the specified ViewMode level
    /// </summary>
    public string? GetWarningForViewMode(ViewMode viewMode)
    {
        return ParameterRiskRegistry.GetWarningForLevel(Name, viewMode);
    }

    /// <summary>
    /// Gets the display priority for this parameter (lower = higher priority)
    /// Safe parameters get higher priority than experimental ones
    /// </summary>
    public int GetDisplayPriority()
    {
        var riskClassification = RiskClassification;
        if (riskClassification == null) return 100;

        return riskClassification.RiskLevel switch
        {
            ParameterRiskLevel.Safe => 10,
            ParameterRiskLevel.Caution => 20,
            ParameterRiskLevel.Experimental => 30,
            _ => 100
        };
    }
}

/// <summary>
/// Rich tooltip information for parameter controls
/// </summary>
public class ParameterTooltip
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string SafeRange { get; set; } = "";
    public string TypicalValue { get; set; } = "";
    public string LowerWarning { get; set; } = "";
    public string HigherWarning { get; set; } = "";
    public string ExtremeWarning { get; set; } = "";
    public bool IsExperimental { get; set; } = false;

    /// <summary>
    /// Generates a formatted tooltip string for UI display
    /// </summary>
    public string GetFormattedTooltip()
    {
        var lines = new List<string>();

        if (!string.IsNullOrEmpty(Description))
            lines.Add(Description);

        if (!string.IsNullOrEmpty(SafeRange))
            lines.Add($"Safe Range: {SafeRange}");

        if (!string.IsNullOrEmpty(TypicalValue))
            lines.Add($"Typical: {TypicalValue}");

        if (!string.IsNullOrEmpty(LowerWarning))
            lines.Add($"⚠️ Low Values: {LowerWarning}");

        if (!string.IsNullOrEmpty(HigherWarning))
            lines.Add($"⚠️ High Values: {HigherWarning}");

        if (!string.IsNullOrEmpty(ExtremeWarning))
            lines.Add($"⛔ Warning: {ExtremeWarning}");

        if (IsExperimental)
            lines.Add("🧪 Experimental Feature");

        return string.Join("\n\n", lines);
    }
}