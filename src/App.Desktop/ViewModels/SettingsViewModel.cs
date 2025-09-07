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

        SaveCommand = new RelayCommand(async () => await _settingsService.SaveAsync().ConfigureAwait(false));
        BrowseLlamaServerCommand = new RelayCommand(BrowseLlamaServer);
        BrowseModelsDirectoryCommand = new RelayCommand(BrowseModelsDirectory);
        BrowseCacheDirectoryCommand = new RelayCommand(BrowseCacheDirectory);

        Categories = new ObservableCollection<string>(new[] { "General", "Paths", "Orchestrator", "Runner" });
        SelectedCategory = "General";
        }

    public RelayCommand SaveCommand { get; }
    public RelayCommand BrowseLlamaServerCommand { get; }
    public RelayCommand BrowseModelsDirectoryCommand { get; }
    public RelayCommand BrowseCacheDirectoryCommand { get; }
    

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
        s.LlamaCpp.ServerExecutablePath = LlamaServerExecutablePath;
        s.LlamaCpp.AdditionalArgs = LlamaAdditionalArgs ?? string.Empty;
        s.LlamaCpp.Port = LlamaPort;
        s.LlamaCpp.GpuLayers = LlamaGpuLayers;
        s.LlamaCpp.UseCuda = LlamaUseCuda;
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
