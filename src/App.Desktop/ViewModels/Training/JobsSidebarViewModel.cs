using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class JobsSidebarViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ITrainingService _trainingService;
        private readonly List<IDisposable> _disposables = new();
        
        public event EventHandler<TrainingJob?>? SelectedJobChanged;
        
        public ObservableCollection<TrainingJob> Jobs { get; } = new();
        public ObservableCollection<TrainingJob> SelectedJobs { get; } = new();
        
        private TrainingJob? _selectedJob;
        public TrainingJob? SelectedJob
        {
            get => _selectedJob;
            set
            {
                if (SetProperty(ref _selectedJob, value))
                {
                    SelectedJobChanged?.Invoke(this, value);
                }
            }
        }
        
        // Filters
        private bool _filterAll = true;
        public bool FilterAll { get => _filterAll; set => SetProperty(ref _filterAll, value); }
        
        private bool _filterActive;
        public bool FilterActive { get => _filterActive; set => SetProperty(ref _filterActive, value); }
        
        private bool _filterQueued;
        public bool FilterQueued { get => _filterQueued; set => SetProperty(ref _filterQueued, value); }
        
        private bool _filterCompleted;
        public bool FilterCompleted { get => _filterCompleted; set => SetProperty(ref _filterCompleted, value); }
        
        private bool _filterFailed;
        public bool FilterFailed { get => _filterFailed; set => SetProperty(ref _filterFailed, value); }
        
        // Commands
        public ICommand NewJobCommand { get; }
        public ICommand ImportDatasetCommand { get; }
        public ICommand DuplicateCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand RefreshCommand { get; }
        
        public JobsSidebarViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            
            NewJobCommand = new RelayCommand(async _ => await NewJobAsync());
            ImportDatasetCommand = new RelayCommand(async _ => await ImportDatasetAsync());
            DuplicateCommand = new RelayCommand(async _ => await DuplicateSelectedJobAsync(), _ => SelectedJob != null);
            DeleteCommand = new RelayCommand(async _ => await DeleteSelectedJobsAsync(), _ => SelectedJobs.Any());
            ResumeCommand = new RelayCommand(async _ => await ResumeSelectedJobsAsync(), _ => SelectedJobs.Any(j => j.Status == TrainingStatus.Paused));
            RefreshCommand = new RelayCommand(async _ => await LoadJobsAsync());
            
            _ = LoadJobsAsync();
        }
        
        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
        
        private async Task LoadJobsAsync()
        {
            try
            {
                var jobs = await _trainingService.GetJobsAsync();
                Jobs.Clear();
                foreach (var job in jobs.OrderByDescending(j => j.Modified))
                {
                    Jobs.Add(job);
                }
            }
            catch (Exception ex)
            {
                // TODO(training): Show error notification
                System.Diagnostics.Debug.WriteLine($"Failed to load jobs: {ex.Message}");
            }
        }
        
        // TODO(training): Implement job management methods
        private async Task NewJobAsync()
        {
            try
            {
                var job = await _trainingService.CreateJobAsync("New Training Job", TrainingModality.Conversations);
                Jobs.Insert(0, job);
                SelectedJob = job;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }
        
        private async Task ImportDatasetAsync()
        {
            // TODO(training): Show file dialog and import dataset
            await Task.CompletedTask;
        }
        
        private async Task DuplicateSelectedJobAsync()
        {
            if (SelectedJob == null) return;
            
            try
            {
                var duplicatedJob = await _trainingService.DuplicateJobAsync(SelectedJob.Id);
                Jobs.Insert(0, duplicatedJob);
                SelectedJob = duplicatedJob;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to duplicate job: {ex.Message}");
            }
        }
        
        private async Task DeleteSelectedJobsAsync()
        {
            if (!SelectedJobs.Any()) return;
            
            try
            {
                var jobIds = SelectedJobs.Select(j => j.Id).ToList();
                await _trainingService.DeleteMultipleAsync(jobIds);
                
                foreach (var job in SelectedJobs.ToList())
                {
                    Jobs.Remove(job);
                }
                SelectedJobs.Clear();
                SelectedJob = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete jobs: {ex.Message}");
            }
        }
        
        private async Task ResumeSelectedJobsAsync()
        {
            var pausedJobs = SelectedJobs.Where(j => j.Status == TrainingStatus.Paused).ToList();
            if (!pausedJobs.Any()) return;
            
            try
            {
                foreach (var job in pausedJobs)
                {
                    await _trainingService.ResumeJobAsync(job.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to resume jobs: {ex.Message}");
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        private void OnPropertyChanged([CallerMemberName] string? name = null) => 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}