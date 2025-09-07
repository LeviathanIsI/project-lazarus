using System;
using System.IO;
using System.Threading.Tasks;
using Lazarus.Shared.Settings;

namespace Lazarus.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Settings view. Wraps AppSettings for editing and persistence.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

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
        _llamaBinaryDir = s.LlamaCpp.BinaryDir ?? string.Empty;
        _llamaDefaultPort = s.LlamaCpp.DefaultPort;
        _llamaStartupTimeoutSec = s.LlamaCpp.StartupTimeoutSec;

        SaveCommand = new RelayCommand(async () => await _settingsService.SaveAsync().ConfigureAwait(false));
        ResetCommand = new RelayCommand(async () => await _settingsService.ResetToDefaultsAsync().ConfigureAwait(false));
        ImportCommand = new RelayCommand<string?>(async path =>
        {
            if (!string.IsNullOrWhiteSpace(path))
                await _settingsService.ImportAsync(path!).ConfigureAwait(false);
        });
        ExportCommand = new RelayCommand<string?>(async path =>
        {
            if (!string.IsNullOrWhiteSpace(path))
                await _settingsService.ExportAsync(path!).ConfigureAwait(false);
        });
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand<string?> ImportCommand { get; }
    public RelayCommand<string?> ExportCommand { get; }

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

    private string _llamaBinaryDir;
    public string LlamaBinaryDir
    {
        get => _llamaBinaryDir;
        set => SetProperty(ref _llamaBinaryDir, value, OnChangedPersist);
    }

    private int _llamaDefaultPort;
    public int LlamaDefaultPort
    {
        get => _llamaDefaultPort;
        set => SetProperty(ref _llamaDefaultPort, value, OnChangedPersist);
    }

    private int _llamaStartupTimeoutSec;
    public int LlamaStartupTimeoutSec
    {
        get => _llamaStartupTimeoutSec;
        set => SetProperty(ref _llamaStartupTimeoutSec, value, OnChangedPersist);
    }

    private void OnChangedPersist()
    {
        // Push values into the settings service and schedule a save.
        _settingsService.Update(s =>
        {
            s.PreferredTheme = PreferredTheme;
            s.Language = Language;
            s.CheckForUpdatesOnStart = CheckForUpdatesOnStart;
            s.ModelsDirectory = ModelsDirectory;
            s.CacheDirectory = CacheDirectory;
            s.OrchestratorBaseUrl = OrchestratorBaseUrl;
            s.OrchestratorStartupTimeoutSec = OrchestratorStartupTimeoutSec;
            s.ActiveRunner = ActiveRunner;
            s.LlamaCpp.BinaryDir = string.IsNullOrWhiteSpace(LlamaBinaryDir) ? null : LlamaBinaryDir;
            s.LlamaCpp.DefaultPort = LlamaDefaultPort;
            s.LlamaCpp.StartupTimeoutSec = LlamaStartupTimeoutSec;
        });
    }
}

