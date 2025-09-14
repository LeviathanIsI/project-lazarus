using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class VideosDesignerViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;

        public string Title => "Video Training";

        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }

        public bool HasJob => CurrentJob != null;
        public TrainingDraft Draft { get; } = new() { Modality = TrainingModality.Videos };
        public ParameterBagProxy Params { get; }

        public ICommand ImportCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SetLearningRateCommand { get; }

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

        public VideosDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);

            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            SetLearningRateCommand = new RelayCommand<string>(rate => SetLearningRate(rate));

            // Set comprehensive video defaults
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
            System.Diagnostics.Debug.WriteLine("Import video files");
            await Task.CompletedTask;
        }

        private async Task CreateJobAsync()
        {
            if (HasJob) return;

            try
            {
                var job = await _trainingService.CreateJobAsync(Draft.Name, Draft.Modality);
                await _trainingService.UpdateJobAsync(job);
                System.Diagnostics.Debug.WriteLine($"Created video job: {job.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }

        private void Clear()
        {
            Draft.VideoFiles.Clear();
            Draft.Datasets.Clear();
            Draft.Params.Clear();
            SetDefaultParameters();
        }

        private void SetDefaultParameters()
        {
            // Core Video Parameters
            Params["BaseModel"] = "stable-video-diffusion";
            Params["BatchSize"] = "2";
            Params["LearningRate"] = "5e-5";
            Params["Epochs"] = "10";
            Params["MaxSteps"] = "10000";

            // Frame & Sequence Settings
            Params["Resolution"] = "512x512";
            Params["ClipLength"] = "16";
            Params["FrameRate"] = "24";
            Params["StrideOverlap"] = "2 (50% Overlap)";

            // Video Augmentations
            Params["TemporalJitter"] = "true";
            Params["FrameDropout"] = "false";
            Params["ColorJitter"] = "true";
            Params["SpatialTransforms"] = "true";
            Params["NoiseInjection"] = "false";

            // Loss & Regularization
            Params["LossFunction"] = "Combined";
            Params["TemporalSmoothingWeight"] = "0.1";
            Params["RegularizationDataset"] = "";

            // Advanced Controls
            Params["Precision"] = "FP16";
            Params["Optimizer"] = "adamw";
            Params["GradientAccumulation"] = "4";
            Params["CheckpointFrequency"] = "1000";
            Params["ValidationSplit"] = "0.1";
        }

        private void SetLearningRate(string? rate)
        {
            if (!string.IsNullOrWhiteSpace(rate))
            {
                Params["LearningRate"] = rate;
                OnPropertyChanged("LearningRate");
            }
        }
    }
}
