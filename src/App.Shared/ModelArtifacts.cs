using System;
using System.Collections.Generic;

namespace Lazarus.Shared;

public enum ModelFormat { Unknown = 0, GGUF, HF }
public enum RunnerKind  { Unknown = 0, LlamaCpp, Vllm, ExLlamaV2 }

public sealed record BaseModelInfo(
    string ModelKey,           // derived from filename or folder
    string DisplayName,        // user-friendly
    ModelFormat Format,
    string FilePath,           // absolute
    RunnerKind PreferredRunner // heuristic (GGUF->LlamaCpp; HF->Vllm)
);

public sealed record AdapterInfo(string Name, string FilePath);
public sealed record TokenizerInfo(string Name, string FilePath);
public sealed record EmbeddingInfo(string Name, string FilePath);

public enum ModelArtifactKind
{
    BaseModel,
    LoRA,
    Tokenizer,
    Embedding
}

public sealed record ModelArtifact(string Name, string FullPath, ModelArtifactKind Kind);

public sealed record ModelParams(
    double Temperature,
    double TopP,
    int    MaxTokens,
    double RepeatPenalty,
    int    Mirostat
)
{
    public static ModelParams Default => new(0.7, 0.9, 4096, 1.1, 0);
}

public sealed record ModelPreset(
    string Name,
    string BaseModelKey,
    List<string> Loras,
    string? Tokenizer,
    string? Embedding,
    ModelParams Params
);

public sealed record DoubleParam(double Min, double Max, double Default, double Step = 0.01);
public sealed record IntParam(int Min, int Max, int Default, int Step = 1);
public sealed record OptionalIntParam(int Min, int Max, int? Default);

public sealed class ParameterSchema
{
    public required DoubleParam Temperature { get; init; }
    public required DoubleParam TopP { get; init; }
    public required IntParam TopK { get; init; }
    public required IntParam MaxTokens { get; init; }
    public required DoubleParam RepetitionPenalty { get; init; }
    public required DoubleParam PresencePenalty { get; init; }
    public required DoubleParam FrequencyPenalty { get; init; }
    public OptionalIntParam? Seed { get; init; }
    public OptionalIntParam? ContextWindow { get; init; }

    public static ParameterSchema Default => new()
    {
        Temperature = new DoubleParam(0.0, 2.0, 0.7, 0.01),
        TopP = new DoubleParam(0.0, 1.0, 0.9, 0.01),
        TopK = new IntParam(0, 200, 40, 1),
        MaxTokens = new IntParam(1, 8192, 1024, 1),
        RepetitionPenalty = new DoubleParam(0.0, 2.0, 1.1, 0.01),
        PresencePenalty = new DoubleParam(0.0, 2.0, 0.0, 0.01),
        FrequencyPenalty = new DoubleParam(0.0, 2.0, 0.0, 0.01),
        Seed = new OptionalIntParam(int.MinValue, int.MaxValue, null),
        ContextWindow = new OptionalIntParam(256, 32768, null)
    };
}
