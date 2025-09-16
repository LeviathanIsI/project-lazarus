using System;

namespace Lazarus.Desktop.Services;

public interface IOrchestratorRunnerClient : IDisposable
{
    Task<bool> LoadModelAsync(string modelPath, IEnumerable<string>? loras = null, double? loraScale = null, CancellationToken cancellationToken = default);
    Task<bool> UnloadAsync(CancellationToken cancellationToken = default);
    Task<RunnerProcessStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    string? LastError { get; }
    event EventHandler<RunnerProcessStatus>? RunnerStatusChanged;
}

public sealed record RunnerProcessStatus(
    bool IsRunning,
    string? ModelPath,
    int? Pid,
    int? Port = null,
    string? ExePath = null,
    string? OutLog = null,
    string? ErrLog = null,
    int? LorasApplied = null,
    string? LaunchArgs = null,
    string? CmdPath = null,
    int? LoraEvidenceCount = null
);
