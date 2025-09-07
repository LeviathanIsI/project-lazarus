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
    // private readonly Services.IOrchestratorClient? _orchestratorClient;

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            // Initialize from current settings
            var s = _settingsService.Current;
        _preferredTheme = s.PreferredTheme ?? "Dark";
        _language = s.Language ?? "en-US";
        _checkForUpdatesOnStart = s.CheckForUpdatesOnStart;
        _modelsDirectory = s.ModelsDirectory;
        _cacheDirectory = s.CacheDirectory;
        _orchestratorBaseUrl = s.OrchestratorBaseUrl;
        _orchestratorStartupTimeoutSec = s.OrchestratorStartupTimeoutSec;
        _activeRunner = s.ActiveRunner;
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

        Categories = new ObservableCollection<string>(new[] { "General", "Paths", "Orchestrator", "Runners", "Models", "Audio", "RAG" });
        SelectedCategory = "General";
        }

    public RelayCommand SaveCommand { get; }
    public RelayCommand BrowseLlamaServerCommand { get; }
    public RelayCommand BrowseModelsDirectoryCommand { get; }
    public RelayCommand BrowseCacheDirectoryCommand { get; }
    public RelayCommand BrowseActiveModelCommand { get; }
    public RelayCommand BrowsePiperExecutableCommand { get; }
    public RelayCommand BrowseFasterWhisperExecutableCommand { get; }
    public RelayCommand BrowseRagDatabaseCommand { get; }
    

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

    private bool _checkForUpdatesOnStart;
    public bool CheckForUpdatesOnStart
    {
        get => _checkForUpdatesOnStart;
        set => SetProperty(ref _checkForUpdatesOnStart, value, OnChangedPersist);
    }

    private string _modelsDirectory;
    public string ModelsDirectory
    {
        get => _modelsDirectory;
        set => SetProperty(ref _modelsDirectory, value, OnChangedPersist);
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

    private string _activeRunner;
    public string ActiveRunner
    {
        get => _activeRunner;
        set => SetProperty(ref _activeRunner, value, OnChangedPersist);
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

    private void OnChangedPersist()
    {
        // Push values into the settings service and schedule a save.
        var s = _settingsService.Current;
        s.PreferredTheme = PreferredTheme;
        s.Language = Language;
        s.CheckForUpdatesOnStart = CheckForUpdatesOnStart;
        s.ModelsDirectory = ModelsDirectory;
        s.CacheDirectory = CacheDirectory;
        s.OrchestratorBaseUrl = OrchestratorBaseUrl;
        s.OrchestratorStartupTimeoutSec = OrchestratorStartupTimeoutSec;
        s.ActiveRunner = ActiveRunner;
        s.ActiveModelId = string.IsNullOrWhiteSpace(ActiveModelId) ? null : ActiveModelId;
        s.LlamaCpp.ServerExecutablePath = LlamaServerExecutablePath;
        s.LlamaCpp.AdditionalArgs = LlamaAdditionalArgs ?? string.Empty;
        s.LlamaCpp.Port = LlamaPort;
        s.LlamaCpp.GpuLayers = LlamaGpuLayers;
        s.LlamaCpp.UseCuda = LlamaUseCuda;
        s.Audio.EnableTts = AudioEnableTts;
        s.Audio.PiperExecutable = AudioPiperExecutable;
        s.Audio.PiperVoice = AudioPiperVoice;
        s.Audio.EnableAsr = AudioEnableAsr;
        s.Audio.FasterWhisperExecutable = AudioFasterWhisperExecutable;
        s.Rag.EnableVectorStore = RagEnableVectorStore;
        s.Rag.DatabasePath = RagDatabasePath;
        s.Rag.UseSQLiteVss = RagUseSQLiteVss;
        s.Logging.Level = LoggingLevel;
        s.Logging.EnableStructured = LoggingEnableStructured;
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
