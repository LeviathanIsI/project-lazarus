using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;
using Lazarus.Shared.OpenAI;
using Lazarus.Shared.Models;
using System.IO;

namespace Lazarus.Desktop.ViewModels;

public class BaseModelViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string _statusText = "Ready";
    private BaseModelDto? _selectedModel;
    private SamplingParameters _currentParameters = new();
    private Dictionary<string, ParameterMetadata> _supportedParameterSchema = new();

    // Test Prompt Properties - The blood ritual components
    private string _testPromptText = "Write a short story about a digital vampire who feeds on corrupted data...";
    private string _testResponse = "";
    private string _testStatus = "Ready to test";
    private bool _isTestRunning = false;

    public BaseModelViewModel()
    {
        BaseModels = new ObservableCollection<BaseModelDto>();

        LoadModelsCommand = new RelayCommand(async _ => await LoadModelsAsync(), _ => !IsLoading);
        LoadModelCommand = new RelayCommand(async model => await LoadModelAsync((BaseModelDto)model!),
            model => !IsLoading && model is BaseModelDto);
        LoadParametersCommand = new RelayCommand(async _ => await LoadModelParametersAsync(), _ => !IsLoading && SelectedModel != null);
        TestParametersCommand = new RelayCommand(async _ => await ExecuteTestAsync(), _ => CanExecuteTest);

        // Load models on the UI thread like a civilized human being
        _ = LoadModelsAsync();
    }

    #region Properties

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public BaseModelDto? SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (SetProperty(ref _selectedModel, value))
            {
                OnPropertyChanged(nameof(CanExecuteTest));
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

    public ObservableCollection<BaseModelDto> BaseModels { get; }

    #endregion

    #region Commands

    public ICommand LoadModelsCommand { get; }
    public ICommand LoadModelCommand { get; }
    public ICommand LoadParametersCommand { get; }
    public ICommand TestParametersCommand { get; }

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
            StatusText = "Scanning models...";

            var inventory = await ApiClient.GetAvailableModelsAsync();

            // Clear and populate on UI thread
            BaseModels.Clear();

            if (inventory?.BaseModels.Any() == true)
            {
                foreach (var model in inventory.BaseModels)
                    BaseModels.Add(model);

                SelectedModel = BaseModels.FirstOrDefault(m => m.IsActive);
                StatusText = $"Found {BaseModels.Count} base models";
            }
            else
            {
                var modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models");
                if (Directory.Exists(modelsPath))
                {
                    foreach (var file in Directory.GetFiles(modelsPath, "*.gguf"))
                    {
                        var fileInfo = new FileInfo(file);
                        BaseModels.Add(new BaseModelDto
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = Path.GetFileNameWithoutExtension(file),
                            FileName = fileInfo.FullName,
                            Size = $"{fileInfo.Length / (1024 * 1024)} MB",
                            ContextLength = 4096,
                            Architecture = "LLM",
                            Quantization = Path.GetExtension(file).Trim('.').ToUpper(),
                            IsActive = false
                        });
                    }
                    SelectedModel = BaseModels.FirstOrDefault();
                    StatusText = $"Found {BaseModels.Count} local models";
                }
                else
                {
                    StatusText = "No models found";
                }
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadModelAsync(BaseModelDto model)
    {
        try
        {
            IsLoading = true;
            StatusText = $"Loading {model.Name}...";

            var success = await ApiClient.LoadModelAsync(model.FileName, model.Name);
            if (success)
            {
                foreach (var m in BaseModels)
                    m.IsActive = m.Id == model.Id;

                SelectedModel = model;
                StatusText = $"Loaded {model.Name}";

                // Load the model's parameter capabilities
                await LoadModelParametersAsync();
            }
            else
            {
                StatusText = $"Failed to load {model.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error loading model: {ex.Message}";
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
            StatusText = "Loading parameter schema...";

            // TODO: Query the orchestrator for this model's supported parameters
            // var schema = await ApiClient.GetModelParameterSchemaAsync(SelectedModel.Id);

            // For now, create a default schema with all parameters enabled
            SupportedParameterSchema = CreateDefaultParameterSchemaWithTooltips();

            // Initialize current parameters with model defaults or sensible defaults
            CurrentParameters = SelectedModel.DefaultParameters ?? new SamplingParameters();

            // Notify UI that all parameter properties may have changed
            OnPropertyChanged(nameof(CurrentParameters));
            OnAllParametersChanged();

            StatusText = $"Loaded {SelectedModel.Name} • Parameters: {SupportedParameterSchema.Count} available";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed to load parameter schema: {ex.Message}";
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