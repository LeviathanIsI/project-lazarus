using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lazarus.Desktop.Configuration;
using Lazarus.Shared.Settings;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Performs an optional update check at startup if enabled by settings.
/// Does not download or install updates; logs availability only.
/// </summary>
internal sealed class UpdateCheckHostedService : IHostedService
{
    private readonly ILogger<UpdateCheckHostedService> _logger;
    private readonly ISettingsService _settings;
    private readonly IUpdateService _updateService;

    public UpdateCheckHostedService(
        ILogger<UpdateCheckHostedService> logger,
        ISettingsService settings,
        IUpdateService updateService)
    {
        _logger = logger;
        _settings = settings;
        _updateService = updateService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            try { await _settings.LoadAsync().ConfigureAwait(false); } catch { }

            if (!_settings.Current.CheckForUpdatesOnStart)
            {
                _logger.LogDebug("CheckForUpdatesOnStart disabled; skipping update check");
                return;
            }

            var result = await _updateService.CheckAsync(cancellationToken).ConfigureAwait(false);
            if (result.Latest is null)
            {
                _logger.LogInformation("Update check did not return a version from {Feed}", result.FeedUrl ?? "<unset>");
                return;
            }
            if (result.IsAvailable)
            {
                _logger.LogInformation("New Lazarus version available: {Latest} (current {Current}). {Notes}", result.Latest, result.Current, result.ReleaseNotesUrl ?? string.Empty);
            }
            else
            {
                _logger.LogInformation("Lazarus is up-to-date (version {Version})", result.Current);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    
}
