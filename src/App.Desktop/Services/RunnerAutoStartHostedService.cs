using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Lazarus.Shared.Settings;
using System.Linq;

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

            // Prepare LoRA adapters if configured
            List<string>? loras = null;
            double? loraScale = null;

            if (!string.IsNullOrWhiteSpace(_settings.Current.ActiveLoraPath))
            {
                _logger.LogInformation("Restoring LoRA adapter: {Path} with scale {Scale}",
                    _settings.Current.ActiveLoraPath, _settings.Current.ActiveLoraScale);

                // Check if the LoRA path exists
                if (System.IO.Directory.Exists(_settings.Current.ActiveLoraPath))
                {
                    // Look for GGUF files for llama.cpp compatibility
                    var ggufFiles = System.IO.Directory.GetFiles(_settings.Current.ActiveLoraPath, "*.gguf",
                        System.IO.SearchOption.AllDirectories);

                    if (ggufFiles.Length > 0)
                    {
                        var orderedAdapters = ggufFiles.OrderBy(System.IO.Path.GetFileName).ToList();
                        loras = orderedAdapters;
                        loraScale = _settings.Current.ActiveLoraScale;
                        _logger.LogInformation("Found GGUF adapter(s): {Paths}", string.Join(", ", orderedAdapters));
                    }
                    else
                    {
                        // Pass the directory and let the runner handle it
                        loras = new List<string> { _settings.Current.ActiveLoraPath };
                        loraScale = _settings.Current.ActiveLoraScale;
                        _logger.LogWarning("No GGUF files found, passing directory: {Path}",
                            _settings.Current.ActiveLoraPath);
                    }
                }
                else if (System.IO.File.Exists(_settings.Current.ActiveLoraPath))
                {
                    if (string.Equals(System.IO.Path.GetExtension(_settings.Current.ActiveLoraPath), ".gguf", StringComparison.OrdinalIgnoreCase))
                    {
                        loras = new List<string> { _settings.Current.ActiveLoraPath };
                        loraScale = _settings.Current.ActiveLoraScale;
                        _logger.LogInformation("Using GGUF adapter file: {Path}", _settings.Current.ActiveLoraPath);
                    }
                    else
                    {
                        _logger.LogWarning("Active LoRA file is not GGUF: {Path}", _settings.Current.ActiveLoraPath);
                    }
                }
                else
                {
                    _logger.LogWarning("Saved LoRA path no longer exists: {Path}",
                        _settings.Current.ActiveLoraPath);
                }
            }

            // Attempt to load the last model with LoRA if configured
            _logger.LogInformation("Auto-starting runner with last model: {ModelPath}", modelPath);
            var ok = await _runnerClient.LoadModelAsync(modelPath, loras, loraScale, cancellationToken).ConfigureAwait(false);
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

