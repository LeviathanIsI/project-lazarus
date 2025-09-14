using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class EntitiesDesignerViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;

        public string Title => "Entities Training";

        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }

        public bool HasJob => CurrentJob != null;
        public TrainingDraft Draft { get; } = new() { Modality = TrainingModality.Entities };
        public ParameterBagProxy Params { get; }

        public ICommand ImportCommand { get; }
        public ICommand CreateJobCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand SetLearningRateCommand { get; }

        public EntitiesDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);

            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            SetLearningRateCommand = new RelayCommand<string>(rate => SetLearningRate(rate));

            // Set comprehensive entity defaults
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
            System.Diagnostics.Debug.WriteLine("Import entity datasets");
            await Task.CompletedTask;
        }

        private async Task CreateJobAsync()
        {
            if (HasJob) return;

            try
            {
                var job = await _trainingService.CreateJobAsync(Draft.Name, Draft.Modality);
                await _trainingService.UpdateJobAsync(job);
                System.Diagnostics.Debug.WriteLine($"Created entities job: {job.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }

        private void Clear()
        {
            Draft.AvatarModels.Clear();
            Draft.Voices.Clear();
            Draft.Datasets.Clear();
            Draft.Params.Clear();
            SetDefaultParameters();
        }

        private void SetDefaultParameters()
        {
            // Core Entity Parameters
            Params["Base3DModel"] = "human-rigged";
            Params["VoiceModel"] = "expressive-voice";
            Params["RecognitionType"] = "Full Multi-Modal";
            Params["LearningRate"] = "5e-4";

            // Lip Sync & Mouth Movement
            Params["VisemeSet"] = "ARPAbet";
            Params["FrameInterpolation"] = "30";
            Params["MouthSensitivity"] = "1.0 (Normal)";
            Params["LatencyTolerance"] = "100";

            // Gesture & Body Animation
            Params["GestureMappingMode"] = "Learned (AI)";
            Params["AnimationPresets"] = "Neutral";
            Params["GestureIntensity"] = "1.0 (Normal)";
            Params["BlendshapeWeights"] = "1.0,0.8,0.6";
            Params["PoseRegularization"] = "true";

            // Timing & Multi-Modal Alignment
            Params["AudioMotionOffset"] = "0 (Synchronized)";
            Params["MotionSamplingRate"] = "30";
            Params["SynchronizationTarget"] = "Multi-Modal";

            // Advanced Entity Controls
            Params["CameraTrackingMode"] = "Look-at";
            Params["EmotionInferenceModel"] = "BERT Emotion";
            Params["HairPhysics"] = "true";
            Params["ClothPhysics"] = "false";
            Params["SecondaryMotion"] = "true";
            Params["RegularizationDataset"] = "";
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
