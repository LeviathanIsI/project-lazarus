namespace Lazarus.Shared.Contracts.Training
{
    // TODO(training): extend as needed per modality; keep DTO-only (no logic)
    public enum TrainingModality { Conversations, Voice, Images, ThreeD, Entities, Videos }
    public enum TrainingStatus { New, Queued, Running, Paused, Stopped, Completed, Failed }

    public sealed class TrainingJob
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TrainingModality Modality { get; set; }
        public TrainingStatus Status { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public int Epoch { get; set; }
        public long Step { get; set; }
        public TimeSpan? ETA { get; set; }
        public string? OutputPath { get; set; }
    }

    public sealed class TrainingDatasetRef
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // jsonl, wav, png, obj, mp4, etc
        public string Path { get; set; } = string.Empty;
        public string? Stats { get; set; }
    }

    public sealed class TrainingConfig
    {
        public string? ModelId { get; set; }
        public string? Recipe { get; set; }
        public Dictionary<string, string> Hyperparams { get; set; } = new();
        public string? OutputPath { get; set; }
    }

    public sealed class TrainingResources
    {
        public List<int> GpuIds { get; set; } = new();
        public int BatchSize { get; set; }
        public int GradientAccum { get; set; }
        public string Precision { get; set; } = "fp16";
    }

    public sealed class TrainingMetricsSnapshot
    {
        public double? Loss { get; set; }
        public double? Accuracy { get; set; }
        public double? LearningRate { get; set; }
        public double? PerStepTimeMs { get; set; }
        public double? VRAM_GB { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public sealed class TrainingLogEvent
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "INFO";
        public string Message { get; set; } = string.Empty;
        public long? Step { get; set; }
        public int? Epoch { get; set; }
    }
}

