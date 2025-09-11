using System;
using System.Collections.Generic;

namespace Lazarus.Shared.Contracts
{
    public sealed class TrainingConfig
    {
        public required string Id { get; init; }
        public required string ModelId { get; set; }
        public required string Recipe { get; set; } // LoRA, QLoRA, Full Fine-tune, etc.
        public required TrainingModality Modality { get; init; }
        public required string OutputPath { get; set; }
        
        // Common hyperparameters
        public double LearningRate { get; set; } = 2e-4;
        public int NumEpochs { get; set; } = 3;
        public int WarmupSteps { get; set; } = 100;
        public string Scheduler { get; set; } = "cosine";
        public double WeightDecay { get; set; } = 0.01;
        
        // Modality-specific parameters
        public Dictionary<string, object> ModalityParams { get; set; } = new();
        
        // Validation
        public double ValidationSplit { get; set; } = 0.1;
        public int EvalSteps { get; set; } = 500;
        public int SaveSteps { get; set; } = 500;
        public int LogSteps { get; set; } = 10;
    }
}
