using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lazarus.Shared.Models.Training;

/// <summary>
/// Unified training profile for SFT / DPO / ORPO across trainers.
/// Serialized to JSON for manifests and API payloads.
/// </summary>
public sealed class TrainingProfile
{
    [JsonPropertyName("task")] public string Task { get; set; } = "sft"; // sft|dpo|orpo
    [JsonPropertyName("baseModel")] public string BaseModel { get; set; } = string.Empty;
    [JsonPropertyName("runnerFormat")] public string RunnerFormat { get; set; } = "hf"; // hf|gguf
    [JsonPropertyName("chatTemplate")] public string ChatTemplate { get; set; } = "ChatML"; // ChatML|Llama3|Qwen2

    [JsonPropertyName("dataset")] public DatasetSpec Dataset { get; set; } = new();
    [JsonPropertyName("optimization")] public OptimizationSpec Optimization { get; set; } = new();
    [JsonPropertyName("schedule")] public ScheduleSpec Schedule { get; set; } = new();
    [JsonPropertyName("batching")] public BatchingSpec Batching { get; set; } = new();
    [JsonPropertyName("eval")] public EvalSpec Eval { get; set; } = new();
    [JsonPropertyName("hardware")] public HardwareSpec Hardware { get; set; } = new();

    [JsonPropertyName("outputName")] public string OutputName { get; set; } = string.Empty;
    [JsonPropertyName("notes")] public string? Notes { get; set; }
}

public sealed class DatasetSpec
{
    [JsonPropertyName("train")] public List<string> Train { get; set; } = new();
    [JsonPropertyName("eval")] public List<string> Eval { get; set; } = new();
}

public sealed class OptimizationSpec
{
    [JsonPropertyName("lora")] public LoRASpec LoRA { get; set; } = new();
    [JsonPropertyName("qlora")] public QLoRASpec QLoRA { get; set; } = new();
}

public sealed class LoRASpec
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("r")] public int R { get; set; } = 16;
    [JsonPropertyName("alpha")] public int Alpha { get; set; } = 32;
    [JsonPropertyName("dropout")] public double Dropout { get; set; } = 0.05;
    [JsonPropertyName("target")] public string Target { get; set; } = "q_proj,v_proj,o_proj,k_proj";
}

public sealed class QLoRASpec
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("bits")] public int Bits { get; set; } = 4;
    [JsonPropertyName("bnbDtype")] public string BnbDtype { get; set; } = "nf4";
}

public sealed class ScheduleSpec
{
    [JsonPropertyName("learningRate")] public double LearningRate { get; set; } = 2e-4;
    [JsonPropertyName("epochs")] public int? Epochs { get; set; } = 3;
    [JsonPropertyName("maxSteps")] public int? MaxSteps { get; set; }
    [JsonPropertyName("warmupRatio")] public double WarmupRatio { get; set; } = 0.03;
    [JsonPropertyName("lrScheduler")] public string LrScheduler { get; set; } = "cosine"; // cosine|linear
    [JsonPropertyName("weightDecay")] public double WeightDecay { get; set; } = 0.01;
}

public sealed class BatchingSpec
{
    [JsonPropertyName("perDeviceBatch")] public int PerDeviceBatch { get; set; } = 4;
    [JsonPropertyName("gradAccum")] public int GradAccum { get; set; } = 4;
    [JsonPropertyName("maxSeqLen")] public int MaxSeqLen { get; set; } = 4096;
}

public sealed class EvalSpec
{
    [JsonPropertyName("perSteps")] public int PerSteps { get; set; } = 200;
    [JsonPropertyName("savePerSteps")] public int SavePerSteps { get; set; } = 500;
    [JsonPropertyName("loggingPerSteps")] public int LoggingPerSteps { get; set; } = 10;
}

public sealed class HardwareSpec
{
    [JsonPropertyName("bf16")] public bool Bf16 { get; set; } = true;
    [JsonPropertyName("fp16")] public bool Fp16 { get; set; } = false;
    [JsonPropertyName("devices")] public string Devices { get; set; } = "auto";
}

