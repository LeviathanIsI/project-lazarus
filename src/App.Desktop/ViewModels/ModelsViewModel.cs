using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.IO;
using System.Windows.Threading;
using Lazarus.Shared;
using Lazarus.Shared.Settings;
using Lazarus.Backend.Services;
using Lazarus.Backend.Adapters;
using Lazarus.Desktop.Extensions;
using Lazarus.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.ViewModels;

public sealed class ModelsViewModel : ViewModelBase
{
    public sealed record RunnerCandidate(string Engine, string DisplayName, string ResolvedPath, string Entrypoint);
    private readonly IModelInventoryService _inventory;
    private readonly IModelPresetService _presets;
    private readonly ILogger<ModelsViewModel> _logger;
    private readonly IOrchestratorRunnerClient _runnerClient;
    private readonly IOrchestratorClient _orchestratorClient;
    private readonly IAppState _appState;
    private readonly ISettingsService _settingsService;
    private readonly Dispatcher _ui;
    private readonly LoraWatcher _watcher;
    private string? _initialRunnerPath;
    private string? _initialRunnerEngine;
    private string? _initialModelPath;
    private string? _initialLoraPath;

    public ModelsViewModel(
        IModelInventoryService inventory,
        IModelPresetService presets,
        ILogger<ModelsViewModel> logger,
        Lazarus.Desktop.Services.IOrchestratorRunnerClient runnerClient,
        IOrchestratorClient orchestratorClient,
        IAppState appState,
        ISettingsService settingsService)
    {
        _inventory = inventory;
        _presets = presets;
        _logger = logger;
        _runnerClient = runnerClient;
        _orchestratorClient = orchestratorClient;
        _appState = appState;
        _settingsService = settingsService;
        _ui = Dispatcher.CurrentDispatcher;
        _watcher = new LoraWatcher();
        _watcher.Changed += (_, __) => RefreshAdapters();
        // Ensure preset folder exists for smooth UX
        _presets.EnsureFolders();

        LoadInitialStateFromSettings();

        // Observe global app state so the view can reflect loaded adapters immediately
        _appState.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(LoadedLoraPath));
            OnPropertyChanged(nameof(LoadedLoraDisplayName));
            OnPropertyChanged(nameof(LoadedTokenizerPath));
            OnPropertyChanged(nameof(LoadedEmbeddingPath));
            OnPropertyChanged(nameof(IsLoraLoaded));
            OnPropertyChanged(nameof(LoraScaleValue));
        };

        RefreshCommand = new RelayCommand(Refresh, () => !IsDisposed);
        LoadSelectedModelCommand = new RelayCommand(
            async () => await LoadSelectedModelAsync(),
            () => SelectedModel is not null && SelectedRunner is not null && _orchestratorClient.IsHealthy && !IsDisposed && !IsLoadingModel);
        UnloadRunnerCommand = new RelayCommand(async () => await UnloadRunnerAsync(), () => !IsDisposed);
        RefreshRunnerStatusCommand = new RelayCommand(async () => { await RefreshRunnerStatusAsync(); await RefreshRunnersCatalogAsync(); }, () => !IsDisposed);
        SavePresetCommand = new RelayCommand(SavePreset, CanSave);
        LoadPresetCommand = new RelayCommand(LoadPreset, () => SelectedPresetName is not null && !IsDisposed);
        LoadTokenizerCommand = new RelayCommand(LoadTokenizer, () => SelectedTokenizer is not null);
        UnloadTokenizerCommand = new RelayCommand(UnloadTokenizer, () => _appState.LoadedTokenizer != null);
        LoadEmbeddingCommand = new RelayCommand(LoadEmbedding, () => SelectedEmbedding is not null);
        UnloadEmbeddingCommand = new RelayCommand(UnloadEmbedding, () => _appState.LoadedEmbedding != null);
        LoadLoraCommand = new RelayCommand(LoadLora, () => SelectedLora is not null && !IsLoadingModel);
        UnloadLoraCommand = new RelayCommand(UnloadLora, () => _appState.LoadedLora != null && !IsLoadingModel);
        VerifyAdapterCommand = new RelayCommand(VerifyAdapter, () => SelectedLora is not null && SelectedModel is not null);

        Refresh();
        RefreshAdapters();
        _ = RefreshRunnersCatalogAsync();
        _ = RefreshRunnerStatusAsync();

        // Observe orchestrator health to update enablement and messaging
        _orchestratorClient.HealthStatusChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(IsOrchestratorHealthy));
            OnPropertyChanged(nameof(LoadDisabledReason));
            OnPropertyChanged(nameof(ShowRunnerHint));
            LoadSelectedModelCommand.RaiseCanExecuteChanged();
        };
    }

    // Collections (bound to dropdowns/lists)
    public ObservableCollection<BaseModelInfo> BaseModels { get; } = new();
    public ObservableCollection<LoraOption> LoraAdapters { get; } = new();
    public ObservableCollection<AdapterInfo> Loras { get; } = new();
    public ObservableCollection<TokenizerInfo> Tokenizers { get; } = new();
    public ObservableCollection<EmbeddingInfo> Embeddings { get; } = new();
    public ObservableCollection<string> PresetNames { get; } = new();

    // Selections
    private BaseModelInfo? _selectedModel;
    public BaseModelInfo? SelectedModel
    {
        get => _selectedModel;
        set
        {
            _selectedModel = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(VisibleRunnerCatalog));
            OnPropertyChanged(nameof(LoadDisabledReason));
            OnPropertyChanged(nameof(ShowRunnerHint));

            // If current runner cannot load this model, clear selection
            if (_selectedModel is not null && _selectedRunner is not null && !IsCompatible(_selectedModel, _selectedRunner))
            {
                SelectedRunner = null;
            }

            // Refresh LoRA adapters to show only compatible ones
            RefreshAdapters();

            LoadSelectedModelCommand.RaiseCanExecuteChanged();
        }
    }

    // Selected single LoRA adapter for now
    private LoraOption? _selectedLora;
    public LoraOption? SelectedLora { get => _selectedLora; set => SetProperty(ref _selectedLora, value); }

    private TokenizerInfo? _selectedTokenizer;
    public TokenizerInfo? SelectedTokenizer
    {
        get => _selectedTokenizer;
        set => SetProperty(ref _selectedTokenizer, value);
    }

    private EmbeddingInfo? _selectedEmbedding;
    public EmbeddingInfo? SelectedEmbedding
    {
        get => _selectedEmbedding;
        set => SetProperty(ref _selectedEmbedding, value);
    }

    // Parameters
    private double _temperature = ModelParams.Default.Temperature;
    public double Temperature { get => _temperature; set => SetProperty(ref _temperature, Clamp(value, Schema.Temperature)); }

    private double _topP = ModelParams.Default.TopP;
    public double TopP { get => _topP; set => SetProperty(ref _topP, Clamp(value, Schema.TopP)); }

    private int _topK = 40; // Default TopK value
    public int TopK { get => _topK; set => SetProperty(ref _topK, Math.Max(1, value)); }

    private int _maxTokens = ModelParams.Default.MaxTokens;
    public int MaxTokens { get => _maxTokens; set { _maxTokens = Clamp(value, Schema.MaxTokens); OnPropertyChanged(); } }

    private double _repeat = ModelParams.Default.RepeatPenalty;
    public double RepeatPenalty { get => _repeat; set { _repeat = Clamp(value, Schema.RepetitionPenalty); OnPropertyChanged(); OnPropertyChanged(nameof(RepetitionPenalty)); } }

    // Back-compat property name
    public double RepetitionPenalty { get => RepeatPenalty; set { RepeatPenalty = value; } }

    private int _mirostat = ModelParams.Default.Mirostat;
    public int Mirostat { get => _mirostat; set => SetProperty(ref _mirostat, Math.Max(0, Math.Min(2, value))); }

    private int? _seed = null;
    public int? Seed { get => _seed; set => SetProperty(ref _seed, value); }

    private double _presencePenalty = 0.0;
    public double PresencePenalty { get => _presencePenalty; set => SetProperty(ref _presencePenalty, value); }

    private double _frequencyPenalty = 0.0;
    public double FrequencyPenalty { get => _frequencyPenalty; set => SetProperty(ref _frequencyPenalty, value); }

    private int? _contextWindow = null;
    public int? ContextWindow { get => _contextWindow; set => SetProperty(ref _contextWindow, value); }

    // Presets UI
    // Presets
    private string? _presetName;
    public string? NewPresetName
    {
        get => _presetName;
        set
        {
            _presetName = value;
            OnPropertyChanged();
            SavePresetCommand.RaiseCanExecuteChanged();
        }
    }

    private string? _selectedPresetName;
    public string? SelectedPresetName
    {
        get => _selectedPresetName;
        set
        {
            _selectedPresetName = value;
            OnPropertyChanged();
            LoadPresetCommand.RaiseCanExecuteChanged();
        }
    }

    public RelayCommand RefreshCommand { get; }
    public RelayCommand SavePresetCommand { get; }
    public RelayCommand LoadPresetCommand { get; }
    public RelayCommand LoadSelectedModelCommand { get; }
    public RelayCommand UnloadRunnerCommand { get; }
    public RelayCommand RefreshRunnerStatusCommand { get; }
    public RelayCommand LoadTokenizerCommand { get; }
    public RelayCommand UnloadTokenizerCommand { get; }
    public RelayCommand LoadEmbeddingCommand { get; }
    public RelayCommand UnloadEmbeddingCommand { get; }
    public RelayCommand LoadLoraCommand { get; }
    public RelayCommand UnloadLoraCommand { get; }
    public RelayCommand VerifyAdapterCommand { get; }

    public ObservableCollection<RunnerCandidate> RunnerCatalog { get; } = new();
    private RunnerCandidate? _selectedRunner;
    public RunnerCandidate? SelectedRunner
    {
        get => _selectedRunner;
        set
        {
            _selectedRunner = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(VisibleBaseModels));
            OnPropertyChanged(nameof(LoadDisabledReason));

            // If selected model is incompatible with this runner, clear the model
            if (_selectedRunner is not null && _selectedModel is not null && !IsCompatible(_selectedModel, _selectedRunner))
            {
                SelectedModel = null;
            }

            // Save runner selection to settings
            if (_selectedRunner is not null)
            {
                _ = SaveRunnerStateAsync(_selectedRunner);
            }

            LoadSelectedModelCommand.RaiseCanExecuteChanged();
        }
    }

    private bool _isRunnerRunning;
    public bool IsRunnerRunning { get => _isRunnerRunning; private set => SetProperty(ref _isRunnerRunning, value); }

    private string? _runnerModelPath;
    public string? RunnerModelPath { get => _runnerModelPath; private set => SetProperty(ref _runnerModelPath, value); }

    private int? _runnerPid;
    public int? RunnerPid { get => _runnerPid; private set => SetProperty(ref _runnerPid, value); }

    private string? _runnerStatusMessage;
    public string? RunnerStatusMessage { get => _runnerStatusMessage; private set => SetProperty(ref _runnerStatusMessage, value); }

    private string? _runnerExePath;
    public string? RunnerExePath { get => _runnerExePath; private set => SetProperty(ref _runnerExePath, value); }

    private int? _runnerPort;
    public int? RunnerPort { get => _runnerPort; private set => SetProperty(ref _runnerPort, value); }

    private string? _runnerErrLog;
    public string? RunnerErrLog { get => _runnerErrLog; private set => SetProperty(ref _runnerErrLog, value); }

    private string? _runnerOutLog;
    public string? RunnerOutLog { get => _runnerOutLog; private set => SetProperty(ref _runnerOutLog, value); }

    private int? _lorasApplied;
    public int? LorasApplied { get => _lorasApplied; private set => SetProperty(ref _lorasApplied, value); }

    private string? _launchArgs;
    public string? LaunchArgs { get => _launchArgs; private set => SetProperty(ref _launchArgs, value); }

    private string? _cmdPath;
    public string? CmdPath { get => _cmdPath; private set => SetProperty(ref _cmdPath, value); }

    private int? _loraEvidenceCount;
    public int? LoraEvidenceCount { get => _loraEvidenceCount; private set => SetProperty(ref _loraEvidenceCount, value); }

    private bool _isLoadingModel;
    public bool IsLoadingModel
    {
        get => _isLoadingModel;
        private set
        {
            if (SetProperty(ref _isLoadingModel, value))
            {
                LoadSelectedModelCommand.RaiseCanExecuteChanged();
                LoadLoraCommand.RaiseCanExecuteChanged();
                UnloadLoraCommand.RaiseCanExecuteChanged();
            }
        }
    }
    private bool _isLoadingLora;
    public bool IsLoadingLora { get => _isLoadingLora; private set => SetProperty(ref _isLoadingLora, value); }

    // Adapter load state (proxy to global AppState)
    public string? LoadedLoraPath => _appState.LoadedLora;
    public string? LoadedLoraDisplayName => GetLoadedLoraDisplayName();
    public string? LoadedTokenizerPath => _appState.LoadedTokenizer;
    public string? LoadedEmbeddingPath => _appState.LoadedEmbedding;
    public bool IsLoraLoaded => !string.IsNullOrWhiteSpace(_appState.LoadedLora);

    // LoRA influence (exposed as a clamped double for slider binding)
    private const double LoraMin = 0.0;
    private const double LoraMax = 1.0;
    private const double LoraDefault = 0.7;
    public double LoraScaleValue
    {
        get => Math.Clamp(_appState.LoraScale ?? LoraDefault, LoraMin, LoraMax);
        set
        {
            var clamped = Math.Clamp(value, LoraMin, LoraMax);
            if (_appState.LoraScale != clamped)
            {
                _appState.LoraScale = clamped;
                OnPropertyChanged();
            }
        }
    }

    // Orchestrator health for enablement/messaging
    public bool IsOrchestratorHealthy => _orchestratorClient.IsHealthy;

    // Show hint to choose runner when a model is selected first
    public bool ShowRunnerHint => SelectedModel is not null && SelectedRunner is null && !IsRunnerRunning;

    // Filtered models based on runner selection
    public IEnumerable<BaseModelInfo> VisibleBaseModels
        => SelectedRunner is null ? BaseModels : BaseModels.Where(m => IsCompatible(m, SelectedRunner));

    // Filtered runners based on model selection
    public IEnumerable<RunnerCandidate> VisibleRunnerCatalog
        => SelectedModel is null ? RunnerCatalog : RunnerCatalog.Where(r => IsCompatible(SelectedModel, r));

    // Inline UX hint for why Load is disabled
    public string? LoadDisabledReason
    {
        get
        {
            if (!IsOrchestratorHealthy) return "Orchestrator offline";
            if (SelectedRunner is null) return SelectedModel is null ? "Choose a runner and model" : "Choose a runner for this model";
            if (SelectedModel is null) return "Select a model to enable loading";
            return null;
        }
    }

    private bool CanSave() => !IsDisposed && !string.IsNullOrWhiteSpace(NewPresetName);

    private void LoadInitialStateFromSettings()
    {
        try
        {
            _initialRunnerPath = Normalize(_settingsService.GetValue(nameof(AppSettings.ActiveRunnerPath), string.Empty));
            _initialRunnerEngine = Normalize(_settingsService.GetValue(nameof(AppSettings.ActiveRunnerEngine), string.Empty));
            _initialModelPath = Normalize(_settingsService.GetValue(nameof(AppSettings.ActiveModelId), string.Empty));
            _initialLoraPath = Normalize(_settingsService.GetValue(nameof(AppSettings.ActiveLoraPath), string.Empty));

            var savedScale = _settingsService.GetValue(nameof(AppSettings.ActiveLoraScale), LoraDefault);
            if (savedScale >= LoraMin && savedScale <= LoraMax)
            {
                _appState.LoraScale = savedScale;
            }

            if (!string.IsNullOrEmpty(_initialLoraPath))
            {
                _appState.LoadedLora = _initialLoraPath;
            }

            if (!string.IsNullOrEmpty(_initialModelPath))
            {
                _appState.LoadedModelPath = _initialModelPath;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore saved model selection state");
        }
    }

    private void Refresh()
    {
        ThrowIfDisposed();
        var inv = _inventory.Scan();

        BaseModels.SmartReset(inv.BaseModels);

        // Populate adapters list
        Loras.SmartReset(inv.Loras);

        Tokenizers.SmartReset(inv.Tokenizers);

        Embeddings.SmartReset(inv.Embeddings);

        PresetNames.SmartReset(_presets.List());

        OnPropertyChanged(nameof(BaseModels));
        // no checkbox list anymore
        OnPropertyChanged(nameof(Tokenizers));
        OnPropertyChanged(nameof(Embeddings));
        OnPropertyChanged(nameof(PresetNames));

        ApplyInitialModelSelection();
    }

    private void RefreshAdapters()
    {
        var list = LoraScanner.ScanAll();

        // Filter based on the selected model's format
        if (SelectedModel != null)
        {
            list = list.Where(adapter => IsLoraCompatibleWithModel(adapter, SelectedModel)).ToList();
        }

        _ui.Invoke(() =>
        {
            LoraAdapters.Clear();
            foreach (var item in list) LoraAdapters.Add(item);

            SelectInitialLora(list);
        });
    }



private void SelectInitialLora(IReadOnlyList<LoraOption> options)
{
    if (options.Count == 0)
    {
        SelectedLora = null;
        return;
    }

    LoraOption? chosen = null;

    if (_selectedLora is not null)
    {
        chosen = options.FirstOrDefault(x => string.Equals(x.Path, _selectedLora.Path, StringComparison.OrdinalIgnoreCase));
    }

    if (chosen is null)
    {
        var target = Normalize(_appState.LoadedLora) ?? Normalize(_initialLoraPath);
        if (!string.IsNullOrEmpty(target))
        {
            chosen = options.FirstOrDefault(x => string.Equals(x.Path, target, StringComparison.OrdinalIgnoreCase));
            if (chosen != null)
            {
                _initialLoraPath = null;
            }
        }
    }

    if (chosen is null)
    {
        chosen = options.FirstOrDefault();
    }

    SelectedLora = chosen;
}

private void ApplyInitialModelSelection()
{
    var target = Normalize(_initialModelPath) ?? Normalize(_appState.LoadedModelPath);
    if (string.IsNullOrEmpty(target))
    {
        return;
    }

    if (SelectedModel is not null && string.Equals(SelectedModel.FilePath, target, StringComparison.OrdinalIgnoreCase))
    {
        _initialModelPath = null;
        return;
    }

    var match = BaseModels.FirstOrDefault(m => string.Equals(m.FilePath, target, StringComparison.OrdinalIgnoreCase));
    if (match != null)
    {
        SelectedModel = match;
    }

    _initialModelPath = null;
}

private void ApplyInitialRunnerSelection()
{
    if (SelectedRunner is not null)
    {
        return;
    }

    var targetPath = Normalize(_initialRunnerPath);
    if (string.IsNullOrEmpty(targetPath))
    {
        return;
    }

    var match = RunnerCatalog.FirstOrDefault(r =>
        string.Equals(r.ResolvedPath, targetPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(r.Entrypoint, targetPath, StringComparison.OrdinalIgnoreCase));

    if (match is null)
    {
        return;
    }

    if (!string.IsNullOrEmpty(_initialRunnerEngine) &&
        !string.Equals(match.Engine, _initialRunnerEngine, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    SelectedRunner = match;
    _initialRunnerPath = null;
    _initialRunnerEngine = null;
}
    private bool IsLoraCompatibleWithModel(LoraOption lora, BaseModelInfo model)
    {
        // For GGUF models (llama.cpp), only show GGUF LoRA adapters
        if (model.Format == ModelFormat.GGUF)
        {
            return lora.Format == LoraFormat.GGUF;
        }

        // For HuggingFace models, show safetensors and PyTorch formats
        if (model.Format == ModelFormat.HF)
        {
            return lora.Format == LoraFormat.Safetensors ||
                   lora.Format == LoraFormat.PyTorch;
        }

        // For other formats, show all adapters
        return true;
    }


    private void SavePreset()
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(NewPresetName)) return;

        var baseKey = SelectedModel?.ModelKey ?? string.Empty;
        var preset = new ModelPreset(
            Name: NewPresetName!.Trim(),
            BaseModelKey: baseKey,
            Loras: (_selectedLora is not null ? new System.Collections.Generic.List<string> { _selectedLora.Path } : new System.Collections.Generic.List<string>()),
            Tokenizer: SelectedTokenizer?.Name,
            Embedding: SelectedEmbedding?.Name,
            Params: new ModelParams(Temperature, TopP, MaxTokens, RepeatPenalty, Mirostat)
        );

        _presets.Save(preset);
        _logger.LogInformation("Saved preset {Name}", preset.Name);

        // Refresh list
        PresetNames.Clear();
        foreach (var n in _presets.List()) PresetNames.Add(n);
        OnPropertyChanged(nameof(PresetNames));
    }


private async Task LoadSelectedModelAsync()
{
    ThrowIfDisposed();
    var model = SelectedModel;
    if (model is null)
    {
        RunnerStatusMessage = "Choose a model to load.";
        return;
    }

    var runner = SelectedRunner;
    if (runner is null)
    {
        RunnerStatusMessage = "Choose a runner engine.";
        return;
    }

    if (!string.Equals(runner.Engine, "llama.cpp", StringComparison.OrdinalIgnoreCase))
    {
        RunnerStatusMessage = $"Engine '{runner.Engine}' not supported yet.";
        return;
    }

    IsLoadingModel = true;
    RunnerStatusMessage = "Loading model...";

    List<string>? loras = null;

    try
    {
        await SaveModelStateAsync(model.FilePath).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(_appState.LoadedLora))
        {
            _logger.LogInformation("LoadSelectedModelAsync: LoRA path from app state: {Path}", _appState.LoadedLora);
            RunnerStatusMessage = "Preparing LoRA adapter...";

            try
            {
                if (Directory.Exists(_appState.LoadedLora))
                {
                    var ggufs = Directory.EnumerateFiles(_appState.LoadedLora, "*.gguf", SearchOption.AllDirectories).ToList();
                    if (ggufs.Count > 0)
                    {
                        var orderedAdapters = ggufs.OrderBy(Path.GetFileName).ToList();
                        loras = orderedAdapters;
                        _logger.LogInformation("Found GGUF adapter file(s): {Paths}", string.Join(", ", orderedAdapters));
                    }
                    else
                    {
                        loras = new List<string> { _appState.LoadedLora };
                        _logger.LogWarning("No GGUF files found in LoRA directory, passing directory: {Path}", _appState.LoadedLora);
                        RunnerStatusMessage = "Warning: LoRA adapter may need conversion to GGUF format";
                    }
                }
                else if (File.Exists(_appState.LoadedLora))
                {
                    if (string.Equals(Path.GetExtension(_appState.LoadedLora), ".gguf", StringComparison.OrdinalIgnoreCase))
                    {
                        loras = new List<string> { _appState.LoadedLora };
                        _logger.LogInformation("Using GGUF adapter file: {Path}", _appState.LoadedLora);
                    }
                    else
                    {
                        RunnerStatusMessage = "Warning: Selected LoRA file is not GGUF. Conversion required.";
                        _logger.LogWarning("LoRA file is not GGUF: {Path}", _appState.LoadedLora);
                    }
                }
                else
                {
                    RunnerStatusMessage = "Warning: LoRA path no longer exists.";
                    _logger.LogWarning("LoRA path does not exist: {Path}", _appState.LoadedLora);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing LoRA adapter: {Path}", _appState.LoadedLora);
                RunnerStatusMessage = $"Error accessing LoRA: {ex.Message}";
            }
        }
        else
        {
            _logger.LogInformation("LoadSelectedModelAsync: No LoRA loaded in app state");
        }

        if (loras != null && loras.Count > 0)
        {
            _logger.LogInformation("Prepared {Count} LoRA adapter(s): {Adapters}", loras.Count, string.Join(", ", loras));
        }
        else
        {
            _logger.LogInformation("No LoRA adapters prepared for this load.");
        }

        RunnerStatusMessage = loras != null ? "Loading model with LoRA..." : "Loading model...";
        var ok = await _runnerClient.LoadModelAsync(model.FilePath, loras, _appState.LoraScale).ConfigureAwait(false);

        if (!ok)
        {
            RunnerStatusMessage = _runnerClient.LastError ?? "Failed to load model.";
            _logger.LogWarning("LoadSelectedModel failed: {Message}", RunnerStatusMessage);
            return;
        }

        RunnerStatusMessage = "Verifying model load...";

        await Task.Delay(1500).ConfigureAwait(false);

        var status = await _runnerClient.GetStatusAsync().ConfigureAwait(false);
        var applied = status.LorasApplied ?? 0;
        _logger.LogInformation("Runner after reload: model={Model} pid={Pid} port={Port} loras={Loras}",
            status.ModelPath ?? "<none>", status.Pid, status.Port, applied);

        if (loras != null && applied == 0)
        {
            RunnerStatusMessage = "Warning: Model loaded but LoRA not applied - check logs";
        }
        else if (applied > 0)
        {
            RunnerStatusMessage = $"Model loaded with {applied} LoRA adapter(s).";
        }
        else
        {
            RunnerStatusMessage = "Model loaded successfully.";
        }
    }
    catch (Exception ex)
    {
        RunnerStatusMessage = $"Failed to load model: {ex.Message}";
        _logger.LogError(ex, "LoadSelectedModelAsync failed");
    }
    finally
    {
        IsLoadingModel = false;
        await RefreshRunnerStatusAsync().ConfigureAwait(false);
    }
}

private async Task UnloadRunnerAsync()

    {
        ThrowIfDisposed();
        RunnerStatusMessage = null;
        var ok = await _runnerClient.UnloadAsync().ConfigureAwait(false);
        if (!ok)
        {
            RunnerStatusMessage = _runnerClient.LastError ?? "Failed to unload runner.";
            _logger.LogWarning("UnloadRunner failed: {Message}", RunnerStatusMessage);
        }
        await RefreshRunnerStatusAsync().ConfigureAwait(false);
    }

    private async Task RefreshRunnerStatusAsync()
    {
        var status = await _runnerClient.GetStatusAsync().ConfigureAwait(false);
        IsRunnerRunning = status.IsRunning;
        RunnerModelPath = status.ModelPath;
        RunnerPid = status.Pid;
        RunnerPort = status.Port;
        RunnerExePath = status.ExePath;
        RunnerErrLog = status.ErrLog;
        RunnerOutLog = status.OutLog;
        LorasApplied = status.LorasApplied;
        LaunchArgs = status.LaunchArgs;
        CmdPath = status.CmdPath;
        LoraEvidenceCount = status.LoraEvidenceCount;
        OnPropertyChanged(nameof(IsRunnerRunning));
        OnPropertyChanged(nameof(RunnerModelPath));
        OnPropertyChanged(nameof(RunnerPid));
        OnPropertyChanged(nameof(RunnerPort));
        OnPropertyChanged(nameof(RunnerExePath));
        OnPropertyChanged(nameof(RunnerErrLog));
        OnPropertyChanged(nameof(RunnerOutLog));
        OnPropertyChanged(nameof(LorasApplied));
        OnPropertyChanged(nameof(LaunchArgs));
        OnPropertyChanged(nameof(CmdPath));
        OnPropertyChanged(nameof(LoraEvidenceCount));
        LoadSelectedModelCommand.RaiseCanExecuteChanged();
        LoadLoraCommand.RaiseCanExecuteChanged();
        UnloadLoraCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(LoadDisabledReason));
        _appState.IsRunnerRunning = IsRunnerRunning;
        _appState.RunnerPort = RunnerPort;
        _appState.RunnerPid = RunnerPid;
        _appState.LoadedModelPath = RunnerModelPath;

        // Auto-select base model if a model is already loaded
        if (IsRunnerRunning && !string.IsNullOrWhiteSpace(RunnerModelPath))
        {
            var match = BaseModels.FirstOrDefault(m => string.Equals(m.FilePath, RunnerModelPath, StringComparison.OrdinalIgnoreCase));
            if (match != null) SelectedModel = match;
        }

        // Auto-select runner candidate if we can infer it from exe path
        if (!string.IsNullOrWhiteSpace(RunnerExePath))
        {
            var folder = Path.GetDirectoryName(RunnerExePath);
            if (!string.IsNullOrWhiteSpace(folder))
            {
                var cand = RunnerCatalog.FirstOrDefault(r => string.Equals(r.ResolvedPath, folder, StringComparison.OrdinalIgnoreCase));
                if (cand != null) SelectedRunner = cand;
            }
        }
    }

    private Task RefreshRunnersCatalogAsync()
    {
        try
        {
            var list = ScanRunners();
            RunnerCatalog.SmartReset(list);
            OnPropertyChanged(nameof(RunnerCatalog));
            OnPropertyChanged(nameof(VisibleRunnerCatalog));
            ApplyInitialRunnerSelection();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed scanning runners");
        }
        return Task.CompletedTask;
    }

    private static IReadOnlyList<RunnerCandidate> ScanRunners()
    {
        var results = new System.Collections.Generic.List<RunnerCandidate>();
        var root = LazarusPaths.Runners.RootDir;
        if (!System.IO.Directory.Exists(root)) return results;

        // Build candidate engine directories from domain roots + legacy flat
        var engineDirs = new System.Collections.Generic.List<string>();
        void AddTop(string dir)
        {
            try
            {
                if (System.IO.Directory.Exists(dir))
                    engineDirs.AddRange(System.IO.Directory.EnumerateDirectories(dir, "*", System.IO.SearchOption.TopDirectoryOnly));
            }
            catch { }
        }

        // Domain roots: Chats/Images/Videos/Audio/Avatars/Shared (Chats hosts LLM runners we scan for)
        AddTop(LazarusPaths.Runners.ChatsRoot);
        // Keep legacy flat engines for back-compat
        AddTop(root);

        // De-duplicate
        var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var engineDir in engineDirs)
        {
            if (!seen.Add(engineDir)) continue;
            var engineKey = System.IO.Path.GetFileName(engineDir).Trim();
            string[] patterns = engineKey.ToLowerInvariant() switch
            {
                "llama.cpp" => new[] { "llama-server.exe" },
                "vllm" => new[] { "start-vllm.cmd", "start-vllm.bat", "vllm*.exe" },
                "exllamav2" => new[] { "exllamav2*.exe", "*exllama*.exe", "exllamav2*.cmd" },
                _ => System.Array.Empty<string>()
            };
            if (patterns.Length == 0) continue;

            foreach (var pattern in patterns)
            {
                System.Collections.Generic.IEnumerable<string> files;
                try { files = System.IO.Directory.EnumerateFiles(engineDir, pattern, System.IO.SearchOption.AllDirectories); }
                catch { continue; }
                foreach (var exe in files)
                {
                    var folder = System.IO.Path.GetDirectoryName(exe)!;
                    string leaf;
                    try { leaf = new System.IO.DirectoryInfo(folder).Name; }
                    catch { leaf = folder; }
                    results.Add(new RunnerCandidate(engineKey, leaf, folder, exe));
                }
            }
        }

        return results
            .GroupBy(r => new Tuple<string, string>(r.Engine.ToLowerInvariant(), r.ResolvedPath), System.Collections.Generic.EqualityComparer<Tuple<string, string>>.Default)
            .Select(g => g.First())
            .OrderBy(r => r.Engine)
            .ThenBy(r => r.DisplayName)
            .ToList();
    }

    private void LoadPreset()
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(SelectedPresetName)) return;

        var preset = _presets.Load(SelectedPresetName!);
        if (preset is null) return;

        // Map selections back
        SelectedModel = BaseModels.FirstOrDefault(m => string.Equals(m.ModelKey, preset.BaseModelKey, StringComparison.OrdinalIgnoreCase));
        SelectedTokenizer = Tokenizers.FirstOrDefault(m => string.Equals(m.Name, preset.Tokenizer, StringComparison.OrdinalIgnoreCase));
        SelectedEmbedding = Embeddings.FirstOrDefault(m => string.Equals(m.Name, preset.Embedding, StringComparison.OrdinalIgnoreCase));

        _selectedLora = LoraAdapters.FirstOrDefault(a => preset.Loras.Any(path => string.Equals(path, a.Path, StringComparison.OrdinalIgnoreCase)));
        OnPropertyChanged(nameof(SelectedLora));

        Temperature = preset.Params.Temperature;
        TopP = preset.Params.TopP;
        MaxTokens = preset.Params.MaxTokens;
        RepeatPenalty = preset.Params.RepeatPenalty;
        Mirostat = preset.Params.Mirostat;

        _logger.LogInformation("Loaded preset {Name}", preset.Name);
    }

    private void LoadTokenizer() { if (SelectedTokenizer != null) _appState.LoadedTokenizer = SelectedTokenizer.FilePath; }
    private void UnloadTokenizer() { _appState.LoadedTokenizer = null; }
    private void LoadEmbedding() { if (SelectedEmbedding != null) _appState.LoadedEmbedding = SelectedEmbedding.FilePath; }
    private void UnloadEmbedding() { _appState.LoadedEmbedding = null; }

private async void LoadLora()
{
    if (SelectedLora == null) return;

    IsLoadingLora = true;

    try
    {
        if (SelectedModel is not null && SelectedModel.Format == ModelFormat.GGUF &&
            SelectedLora.Format != LoraFormat.GGUF)
        {
            var hasGguf = false;
            try
            {
                hasGguf = Directory.EnumerateFiles(SelectedLora.Path, "*.gguf", SearchOption.AllDirectories).Any();
            }
            catch
            {
            }

            if (!hasGguf)
            {
                RunnerStatusMessage = $"?? LoRA adapter format ({SelectedLora.Format}) needs conversion to GGUF for llama.cpp";
                _logger.LogWarning("LoRA adapter {Path} is in {Format} format but needs GGUF for llama.cpp",
                    SelectedLora.Path, SelectedLora.Format);
            }
        }

        _appState.LoadedLora = SelectedLora.Path;
        _logger.LogInformation("LoRA selected: {Path}", _appState.LoadedLora);

        _ = SaveLoraStateAsync(SelectedLora.Path, _appState.LoraScale ?? LoraDefault);

        if (IsRunnerRunning && SelectedModel is not null)
        {
            RunnerStatusMessage = "Loading LoRA adapter...";
            _logger.LogInformation("Reloading runner to apply LoRA...");
            await LoadSelectedModelAsync();
        }
    }
    catch (Exception ex)
    {
        RunnerStatusMessage = $"Failed to load LoRA: {ex.Message}";
        _logger.LogError(ex, "LoadLora failed");
    }
    finally
    {
        IsLoadingLora = false;
    }
}

private async void UnloadLora()
{
    try
    {
        _appState.LoadedLora = null;
        _logger.LogInformation("LoRA cleared.");

        _ = SaveLoraStateAsync(null, LoraDefault);

        if (IsRunnerRunning && SelectedModel is not null)
        {
            RunnerStatusMessage = "Unloading LoRA adapter...";
            _logger.LogInformation("Reloading runner to remove LoRA...");
            await LoadSelectedModelAsync();
        }
    }
    catch (Exception ex)
    {
        RunnerStatusMessage = $"Failed to unload LoRA: {ex.Message}";
        _logger.LogError(ex, "UnloadLora failed");
    }
}

private async void VerifyAdapter()

    {
        if (SelectedLora == null || SelectedModel == null) return;

        try
        {
            RunnerStatusMessage = "Verifying adapter...";

            // Step 1: Check if the runner is running with the LoRA path
            var status = await _runnerClient.GetStatusAsync().ConfigureAwait(false);

            if (!status.IsRunning || string.IsNullOrWhiteSpace(status.ModelPath))
            {
                RunnerStatusMessage = "❌ Runner not running. Load a model first.";
                return;
            }

            // Step 2: Check if the loaded LoRA matches the selected one
            var isLoraActive = string.Equals(_appState.LoadedLora, SelectedLora.Path, StringComparison.OrdinalIgnoreCase);

            if (!isLoraActive)
            {
                RunnerStatusMessage = $"❌ LoRA INACTIVE: Expected {SelectedLora.Display}, but {_appState.LoadedLora ?? "none"} is loaded";
                _logger.LogWarning("LoRA verification failed: Expected {Expected}, but {Actual} is loaded", SelectedLora.Path, _appState.LoadedLora);
                return;
            }

            // Step 3: Check if LoRA was actually applied (from runner status)
            if (status.LorasApplied.HasValue && status.LorasApplied > 0)
            {
                RunnerStatusMessage = $"✅ LoRA VERIFIED: {SelectedLora.Display} ({status.LorasApplied} adapter(s) active)";
                _logger.LogInformation("LoRA verification successful: {Display} at {Path} - {Count} adapters applied",
                    SelectedLora.Display, SelectedLora.Path, status.LorasApplied);
            }
            else
            {
                RunnerStatusMessage = $"⚠️ LoRA LOADED but NOT APPLIED: Check logs for errors";
                _logger.LogWarning("LoRA loaded but not applied: {Path} - Check llama.cpp logs", SelectedLora.Path);
            }
        }
        catch (Exception ex)
        {
            RunnerStatusMessage = $"❌ Verification failed: {ex.Message}";
            _logger.LogError(ex, "LoRA verification failed");
        }
    }

    private string? GetLoadedLoraDisplayName()
    {
        if (string.IsNullOrWhiteSpace(_appState.LoadedLora)) return null;
        
        var loadedLora = LoraAdapters.FirstOrDefault(l => string.Equals(l.Path, _appState.LoadedLora, StringComparison.OrdinalIgnoreCase));
        return loadedLora?.Display ?? Path.GetFileName(_appState.LoadedLora);
    }

    protected override void OnDisposing()
    {
        _logger.LogDebug("ModelsViewModel disposed");
        _watcher.Dispose();
    }

    private async Task SaveLoraStateAsync(string? loraPath, double loraScale)
    {
        try
        {
            await _settingsService.SetValueAsync(nameof(AppSettings.ActiveLoraPath), loraPath ?? string.Empty);
            await _settingsService.SetValueAsync(nameof(AppSettings.ActiveLoraScale), loraScale);
            _logger.LogDebug("Saved LoRA state: {Path}, Scale: {Scale}", loraPath, loraScale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save LoRA state");
        }
    }

    private async Task SaveRunnerStateAsync(RunnerCandidate runner)
    {
        try
        {
            await _settingsService.SetValueAsync(nameof(AppSettings.ActiveRunnerPath), runner.ResolvedPath);
            await _settingsService.SetValueAsync(nameof(AppSettings.ActiveRunnerEngine), runner.Engine);
            _logger.LogDebug("Saved runner state: {Path}, Engine: {Engine}", runner.ResolvedPath, runner.Engine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save runner state");
        }
    }

    private async Task SaveModelStateAsync(string modelPath)
    {
        try
        {
            await _settingsService.SetValueAsync(nameof(AppSettings.ActiveModelId), modelPath);
            _logger.LogDebug("Saved model state: {Path}", modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save model state");
        }
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly ParameterSchema Schema = ParameterSchema.Default;
    private static double Clamp(double value, DoubleParam param)
        => Math.Max(param.Min, Math.Min(param.Max, value));

    private static int Clamp(int value, IntParam param)
        => Math.Max(param.Min, Math.Min(param.Max, value));

    private static bool IsCompatible(BaseModelInfo model, RunnerCandidate runner)
    {
        var engine = runner.Engine.ToLowerInvariant();
        return engine switch
        {
            "llama.cpp" => model.Format == ModelFormat.GGUF,
            "vllm" => model.Format == ModelFormat.HF || model.Format == ModelFormat.ONNX, // vLLM supports HF and ONNX
            "exllamav2" => model.Format == ModelFormat.HF, // ExLlamaV2 primarily for HF models
            _ => false
        };
    }
}
