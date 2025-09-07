using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.Services;

internal sealed class OrchestratorBootstrapHostedService : IHostedService
{
    private readonly ILogger<OrchestratorBootstrapHostedService> _logger;
    private readonly IOrchestratorProcessService _proc;

    public OrchestratorBootstrapHostedService(ILogger<OrchestratorBootstrapHostedService> logger, IOrchestratorProcessService proc)
    {
        _logger = logger;
        _proc = proc;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("OrchestratorBootstrapHostedService starting");
        await _proc.StartIfNeededAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("OrchestratorBootstrapHostedService stopping");
        await _proc.StopIfOwnedAsync(cancellationToken).ConfigureAwait(false);
    }
}

