using System.ComponentModel;

namespace Lazarus.Desktop.Services;

public sealed class AppState : IAppState
{
    private string? _loadedLora;
    private string? _loadedTokenizer;
    private string? _loadedEmbedding;
    private double? _loraScale;
    private bool _isRunnerRunning;
    private int? _runnerPort;
    private int? _runnerPid;
    private string? _loadedModelPath;

    public string? LoadedLora
    {
        get => _loadedLora;
        set { if (_loadedLora != value) { _loadedLora = value; OnPropertyChanged(nameof(LoadedLora)); } }
    }

    public string? LoadedTokenizer
    {
        get => _loadedTokenizer;
        set { if (_loadedTokenizer != value) { _loadedTokenizer = value; OnPropertyChanged(nameof(LoadedTokenizer)); } }
    }

    public string? LoadedEmbedding
    {
        get => _loadedEmbedding;
        set { if (_loadedEmbedding != value) { _loadedEmbedding = value; OnPropertyChanged(nameof(LoadedEmbedding)); } }
    }

    public double? LoraScale
    {
        get => _loraScale;
        set { if (_loraScale != value) { _loraScale = value; OnPropertyChanged(nameof(LoraScale)); } }
    }

    public bool IsRunnerRunning
    {
        get => _isRunnerRunning;
        set { if (_isRunnerRunning != value) { _isRunnerRunning = value; OnPropertyChanged(nameof(IsRunnerRunning)); } }
    }

    public int? RunnerPort
    {
        get => _runnerPort;
        set { if (_runnerPort != value) { _runnerPort = value; OnPropertyChanged(nameof(RunnerPort)); } }
    }

    public int? RunnerPid
    {
        get => _runnerPid;
        set { if (_runnerPid != value) { _runnerPid = value; OnPropertyChanged(nameof(RunnerPid)); } }
    }

    public string? LoadedModelPath
    {
        get => _loadedModelPath;
        set { if (_loadedModelPath != value) { _loadedModelPath = value; OnPropertyChanged(nameof(LoadedModelPath)); } }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
