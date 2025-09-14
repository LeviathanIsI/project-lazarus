using System;

namespace Lazarus.Shared.Contracts
{
    public enum DatasetType
    {
        ConversationJsonl,
        VoiceAudioTranscript,
        ImageDirectory,
        ThreeDMeshes,
        EntityVoiceAvatar,
        VideoClips
    }

    public sealed class TrainingDatasetRef
    {
        public required string Id { get; init; }
        public required string Name { get; set; }
        public required DatasetType Type { get; init; }
        public required string Path { get; init; }
        public required TrainingModality Modality { get; init; }
        public DateTime Created { get; init; } = DateTime.UtcNow;
        public long SizeBytes { get; set; }

        // Type-specific stats
        public DatasetStats Stats { get; set; } = new();
    }

    public sealed class DatasetStats
    {
        // Common
        public int TotalItems { get; set; }

        // Text/Conversations
        public long TotalTokens { get; set; }
        public int AverageTokensPerItem { get; set; }

        // Audio/Voice
        public TimeSpan TotalDuration { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }

        // Images
        public string? CommonResolution { get; set; }
        public string? Format { get; set; }

        // 3D
        public int TotalVertices { get; set; }
        public int TotalFaces { get; set; }

        // Video
        public double AverageFrameRate { get; set; }
        public string? Resolution { get; set; }
        public TimeSpan AverageDuration { get; set; }
    }
}
