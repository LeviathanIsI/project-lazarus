using System;
using System.Threading.Tasks;
using Lazarus.Shared.Settings;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Settings view. Wraps AppSettings for editing and persistence.
/// </summary>
    public sealed class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly Lazarus.Backend.Services.IModelInventoryService? _inventory;
        private readonly Lazarus.Data.Repositories.ISettingsRepository? _kvSettings;
    // private readonly Services.IOrchestratorClient? _orchestratorClient;

        public SettingsViewModel(
            ISettingsService settingsService,
            Lazarus.Backend.Services.IModelInventoryService? inventory = null,
            Lazarus.Data.Repositories.ISettingsRepository? kvSettings = null)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _inventory = inventory;
            _kvSettings = kvSettings;
            // Initialize from current settings
            var s = _settingsService.Current;
        _preferredTheme = s.PreferredTheme ?? "Dark";
        _language = s.Language ?? "en-US";
        _checkForUpdatesOnStart = s.CheckForUpdatesOnStart;
        _startOrchestratorWithApp = s.StartOrchestratorWithApp;
        _autoSaveConversations = s.AutoSaveConversations;
        _modelsDirectory = s.ModelsDirectory;
        _cacheDirectory = s.CacheDirectory;
        _orchestratorBaseUrl = s.OrchestratorBaseUrl;
        _orchestratorStartupTimeoutSec = s.OrchestratorStartupTimeoutSec;
        _activeRunner = s.ActiveRunner;
        _autoStartLastRunner = s.AutoStartLastRunner;
        _llamaServerExecutablePath = s.LlamaCpp.ServerExecutablePath;
        _llamaAdditionalArgs = s.LlamaCpp.AdditionalArgs;
        _llamaPort = s.LlamaCpp.Port;
        _llamaGpuLayers = s.LlamaCpp.GpuLayers;
        _llamaUseCuda = s.LlamaCpp.UseCuda;
        // Audio
        _audioEnableTts = s.Audio.EnableTts;
        _audioPiperExecutable = s.Audio.PiperExecutable;
        _audioPiperVoice = s.Audio.PiperVoice;
        _audioEnableAsr = s.Audio.EnableAsr;
        _audioFasterWhisperExecutable = s.Audio.FasterWhisperExecutable;

        SaveCommand = new RelayCommand(async () => await _settingsService.SaveAsync().ConfigureAwait(false));
        BrowseLlamaServerCommand = new RelayCommand(BrowseLlamaServer);
        BrowseModelsDirectoryCommand = new RelayCommand(BrowseModelsDirectory);
        BrowseCacheDirectoryCommand = new RelayCommand(BrowseCacheDirectory);
        BrowseActiveModelCommand = new RelayCommand(BrowseActiveModel);
        BrowsePiperExecutableCommand = new RelayCommand(BrowsePiperExecutable);
        BrowseFasterWhisperExecutableCommand = new RelayCommand(BrowseFasterWhisperExecutable);
        BrowseRagDatabaseCommand = new RelayCommand(BrowseRagDatabase);
        BrowseExportedChatsDirectoryCommand = new RelayCommand(BrowseExportedChatsDirectory);

        Categories = new ObservableCollection<string>(new[] { "General", "Global Actions", "Paths", "Orchestrator", "Runners", "Models", "Audio", "RAG", "Training", "Logging", "Advanced" });
        SelectedCategory = "General";
        
        ResetAllCommand = new RelayCommand(async () => await _settingsService.SaveAsync(AppSettings.CreateDefault()).ConfigureAwait(false));
        BrowseExportedChatsDirectoryCommand = new RelayCommand(BrowseExportedChatsDirectory);
        BrowseTrainingWorkingDirectoryCommand = new RelayCommand(BrowseTrainingWorkingDirectory);
        // Keep this VM in sync if settings are changed externally (e.g., reset)
        _settingsService.SettingsChanged += (_, updated) =>
        {
            try { System.Windows.Application.Current.Dispatcher.Invoke(() => SyncFromService(updated)); }
            catch { SyncFromService(updated); }
        };

        RefreshDefaultModelChoices();
        }

    public RelayCommand SaveCommand { get; }
    public RelayCommand BrowseLlamaServerCommand { get; }
    public RelayCommand BrowseModelsDirectoryCommand { get; }
    public RelayCommand BrowseCacheDirectoryCommand { get; }
    public RelayCommand BrowseActiveModelCommand { get; }
    public RelayCommand BrowsePiperExecutableCommand { get; }
    public RelayCommand BrowseFasterWhisperExecutableCommand { get; }
    public RelayCommand BrowseRagDatabaseCommand { get; }
    public RelayCommand ResetAllCommand { get; }
    public RelayCommand BrowseExportedChatsDirectoryCommand { get; }
    public RelayCommand BrowseTrainingWorkingDirectoryCommand { get; }
    

    private string _preferredTheme;
    public string PreferredTheme
    {
        get => _preferredTheme;
        set => SetProperty(ref _preferredTheme, value, OnChangedPersist);
    }

    private string _language;
    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value, OnChangedPersist);
    }

    // UI Font size for preview
    private double _uiFontSize = 13.0;
    public double UiFontSize
    {
        get => _uiFontSize;
        set => SetProperty(ref _uiFontSize, value, OnChangedPersist);
    }

    private bool _checkForUpdatesOnStart;
    public bool CheckForUpdatesOnStart
    {
        get => _checkForUpdatesOnStart;
        set => SetProperty(ref _checkForUpdatesOnStart, value, OnChangedPersist);
    }

    private bool _autoSaveConversations;
    public bool AutoSaveConversations
    {
        get => _autoSaveConversations;
        set
        {
            if (SetProperty(ref _autoSaveConversations, value, OnChangedPersist))
            {
                // Best-effort mirror into DB-backed settings for other layers to consume
                try { _ = _kvSettings?.SetValueAsync("App.AutoSave", value); } catch { }
            }
        }
    }

    private bool _startOrchestratorWithApp;
    public bool StartOrchestratorWithApp
    {
        get => _startOrchestratorWithApp;
        set => SetProperty(ref _startOrchestratorWithApp, value, OnChangedPersist);
    }

    private string _modelsDirectory;
    public string ModelsDirectory
    {
        get => _modelsDirectory;
        set => SetProperty(ref _modelsDirectory, value, OnChangedPersist);
    }
    
    private string _exportedChatsDirectory = string.Empty;
    public string ExportedChatsDirectory
    {
        get => _exportedChatsDirectory;
        set => SetProperty(ref _exportedChatsDirectory, value, OnChangedPersist);
    }

    private string _cacheDirectory;
    public string CacheDirectory
    {
        get => _cacheDirectory;
        set => SetProperty(ref _cacheDirectory, value, OnChangedPersist);
    }

    private string _orchestratorBaseUrl;
    public string OrchestratorBaseUrl
    {
        get => _orchestratorBaseUrl;
        set => SetProperty(ref _orchestratorBaseUrl, value, OnChangedPersist);
    }

    private int _orchestratorStartupTimeoutSec;
    public int OrchestratorStartupTimeoutSec
    {
        get => _orchestratorStartupTimeoutSec;
        set => SetProperty(ref _orchestratorStartupTimeoutSec, value, OnChangedPersist);
    }
    
    private int _orchestratorHealthCheckIntervalSec = 10;
    public int OrchestratorHealthCheckIntervalSec
    {
        get => _orchestratorHealthCheckIntervalSec;
        set => SetProperty(ref _orchestratorHealthCheckIntervalSec, value, OnChangedPersist);
    }
    
    private bool _orchestratorAutoRestartOnCrash;
    public bool OrchestratorAutoRestartOnCrash
    {
        get => _orchestratorAutoRestartOnCrash;
        set => SetProperty(ref _orchestratorAutoRestartOnCrash, value, OnChangedPersist);
    }

    private string _activeRunner;
    public string ActiveRunner
    {
        get => _activeRunner;
        set => SetProperty(ref _activeRunner, value, OnChangedPersist);
    }

    private bool _autoStartLastRunner;
    public bool AutoStartLastRunner
    {
        get => _autoStartLastRunner;
        set => SetProperty(ref _autoStartLastRunner, value, OnChangedPersist);
    }

    private string? _activeModelId;
    public string? ActiveModelId
    {
        get => _activeModelId;
        set => SetProperty(ref _activeModelId, value, OnChangedPersist);
    }

    private string _llamaServerExecutablePath;
    public string LlamaServerExecutablePath
    {
        get => _llamaServerExecutablePath;
        set => SetProperty(ref _llamaServerExecutablePath, value, OnChangedPersist);
    }

    private string _llamaAdditionalArgs;
    public string LlamaAdditionalArgs
    {
        get => _llamaAdditionalArgs;
        set => SetProperty(ref _llamaAdditionalArgs, value, OnChangedPersist);
    }

    private int _llamaPort;
    public int LlamaPort
    {
        get => _llamaPort;
        set => SetProperty(ref _llamaPort, value, OnChangedPersist);
    }

    private int _llamaGpuLayers;
    public int LlamaGpuLayers
    {
        get => _llamaGpuLayers;
        set => SetProperty(ref _llamaGpuLayers, value, OnChangedPersist);
    }

    private bool _llamaUseCuda;
    public bool LlamaUseCuda
    {
        get => _llamaUseCuda;
        set => SetProperty(ref _llamaUseCuda, value, OnChangedPersist);
    }

    // --- Audio ---
    private bool _audioEnableTts;
    public bool AudioEnableTts
    {
        get => _audioEnableTts;
        set => SetProperty(ref _audioEnableTts, value, OnChangedPersist);
    }

    private string _audioPiperExecutable = string.Empty;
    public string AudioPiperExecutable
    {
        get => _audioPiperExecutable;
        set => SetProperty(ref _audioPiperExecutable, value, OnChangedPersist);
    }

    private string _audioPiperVoice = string.Empty;
    public string AudioPiperVoice
    {
        get => _audioPiperVoice;
        set => SetProperty(ref _audioPiperVoice, value, OnChangedPersist);
    }

    private bool _audioEnableAsr;
    public bool AudioEnableAsr
    {
        get => _audioEnableAsr;
        set => SetProperty(ref _audioEnableAsr, value, OnChangedPersist);
    }

    private string _audioFasterWhisperExecutable = string.Empty;
    public string AudioFasterWhisperExecutable
    {
        get => _audioFasterWhisperExecutable;
        set => SetProperty(ref _audioFasterWhisperExecutable, value, OnChangedPersist);
    }

    private bool _audioNoiseSuppression = true;
    public bool AudioNoiseSuppression
    {
        get => _audioNoiseSuppression;
        set => SetProperty(ref _audioNoiseSuppression, value, OnChangedPersist);
    }

    private string _audioQuality = "Balanced";
    public string AudioQuality
    {
        get => _audioQuality;
        set => SetProperty(ref _audioQuality, value, OnChangedPersist);
    }

    private string _audioSpeechRecognition = "Faster-Whisper";
    public string AudioSpeechRecognition
    {
        get => _audioSpeechRecognition;
        set => SetProperty(ref _audioSpeechRecognition, value, OnChangedPersist);
    }

    // --- Logging ---
    private string _loggingLevel = "Information";
    public string LoggingLevel
    {
        get => _loggingLevel;
        set => SetProperty(ref _loggingLevel, value, OnChangedPersist);
    }

    private bool _loggingEnableStructured = true;
    public bool LoggingEnableStructured
    {
        get => _loggingEnableStructured;
        set => SetProperty(ref _loggingEnableStructured, value, OnChangedPersist);
    }

    // --- RAG ---
    private bool _ragEnableVectorStore;
    public bool RagEnableVectorStore
    {
        get => _ragEnableVectorStore;
        set => SetProperty(ref _ragEnableVectorStore, value, OnChangedPersist);
    }

    private string _ragDatabasePath = string.Empty;
    public string RagDatabasePath
    {
        get => _ragDatabasePath;
        set => SetProperty(ref _ragDatabasePath, value, OnChangedPersist);
    }

    private bool _ragUseSQLiteVss;
    public bool RagUseSQLiteVss
    {
        get => _ragUseSQLiteVss;
        set => SetProperty(ref _ragUseSQLiteVss, value, OnChangedPersist);
    }
    
    private int _ragDocumentChunkTokens;
    public int RagDocumentChunkTokens { get => _ragDocumentChunkTokens; set => SetProperty(ref _ragDocumentChunkTokens, value, OnChangedPersist); }
    
    private double _ragSimilarityThreshold;
    public double RagSimilarityThreshold { get => _ragSimilarityThreshold; set => SetProperty(ref _ragSimilarityThreshold, value, OnChangedPersist); }
    
    private string _ragStorageEngine = "SQLite";
    public string RagStorageEngine { get => _ragStorageEngine; set => SetProperty(ref _ragStorageEngine, value, OnChangedPersist); }
    
    // --- Logging extras ---
    private int _logRetentionDays = 7;
    public int LogRetentionDays { get => _logRetentionDays; set => SetProperty(ref _logRetentionDays, value, OnChangedPersist); }
    
    private bool _sendCrashReports;
    public bool SendCrashReports { get => _sendCrashReports; set => SetProperty(ref _sendCrashReports, value, OnChangedPersist); }
    
    // --- Runner limits/global ---
    private int _maxConcurrentTasks = 2;
    public int MaxConcurrentTasks { get => _maxConcurrentTasks; set => SetProperty(ref _maxConcurrentTasks, value, OnChangedPersist); }
    
    private int _llamaMemoryLimitPercent = 100;
    public int LlamaMemoryLimitPercent { get => _llamaMemoryLimitPercent; set => SetProperty(ref _llamaMemoryLimitPercent, value, OnChangedPersist); }
    
    // --- Advanced ---
    private bool _experimentalFeatures;
    public bool ExperimentalFeatures { get => _experimentalFeatures; set => SetProperty(ref _experimentalFeatures, value, OnChangedPersist); }
    
    private int _memoryLimitMb;
    public int MemoryLimitMb { get => _memoryLimitMb; set => SetProperty(ref _memoryLimitMb, value, OnChangedPersist); }
    
    private string? _networkProxy;
    public string? NetworkProxy { get => _networkProxy; set => SetProperty(ref _networkProxy, value, OnChangedPersist); }
    
    private bool _developerMode;
    public bool DeveloperMode { get => _developerMode; set => SetProperty(ref _developerMode, value, OnChangedPersist); }

    // --- Training ---
    private string _trainingDefaultTrainer = "llama-factory";
    public string TrainingDefaultTrainer { get => _trainingDefaultTrainer; set => SetProperty(ref _trainingDefaultTrainer, value, OnChangedPersist); }
    private string _trainingWorkingDirectory = string.Empty;
    public string TrainingWorkingDirectory { get => _trainingWorkingDirectory; set => SetProperty(ref _trainingWorkingDirectory, value, OnChangedPersist); }
    private int _trainingCheckpointIntervalMinutes = 15;
    public int TrainingCheckpointIntervalMinutes { get => _trainingCheckpointIntervalMinutes; set => SetProperty(ref _trainingCheckpointIntervalMinutes, value, OnChangedPersist); }
    private int _trainingDataFractionPercent = 100;
    public int TrainingDataFractionPercent { get => _trainingDataFractionPercent; set => SetProperty(ref _trainingDataFractionPercent, value, OnChangedPersist); }
    private double _trainingLearningRate = 0.0003;
    public double TrainingLearningRate { get => _trainingLearningRate; set => SetProperty(ref _trainingLearningRate, value, OnChangedPersist); }

    private void SyncFromService(AppSettings s)
    {
        // General
        _preferredTheme = s.PreferredTheme ?? "Dark"; OnPropertyChanged(nameof(PreferredTheme));
        _language = s.Language ?? "en-US"; OnPropertyChanged(nameof(Language));
        _checkForUpdatesOnStart = s.CheckForUpdatesOnStart; OnPropertyChanged(nameof(CheckForUpdatesOnStart));
        _startOrchestratorWithApp = s.StartOrchestratorWithApp; OnPropertyChanged(nameof(StartOrchestratorWithApp));
        _autoStartLastRunner = s.AutoStartLastRunner; OnPropertyChanged(nameof(AutoStartLastRunner));
        _autoSaveConversations = s.AutoSaveConversations; OnPropertyChanged(nameof(AutoSaveConversations));
        _uiFontSize = s.Ui.FontSize; OnPropertyChanged(nameof(UiFontSize));
        _autoSaveConversations = s.AutoSaveConversations; OnPropertyChanged(nameof(AutoSaveConversations));

        // Paths
        _modelsDirectory = s.ModelsDirectory; OnPropertyChanged(nameof(ModelsDirectory));
        _exportedChatsDirectory = s.ExportedChatsDirectory; OnPropertyChanged(nameof(ExportedChatsDirectory));
        _cacheDirectory = s.CacheDirectory; OnPropertyChanged(nameof(CacheDirectory));

        // Orchestrator
        _orchestratorBaseUrl = s.OrchestratorBaseUrl; OnPropertyChanged(nameof(OrchestratorBaseUrl));
        _orchestratorStartupTimeoutSec = s.OrchestratorStartupTimeoutSec; OnPropertyChanged(nameof(OrchestratorStartupTimeoutSec));
        _orchestratorHealthCheckIntervalSec = s.OrchestratorHealthCheckIntervalSec; OnPropertyChanged(nameof(OrchestratorHealthCheckIntervalSec));
        _orchestratorAutoRestartOnCrash = s.OrchestratorAutoRestartOnCrash; OnPropertyChanged(nameof(OrchestratorAutoRestartOnCrash));

        // Runner
        _activeRunner = s.ActiveRunner; OnPropertyChanged(nameof(ActiveRunner));
        _activeModelId = string.IsNullOrWhiteSpace(s.ActiveModelId) ? null : s.ActiveModelId; OnPropertyChanged(nameof(ActiveModelId));
        _llamaServerExecutablePath = s.LlamaCpp.ServerExecutablePath; OnPropertyChanged(nameof(LlamaServerExecutablePath));
        _llamaAdditionalArgs = s.LlamaCpp.AdditionalArgs; OnPropertyChanged(nameof(LlamaAdditionalArgs));
        _llamaPort = s.LlamaCpp.Port; OnPropertyChanged(nameof(LlamaPort));
        _llamaGpuLayers = s.LlamaCpp.GpuLayers; OnPropertyChanged(nameof(LlamaGpuLayers));
        _llamaUseCuda = s.LlamaCpp.UseCuda; OnPropertyChanged(nameof(LlamaUseCuda));

        // Audio
        _audioEnableTts = s.Audio.EnableTts; OnPropertyChanged(nameof(AudioEnableTts));
        _audioPiperExecutable = s.Audio.PiperExecutable; OnPropertyChanged(nameof(AudioPiperExecutable));
        _audioPiperVoice = s.Audio.PiperVoice; OnPropertyChanged(nameof(AudioPiperVoice));
        _audioEnableAsr = s.Audio.EnableAsr; OnPropertyChanged(nameof(AudioEnableAsr));
        _audioFasterWhisperExecutable = s.Audio.FasterWhisperExecutable; OnPropertyChanged(nameof(AudioFasterWhisperExecutable));

        // Logging
        _loggingLevel = s.Logging.Level; OnPropertyChanged(nameof(LoggingLevel));
        _loggingEnableStructured = s.Logging.EnableStructured; OnPropertyChanged(nameof(LoggingEnableStructured));

        // RAG
        _ragEnableVectorStore = s.Rag.EnableVectorStore; OnPropertyChanged(nameof(RagEnableVectorStore));
        _ragDatabasePath = s.Rag.DatabasePath; OnPropertyChanged(nameof(RagDatabasePath));
        _ragUseSQLiteVss = s.Rag.UseSQLiteVss; OnPropertyChanged(nameof(RagUseSQLiteVss));
        _ragDocumentChunkTokens = s.Rag.DocumentChunkTokens; OnPropertyChanged(nameof(RagDocumentChunkTokens));
        _ragSimilarityThreshold = s.Rag.SimilarityThreshold; OnPropertyChanged(nameof(RagSimilarityThreshold));
        _ragStorageEngine = s.Rag.StorageEngine; OnPropertyChanged(nameof(RagStorageEngine));

        _logRetentionDays = s.Logging.RetentionDays; OnPropertyChanged(nameof(LogRetentionDays));
        _sendCrashReports = s.Logging.SendCrashReports; OnPropertyChanged(nameof(SendCrashReports));

        _maxConcurrentTasks = s.MaxConcurrentTasks; OnPropertyChanged(nameof(MaxConcurrentTasks));
        _llamaMemoryLimitPercent = s.LlamaCpp.MemoryLimitPercent; OnPropertyChanged(nameof(LlamaMemoryLimitPercent));

        _experimentalFeatures = s.ExperimentalFeatures; OnPropertyChanged(nameof(ExperimentalFeatures));
        _memoryLimitMb = s.MemoryLimitMb; OnPropertyChanged(nameof(MemoryLimitMb));
        _networkProxy = s.NetworkProxy; OnPropertyChanged(nameof(NetworkProxy));
        _developerMode = s.DeveloperMode; OnPropertyChanged(nameof(DeveloperMode));

        _trainingDefaultTrainer = s.Training.DefaultTrainer; OnPropertyChanged(nameof(TrainingDefaultTrainer));
        _trainingWorkingDirectory = s.Training.WorkingDirectory; OnPropertyChanged(nameof(TrainingWorkingDirectory));
        _trainingCheckpointIntervalMinutes = s.Training.CheckpointIntervalMinutes; OnPropertyChanged(nameof(TrainingCheckpointIntervalMinutes));
        _trainingDataFractionPercent = s.Training.DataFractionPercent; OnPropertyChanged(nameof(TrainingDataFractionPercent));
        _trainingLearningRate = s.Training.LearningRate; OnPropertyChanged(nameof(TrainingLearningRate));
    }

    // --- Default model selection (for plain-English General page) ---
    public sealed record ModelChoice(string Name, string Path);
    public ObservableCollection<ModelChoice> DefaultModelChoices { get; } = new();
    private void RefreshDefaultModelChoices()
    {
        DefaultModelChoices.Clear();
        try
        {
            var inv = _inventory?.Scan();
            if (inv?.BaseModels is not null)
            {
                foreach (var m in inv.BaseModels)
                    DefaultModelChoices.Add(new ModelChoice(m.DisplayName, m.FilePath));
            }
        }
        catch { }
        OnPropertyChanged(nameof(DefaultModelChoices));
    }

    private void OnChangedPersist()
    {
        // Push values into the settings service and schedule a save.
        var s = _settingsService.Current;
        s.PreferredTheme = PreferredTheme;
        s.Language = Language;
        s.CheckForUpdatesOnStart = CheckForUpdatesOnStart;
        s.StartOrchestratorWithApp = StartOrchestratorWithApp;
        s.AutoSaveConversations = AutoSaveConversations;
        s.ModelsDirectory = ModelsDirectory;
        s.ExportedChatsDirectory = ExportedChatsDirectory;
        s.CacheDirectory = CacheDirectory;
        s.OrchestratorBaseUrl = OrchestratorBaseUrl;
        s.OrchestratorStartupTimeoutSec = OrchestratorStartupTimeoutSec;
        s.OrchestratorHealthCheckIntervalSec = OrchestratorHealthCheckIntervalSec;
        s.OrchestratorAutoRestartOnCrash = OrchestratorAutoRestartOnCrash;
        s.ActiveRunner = ActiveRunner;
        s.AutoStartLastRunner = AutoStartLastRunner;
        s.ActiveModelId = string.IsNullOrWhiteSpace(ActiveModelId) ? null : ActiveModelId;
        s.LlamaCpp.ServerExecutablePath = LlamaServerExecutablePath;
        s.LlamaCpp.AdditionalArgs = LlamaAdditionalArgs ?? string.Empty;
        s.LlamaCpp.Port = LlamaPort;
        s.LlamaCpp.GpuLayers = LlamaGpuLayers;
        s.LlamaCpp.UseCuda = LlamaUseCuda;
        s.LlamaCpp.MemoryLimitPercent = LlamaMemoryLimitPercent;
        s.Audio.EnableTts = AudioEnableTts;
        s.Audio.PiperExecutable = AudioPiperExecutable;
        s.Audio.PiperVoice = AudioPiperVoice;
        s.Audio.EnableAsr = AudioEnableAsr;
        s.Audio.FasterWhisperExecutable = AudioFasterWhisperExecutable;
        s.Audio.NoiseSuppression = AudioNoiseSuppression;
        s.Audio.Quality = AudioQuality;
        s.Audio.SpeechRecognition = AudioSpeechRecognition;
        s.Rag.EnableVectorStore = RagEnableVectorStore;
        s.Rag.DatabasePath = RagDatabasePath;
        s.Rag.UseSQLiteVss = RagUseSQLiteVss;
        s.Rag.DocumentChunkTokens = RagDocumentChunkTokens;
        s.Rag.SimilarityThreshold = RagSimilarityThreshold;
        s.Rag.StorageEngine = RagStorageEngine;
        s.Ui.FontSize = UiFontSize;
        s.Logging.Level = LoggingLevel;
        s.Logging.EnableStructured = LoggingEnableStructured;
        s.Logging.RetentionDays = LogRetentionDays;
        s.Logging.SendCrashReports = SendCrashReports;
        s.MaxConcurrentTasks = MaxConcurrentTasks;
        s.ExperimentalFeatures = ExperimentalFeatures;
        s.MemoryLimitMb = MemoryLimitMb;
        s.NetworkProxy = NetworkProxy;
        s.DeveloperMode = DeveloperMode;
        s.Training.DefaultTrainer = TrainingDefaultTrainer;
        s.Training.WorkingDirectory = TrainingWorkingDirectory;
        s.Training.CheckpointIntervalMinutes = TrainingCheckpointIntervalMinutes;
        s.Training.DataFractionPercent = TrainingDataFractionPercent;
        s.Training.LearningRate = TrainingLearningRate;
        _ = _settingsService.SaveAsync();
    }

    public ObservableCollection<string> Categories { get; }
    private string _selectedCategory = "General";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }
    private void BrowseLlamaServer()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select llama-server.exe",
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = System.IO.Path.GetFileName(LlamaServerExecutablePath),
            InitialDirectory = TryInitialDir(LlamaServerExecutablePath)
        };
        if (dlg.ShowDialog() == true)
        {
            LlamaServerExecutablePath = dlg.FileName;
        }
    }

    private void BrowseModelsDirectory()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Models folder",
            CheckFileExists = false,
            CheckPathExists = true,
            ValidateNames = false,
            FileName = "Select Folder"
        };
        if (dlg.ShowDialog() == true)
        {
            var dir = System.IO.Path.GetDirectoryName(dlg.FileName);
            if (!string.IsNullOrWhiteSpace(dir)) ModelsDirectory = dir!;
        }
    }

    private void BrowseCacheDirectory()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Cache folder",
            CheckFileExists = false,
            CheckPathExists = true,
            ValidateNames = false,
            FileName = "Select Folder"
        };
        if (dlg.ShowDialog() == true)
        {
            var dir = System.IO.Path.GetDirectoryName(dlg.FileName);
            if (!string.IsNullOrWhiteSpace(dir)) CacheDirectory = dir!;
        }
    }

    private void BrowseExportedChatsDirectory()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Exported Chats folder",
            CheckFileExists = false,
            CheckPathExists = true,
            ValidateNames = false,
            FileName = "Select Folder"
        };
        if (dlg.ShowDialog() == true)
        {
            var dir = System.IO.Path.GetDirectoryName(dlg.FileName);
            if (!string.IsNullOrWhiteSpace(dir)) ExportedChatsDirectory = dir!;
        }
    }

    private void BrowsePiperExecutable()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select piper.exe",
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = System.IO.Path.GetFileName(AudioPiperExecutable),
            InitialDirectory = TryInitialDir(AudioPiperExecutable)
        };
        if (dlg.ShowDialog() == true)
        {
            AudioPiperExecutable = dlg.FileName;
        }
    }

    private void BrowseFasterWhisperExecutable()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select faster-whisper.exe",
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = System.IO.Path.GetFileName(AudioFasterWhisperExecutable),
            InitialDirectory = TryInitialDir(AudioFasterWhisperExecutable)
        };
        if (dlg.ShowDialog() == true)
        {
            AudioFasterWhisperExecutable = dlg.FileName;
        }
    }

    private void BrowseRagDatabase()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select RAG Database (*.db)",
            Filter = "SQLite DB (*.db;*.sqlite)|*.db;*.sqlite|All files (*.*)|*.*",
            CheckPathExists = true,
            CheckFileExists = false,
            ValidateNames = false,
            FileName = System.IO.Path.GetFileName(RagDatabasePath)
        };
        if (dlg.ShowDialog() == true)
        {
            var chosen = dlg.FileName;
            if (!string.IsNullOrWhiteSpace(chosen)) RagDatabasePath = chosen;
        }
    }

    private void BrowseActiveModel()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Model File",
            Filter = "Models (*.gguf;*.safetensors)|*.gguf;*.safetensors|All files (*.*)|*.*",
            CheckFileExists = true,
            CheckPathExists = true
        };
        if (dlg.ShowDialog() == true)
        {
            ActiveModelId = dlg.FileName;
        }
    }

    private void BrowseTrainingWorkingDirectory()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select Training Working folder",
            CheckFileExists = false,
            CheckPathExists = true,
            ValidateNames = false,
            FileName = "Select Folder"
        };
        if (dlg.ShowDialog() == true)
        {
            var dir = System.IO.Path.GetDirectoryName(dlg.FileName);
            if (!string.IsNullOrWhiteSpace(dir)) TrainingWorkingDirectory = dir!;
        }
    }

    private static string TryInitialDir(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                var dir = System.IO.Path.GetDirectoryName(ExpandEnv(path));
                if (!string.IsNullOrWhiteSpace(dir) && System.IO.Directory.Exists(dir)) return dir!;
            }
        }
        catch { }
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private static string ExpandEnv(string path) =>
        string.IsNullOrWhiteSpace(path) ? path : Environment.ExpandEnvironmentVariables(path);
}
