using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class ConversationsDesignerViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;
        
        public string Title => "Conversations Training";
        
        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set => SetProperty(ref _currentJob, value);
        }
        
        public bool HasJob => CurrentJob != null;
        public TrainingDraft Draft { get; } = new() { Modality = TrainingModality.Conversations };
        public ParameterBagProxy Params { get; }
        
        // Commands
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
        
        public ConversationsDesignerViewModel(ITrainingService trainingService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);
            
            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            SetLearningRateCommand = new RelayCommand<string>(rate => SetLearningRate(rate));
            
            // Set comprehensive default parameters
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
            // TODO: Show file dialog for JSONL files
            System.Diagnostics.Debug.WriteLine("Import conversation dataset");
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
                
                System.Diagnostics.Debug.WriteLine($"Created conversation job: {job.Name}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }
        
        private void SetDefaultParameters()
        {
            // Core settings
            Params["BaseModel"] = "llama-2-7b-chat";
            Params["TrainingType"] = "LoRA";
            Params["LearningRate"] = "2e-4";
            Params["BatchSize"] = "4";
            Params["GradientAccumulation"] = "4";
            Params["MaxSeqLength"] = "2048";
            Params["ChatTemplate"] = "ChatML";
            Params["Epochs"] = "3";
            Params["MaxSteps"] = "1000";
            
            // Advanced settings
            Params["WarmupSteps"] = "100";
            Params["LRScheduler"] = "cosine";
            Params["Optimizer"] = "adamw_torch";
            Params["LossObjective"] = "cross_entropy";
            Params["LoRArank"] = "16";
            Params["LoRAalpha"] = "32";
            Params["LoRAdropout"] = "0.1";
            Params["ValidationSplit"] = "0.1";
            Params["EvalSteps"] = "500";
            Params["SaveSteps"] = "500";
            Params["KeepCheckpoints"] = "3";
            Params["GradientCheckpointing"] = "true";
            Params["FlashAttention"] = "true";
            Params["PackSequences"] = "false";
        }
        
        private void SetLearningRate(string? rate)
        {
            if (!string.IsNullOrWhiteSpace(rate))
            {
                Params["LearningRate"] = rate;
                OnPropertyChanged("LearningRate");
            }
        }
        
        private void Clear()
        {
            Draft.Datasets.Clear();
            Draft.Params.Clear();
            SetDefaultParameters();
        }
    }
}
