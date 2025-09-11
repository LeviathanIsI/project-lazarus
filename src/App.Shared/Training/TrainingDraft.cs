using System.Collections.Generic;
using System.Collections.ObjectModel;
using Lazarus.Shared.Contracts;

namespace Lazarus.Shared.Training
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
        
        // Modality-specific asset collections
        
        // Voice assets
        public ObservableCollection<string> AudioFiles { get; } = new();
        
        // Images assets
        public ObservableCollection<string> ImageFiles { get; } = new();
        
        // 3D Models assets
        public ObservableCollection<string> ModelFiles { get; } = new();
        
        // Entities assets
        public ObservableCollection<string> AvatarModels { get; } = new();
        public ObservableCollection<string> Voices { get; } = new();
        
        // Videos assets
        public ObservableCollection<string> VideoFiles { get; } = new();
    }
}
