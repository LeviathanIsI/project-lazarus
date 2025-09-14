using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class ImagesDesignerViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;

        public string Title => "Image Training";

        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }

        public bool HasJob => CurrentJob != null;
        public TrainingDraft Draft { get; } = new() { Modality = TrainingModality.Images };
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

        public ImagesDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);

            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            SetLearningRateCommand = new RelayCommand<string>(rate => SetLearningRate(rate));
            SetBatchSizeCommand = new RelayCommand<string>(size => SetBatchSize(size));

            // Set comprehensive image defaults
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
            System.Diagnostics.Debug.WriteLine("Import image datasets");
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

                System.Diagnostics.Debug.WriteLine($"Created image job: {job.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }

        private void SetDefaultParameters()
        {
            // Core image settings
            Params["BaseModel"] = "stable-diffusion-xl";
            Params["TrainingType"] = "LoRA";
            Params["Resolution"] = "1024x1024";
            Params["ResolutionStrategy"] = "Fixed";
            Params["LearningRate"] = "1e-4";
            Params["BatchSize"] = "1";
            Params["GradientAccumulation"] = "4";
            Params["Precision"] = "FP16";
            Params["Epochs"] = "10";
            Params["MaxSteps"] = "2000";

            // Advanced image settings
            Params["Optimizer"] = "adamw";
            Params["LRScheduler"] = "constant_with_warmup";
            Params["WarmupSteps"] = "100";
            Params["ValidationSplit"] = "0.1";
            Params["RegularizationPath"] = "";

            // Image augmentation (conservative defaults)
            Params["RandomFlip"] = "true";
            Params["ColorJitter"] = "false";
            Params["RandomBlur"] = "false";
            Params["RandomRotation"] = "false";
            Params["NoiseInjection"] = "false";
            Params["Cutout"] = "false";

            // Performance options
            Params["GradientCheckpointing"] = "true";
            Params["UseEMA"] = "true";
            Params["CacheLatents"] = "true";
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
            Draft.ImageFiles.Clear();
            Draft.Datasets.Clear();
            Draft.Params.Clear();
            SetDefaultParameters();
        }
    }
}
