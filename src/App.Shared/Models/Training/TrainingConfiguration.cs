using System;

namespace Lazarus.Shared.Models.Training
{
    /// <summary>
    /// Conversation training configuration captured from the UI.
    /// Values are persisted under LazarusPaths.SystemData.Config.
    /// </summary>
    public sealed class TrainingConfiguration
    {
        public string BaseModel { get; set; } = string.Empty;
        public TrainingType Type { get; set; } = TrainingType.LoRA;
        public double LearningRate { get; set; } = 2e-4;
        public int BatchSize { get; set; } = 4;
        public int GradientAccumulation { get; set; } = 4;
        public int MaxSequenceLength { get; set; } = 2048;
        public string ChatTemplate { get; set; } = "ChatML";
        public TrainingDuration Duration { get; set; } = TrainingDuration.Epochs;
        public int Steps { get; set; } = 0;
        public int Epochs { get; set; } = 3;
    }

    public enum TrainingType { FineTuning, LoRA, QLoRA }
    public enum TrainingDuration { Steps, Epochs }
    public enum TrainingStatus { Created, Running, Paused, Stopped, Completed, Failed }
}


