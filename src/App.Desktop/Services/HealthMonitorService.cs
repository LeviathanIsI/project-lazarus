using Lazarus.Desktop.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Background service that monitors the health of the orchestrator and runners.
/// </summary>
internal sealed class HealthMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HealthMonitorService> _logger;
    private readonly IOptionsMonitor<OrchestratorOptions> _options;
    private readonly Lazarus.Shared.Settings.ISettingsService _settings;
    private readonly IOrchestratorProcessService _proc;

    public HealthMonitorService(
        IServiceScopeFactory scopeFactory,
        ILogger<HealthMonitorService> logger,
        IOptionsMonitor<OrchestratorOptions> options,
        Lazarus.Shared.Settings.ISettingsService settings,
        IOrchestratorProcessService proc)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _proc = proc ?? throw new ArgumentNullException(nameof(proc));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health monitor service starting");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var interval = TimeSpan.FromSeconds(Math.Max(1, _settings.Current.OrchestratorHealthCheckIntervalSec));

                try
                {
                    await PerformHealthChecksAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Error during health check cycle");
                }

                // Wait for the configured interval or until cancellation
                try
                {
                    await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Health monitor service failed with unhandled exception");
            throw;
        }
        finally
        {
            _logger.LogInformation("Health monitor service stopped");
        }
    }

    private async Task PerformHealthChecksAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var orchestratorClient = scope.ServiceProvider.GetRequiredService<IOrchestratorClient>();

        // Check orchestrator health
        var isOrchestratorHealthy = await orchestratorClient.CheckHealthAsync(cancellationToken)
            .ConfigureAwait(false);

        if (isOrchestratorHealthy)
        {
            // If orchestrator is healthy, also check runner statuses
            try
            {
                var runnerStatuses = await orchestratorClient.GetRunnerStatusAsync(cancellationToken)
                    .ConfigureAwait(false);

                var unhealthyRunners = runnerStatuses
                    .Where(status => !status.IsHealthy)
                    .ToList();

                if (unhealthyRunners.Any())
                {
                    _logger.LogWarning("Found {Count} unhealthy runners: {RunnerIds}",
                        unhealthyRunners.Count,
                        string.Join(", ", unhealthyRunners.Select(r => r.Id)));
                }

                _logger.LogDebug("Health check completed - Orchestrator: {OrchestratorHealth}, Runners: {TotalRunners} ({HealthyRunners} healthy)",
                    isOrchestratorHealthy ? "Healthy" : "Unhealthy",
                    runnerStatuses.Count(),
                    runnerStatuses.Count(r => r.IsHealthy));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check runner health status");
            }
        }
        else
        {
            _logger.LogWarning("Orchestrator health check failed - skipping runner status checks");
            if (_settings.Current.OrchestratorAutoRestartOnCrash)
            {
                _logger.LogInformation("Attempting auto-restart of orchestrator (enabled in settings)");
                try { await _proc.StartIfNeededAsync(cancellationToken).ConfigureAwait(false); } catch { }
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health monitor service stop requested");
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
