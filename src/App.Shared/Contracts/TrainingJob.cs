using System;

namespace Lazarus.Shared.Contracts
{
    public enum TrainingModality
    {
        Conversations,
        Voice,
        Images,
        ThreeD,
        Entities,
        Videos
    }

    public enum TrainingStatus
    {
        Draft,
        Queued,
        Running,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public sealed class TrainingJob
    {
        public required string Id { get; init; }
        public required string Name { get; set; }
        public required TrainingModality Modality { get; init; }
        public TrainingStatus Status { get; set; } = TrainingStatus.Draft;
        public DateTime Created { get; init; } = DateTime.UtcNow;
        public DateTime Modified { get; set; } = DateTime.UtcNow;
        public int CurrentEpoch { get; set; }
        public long CurrentStep { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public required string OutputPath { get; set; }
        public double Progress { get; set; } // 0.0 to 1.0
        public string? LastError { get; set; }

        // Configuration references
        public required string ConfigId { get; set; }
        public required string ResourcesId { get; set; }
        public List<string> DatasetIds { get; set; } = new();

        // Runtime metadata
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan? Duration => CompletedAt?.Subtract(StartedAt ?? DateTime.UtcNow);

        // UI persistence
        public string? LastOpenTab { get; set; }
        public bool MonitorDockOpen { get; set; }
    }
}
