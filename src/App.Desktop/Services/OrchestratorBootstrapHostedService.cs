using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.Services;

internal sealed class OrchestratorBootstrapHostedService : IHostedService
{
    private readonly ILogger<OrchestratorBootstrapHostedService> _logger;
    private readonly IOrchestratorProcessService _proc;
    private readonly Lazarus.Shared.Settings.ISettingsService _settings;

    public OrchestratorBootstrapHostedService(ILogger<OrchestratorBootstrapHostedService> logger, IOrchestratorProcessService proc, Lazarus.Shared.Settings.ISettingsService settings)
    {
        _logger = logger;
        _proc = proc;
        _settings = settings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("OrchestratorBootstrapHostedService starting");
        // Load settings to honor persisted preference before attempting start
        try { await _settings.LoadAsync().ConfigureAwait(false); } catch { }

        if (!_settings.Current.StartOrchestratorWithApp)
        {
            _logger.LogInformation("StartOrchestratorWithApp is disabled; skipping orchestrator auto-start");
            return;
        }

        await _proc.StartIfNeededAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("OrchestratorBootstrapHostedService stopping");
        await _proc.StopIfOwnedAsync(cancellationToken).ConfigureAwait(false);
    }
}
