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
    public sealed class TrainingViewModel : ViewModelBase, IDisposable
    {
        private readonly ITrainingService _trainingService;
        private readonly List<IDisposable> _disposables = new();
        
        public JobsSidebarViewModel JobsSidebar { get; }
        public JobDesignerViewModel JobDesigner { get; }
        public MonitorDockViewModel MonitorDock { get; }
        public InspectorViewModel Inspector { get; }
        
        // Modality-specific designers
        public ConversationsDesignerViewModel Conversations { get; }
        public VoiceDesignerViewModel Voice { get; }
        public ImagesDesignerViewModel Images { get; }
        public ThreeDModelsDesignerViewModel ThreeDModels { get; }
        public EntitiesDesignerViewModel Entities { get; }
        public VideosDesignerViewModel Videos { get; }
        public DesignProgressViewModel DesignProgress { get; }
        
        private object? _activeDesigner;
        public object? ActiveDesigner
        {
            get => _activeDesigner;
            set => SetProperty(ref _activeDesigner, value);
        }

        private string _selectedModality = "Conversations";
        public string SelectedModality
        {
            get => _selectedModality;
            set 
            { 
                if (SetProperty(ref _selectedModality, value))
                {
                    // Switch active designer based on modality
                    ActiveDesigner = value switch
                    {
                        "Conversations" => Conversations,
                        "Voice" => Voice,
                        "Images" => Images,
                        "ThreeD" => ThreeDModels,
                        "Entities" => Entities,
                        "Videos" => Videos,
                        _ => Conversations
                    };
                    
                    // Handle design progress toggle separately
                    if (value == "DesignProgress")
                    {
                        IsProgressMode = true;
                    }
                }
            }
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
        public ICommand ToggleMonitorDockCommand { get; }

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
            
            // Create modality-specific designers
            Conversations = new ConversationsDesignerViewModel(_trainingService);
            Voice = new VoiceDesignerViewModel(_trainingService);
            Images = new ImagesDesignerViewModel(_trainingService);
            ThreeDModels = new ThreeDModelsDesignerViewModel(_trainingService);
            Entities = new EntitiesDesignerViewModel(_trainingService);
            Videos = new VideosDesignerViewModel(_trainingService);
            DesignProgress = new DesignProgressViewModel();
            
            // Set default active designer
            ActiveDesigner = Conversations;
            
            StartCommand = new RelayCommand(async _ => await StartSelectedJobsAsync(), _ => CanStart);
            PauseCommand = new RelayCommand(async _ => await PauseSelectedJobsAsync(), _ => CanPause);
            StopCommand = new RelayCommand(async _ => await StopSelectedJobsAsync(), _ => CanStop);
            ExportCommand = new RelayCommand(async _ => await ExportSelectedJobAsync(), _ => CanExport);
            ToggleMonitorDockCommand = new RelayCommand(_ => IsMonitorOpen = !IsMonitorOpen);
            
            // Wire up cross-VM communication
            JobsSidebar.SelectedJobChanged += OnSelectedJobChanged;
            
            _disposables.Add(JobsSidebar);
            _disposables.Add(JobDesigner);
            _disposables.Add(MonitorDock);
            _disposables.Add(Inspector);
        }

        protected override void OnDisposing()
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
            
            // Notify all modality designers
            Conversations.SetCurrentJob(selectedJob);
            Voice.SetCurrentJob(selectedJob);
            Images.SetCurrentJob(selectedJob);
            ThreeDModels.SetCurrentJob(selectedJob);
            Entities.SetCurrentJob(selectedJob);
            Videos.SetCurrentJob(selectedJob);
            DesignProgress.SetCurrentJob(selectedJob);
            
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

    }
}

