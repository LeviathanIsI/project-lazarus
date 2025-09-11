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
        public ICommand SetLearningRateCommand { get; }
        public ICommand SetBatchSizeCommand { get; }
        
        // UI state for training duration selection
        private bool _useEpochs = true;
        public bool UseEpochs
        {
            get => _useEpochs;
            set
            {
                if (SetProperty(ref _useEpochs, value))
                {
                    OnPropertyChanged(nameof(UseSteps));
                }
            }
        }
        
        public bool UseSteps
        {
            get => !_useEpochs;
            set => UseEpochs = !value;
        }
        
        public VoiceDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);
            
            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            SetLearningRateCommand = new RelayCommand<string>(rate => SetLearningRate(rate));
            SetBatchSizeCommand = new RelayCommand<string>(size => SetBatchSize(size));
            
            // Set comprehensive voice defaults
            SetDefaultParameters();
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
        
        private void SetDefaultParameters()
        {
            // Core voice settings
            Params["BaseModel"] = "whisper-large";
            Params["TrainingType"] = "TTS Fine-tune";
            Params["SampleRate"] = "22050";
            Params["EmbeddingSize"] = "256";
            Params["LearningRate"] = "1e-4";
            Params["BatchSize"] = "4";
            Params["Epochs"] = "10";
            Params["MaxSteps"] = "5000";
            
            // Advanced voice settings
            Params["Optimizer"] = "adamw";
            Params["LossFunction"] = "mse";
            Params["WarmupSteps"] = "500";
            Params["LRScheduler"] = "cosine";
            Params["ValidationSplit"] = "0.1";
            Params["SaveSteps"] = "1000";
            
            // Audio augmentation
            Params["PitchShift"] = "true";
            Params["TimeStretch"] = "false";
            Params["BackgroundNoise"] = "true";
            Params["VolumeJitter"] = "true";
            Params["SpectralMasking"] = "false";
            Params["FormantShift"] = "false";
            
            // Performance options
            Params["MixedPrecision"] = "true";
            Params["GradientClipping"] = "true";
            Params["DynamicBatching"] = "false";
        }
        
        private void SetLearningRate(string? rate)
        {
            if (!string.IsNullOrWhiteSpace(rate))
            {
                Params["LearningRate"] = rate;
                OnPropertyChanged("LearningRate");
            }
        }
        
        private void SetBatchSize(string? size)
        {
            if (!string.IsNullOrWhiteSpace(size))
            {
                Params["BatchSize"] = size;
                OnPropertyChanged("BatchSize");
            }
        }
        
        private void Clear()
        {
            Draft.AudioFiles.Clear();
            Draft.Datasets.Clear();
            Draft.Params.Clear();
            SetDefaultParameters();
        }
    }
}
