using System;

namespace Lazarus.Desktop.Services;

public sealed class RunnerStatusProvider
{
    public sealed record RunnerState(bool IsHealthy, bool IsRunning, string? ModelName, string? ModelPath);

    public event EventHandler<RunnerState>? RunnerStateChanged;

    public RunnerState Current { get; private set; } = new RunnerState(IsHealthy: false, IsRunning: false, ModelName: null, ModelPath: null);

    // Minimal API surface to notify listeners if other services update status later
    public void SetState(bool isHealthy, bool isRunning, string? modelName, string? modelPath)
    {
        var state = new RunnerState(isHealthy, isRunning, modelName, modelPath);
        Current = state;
        RunnerStateChanged?.Invoke(this, state);
    }
}

