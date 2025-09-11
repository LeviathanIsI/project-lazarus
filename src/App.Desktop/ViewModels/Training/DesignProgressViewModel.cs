using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class DesignProgressViewModel : ViewModelBase
    {
        public string Title => "Design Progress";
        
        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }
        
        public bool HasJob => CurrentJob != null;
        
        public void SetCurrentJob(TrainingJob? job)
        {
            CurrentJob = job;
            OnPropertyChanged(nameof(HasJob));
        }
    }
}
