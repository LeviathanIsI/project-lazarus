using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;
using Lazarus.Backend.Services;
using Lazarus.Shared.Models.Training;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class ConversationsDesignerViewModel : ViewModelBase
    {
        private readonly ITrainingService _trainingService;
        private readonly IConversationTrainingService _conversationService;
        
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
        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ExportCommand { get; }
        
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
        
        private Guid? _conversationJobId;
        private double _progress;
        public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }

        public ConversationsDesignerViewModel(ITrainingService trainingService, IConversationTrainingService conversationService)
        {
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            _conversationService = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            Params = new ParameterBagProxy(Draft);
            
            ImportCommand = new RelayCommand(async _ => await ImportAsync());
            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => !HasJob);
            ClearCommand = new RelayCommand(_ => Clear());
            SetLearningRateCommand = new RelayCommand<string>(rate => SetLearningRate(rate));
            StartCommand = new RelayCommand(async _ => await StartAsync(), _ => _conversationJobId.HasValue);
            PauseCommand = new RelayCommand(async _ => await PauseAsync(), _ => _conversationJobId.HasValue);
            StopCommand = new RelayCommand(async _ => await StopAsync(), _ => _conversationJobId.HasValue);
            ExportCommand = new RelayCommand(async _ => await ExportAsync(), _ => _conversationJobId.HasValue);

            _conversationService.ProgressChanged += OnProgressChanged;
            
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
            var ofd = new OpenFileDialog
            {
                Title = "Import Conversation JSONL",
                Filter = "JSONL files (*.jsonl)|*.jsonl|All files (*.*)|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    await _conversationService.ImportFromJsonlAsync(ofd.FileName);
                    System.Diagnostics.Debug.WriteLine($"Imported dataset: {ofd.FileName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to import JSONL: {ex.Message}");
                }
            }
        }
        
        private async Task CreateJobAsync()
        {
            if (HasJob) return;
            
            try
            {
                var config = BuildConfigFromParams();
                var convJob = await _conversationService.CreateJobAsync(config);
                _conversationJobId = convJob.Id;
                OnPropertyChanged(nameof(HasJob));
                System.Diagnostics.Debug.WriteLine($"Created conversation training job: {convJob.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create job: {ex.Message}");
            }
        }

        public async Task StartAsync()
        {
            if (!_conversationJobId.HasValue) return;
            try { await _conversationService.StartTrainingAsync(_conversationJobId.Value); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Start failed: {ex.Message}"); }
        }

        public async Task PauseAsync()
        {
            if (!_conversationJobId.HasValue) return;
            try { await _conversationService.PauseTrainingAsync(_conversationJobId.Value); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Pause failed: {ex.Message}"); }
        }

        public async Task StopAsync()
        {
            if (!_conversationJobId.HasValue) return;
            try { await _conversationService.StopTrainingAsync(_conversationJobId.Value); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Stop failed: {ex.Message}"); }
        }

        public async Task ExportAsync()
        {
            if (!_conversationJobId.HasValue) return;
            try
            {
                var path = await _conversationService.ExportToJsonlAsync(_conversationJobId.Value);
                System.Diagnostics.Debug.WriteLine($"Exported to {path}");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}"); }
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
            _conversationJobId = null;
            Progress = 0;
            OnPropertyChanged(nameof(HasJob));
        }

        private TrainingConfiguration BuildConfigFromParams()
        {
            var cfg = new TrainingConfiguration
            {
                BaseModel = Params["BaseModel"],
                Type = ParseTrainingType(Params["TrainingType"]),
                LearningRate = TryParseDouble(Params["LearningRate"], 2e-4),
                BatchSize = TryParseInt(Params["BatchSize"], 4),
                GradientAccumulation = TryParseInt(Params["GradientAccumulation"], 4),
                MaxSequenceLength = TryParseInt(Params["MaxSeqLength"], 2048),
                ChatTemplate = Params["ChatTemplate"],
                Duration = UseEpochs ? TrainingDuration.Epochs : TrainingDuration.Steps,
                Steps = TryParseInt(Params["MaxSteps"], 0),
                Epochs = TryParseInt(Params["Epochs"], 3)
            };
            return cfg;
        }

        private static TrainingType ParseTrainingType(string? value) => value switch
        {
            "LoRA" => TrainingType.LoRA,
            "QLoRA" => TrainingType.QLoRA,
            _ => TrainingType.FineTuning
        };

        private static int TryParseInt(string? s, int def) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;
        private static double TryParseDouble(string? s, double def) => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

        private void OnProgressChanged(object? sender, TrainingProgressEventArgs e)
        {
            if (_conversationJobId.HasValue && e.JobId == _conversationJobId.Value)
            {
                Progress = e.Progress;
            }
        }
    }
}
