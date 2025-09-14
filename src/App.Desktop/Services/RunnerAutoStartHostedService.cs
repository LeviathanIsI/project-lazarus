using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Lazarus.Shared.Settings;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Attempts to auto-start the last used runner/model on app startup
/// if enabled in settings and the orchestrator is reachable.
/// </summary>
internal sealed class RunnerAutoStartHostedService : IHostedService
{
    private readonly ILogger<RunnerAutoStartHostedService> _logger;
    private readonly ISettingsService _settings;
    private readonly IOrchestratorClient _orchestratorClient;
    private readonly IOrchestratorRunnerClient _runnerClient;

    public RunnerAutoStartHostedService(
        ILogger<RunnerAutoStartHostedService> logger,
        ISettingsService settings,
        IOrchestratorClient orchestratorClient,
        IOrchestratorRunnerClient runnerClient)
    {
        _logger = logger;
        _settings = settings;
        _orchestratorClient = orchestratorClient;
        _runnerClient = runnerClient;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Ensure we have settings loaded
            try { await _settings.LoadAsync().ConfigureAwait(false); } catch { }

            if (!_settings.Current.AutoStartLastRunner)
            {
                _logger.LogInformation("AutoStartLastRunner disabled; skipping runner auto-start");
                return;
            }

            var modelPath = _settings.Current.ActiveModelId;
            if (string.IsNullOrWhiteSpace(modelPath))
            {
                _logger.LogInformation("No ActiveModelId configured; skipping runner auto-start");
                return;
            }

            // Wait for orchestrator to be healthy up to the configured timeout
            var timeoutSec = Math.Max(5, _settings.Current.OrchestratorStartupTimeoutSec);
            var deadline = DateTimeOffset.UtcNow.AddSeconds(timeoutSec);
            while (DateTimeOffset.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                if (await _orchestratorClient.CheckHealthAsync(cancellationToken).ConfigureAwait(false))
                    break;
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }

            if (!_orchestratorClient.IsHealthy)
            {
                _logger.LogWarning("Orchestrator not healthy within {Timeout}s; skipping runner auto-start", timeoutSec);
                return;
            }

            var status = await _runnerClient.GetStatusAsync(cancellationToken).ConfigureAwait(false);
            if (status.IsRunning)
            {
                _logger.LogInformation("Runner already running (PID {Pid}); skipping auto-start", status.Pid);
                return;
            }

            // Attempt to load the last model
            _logger.LogInformation("Auto-starting runner with last model: {ModelPath}", modelPath);
            var ok = await _runnerClient.LoadModelAsync(modelPath, cancellationToken).ConfigureAwait(false);
            if (!ok)
            {
                _logger.LogWarning("Auto-start last runner failed: {Error}", _runnerClient.LastError ?? "unknown error");
            }
            else
            {
                _logger.LogInformation("Runner auto-started successfully");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during runner auto-start");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

