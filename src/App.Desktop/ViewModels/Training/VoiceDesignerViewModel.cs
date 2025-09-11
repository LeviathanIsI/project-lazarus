using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class VoiceDesignerViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;
        
        public string Title => "Voice Training";
        
        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }
        
        public bool HasJob => CurrentJob != null;
        public TrainingDraft Draft { get; } = new() { Modality = TrainingModality.Voice };
        public ParameterBagProxy Params { get; }
        
        public ICommand ImportCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand ClearCommand { get; }
        
        public VoiceDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);
            
            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            
            // Set voice-specific defaults
            Params["BaseModel"] = "whisper-large";
            Params["TrainingType"] = "Fine-tune";
            Params["LearningRate"] = "1e-4";
            Params["SampleRate"] = "22050";
        }
        
        public void SetCurrentJob(TrainingJob? job)
        {
            CurrentJob = job;
            Params.SetCurrentJob(job);
            OnPropertyChanged(nameof(HasJob));
        }
        
        private async Task ImportAsync()
        {
            System.Diagnostics.Debug.WriteLine("Import voice datasets");
            await Task.CompletedTask;
        }
        
        private async Task CreateJobAsync()
        {
            if (HasJob) return;
            
            try
            {
                var job = await _trainingService.CreateJobAsync(Draft.Name, Draft.Modality);
                job.OutputPath = Params["OutputPath"];
                await _trainingService.UpdateJobAsync(job);
                
                System.Diagnostics.Debug.WriteLine($"Created voice job: {job.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }
        
        private void Clear()
        {
            Draft.AudioFiles.Clear();
            Draft.Datasets.Clear();
            Draft.Params.Clear();
        }
    }
}
