using System;
using System.Threading.Tasks;

namespace Lazarus.Desktop.Services;

public sealed class RunnerStatusProvider
{
    public sealed record RunnerState(bool IsHealthy, bool IsRunning, string? ModelName, string? ModelPath);

    private readonly IOrchestratorClient? _orchestrator;
    private readonly IOrchestratorRunnerClient? _runnerClient;

    public event EventHandler<RunnerState>? RunnerStateChanged;

    public RunnerState Current { get; private set; } = new RunnerState(IsHealthy: false, IsRunning: false, ModelName: null, ModelPath: null);

    public RunnerStatusProvider() { }

    // When DI provides orchestrator + runner client, subscribe to drive global state
    public RunnerStatusProvider(IOrchestratorClient orchestrator, IOrchestratorRunnerClient runnerClient)
    {
        _orchestrator = orchestrator;
        _runnerClient = runnerClient;

        try
        {
            _runnerClient.RunnerStatusChanged += OnRunnerStatusChanged;
            // Prime status in the background
            _ = Task.Run(async () =>
            {
                try
                {
                    var s = await _runnerClient.GetStatusAsync().ConfigureAwait(false);
                    SetState(_orchestrator?.IsHealthy ?? false,
                             s.IsRunning,
                             string.IsNullOrWhiteSpace(s.ModelPath) ? null : System.IO.Path.GetFileNameWithoutExtension(s.ModelPath),
                             s.ModelPath);
                }
                catch { }
            });
        }
        catch { }
    }

    private void OnRunnerStatusChanged(object? sender, RunnerProcessStatus s)
    {
        try
        {
            SetState(_orchestrator?.IsHealthy ?? false,
                     s.IsRunning,
                     string.IsNullOrWhiteSpace(s.ModelPath) ? null : System.IO.Path.GetFileNameWithoutExtension(s.ModelPath),
                     s.ModelPath);
        }
        catch { }
    }

    // Minimal API surface to notify listeners if other services update status later
    public void SetState(bool isHealthy, bool isRunning, string? modelName, string? modelPath)
    {
        var state = new RunnerState(isHealthy, isRunning, modelName, modelPath);
        Current = state;
        RunnerStateChanged?.Invoke(this, state);
    }
}
