using System.ComponentModel;

namespace Lazarus.Desktop.Services;

public interface IAppState : INotifyPropertyChanged
{
    string? LoadedLora { get; set; }
    string? LoadedTokenizer { get; set; }
    string? LoadedEmbedding { get; set; }
    double? LoraScale { get; set; }
    bool IsRunnerRunning { get; set; }
    int? RunnerPort { get; set; }
    int? RunnerPid { get; set; }
    string? LoadedModelPath { get; set; }
}
