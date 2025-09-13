using System;
using Lazarus.Shared.Contracts;

namespace Lazarus.Shared.Training
{
    public enum TrainingTask { SFT, DPO, ORPO }
    public enum TrainerBackend { LLaMAFactory, Axolotl, Unsloth }
    public enum DatasetKind { Conversations, Preferences, Eval }

    public sealed class TrainingProfile
    {
        public TrainingModality Modality { get; init; } = TrainingModality.Conversations;
        public TrainerBackend Trainer { get; init; } = TrainerBackend.LLaMAFactory;
        public TrainingTask Task { get; init; } = TrainingTask.SFT;

        public string BaseModel { get; init; } = string.Empty;
        public string ChatTemplate { get; init; } = "ChatML";

        // Data
        public string[] TrainFiles { get; init; } = Array.Empty<string>();
        public string[] EvalFiles { get; init; } = Array.Empty<string>();
        public string[]? PreferenceFiles { get; init; } // DPO/ORPO

        // Core schedule
        public double LearningRate { get; init; } = 2e-4;
        public int? Epochs { get; init; } = 3;
        public int? MaxSteps { get; init; }

        // Batching
        public int PerDeviceBatch { get; init; } = 4;
        public int GradAccum { get; init; } = 4;
        public int MaxSeqLen { get; init; } = 2048;

        // Adapters
        public bool UseLoRA { get; init; } = true;
        public bool UseQLoRA { get; init; } = false;
        public int LoRARank { get; init; } = 16;
        public int LoRAAlpha { get; init; } = 32;
        public double LoRADropout { get; init; } = 0.1;

        // Extras
        public int WarmupSteps { get; init; } = 100;
        public string LrScheduler { get; init; } = "cosine";
        public string Optimizer { get; init; } = "adamw_torch";
        public double? ValidationSplit { get; init; } = 0.1;
        public int EvalEverySteps { get; init; } = 500;
        public int SaveEverySteps { get; init; } = 500;
        public int KeepCheckpoints { get; init; } = 3;

        public bool GradientCheckpointing { get; init; } = true;
        public bool FlashAttention { get; init; } = true;
        public bool PackSequences { get; init; } = false;

        public string OutputName { get; init; } = string.Empty;
        public string Notes { get; init; } = string.Empty;
    }
}

