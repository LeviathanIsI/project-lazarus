using System;

namespace Lazarus.Desktop.Services;

public interface IOrchestratorRunnerClient : IDisposable
{
    Task<bool> LoadModelAsync(string modelPath, CancellationToken cancellationToken = default);
    Task<bool> UnloadAsync(CancellationToken cancellationToken = default);
    Task<RunnerProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    string? LastError { get; }
}

public sealed record RunnerProcessStatus(
    bool IsRunning,
    string? ModelPath,
    int? Pid
);
