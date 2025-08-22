using System.Collections.Generic;

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
}

/// <summary>
/// Metadata for dynamic parameter UI generation
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
}