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
    public sealed class TrainingViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ITrainingService _trainingService;
        private readonly List<IDisposable> _disposables = new();
        
        public JobsSidebarViewModel JobsSidebar { get; }
        public JobDesignerViewModel JobDesigner { get; }
        public MonitorDockViewModel MonitorDock { get; }
        public InspectorViewModel Inspector { get; }

        private string _selectedModality = "Conversations";
        public string SelectedModality
        {
            get => _selectedModality;
            set { _selectedModality = value; OnPropertyChanged(); }
        }

        private bool _isProgressMode;
        public bool IsProgressMode { get => _isProgressMode; set { _isProgressMode = value; OnPropertyChanged(); } }

        private bool _isMonitorOpen;
        public bool IsMonitorOpen { get => _isMonitorOpen; set { _isMonitorOpen = value; OnPropertyChanged(); } }

        // Actions
        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ExportCommand { get; }

        public bool CanStart { get; private set; } = true;
        public bool CanPause { get; private set; } = false;
        public bool CanStop  { get; private set; } = false;
        public bool CanExport{ get; private set; } = false;

        public TrainingViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            
            JobsSidebar = new JobsSidebarViewModel(_trainingService);
            JobDesigner = new JobDesignerViewModel(_trainingService);
            MonitorDock = new MonitorDockViewModel(_trainingService);
            Inspector = new InspectorViewModel(_trainingService);
            
            StartCommand = new RelayCommand(async _ => await StartSelectedJobsAsync(), _ => CanStart);
            PauseCommand = new RelayCommand(async _ => await PauseSelectedJobsAsync(), _ => CanPause);
            StopCommand = new RelayCommand(async _ => await StopSelectedJobsAsync(), _ => CanStop);
            ExportCommand = new RelayCommand(async _ => await ExportSelectedJobAsync(), _ => CanExport);
            
            // Wire up cross-VM communication
            JobsSidebar.SelectedJobChanged += OnSelectedJobChanged;
            
            _disposables.Add(JobsSidebar);
            _disposables.Add(JobDesigner);
            _disposables.Add(MonitorDock);
            _disposables.Add(Inspector);
        }

        public void Dispose()
        {
            JobsSidebar.SelectedJobChanged -= OnSelectedJobChanged;
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
        
        private void OnSelectedJobChanged(object? sender, TrainingJob? selectedJob)
        {
            JobDesigner.SetSelectedJob(selectedJob);
            Inspector.SetSelectedJob(selectedJob);
            MonitorDock.SetSelectedJob(selectedJob);
            UpdateCanExecuteStates();
        }
        
        private void UpdateCanExecuteStates()
        {
            var selectedJobs = JobsSidebar.SelectedJobs;
            CanStart = selectedJobs.Any(j => j.Status == TrainingStatus.Draft || j.Status == TrainingStatus.Paused);
            CanPause = selectedJobs.Any(j => j.Status == TrainingStatus.Running);
            CanStop = selectedJobs.Any(j => j.Status == TrainingStatus.Running || j.Status == TrainingStatus.Paused);
            CanExport = selectedJobs.Count == 1 && selectedJobs.First().Status == TrainingStatus.Completed;
            
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanStop));
            OnPropertyChanged(nameof(CanExport));
        }
        
        // TODO(training): Implement job control methods
        private async Task StartSelectedJobsAsync()
        {
            var jobIds = JobsSidebar.SelectedJobs.Where(j => j.Status == TrainingStatus.Draft || j.Status == TrainingStatus.Paused).Select(j => j.Id);
            await _trainingService.StartMultipleAsync(jobIds);
        }
        
        private async Task PauseSelectedJobsAsync()
        {
            var jobIds = JobsSidebar.SelectedJobs.Where(j => j.Status == TrainingStatus.Running).Select(j => j.Id);
            await _trainingService.PauseMultipleAsync(jobIds);
        }
        
        private async Task StopSelectedJobsAsync()
        {
            var jobIds = JobsSidebar.SelectedJobs.Where(j => j.Status == TrainingStatus.Running || j.Status == TrainingStatus.Paused).Select(j => j.Id);
            await _trainingService.StopMultipleAsync(jobIds);
        }
        
        private async Task ExportSelectedJobAsync()
        {
            var job = JobsSidebar.SelectedJobs.FirstOrDefault();
            if (job?.Status == TrainingStatus.Completed)
            {
                // TODO(training): Show file dialog for export path
                await _trainingService.ExportJobAsync(job.Id, job.OutputPath);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

