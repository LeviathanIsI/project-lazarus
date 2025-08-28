using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;
using Lazarus.Shared.OpenAI;
using Lazarus.Shared.Models;
using System.IO;
using System.Linq;
using System.Timers;
using Lazarus.Desktop.Services;

namespace Lazarus.Desktop.ViewModels;

public class BaseModelViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string _statusText = "Ready";
    private BaseModelDto? _selectedModel;
    private SamplingParameters _currentParameters = new();
    private Dictionary<string, ParameterMetadata> _supportedParameterSchema = new();

    // New: filtering/sorting
    private string _searchFilter = string.Empty;
    private string _selectedSortOption = "A–Z";
    public ObservableCollection<BaseModelDto> FilteredModels { get; } = new();

    // Runner selection
    private string _selectedRunner = "llama.cpp";
    public string SelectedRunner
    {
        get => _selectedRunner;
        set => SetProperty(ref _selectedRunner, value);
    }

    // FS watcher
    private FileSystemWatcher? _watcher;
    private readonly System.Timers.Timer _debounceTimer = new System.Timers.Timer(300) { AutoReset = false };

    // Test Prompt Properties - The blood ritual components
    private string _testPromptText = "Write a short story about a digital vampire who feeds on corrupted data...";
    private string _testResponse = "";
    private string _testStatus = "Ready to test";
    private bool _isTestRunning = false;

    private readonly GlobalModelStateService _globalState;

    public BaseModelViewModel(GlobalModelStateService globalState)
    {
        _globalState = globalState;
        BaseModels = new ObservableCollection<BaseModelDto>();

        LoadModelsCommand = new RelayCommand(async _ => await LoadModelsAsync(), _ => !IsLoading);
        LoadModelCommand = new RelayCommand(async model =>
        {
            var target = model as BaseModelDto ?? SelectedModel;
            if (target == null) return; // require explicit selection present
            await LoadModelAsync(target);
        },
        _ => !IsLoading && SelectedModel != null);
        LoadParametersCommand = new RelayCommand(async _ => await LoadModelParametersAsync(), _ => !IsLoading && SelectedModel != null);
        TestParametersCommand = new RelayCommand(async _ => await ExecuteTestAsync(), _ => CanExecuteTest);
        UnloadModelCommand = new RelayCommand(async _ => await UnloadModelAsync(), _ => !IsLoading && IsModelLoaded);
        SelectModelCommand = new RelayCommand(model => SelectModel(model as BaseModelDto));

        _debounceTimer.Elapsed += (_, __) => _ = LoadModelsAsync();

        // React to global state changes (e.g., model loaded elsewhere)
        _globalState.ModelLoaded += (_, info) => UpdateActiveFromGlobal(info);
        _globalState.ModelUnloaded += (_, __) => ClearActiveSelection();
    }

    #region Properties

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(CanLoadModel));
                try { System.Windows.Input.CommandManager.InvalidateRequerySuggested(); } catch { }
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (SetProperty(ref _statusText, value))
                OnPropertyChanged(nameof(LoadingStatus));
        }
    }

    // Alias for XAML binding
    public string LoadingStatus => StatusText;

    public string SearchFilter
    {
        get => _searchFilter;
        set
        {
            if (SetProperty(ref _searchFilter, value))
                ApplyFilterAndSort();
        }
    }

    public string SelectedSortOption
    {
        get => _selectedSortOption;
        set
        {
            if (SetProperty(ref _selectedSortOption, value))
                ApplyFilterAndSort();
        }
    }

    public BaseModelDto? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (SetProperty(ref _selectedModel, value))
            {
                OnPropertyChanged(nameof(CanExecuteTest));
                OnPropertyChanged(nameof(CanLoadModel));
                if (value != null)
                {
                    foreach (var m in BaseModels) m.IsSelected = ReferenceEquals(m, value);
                }
                try { System.Windows.Input.CommandManager.InvalidateRequerySuggested(); } catch { }
            }
        }
    }

    public SamplingParameters CurrentParameters
    {
        get => _currentParameters;
        set => SetProperty(ref _currentParameters, value);
    }

    public Dictionary<string, ParameterMetadata> SupportedParameterSchema
    {
        get => _supportedParameterSchema;
        set => SetProperty(ref _supportedParameterSchema, value);
    }

    // Test Prompt Properties
    public string TestPromptText
    {
        get => _testPromptText;
        set
        {
            if (SetProperty(ref _testPromptText, value))
            {
                OnPropertyChanged(nameof(CanExecuteTest));
            }
        }
    }

    public string TestResponse
    {
        get => _testResponse;
        set => SetProperty(ref _testResponse, value);
    }

    public string TestStatus
    {
        get => _testStatus;
        set => SetProperty(ref _testStatus, value);
    }

    public bool IsTestRunning
    {
        get => _isTestRunning;
        set
        {
            if (SetProperty(ref _isTestRunning, value))
            {
                OnPropertyChanged(nameof(CanExecuteTest));
            }
        }
    }

    public bool CanExecuteTest => SelectedModel != null && !IsTestRunning && !string.IsNullOrWhiteSpace(TestPromptText);
    public bool CanLoadModel => SelectedModel != null && !IsLoading;
    public bool IsModelLoaded => BaseModels.Any(m => m.IsActive);

    public ObservableCollection<BaseModelDto> BaseModels { get; }

    #endregion

    #region Commands

    public ICommand LoadModelsCommand { get; }
    public ICommand LoadModelCommand { get; }
    public ICommand LoadParametersCommand { get; }
    public ICommand TestParametersCommand { get; }
    public ICommand UnloadModelCommand { get; }
    public ICommand SelectModelCommand { get; }

    // Aliases expected by XAML
    public ICommand ScanModelsCommand => LoadModelsCommand;
    public ICommand TestModelCommand => TestParametersCommand;

    #endregion

    #region Core Sampling Parameters (Bindable Properties)

    public float Temperature
    {
        get => CurrentParameters.Temperature;
        set
        {
            if (Math.Abs(CurrentParameters.Temperature - value) > 0.001f)
            {
                CurrentParameters.Temperature = value;
                OnPropertyChanged();
            }
        }
    }

    public float TopP
    {
        get => CurrentParameters.TopP;
        set
        {
            if (Math.Abs(CurrentParameters.TopP - value) > 0.001f)
            {
                CurrentParameters.TopP = value;
                OnPropertyChanged();
            }
        }
    }

    public int TopK
    {
        get => CurrentParameters.TopK;
        set
        {
            if (CurrentParameters.TopK != value)
            {
                CurrentParameters.TopK = value;
                OnPropertyChanged();
            }
        }
    }

    public float MinP
    {
        get => CurrentParameters.MinP;
        set
        {
            if (Math.Abs(CurrentParameters.MinP - value) > 0.001f)
            {
                CurrentParameters.MinP = value;
                OnPropertyChanged();
            }
        }
    }

    public float TypicalP
    {
        get => CurrentParameters.TypicalP;
        set
        {
            if (Math.Abs(CurrentParameters.TypicalP - value) > 0.001f)
            {
                CurrentParameters.TypicalP = value;
                OnPropertyChanged();
            }
        }
    }

    public float RepetitionPenalty
    {
        get => CurrentParameters.RepetitionPenalty;
        set
        {
            if (Math.Abs(CurrentParameters.RepetitionPenalty - value) > 0.001f)
            {
                CurrentParameters.RepetitionPenalty = value;
                OnPropertyChanged();
            }
        }
    }

    public float FrequencyPenalty
    {
        get => CurrentParameters.FrequencyPenalty;
        set
        {
            if (Math.Abs(CurrentParameters.FrequencyPenalty - value) > 0.001f)
            {
                CurrentParameters.FrequencyPenalty = value;
                OnPropertyChanged();
            }
        }
    }

    public float PresencePenalty
    {
        get => CurrentParameters.PresencePenalty;
        set
        {
            if (Math.Abs(CurrentParameters.PresencePenalty - value) > 0.001f)
            {
                CurrentParameters.PresencePenalty = value;
                OnPropertyChanged();
            }
        }
    }

    public int MaxTokens
    {
        get => CurrentParameters.MaxTokens;
        set
        {
            if (CurrentParameters.MaxTokens != value)
            {
                CurrentParameters.MaxTokens = value;
                OnPropertyChanged();
            }
        }
    }

    public int Seed
    {
        get => CurrentParameters.Seed;
        set
        {
            if (CurrentParameters.Seed != value)
            {
                CurrentParameters.Seed = value;
                OnPropertyChanged();
            }
        }
    }

    // Mirostat Parameters
    public int MirostatMode
    {
        get => CurrentParameters.MirostatMode;
        set
        {
            if (CurrentParameters.MirostatMode != value)
            {
                CurrentParameters.MirostatMode = value;
                OnPropertyChanged();
            }
        }
    }

    public float MirostatTau
    {
        get => CurrentParameters.MirostatTau;
        set
        {
            if (Math.Abs(CurrentParameters.MirostatTau - value) > 0.001f)
            {
                CurrentParameters.MirostatTau = value;
                OnPropertyChanged();
            }
        }
    }

    public float MirostatEta
    {
        get => CurrentParameters.MirostatEta;
        set
        {
            if (Math.Abs(CurrentParameters.MirostatEta - value) > 0.001f)
            {
                CurrentParameters.MirostatEta = value;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    #region Methods

    private async Task LoadModelsAsync()
    {
        try
        {
            IsLoading = true;
            await UpdateUIAsync(() => StatusText = "Scanning models...");

            var inventory = await ApiClient.GetAvailableModelsAsync();
            BaseModels.Clear();

            if (inventory?.BaseModels.Any() == true)
            {
                foreach (var model in inventory.BaseModels)
                    BaseModels.Add(model);
                // Only reflect active selection if a model is already active
                SelectedModel = BaseModels.FirstOrDefault(m => m.IsActive);
                await UpdateUIAsync(() => StatusText = $"Found {BaseModels.Count} base models");
            }
            else
            {
                var baseDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                var lazarusDir = Path.Combine(baseDir, "Lazarus");
                var modelsMain = Path.Combine(lazarusDir, "models", "main");
                Directory.CreateDirectory(modelsMain);
                var exts = new HashSet<string>(new[] { ".gguf", ".safetensors", ".bin", ".pth" }, System.StringComparer.OrdinalIgnoreCase);
                foreach (var file in Directory.EnumerateFiles(modelsMain, "*.*", SearchOption.TopDirectoryOnly))
                {
                    if (!exts.Contains(Path.GetExtension(file))) continue;
                    var fi = new FileInfo(file);
                    BaseModels.Add(new BaseModelDto
                    {
                        Id = System.Guid.NewGuid().ToString(),
                        Name = Path.GetFileNameWithoutExtension(file),
                        FileName = fi.FullName,
                        Size = HumanSize(fi.Length),
                        ContextLength = 4096,
                        Architecture = InferArchitecture(file),
                        Quantization = InferQuantization(file),
                        IsActive = false
                    });
                }
                // Do not auto-select the first model. Require explicit user click.
                SelectedModel = null;
                await UpdateUIAsync(() => StatusText = BaseModels.Count > 0 ? $"Found {BaseModels.Count} local models" : "No models found");
            }

            // Reflect into filtered view
            ApplyFilterAndSort();
            EnsureWatcher();

            // Sync selection from global state
            if (_globalState.CurrentModel != null)
            {
                UpdateActiveFromGlobal(_globalState.CurrentModel);
                if (!string.IsNullOrWhiteSpace(_globalState.CurrentModel.InferenceEngine))
                    SelectedRunner = _globalState.CurrentModel.InferenceEngine;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task InitializeAsync()
    {
        await LoadModelsAsync();
    }

    private void UpdateActiveFromGlobal(GlobalModelInfo info)
    {
        try
        {
            if (info == null) return;
            var match = BaseModels.FirstOrDefault(m =>
                (!string.IsNullOrWhiteSpace(m.FileName) && string.Equals(m.FileName, info.FilePath, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.Name) && string.Equals(m.Name, info.Name, StringComparison.OrdinalIgnoreCase)));
            foreach (var m in BaseModels) m.IsActive = false;
            if (match != null)
            {
                match.IsActive = true;
                SelectedModel = match;
                StatusText = $"Active: {match.Name}";
                OnPropertyChanged(nameof(IsModelLoaded));
            }
        }
        catch { }
    }

    private void ClearActiveSelection()
    {
        foreach (var m in BaseModels) m.IsActive = false;
        // Do not auto-select after unload
        SelectedModel = null;
        StatusText = "Ready";
        OnPropertyChanged(nameof(IsModelLoaded));
    }

    private async Task LoadModelAsync(BaseModelDto model)
    {
        try
        {
            IsLoading = true;
            
            // Update UI on UI thread safely
            await UpdateUIAsync(() => StatusText = $"Loading {model.Name}...");

            // Update global state -> Loading
            _globalState.SetLoading(model.Name, SelectedRunner);

            var success = await ApiClient.LoadModelAsync(model.FileName, model.Name);
            if (success)
            {
                foreach (var m in BaseModels)
                    m.IsActive = m.Id == model.Id;

                SelectedModel = model;
                // Update UI on UI thread safely
                await UpdateUIAsync(() => StatusText = $"Loaded {model.Name}");
                OnPropertyChanged(nameof(IsModelLoaded));
                try { System.Windows.Input.CommandManager.InvalidateRequerySuggested(); } catch { }

                // Load the model's parameter capabilities
                await LoadModelParametersAsync();

                // Publish to global state
                _globalState.SetLoaded(new GlobalModelInfo
                {
                    Name = model.Name,
                    FilePath = model.FileName ?? string.Empty,
                    Size = model.Size ?? string.Empty,
                    InferenceEngine = SelectedRunner,
                    ContextLength = model.ContextLength
                });
            }
            else
            {
                // Update UI on UI thread safely
                await UpdateUIAsync(() => StatusText = $"Failed to load {model.Name}");
                _globalState.SetError($"Failed to load {model.Name}");
            }
        }
        catch (Exception ex)
        {
            // Update UI on UI thread safely
            await UpdateUIAsync(() => StatusText = $"Error loading model: {ex.Message}");
            _globalState.SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UnloadModelAsync()
    {
        try
        {
            IsLoading = true;
            await UpdateUIAsync(() => StatusText = "Unloading model...");
            var result = await ApiClient.UnloadModelAsync();
            if (result)
            {
                _globalState.SetUnloaded();
                ClearActiveSelection();
                await UpdateUIAsync(() => StatusText = "Model unloaded");
                try { System.Windows.Input.CommandManager.InvalidateRequerySuggested(); } catch { }
            }
            else
            {
                await UpdateUIAsync(() => StatusText = "Failed to unload model");
            }
        }
        catch (Exception ex)
        {
            await UpdateUIAsync(() => StatusText = $"Error unloading model: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadModelParametersAsync()
    {
        if (SelectedModel == null) return;

        try
        {
            // Update UI on UI thread safely
            await UpdateUIAsync(() => StatusText = "Loading parameter schema...");

            // TODO: Query the orchestrator for this model's supported parameters
            // var schema = await ApiClient.GetModelParameterSchemaAsync(SelectedModel.Id);

            // For now, create a default schema with all parameters enabled
            var defaultSchema = CreateDefaultParameterSchemaWithTooltips();
            var defaultParameters = SelectedModel.DefaultParameters ?? new SamplingParameters();
            
            // Update UI on UI thread safely
            await UpdateUIAsync(() =>
            {
                SupportedParameterSchema = defaultSchema;
                CurrentParameters = defaultParameters;
                
                // Notify UI that all parameter properties may have changed
                OnPropertyChanged(nameof(CurrentParameters));
                OnAllParametersChanged();
            });

            // Update UI on UI thread safely
            await UpdateUIAsync(() => StatusText = $"Loaded {SelectedModel.Name} • Parameters: {SupportedParameterSchema.Count} available");
        }
        catch (Exception ex)
        {
            // Update UI on UI thread safely
            await UpdateUIAsync(() => StatusText = $"Failed to load parameter schema: {ex.Message}");
        }
    }

    private async Task ExecuteTestAsync()
    {
        if (SelectedModel == null || string.IsNullOrWhiteSpace(TestPromptText)) return;

        try
        {
            IsTestRunning = true;
            TestStatus = "Sacrificing prompt to the digital gods...";
            TestResponse = "";

            var request = new ChatCompletionRequest
            {
                Model = SelectedModel.Name,
                Messages = new List<Lazarus.Shared.OpenAI.ChatMessage>
            {
                new() { Role = "user", Content = TestPromptText }
            },
                Temperature = Temperature,
                TopP = TopP,
                TopK = TopK,
                MaxTokens = MaxTokens,
                RepetitionPenalty = RepetitionPenalty,
                FrequencyPenalty = FrequencyPenalty,
                PresencePenalty = PresencePenalty,
                Seed = Seed == -1 ? null : Seed
            };

            var response = await ApiClient.ChatCompletionAsync(request);

            if (response?.Choices != null && response.Choices.Count > 0)
            {
                TestResponse = response.Choices[0].Message.Content ?? "";
                TestStatus = "Digital ritual complete";
            }
            else
            {
                TestResponse = "The void stared back... no response from model.";
                TestStatus = "Model remained silent";
            }
        }
        catch (Exception ex)
        {
            TestResponse = $"The dark magic failed: {ex.Message}";
            TestStatus = "Ritual failed";
        }
        finally
        {
            IsTestRunning = false;
        }
    }

    private Dictionary<string, ParameterMetadata> CreateDefaultParameterSchemaWithTooltips()
    {
        var tooltips = SamplingParameters.GetParameterTooltips();

        return new Dictionary<string, ParameterMetadata>
        {
            ["Temperature"] = new()
            {
                Name = "Temperature",
                Description = tooltips["Temperature"].GetFormattedTooltip(),
                Type = "float",
                MinValue = 0.0f,
                MaxValue = 2.0f,
                DefaultValue = 0.7f
            },
            ["TopP"] = new()
            {
                Name = "Top-P",
                Description = tooltips["TopP"].GetFormattedTooltip(),
                Type = "float",
                MinValue = 0.0f,
                MaxValue = 1.0f,
                DefaultValue = 0.9f
            },
            ["TopK"] = new()
            {
                Name = "Top-K",
                Description = tooltips["TopK"].GetFormattedTooltip(),
                Type = "int",
                MinValue = 1,
                MaxValue = 100,
                DefaultValue = 40
            },
            ["MinP"] = new()
            {
                Name = "Min-P",
                Description = tooltips["MinP"].GetFormattedTooltip(),
                Type = "float",
                MinValue = 0.0f,
                MaxValue = 1.0f,
                DefaultValue = 0.05f
            },
            ["TypicalP"] = new()
            {
                Name = "Typical-P",
                Description = tooltips["TypicalP"].GetFormattedTooltip(),
                Type = "float",
                MinValue = 0.0f,
                MaxValue = 1.0f,
                DefaultValue = 1.0f
            },
            ["RepetitionPenalty"] = new()
            {
                Name = "Repetition Penalty",
                Description = tooltips["RepetitionPenalty"].GetFormattedTooltip(),
                Type = "float",
                MinValue = 0.8f,
                MaxValue = 1.3f,
                DefaultValue = 1.1f
            },
            ["FrequencyPenalty"] = new()
            {
                Name = "Frequency Penalty",
                Description = tooltips["FrequencyPenalty"].GetFormattedTooltip(),
                Type = "float",
                MinValue = -2.0f,
                MaxValue = 2.0f,
                DefaultValue = 0.0f
            },
            ["PresencePenalty"] = new()
            {
                Name = "Presence Penalty",
                Description = tooltips["PresencePenalty"].GetFormattedTooltip(),
                Type = "float",
                MinValue = -2.0f,
                MaxValue = 2.0f,
                DefaultValue = 0.0f
            },
            ["MaxTokens"] = new()
            {
                Name = "Max Tokens",
                Description = tooltips["MaxTokens"].GetFormattedTooltip(),
                Type = "int",
                MinValue = 1,
                MaxValue = 8192,
                DefaultValue = 1024
            },
            ["Seed"] = new()
            {
                Name = "Seed",
                Description = tooltips["Seed"].GetFormattedTooltip(),
                Type = "int",
                MinValue = -1,
                MaxValue = int.MaxValue,
                DefaultValue = -1
            },
            ["MirostatMode"] = new()
            {
                Name = "Mirostat Mode",
                Description = tooltips["MirostatMode"].GetFormattedTooltip(),
                Type = "int",
                MinValue = 0,
                MaxValue = 2,
                DefaultValue = 0,
                AllowedValues = new List<object> { 0, 1, 2 }
            },
            ["MirostatTau"] = new()
            {
                Name = "Mirostat Tau",
                Description = tooltips["MirostatTau"].GetFormattedTooltip(),
                Type = "float",
                MinValue = 0.1f,
                MaxValue = 10.0f,
                DefaultValue = 5.0f
            },
            ["MirostatEta"] = new()
            {
                Name = "Mirostat Eta",
                Description = tooltips["MirostatEta"].GetFormattedTooltip(),
                Type = "float",
                MinValue = 0.01f,
                MaxValue = 1.0f,
                DefaultValue = 0.1f
            }
        };
    }

    private void OnAllParametersChanged()
    {
        // Notify UI that all parameter properties have potentially changed
        var parameterProperties = new[]
        {
            nameof(Temperature), nameof(TopP), nameof(TopK), nameof(MinP), nameof(TypicalP),
            nameof(RepetitionPenalty), nameof(FrequencyPenalty), nameof(PresencePenalty),
            nameof(MaxTokens), nameof(Seed), nameof(MirostatMode), nameof(MirostatTau), nameof(MirostatEta)
        };

        foreach (var prop in parameterProperties)
        {
            OnPropertyChanged(prop);
        }
    }

    private void ApplyFilterAndSort()
    {
        var q = BaseModels.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            var term = SearchFilter.Trim();
            q = q.Where(m => m.Name?.IndexOf(term, System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
        q = SelectedSortOption switch
        {
            "Size" => q.OrderByDescending(m => ParseSizeBytes(m.Size)),
            "Date Added" => q, // TODO: keep arrival time; default unchanged
            _ => q.OrderBy(m => m.Name)
        };

        FilteredModels.Clear();
        foreach (var m in q)
        {
            m.IsSelected = SelectedModel != null && ReferenceEquals(m, SelectedModel);
            FilteredModels.Add(m);
        }
    }

    private void SelectModel(BaseModelDto? model)
    {
        if (model == null) return;
        SelectedModel = model;
    }

    private static string HumanSize(long bytes)
    {
        double size = bytes;
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        int unit = 0;
        while (size >= 1024 && unit < units.Length - 1) { size /= 1024; unit++; }
        return $"{size:F1} {units[unit]}";
    }

    private static long ParseSizeBytes(string size)
    {
        if (string.IsNullOrWhiteSpace(size)) return 0;
        var parts = size.Split(' ');
        if (parts.Length != 2) return 0;
        if (!double.TryParse(parts[0], out var v)) return 0;
        var unit = parts[1].ToUpperInvariant();
        return unit switch
        {
            "TB" => (long)(v * 1024L * 1024L * 1024L * 1024L),
            "GB" => (long)(v * 1024L * 1024L * 1024L),
            "MB" => (long)(v * 1024L * 1024L),
            "KB" => (long)(v * 1024L),
            _ => (long)v
        };
    }

    private static string InferArchitecture(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
        if (name.Contains("mistral")) return "Mistral";
        if (name.Contains("llama")) return "Llama";
        return "LLM";
    }

    private static string InferQuantization(string file)
    {
        var ext = Path.GetExtension(file).Trim('.');
        if (!string.IsNullOrEmpty(ext)) return ext.ToUpperInvariant();
        return "";
    }

    private void EnsureWatcher()
    {
        _watcher?.Dispose();
        try
        {
            var baseDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            var lazarusDir = Path.Combine(baseDir, "Lazarus");
            var modelsMain = Path.Combine(lazarusDir, "models", "main");
            Directory.CreateDirectory(modelsMain);
            _watcher = new FileSystemWatcher(modelsMain)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                Filter = "*.*",
                EnableRaisingEvents = true
            };
            _watcher.Created += (_, __) => _debounceTimer.Start();
            _watcher.Deleted += (_, __) => _debounceTimer.Start();
            _watcher.Renamed += (_, __) => _debounceTimer.Start();
            _watcher.Changed += (_, __) => _debounceTimer.Start();
        }
        catch { }
    }

    #endregion

    #region UI Thread Safety

    /// <summary>
    /// Safely update UI properties on the UI thread
    /// </summary>
    private async Task UpdateUIAsync(Action uiAction)
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app?.Dispatcher == null) return;

            if (app.Dispatcher.CheckAccess())
            {
                uiAction();
            }
            else
            {
                await app.Dispatcher.InvokeAsync(uiAction);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BaseModelViewModel] UI update failed: {ex.Message}");
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}