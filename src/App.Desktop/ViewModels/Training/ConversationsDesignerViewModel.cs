using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Lazarus.Shared.Contracts;
using Lazarus.Shared.Training;
using Lazarus.Backend.Services;
using Microsoft.Win32;

namespace Lazarus.Desktop.ViewModels.Training
{
    public sealed class ConversationsDesignerViewModel : ViewModelBase
    {
        private readonly IConversationTrainingService _svc;
        private readonly ITrainingService _trainingService;

        public string Title => "Conversations Training";

        // UI state
        public ObservableCollection<string> TrainFiles { get; } = new();
        public ObservableCollection<string> EvalFiles { get; } = new();
        public ObservableCollection<string> PrefFiles { get; } = new();

        public TrainerBackend SelectedTrainer
        {
            get => _selectedTrainer;
            set
            {
                if (SetProperty(ref _selectedTrainer, value))
                {
                    ApplyTrainerIntelligence();
                    RaiseCommandCanExecutes();
                }
            }
        }
        private TrainerBackend _selectedTrainer = TrainerBackend.LLaMAFactory;

        public TrainingTask SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (SetProperty(ref _selectedTask, value))
                {
                    OnPropertyChanged(nameof(NeedsPreferenceData));
                    // Adjust LR for LLaMAFactory when task implies full FT vs adapters
                    if (SelectedTrainer == TrainerBackend.LLaMAFactory)
                    {
                        var trainingType = Params["TrainingType"]?.Trim();
                        if (string.Equals(trainingType, "Full Fine-tune", StringComparison.OrdinalIgnoreCase))
                            Params["LearningRate"] = "5e-5";
                        else
                            Params["LearningRate"] = "2e-4";
                        OnPropertyChanged(nameof(Params));
                    }
                }
            }
        }
        private TrainingTask _selectedTask = TrainingTask.SFT;

        public bool NeedsPreferenceData => SelectedTask != TrainingTask.SFT;

        // Job
        private TrainingJob? _currentJob;
        public TrainingJob? CurrentJob
        {
            get => _currentJob;
            private set { if (SetProperty(ref _currentJob, value)) { OnPropertyChanged(nameof(HasJob)); RaiseCommandCanExecutes(); } }
        }
        public bool HasJob => CurrentJob != null;
        private Guid? _jobId;

        // Draft bag
        public TrainingDraft Draft { get; } = new() { Modality = TrainingModality.Conversations };
        public ParameterBagProxy Params { get; }

        // Progress
        private double _progress;
        public double Progress { get => _progress; private set => SetProperty(ref _progress, value); }

        // LLaMAFactory compatibility status
        public enum LfStatus { None, Valid, Warning, Error }
        private LfStatus _lfStatus;
        public LfStatus LlamaFactoryStatus { get => _lfStatus; private set { if (SetProperty(ref _lfStatus, value)) OnPropertyChanged(nameof(IsLfStatusVisible)); } }
        private string _lfStatusMessage = string.Empty;
        public string LlamaFactoryStatusMessage { get => _lfStatusMessage; private set => SetProperty(ref _lfStatusMessage, value); }
        public bool IsLfStatusVisible => SelectedTrainer == TrainerBackend.LLaMAFactory && LlamaFactoryStatus != LfStatus.None;

        // Visibility flags for incompatible options
        private bool _showGcOption = true, _showFa2Option = true, _showPackSeqOption = true;
        public bool ShowGradientCheckpointingOption { get => _showGcOption; private set => SetProperty(ref _showGcOption, value); }
        public bool ShowFlashAttention2Option { get => _showFa2Option; private set => SetProperty(ref _showFa2Option, value); }
        public bool ShowPackSequencesOption { get => _showPackSeqOption; private set => SetProperty(ref _showPackSeqOption, value); }

        // Commands
        public ICommand ImportTrainCommand { get; }
        public ICommand ImportEvalCommand { get; }
        public ICommand ImportPrefCommand { get; }

        public RelayCommand CreateJobCommand { get; }
        public RelayCommand CreateAndStartCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand PauseCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand ExportCommand { get; }

        public ICommand ClearCommand { get; }
        public ICommand SetLearningRateCommand { get; }

        // Epochs/Steps toggle
        private bool _useEpochs = true;
        public bool UseEpochs
        {
            get => _useEpochs;
            set { if (SetProperty(ref _useEpochs, value)) OnPropertyChanged(nameof(UseSteps)); }
        }
        public bool UseSteps { get => !_useEpochs; set => UseEpochs = !value; }

        public event EventHandler<TrainingJob>? JobCreated;

        public ConversationsDesignerViewModel(IConversationTrainingService conversationService, ITrainingService trainingService)
        {
            _svc = conversationService ?? throw new ArgumentNullException(nameof(conversationService));
            _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
            Params = new ParameterBagProxy(Draft);
            SetDefaultParameters();

            ImportTrainCommand = new RelayCommand(async _ => await ImportAsync(DatasetKind.Conversations));
            ImportEvalCommand = new RelayCommand(async _ => await ImportAsync(DatasetKind.Eval));
            ImportPrefCommand = new RelayCommand(async _ => await ImportAsync(DatasetKind.Preferences), _ => NeedsPreferenceData);

            CreateJobCommand = new RelayCommand(async _ => await CreateJobAsync(), _ => CanCreateJob());
            CreateAndStartCommand = new RelayCommand(async _ =>
            {
                if (await CreateJobAsync()) await StartAsync();
            }, _ => CanCreateJob());

            StartCommand = new RelayCommand(async _ => await StartAsync(), _ => _jobId.HasValue);
            PauseCommand = new RelayCommand(async _ => await PauseAsync(), _ => _jobId.HasValue);
            StopCommand = new RelayCommand(async _ => await StopAsync(), _ => _jobId.HasValue);
            ExportCommand = new RelayCommand(async _ => await ExportAsync(), _ => _jobId.HasValue);

            ClearCommand = new RelayCommand(_ => Clear());
            SetLearningRateCommand = new RelayCommand<string>(rate => SetLearningRate(rate));

            _svc.ProgressChanged += OnProgressChanged;
            ApplyTrainerIntelligence();
        }

        public void SetCurrentJob(TrainingJob? job)
        {
            CurrentJob = job;
            Params.SetCurrentJob(job);
        }

        private void RaiseCommandCanExecutes()
        {
            CreateJobCommand.RaiseCanExecuteChanged();
            CreateAndStartCommand.RaiseCanExecuteChanged();
            StartCommand.RaiseCanExecuteChanged();
            PauseCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
            ExportCommand.RaiseCanExecuteChanged();
        }

        private async Task ImportAsync(DatasetKind kind)
        {
            var ofd = new OpenFileDialog
            {
                Title = $"Import {(kind == DatasetKind.Conversations ? "Conversation" : kind == DatasetKind.Preferences ? "Preference" : "Eval")} JSONL",
                Filter = "JSONL files (*.jsonl)|*.jsonl|All files (*.*)|*.*",
                Multiselect = true
            };
            if (ofd.ShowDialog() != true) return;

            foreach (var file in ofd.FileNames)
            {
                try
                {
                    var normalized = await _svc.ImportFromJsonlAsync(file, kind);
                    switch (kind)
                    {
                        case DatasetKind.Conversations: TrainFiles.Add(normalized); break;
                        case DatasetKind.Preferences: PrefFiles.Add(normalized); break;
                        case DatasetKind.Eval: EvalFiles.Add(normalized); break;
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Import failed: {ex.Message}"); }
            }
            RaiseCommandCanExecutes();
        }

        private async Task<bool> CreateJobAsync()
        {
            if (HasJob) return false;
            try
            {
                // Last-moment validations and intelligent defaults
                if (SelectedTrainer == TrainerBackend.LLaMAFactory)
                {
                    AutoSetChatTemplateFromBaseModel();
                    AutoSetLearningRateForTrainer();
                    if (!ValidateBaseModelPath()) return false;
                    EstimateVramAndWarn();
                }
                var profile = BuildProfileFromParams();
                var job = await _svc.CreateJobAsync(profile);
                _jobId = Guid.Parse(job.Id);
                CurrentJob = job;
                // Surface in the Jobs sidebar by notifying parent; optionally mirror to training service store
                try { await _trainingService.UpdateJobAsync(job); } catch { /* mock may be no-op */ }
                JobCreated?.Invoke(this, job);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create job failed: {ex.Message}");
                return false;
            }
        }

        public async Task StartAsync()
        {
            if (!_jobId.HasValue) return;
            try { await _svc.StartTrainingAsync(_jobId.Value); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Start failed: {ex.Message}"); }
        }

        public async Task PauseAsync()
        {
            if (!_jobId.HasValue) return;
            try { await _svc.PauseTrainingAsync(_jobId.Value); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Pause failed: {ex.Message}"); }
        }

        public async Task StopAsync()
        {
            if (!_jobId.HasValue) return;
            try { await _svc.StopTrainingAsync(_jobId.Value); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Stop failed: {ex.Message}"); }
        }

        public async Task ExportAsync()
        {
            if (!_jobId.HasValue) return;
            try
            {
                var path = await _svc.ExportArtifactsAsync(_jobId.Value);
                System.Diagnostics.Debug.WriteLine($"Exported artifacts to {path}");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}"); }
        }

        private void Clear()
        {
            TrainFiles.Clear();
            EvalFiles.Clear();
            PrefFiles.Clear();
            Draft.Datasets.Clear();
            Draft.Params.Clear();
            SetDefaultParameters();
            _jobId = null;
            CurrentJob = null;
            Progress = 0;
            RaiseCommandCanExecutes();
        }

        private void SetDefaultParameters()
        {
            Params["BaseModel"] = "llama-2-7b-chat";
            Params["TrainingType"] = "LoRA";          // maps to UseLoRA/UseQLoRA
            Params["LearningRate"] = "2e-4";
            Params["BatchSize"] = "4";
            Params["GradientAccumulation"] = "4";
            Params["MaxSeqLength"] = "2048";
            Params["ChatTemplate"] = "ChatML";
            Params["Epochs"] = "3";
            Params["MaxSteps"] = "1000";

            Params["WarmupSteps"] = "100";
            Params["LRScheduler"] = "cosine";
            Params["Optimizer"] = "adamw_torch";
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
            if (string.IsNullOrWhiteSpace(rate)) return;
            Params["LearningRate"] = rate;
            OnPropertyChanged(nameof(Params));
        }

        private bool CanCreateJob()
        {
            if (TrainFiles.Count == 0) return false;
            if (NeedsPreferenceData && PrefFiles.Count == 0) return false;
            return true;
        }

        private Lazarus.Shared.Training.TrainingProfile BuildProfileFromParams()
        {
            var trainingType = Params["TrainingType"]?.Trim();
            var (useLoRA, useQLoRA) = trainingType switch
            {
                "QLoRA" => (true, true),
                "LoRA" => (true, false),
                _ => (false, false) // full FT (not recommended here)
            };

            return new Lazarus.Shared.Training.TrainingProfile
            {
                Trainer = SelectedTrainer,
                Task = SelectedTask,
                BaseModel = Params["BaseModel"]!,
                ChatTemplate = Params["ChatTemplate"]!,
                TrainFiles = TrainFiles.ToArray(),
                EvalFiles = EvalFiles.ToArray(),
                PreferenceFiles = NeedsPreferenceData ? PrefFiles.ToArray() : null,

                LearningRate = TryParseDouble(Params["LearningRate"], 2e-4),
                Epochs = UseEpochs ? TryParseInt(Params["Epochs"], 3) : null,
                MaxSteps = UseSteps ? TryParseInt(Params["MaxSteps"], 0) : null,

                PerDeviceBatch = TryParseInt(Params["BatchSize"], 4),
                GradAccum = TryParseInt(Params["GradientAccumulation"], 4),
                MaxSeqLen = TryParseInt(Params["MaxSeqLength"], 2048),

                UseLoRA = useLoRA,
                UseQLoRA = useQLoRA,
                LoRARank = TryParseInt(Params["LoRArank"], 16),
                LoRAAlpha = TryParseInt(Params["LoRAalpha"], 32),
                LoRADropout = TryParseDouble(Params["LoRAdropout"], 0.1),

                WarmupSteps = TryParseInt(Params["WarmupSteps"], 100),
                LrScheduler = Params["LRScheduler"] ?? "cosine",
                Optimizer = Params["Optimizer"] ?? "adamw_torch",
                ValidationSplit = TryParseDoubleN(Params["ValidationSplit"], 0.1),
                EvalEverySteps = TryParseInt(Params["EvalSteps"], 500),
                SaveEverySteps = TryParseInt(Params["SaveSteps"], 500),
                KeepCheckpoints = TryParseInt(Params["KeepCheckpoints"], 3),

                GradientCheckpointing = TryParseBool(Params["GradientCheckpointing"], true),
                FlashAttention = TryParseBool(Params["FlashAttention"], true),
                PackSequences = TryParseBool(Params["PackSequences"], false),

                OutputName = $"conv-{Params["BaseModel"]}-{SelectedTask.ToString().ToLower()}-{DateTime.UtcNow:yyyyMMddHHmm}"
            };
        }

        private static int TryParseInt(string? s, int def) =>
            int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;

        private static double TryParseDouble(string? s, double def) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

        private static double? TryParseDoubleN(string? s, double? def) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

        private static bool TryParseBool(string? s, bool def) =>
            bool.TryParse(s, out var v) ? v : def;

        private void OnProgressChanged(object? sender, TrainingProgressEventArgs e)
        {
            if (_jobId.HasValue && e.JobId == _jobId.Value) Progress = e.Progress;
        }

        // --- LLaMAFactory intelligence helpers ---
        private void ApplyTrainerIntelligence()
        {
            var isLf = SelectedTrainer == TrainerBackend.LLaMAFactory;
            ShowGradientCheckpointingOption = !isLf; // hide when LF due to functools.partial bug
            ShowFlashAttention2Option = !isLf;       // hide (not supported on Windows builds)
            ShowPackSequencesOption = !isLf;         // hide (incompatible on Windows mixed precision)

            if (isLf)
            {
                AutoSetChatTemplateFromBaseModel();
                AutoSetLearningRateForTrainer();
                Params["Optimizer"] = "adamw_torch";
                Params["LRScheduler"] = "linear";
                Params["GradientAccumulation"] = "4";
                OnPropertyChanged(nameof(Params));
                LlamaFactoryStatus = LfStatus.Warning;
                LlamaFactoryStatusMessage = "LLaMAFactory: adjusted params for Windows; FA2/GC/Pack hidden";
            }
            else
            {
                LlamaFactoryStatus = LfStatus.None;
                LlamaFactoryStatusMessage = string.Empty;
            }
        }

        private void AutoSetChatTemplateFromBaseModel()
        {
            var model = Params["BaseModel"] ?? string.Empty;
            var template = "chatml";
            if (model.IndexOf("Qwen", StringComparison.OrdinalIgnoreCase) >= 0) template = "qwen";
            else if (model.IndexOf("Llama-3", StringComparison.OrdinalIgnoreCase) >= 0 || model.IndexOf("Llama3", StringComparison.OrdinalIgnoreCase) >= 0) template = "llama3";
            else if (model.IndexOf("Mistral", StringComparison.OrdinalIgnoreCase) >= 0) template = "mistral";
            Params["ChatTemplate"] = template;
        }

        private void AutoSetLearningRateForTrainer()
        {
            var trainingType = Params["TrainingType"]?.Trim();
            if (string.Equals(trainingType, "Full Fine-tune", StringComparison.OrdinalIgnoreCase))
                Params["LearningRate"] = "5e-5";
            else
                Params["LearningRate"] = "2e-4"; // LoRA/QLoRA
        }

        private bool ValidateBaseModelPath()
        {
            try
            {
                var modelPath = Params["BaseModel"] ?? string.Empty;
                if (modelPath.Contains(".gguf", StringComparison.OrdinalIgnoreCase))
                {
                    LlamaFactoryStatus = LfStatus.Error;
                    LlamaFactoryStatusMessage = "LLaMAFactory requires Hugging Face format (config.json + safetensors). GGUF is inference-only.";
                    return false;
                }

                if (System.IO.Directory.Exists(modelPath))
                {
                    var required = new[] { "config.json", "tokenizer.json", "tokenizer_config.json" };
                    var hasSt = System.IO.Directory.GetFiles(modelPath, "*.safetensors").Any();
                    var missing = required.Any(f => !System.IO.File.Exists(System.IO.Path.Combine(modelPath, f)));
                    if (missing || !hasSt)
                    {
                        LlamaFactoryStatus = LfStatus.Warning;
                        LlamaFactoryStatusMessage = "Model appears incomplete. Need config.json, tokenizer files, and .safetensors weights.";
                    }
                    else
                    {
                        LlamaFactoryStatus = LfStatus.Valid;
                        LlamaFactoryStatusMessage = "Model folder looks good for LLaMAFactory.";
                    }
                }
                else
                {
                    // If it's not a directory, we still proceed but warn.
                    LlamaFactoryStatus = LfStatus.Warning;
                    LlamaFactoryStatusMessage = "Base model should be a local HF folder (config + safetensors).";
                }
                return true;
            }
            catch (Exception ex)
            {
                LlamaFactoryStatus = LfStatus.Error;
                LlamaFactoryStatusMessage = "Model validation failed: " + ex.Message;
                return false;
            }
        }

        private void EstimateVramAndWarn()
        {
            var batch = TryParseInt(Params["BatchSize"], 4);
            var accum = TryParseInt(Params["GradientAccumulation"], 4);
            var seqlen = TryParseInt(Params["MaxSeqLength"], 2048);
            var estimated = (batch * accum * seqlen * 2) / 1024.0; // rough GB
            if (estimated > 20)
            {
                LlamaFactoryStatus = LfStatus.Warning;
                LlamaFactoryStatusMessage = $"Settings may require ~{estimated:0.#}GB VRAM. Reduce batch or sequence length.";
            }
        }
    }
}
