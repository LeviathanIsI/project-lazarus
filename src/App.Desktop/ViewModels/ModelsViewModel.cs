using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.IO;
using Lazarus.Shared;
using Lazarus.Backend.Services;
using Lazarus.Desktop.Extensions;
using Lazarus.Desktop.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lazarus.Desktop.ViewModels;

public sealed class ModelsViewModel : ViewModelBase
{
    public sealed record RunnerCandidate(string Engine, string DisplayName, string ResolvedPath, string Entrypoint);
    private readonly IModelInventoryService _inventory;
    private readonly IModelPresetService _presets;
    private readonly ILogger<ModelsViewModel> _logger;
    private readonly IOrchestratorRunnerClient _runnerClient;
    private readonly IOrchestratorClient _orchestratorClient;
    private readonly Func<AdapterInfo, SelectableAdapter> _adapterFactory;
    private readonly IAppState _appState;

    public ModelsViewModel(
        IModelInventoryService inventory,
        IModelPresetService presets,
        ILogger<ModelsViewModel> logger,
        Func<AdapterInfo, SelectableAdapter> adapterFactory,
        Lazarus.Desktop.Services.IOrchestratorRunnerClient runnerClient,
        IOrchestratorClient orchestratorClient,
        IAppState appState)
    {
        _inventory = inventory;
        _presets = presets;
        _logger = logger;
        _adapterFactory = adapterFactory;
        _runnerClient = runnerClient;
        _orchestratorClient = orchestratorClient;
        _appState = appState;
        // Ensure preset folder exists for smooth UX
        _presets.EnsureFolders();

        // Observe global app state so the view can reflect loaded adapters immediately
        _appState.PropertyChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(LoadedLoraPath));
            OnPropertyChanged(nameof(LoadedTokenizerPath));
            OnPropertyChanged(nameof(LoadedEmbeddingPath));
            OnPropertyChanged(nameof(IsLoraLoaded));
            OnPropertyChanged(nameof(LoraScaleValue));
        };

        RefreshCommand = new RelayCommand(Refresh, () => !IsDisposed);
        LoadSelectedModelCommand = new RelayCommand(
            async () => await LoadSelectedModelAsync(),
            () => SelectedModel is not null && SelectedRunner is not null && !IsRunnerRunning && _orchestratorClient.IsHealthy && !IsDisposed);
        UnloadRunnerCommand = new RelayCommand(async () => await UnloadRunnerAsync(), () => !IsDisposed);
        RefreshRunnerStatusCommand = new RelayCommand(async () => { await RefreshRunnerStatusAsync(); await RefreshRunnersCatalogAsync(); }, () => !IsDisposed);
        SavePresetCommand = new RelayCommand(SavePreset, CanSave);
        LoadPresetCommand = new RelayCommand(LoadPreset, () => SelectedPresetName is not null && !IsDisposed);
        LoadTokenizerCommand = new RelayCommand(LoadTokenizer, () => SelectedTokenizer is not null);
        UnloadTokenizerCommand = new RelayCommand(UnloadTokenizer, () => _appState.LoadedTokenizer != null);
        LoadEmbeddingCommand = new RelayCommand(LoadEmbedding, () => SelectedEmbedding is not null);
        UnloadEmbeddingCommand = new RelayCommand(UnloadEmbedding, () => _appState.LoadedEmbedding != null);
        LoadLoraCommand = new RelayCommand(LoadLora, () => SelectedLora is not null);
        UnloadLoraCommand = new RelayCommand(UnloadLora, () => _appState.LoadedLora != null);

        Refresh();
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
            LoadSelectedModelCommand.RaiseCanExecuteChanged();
        }
    }

    // Selected single LoRA adapter for now
    private AdapterInfo? _selectedLora;
    public AdapterInfo? SelectedLora { get => _selectedLora; set => SetProperty(ref _selectedLora, value); }

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

    // Adapter load state (proxy to global AppState)
    public string? LoadedLoraPath => _appState.LoadedLora;
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
            if (IsRunnerRunning) return "Runner already running";
            if (!IsOrchestratorHealthy) return "Orchestrator offline";
            if (SelectedRunner is null) return SelectedModel is null ? "Choose a runner and model" : "Choose a runner for this model";
            if (SelectedModel is null) return "Select a model to enable loading";
            return null;
        }
    }

    private bool CanSave() => !IsDisposed && !string.IsNullOrWhiteSpace(NewPresetName);

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
    }

    private void SavePreset()
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(NewPresetName)) return;

        var baseKey = SelectedModel?.ModelKey ?? string.Empty;
        var preset = new ModelPreset(
            Name: NewPresetName!.Trim(),
            BaseModelKey: baseKey,
            Loras: (_selectedLora is not null ? new System.Collections.Generic.List<string> { _selectedLora.Name } : new System.Collections.Generic.List<string>()),
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

        if (SelectedRunner is null)
        {
            RunnerStatusMessage = "Choose a runner engine.";
            return;
        }

        if (!string.Equals(SelectedRunner.Engine, "llama.cpp", StringComparison.OrdinalIgnoreCase))
        {
            RunnerStatusMessage = $"Engine '{SelectedRunner.Engine}' not supported yet.";
            return;
        }

        RunnerStatusMessage = null;
        var ok = await _runnerClient.LoadModelAsync(model.FilePath).ConfigureAwait(false);
        if (!ok)
        {
            RunnerStatusMessage = _runnerClient.LastError ?? "Failed to load model.";
            _logger.LogWarning("LoadSelectedModel failed: {Message}", RunnerStatusMessage);
        }
        await RefreshRunnerStatusAsync().ConfigureAwait(false);
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
        OnPropertyChanged(nameof(IsRunnerRunning));
        OnPropertyChanged(nameof(RunnerModelPath));
        OnPropertyChanged(nameof(RunnerPid));
        OnPropertyChanged(nameof(RunnerPort));
        OnPropertyChanged(nameof(RunnerExePath));
        OnPropertyChanged(nameof(RunnerErrLog));
        OnPropertyChanged(nameof(RunnerOutLog));
        LoadSelectedModelCommand.RaiseCanExecuteChanged();
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

        _selectedLora = Loras.FirstOrDefault(a => preset.Loras.Any(name => string.Equals(name, a.Name, StringComparison.OrdinalIgnoreCase)));
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
    private void LoadLora() { if (SelectedLora != null) _appState.LoadedLora = SelectedLora.FilePath; }
    private void UnloadLora() { _appState.LoadedLora = null; }

    protected override void OnDisposing()
    {
        _logger.LogDebug("ModelsViewModel disposed");
    }

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
            "vllm" => model.Format == ModelFormat.HF,
            "exllamav2" => model.Format == ModelFormat.HF,
            _ => false
        };
    }
}
