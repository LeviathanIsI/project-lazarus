using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Lazarus.Desktop.Services;

public enum ModelLoadStatus
{
    Idle,
    Loading,
    Loaded,
    Error
}

public sealed class GlobalModelInfo
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string Size { get; set; } = "";
    public string InferenceEngine { get; set; } = ""; // llama.cpp, vLLM, etc.
    public int? ContextLength { get; set; }
}

/// <summary>
/// Singleton service holding application-wide model state with persistence.
/// ViewModels bind to this instead of keeping isolated state.
/// </summary>
public sealed class GlobalModelStateService : INotifyPropertyChanged
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private GlobalModelInfo? _currentModel;
    private ModelLoadStatus _loadStatus = ModelLoadStatus.Idle;
    private string _stateFilePath;

    public GlobalModelStateService()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var lazarusDir = Path.Combine(baseDir, "Lazarus");
        Directory.CreateDirectory(lazarusDir);
        _stateFilePath = Path.Combine(lazarusDir, "state.json");
    }

    public GlobalModelInfo? CurrentModel
    {
        get => _currentModel;
        private set { _currentModel = value; OnPropertyChanged(); }
    }

    public ModelLoadStatus LoadStatus
    {
        get => _loadStatus;
        private set { _loadStatus = value; OnPropertyChanged(); OnLoadStatusChanged(); }
    }

    // Events for UI/VMs
    public event EventHandler<GlobalModelInfo>? ModelLoaded;
    public event EventHandler? ModelUnloaded;
    public event EventHandler<ModelLoadStatus>? LoadStatusChanged;

    public void SetLoading(string modelName, string inferenceEngine)
    {
        CurrentModel = new GlobalModelInfo { Name = modelName, InferenceEngine = inferenceEngine };
        LoadStatus = ModelLoadStatus.Loading;
        PersistSafe();
    }

    public void SetLoaded(GlobalModelInfo info)
    {
        CurrentModel = info;
        LoadStatus = ModelLoadStatus.Loaded;
        PersistSafe();
        ModelLoaded?.Invoke(this, info);
    }

    public void SetError(string? message = null)
    {
        LoadStatus = ModelLoadStatus.Error;
        PersistSafe();
    }

    public void SetUnloaded()
    {
        CurrentModel = null;
        LoadStatus = ModelLoadStatus.Idle;
        PersistSafe();
        ModelUnloaded?.Invoke(this, EventArgs.Empty);
    }

    public void Restore()
    {
        try
        {
            if (!File.Exists(_stateFilePath)) return;
            var json = File.ReadAllText(_stateFilePath);
            var dto = JsonSerializer.Deserialize<PersistedState>(json, JsonOptions);
            if (dto == null) return;

            if (!string.IsNullOrWhiteSpace(dto.Model?.FilePath) && File.Exists(dto.Model.FilePath))
            {
                CurrentModel = dto.Model;
                LoadStatus = dto.Status;
            }
            else
            {
                // File moved/deleted - clear but keep last known name for info
                CurrentModel = string.IsNullOrWhiteSpace(dto.Model?.Name) ? null : new GlobalModelInfo { Name = dto.Model!.Name };
                LoadStatus = ModelLoadStatus.Idle;
            }
        }
        catch { }
    }

    private void PersistSafe()
    {
        try
        {
            var dto = new PersistedState { Model = CurrentModel, Status = LoadStatus };
            var json = JsonSerializer.Serialize(dto, JsonOptions);
            File.WriteAllText(_stateFilePath, json);
        }
        catch { }
    }

    private void OnLoadStatusChanged()
    {
        LoadStatusChanged?.Invoke(this, _loadStatus);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private sealed class PersistedState
    {
        public GlobalModelInfo? Model { get; set; }
        public ModelLoadStatus Status { get; set; }
    }
}



