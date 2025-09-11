using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    /// <summary>
    /// Proxy for unified parameter editing that works with either CurrentJob or Draft.
    /// </summary>
    public sealed class ParameterBagProxy : INotifyPropertyChanged
    {
        private TrainingJob? _currentJob;
        private readonly TrainingDraft _draft;
        
        public ParameterBagProxy(TrainingDraft draft)
        {
            _draft = draft ?? throw new ArgumentNullException(nameof(draft));
        }
        
        public void SetCurrentJob(TrainingJob? job)
        {
            _currentJob = job;
            OnPropertyChanged(); // Notify all indexed properties may have changed
        }
        
        public string this[string key]
        {
            get
            {
                if (_currentJob != null && _currentJob.ConfigId != null)
                {
                    // TODO: Get from job config when implemented
                    return "";
                }
                return _draft.Params.TryGetValue(key, out var value) ? value : "";
            }
            set
            {
                if (_currentJob != null && _currentJob.ConfigId != null)
                {
                    // TODO: Update job config when implemented
                }
                else
                {
                    _draft.Params[key] = value;
                }
                OnPropertyChanged($"Item[{key}]");
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
