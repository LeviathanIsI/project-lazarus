using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class ThreeDModelsDesignerViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;

        public string Title => "3D Models Training";

        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }

        public bool HasJob => CurrentJob != null;
        public TrainingDraft Draft { get; } = new() { Modality = TrainingModality.ThreeD };
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

        public ThreeDModelsDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);

            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            SetLearningRateCommand = new RelayCommand<string>(rate => SetLearningRate(rate));
            SetBatchSizeCommand = new RelayCommand<string>(size => SetBatchSize(size));

            // Set comprehensive 3D defaults
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
            System.Diagnostics.Debug.WriteLine("Import 3D model files");
            await Task.CompletedTask;
        }

        private async Task CreateJobAsync()
        {
            if (HasJob) return;

            try
            {
                var job = await _trainingService.CreateJobAsync(Draft.Name, Draft.Modality);
                await _trainingService.UpdateJobAsync(job);
                System.Diagnostics.Debug.WriteLine($"Created 3D job: {job.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }

        private void Clear()
        {
            Draft.ModelFiles.Clear();
            Draft.Datasets.Clear();
            Draft.Params.Clear();
            SetDefaultParameters();
        }

        private void SetDefaultParameters()
        {
            // Core 3D settings
            Params["BaseModel"] = "instant-ngp";
            Params["TrainingType"] = "NeRF";
            Params["Resolution"] = "512³";
            Params["SamplesPerRay"] = "128";
            Params["LearningRate"] = "1e-3";
            Params["BatchSize"] = "4096";
            Params["Epochs"] = "200";
            Params["MaxSteps"] = "100000";

            // Loss and precision
            Params["LossFunction"] = "L2 (MSE)";
            Params["Precision"] = "FP16";

            // Advanced settings
            Params["Optimizer"] = "adam";
            Params["LRScheduler"] = "cosine";
            Params["RegularizationWeight"] = "0.01";
            Params["ValidationSplit"] = "0.1";
            Params["GradClipNorm"] = "1.0";
            Params["CheckpointFreq"] = "1000";

            // 3D Augmentation
            Params["ViewJitter"] = "true";
            Params["LightingChanges"] = "false";
            Params["CameraPoseNoise"] = "true";

            // Mesh regularization
            Params["SurfaceSmoothing"] = "true";
            Params["LaplacianReg"] = "false";
            Params["NormalConsistency"] = "true";

            // NeRF options
            Params["HierarchicalSampling"] = "true";
            Params["WhiteBackground"] = "false";
            Params["RawNoiseStd"] = "true";

            // NeRF Advanced Settings
            Params["RayBatchSize"] = "8192";
            Params["NearClipping"] = "0.1";
            Params["FarClipping"] = "10.0";
            Params["SceneScale"] = "1.0";
            Params["CoarseFineSteps"] = "64,128";
            Params["RenderResolution"] = "800x800";
            Params["NerfRegWeight"] = "0.001";
            Params["TVLoss"] = "true";
            Params["SparsityLoss"] = "false";
            Params["TVLossWeight"] = "0.001";
            Params["SparsityLossWeight"] = "0.0001";

            // Mesh Training Settings
            Params["VertexCountLimit"] = "100K";
            Params["TextureResolution"] = "1024x1024";
            Params["SurfaceLossFunction"] = "Chamfer Distance";
            Params["LaplacianWeight"] = "0.1";
            Params["MeshCheckpointFreq"] = "1000";
            Params["DecimationToggle"] = "false";

            // Point Cloud Training Settings
            Params["NumberOfPoints"] = "100K";
            Params["PointRadius"] = "0.01";
            Params["DistanceMetric"] = "Chamfer Distance";
            Params["NoiseInjection"] = "0.001";
            Params["ColorLearning"] = "true";
            Params["SpatialAugmentations"] = "true";

            // General Advanced Controls
            Params["GeneralOptimizer"] = "adamw";
            Params["GeneralLRScheduler"] = "cosine";
            Params["GeneralGradClip"] = "1.0";
            Params["GeneralPrecision"] = "FP16";
            Params["GeneralValidationSplit"] = "0.1";
            Params["GeneralTrainingSteps"] = "100000";
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
    }
}
