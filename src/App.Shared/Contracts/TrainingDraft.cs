using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lazarus.Shared.Contracts
{
    /// <summary>
    /// Draft configuration for training jobs that exists before job creation.
    /// Allows UI interaction and asset import without requiring a persisted job.
    /// </summary>
    public sealed class TrainingDraft
    {
        public string Name { get; set; } = "New Training";
        public TrainingModality Modality { get; set; } = TrainingModality.Conversations;
        public ObservableCollection<TrainingDatasetRef> Datasets { get; } = new();
        public Dictionary<string, string> Params { get; } = new();
        
        // Quick access to common parameters
        public string BaseModel
        {
            get => Params.TryGetValue("BaseModel", out var value) ? value : "llama-2-7b-chat";
            set => Params["BaseModel"] = value;
        }
        
        public string TrainingType
        {
            get => Params.TryGetValue("TrainingType", out var value) ? value : "LoRA";
            set => Params["TrainingType"] = value;
        }
        
        public string LearningRate
        {
            get => Params.TryGetValue("LearningRate", out var value) ? value : "2e-4";
            set => Params["LearningRate"] = value;
        }
        
        public string OutputPath
        {
            get => Params.TryGetValue("OutputPath", out var value) ? value : $"./models/{Name.Replace(" ", "-").ToLower()}";
            set => Params["OutputPath"] = value;
        }
    }
}
