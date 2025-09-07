using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Lazarus.Shared;
using Lazarus.Backend.Services;
using Lazarus.Desktop.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lazarus.Desktop.ViewModels;

public sealed class SelectableAdapter : ViewModelBase
{
    public SelectableAdapter(Lazarus.Shared.AdapterInfo item) { Item = item; }

    public Lazarus.Shared.AdapterInfo Item { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public sealed class ModelsViewModel : ViewModelBase
{
    private readonly IModelInventoryService _inventory;
    private readonly IModelPresetService _presets;
    private readonly ILogger<ModelsViewModel> _logger;

    public ModelsViewModel() : this(new ModelInventoryService(), new ModelPresetService(), NullLogger<ModelsViewModel>.Instance) { }

    public ModelsViewModel(IModelInventoryService inventory, IModelPresetService presets)
        : this(inventory, presets, NullLogger<ModelsViewModel>.Instance) { }

    public ModelsViewModel(IModelInventoryService inventory, IModelPresetService presets, ILogger<ModelsViewModel> logger)
    {
        _inventory = inventory;
        _presets = presets;
        _logger = logger;
        // Ensure preset folder exists for smooth UX
        _presets.EnsureFolders();

        RefreshCommand = new RelayCommand(Refresh, () => !IsDisposed);
        SavePresetCommand = new RelayCommand(SavePreset, CanSave);
        LoadPresetCommand = new RelayCommand(LoadPreset, () => SelectedPresetName is not null && !IsDisposed);

        Refresh();
    }

    // Collections (bound to dropdowns/lists)
    public ObservableCollection<BaseModelInfo> BaseModels { get; } = new();
    public ObservableCollection<AdapterInfo>   Loras      { get; } = new();
    public ObservableCollection<TokenizerInfo> Tokenizers { get; } = new();
    public ObservableCollection<EmbeddingInfo> Embeddings { get; } = new();
    public ObservableCollection<string>        PresetNames{ get; } = new();

    // Selections
    private BaseModelInfo? _selectedModel;
    public BaseModelInfo? SelectedModel { get => _selectedModel; set { _selectedModel = value; OnPropertyChanged(); } }

    // Legacy checkbox selection support alongside explicit list
    public ObservableCollection<SelectableAdapter> LoRAs { get; } = new();
    public ObservableCollection<AdapterInfo> SelectedLoras { get; } = new();

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

    private bool CanSave() => !IsDisposed && !string.IsNullOrWhiteSpace(NewPresetName);

    private void Refresh()
    {
        ThrowIfDisposed();
        var inv = _inventory.Scan();

        BaseModels.SmartReset(inv.BaseModels);

        // Populate both raw adapter list and checkbox-friendly list
        Loras.SmartReset(inv.Loras);
        LoRAs.Clear();
        foreach (var l in inv.Loras) LoRAs.Add(new SelectableAdapter(l));

        Tokenizers.SmartReset(inv.Tokenizers);

        Embeddings.SmartReset(inv.Embeddings);

        PresetNames.SmartReset(_presets.List());

        OnPropertyChanged(nameof(BaseModels));
        OnPropertyChanged(nameof(LoRAs));
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
            Loras: (SelectedLoras.Any() ? SelectedLoras.Select(x => x.Name) : LoRAs.Where(l => l.IsSelected).Select(l => l.Item.Name)).ToList(),
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

        SelectedLoras.Clear();
        foreach (var name in preset.Loras)
        {
            var match = Loras.FirstOrDefault(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match != null) SelectedLoras.Add(match);
        }
        // Keep checkbox list in sync if UI uses it
        foreach (var l in LoRAs) l.IsSelected = preset.Loras.Any(name => string.Equals(name, l.Item.Name, StringComparison.OrdinalIgnoreCase));

        Temperature = preset.Params.Temperature;
        TopP = preset.Params.TopP;
        MaxTokens = preset.Params.MaxTokens;
        RepeatPenalty = preset.Params.RepeatPenalty;
        Mirostat = preset.Params.Mirostat;

        _logger.LogInformation("Loaded preset {Name}", preset.Name);
    }

    protected override void OnDisposing()
    {
        _logger.LogDebug("ModelsViewModel disposed");
    }

    private static readonly ParameterSchema Schema = ParameterSchema.Default;
    private static double Clamp(double value, DoubleParam param)
        => Math.Max(param.Min, Math.Min(param.Max, value));

    private static int Clamp(int value, IntParam param)
        => Math.Max(param.Min, Math.Min(param.Max, value));
}
