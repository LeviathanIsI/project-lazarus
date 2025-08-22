using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Lazarus.Desktop.Helpers;
using Lazarus.Shared.OpenAI;
using System.IO;
using System.Linq;



namespace Lazarus.Desktop.ViewModels;

public class ModelsViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string _statusText = "Ready";
    private SystemInfo? _systemInfo;
    private BaseModelDto? _selectedModel;

    public ModelsViewModel()
    {
        BaseModels = new ObservableCollection<BaseModelDto>();
        LoRAs = new ObservableCollection<LoraDto>();
        VAEs = new ObservableCollection<VaeDto>();
        Embeddings = new ObservableCollection<EmbeddingDto>();
        Hypernetworks = new ObservableCollection<HypernetworkDto>();

        LoadModelsCommand = new RelayCommand(async _ => await LoadModelsAsync(), _ => !IsLoading);
        LoadModelCommand = new RelayCommand(async model => await LoadModelAsync((BaseModelDto)model!),
            model => !IsLoading && model is BaseModelDto);
        RefreshSystemCommand = new RelayCommand(async _ => await RefreshSystemInfoAsync(), _ => !IsLoading);

        // Load data on startup
        _ = Task.Run(async () =>
        {
            await LoadModelsAsync();
            await RefreshSystemInfoAsync();
        });
    }

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

    public SystemInfo? SystemInfo
    {
        get => _systemInfo;
        set => SetProperty(ref _systemInfo, value);
    }

    public BaseModelDto? SelectedModel
    {
        get => _selectedModel;
        set => SetProperty(ref _selectedModel, value);
    }

    public ObservableCollection<BaseModelDto> BaseModels { get; }
    public ObservableCollection<LoraDto> LoRAs { get; }
    public ObservableCollection<VaeDto> VAEs { get; }
    public ObservableCollection<EmbeddingDto> Embeddings { get; }
    public ObservableCollection<HypernetworkDto> Hypernetworks { get; }

    public ICommand LoadModelsCommand { get; }
    public ICommand LoadModelCommand { get; }
    public ICommand RefreshSystemCommand { get; }

    private async Task LoadModelsAsync()
    {
        try
        {
            IsLoading = true;
            StatusText = "Scanning models...";

            var inventory = await ApiClient.GetAvailableModelsAsync();

            BaseModels.Clear();
            LoRAs.Clear();
            VAEs.Clear();
            Embeddings.Clear();
            Hypernetworks.Clear();

            if (inventory != null && inventory.BaseModels.Any())
            {
                foreach (var model in inventory.BaseModels)
                    BaseModels.Add(model);

                foreach (var lora in inventory.Loras)
                    LoRAs.Add(lora);

                foreach (var vae in inventory.Vaes)
                    VAEs.Add(vae);

                foreach (var embedding in inventory.Embeddings)
                    Embeddings.Add(embedding);

                foreach (var hypernetwork in inventory.Hypernetworks)
                    Hypernetworks.Add(hypernetwork);

                SelectedModel = BaseModels.FirstOrDefault(m => m.IsActive);
                StatusText = $"Found {BaseModels.Count} models, {LoRAs.Count} LoRAs, {VAEs.Count} VAEs";
            }
            else
            {
                // fallback: scan local "models" folder
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
                            ContextLength = 4096, // default fallback
                            Architecture = "LLM", // fallback
                            Quantization = Path.GetExtension(file).Trim('.').ToUpper(),
                            IsActive = false
                        });
                    }

                    SelectedModel = BaseModels.FirstOrDefault();
                    StatusText = $"Found {BaseModels.Count} local model(s)";
                }
                else
                {
                    StatusText = "No models found (API or local)";
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
                // Update active state
                foreach (var m in BaseModels)
                {
                    var isActive = m.Id == model.Id;
                    if (m.IsActive != isActive)
                    {
                        var updated = m with { IsActive = isActive };
                        var index = BaseModels.IndexOf(m);
                        BaseModels[index] = updated;
                    }
                }

                SelectedModel = BaseModels.First(m => m.Id == model.Id);
                StatusText = $"Loaded {model.Name}";
            }
            else
            {
                StatusText = $"Failed to load {model.Name}";
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

    private async Task RefreshSystemInfoAsync()
    {
        try
        {
            var info = await ApiClient.GetSystemInfoAsync();
            if (info != null)
            {
                SystemInfo = info;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"System info error: {ex.Message}";
        }
    }

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
}