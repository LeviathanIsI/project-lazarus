using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lazarus.Desktop.Services;

/// <summary>
/// Background service that checks for application updates on startup
/// </summary>
public class UpdateCheckHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateCheckHostedService> _logger;

    public UpdateCheckHostedService(
        IServiceProvider serviceProvider,
        ILogger<UpdateCheckHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            // Check if auto-update check is enabled
            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            var settings = settingsService.Current;

            if (!settings.AutoUpdateCheck)
            {
                _logger.LogInformation("Auto-update check is disabled in settings");
                return;
            }

            _logger.LogInformation("Checking for application updates...");

            var updateService = _serviceProvider.GetRequiredService<IUpdateService>();
            var updateResult = await updateService.CheckAsync(stoppingToken);

            if (updateResult.IsAvailable)
            {
                _logger.LogInformation("Update available: {Latest} (current: {Current})", 
                    updateResult.Latest, updateResult.Current);

                // Notify the user about the available update
                // This could trigger a notification or update the UI
                await NotifyUpdateAvailable(updateResult);
            }
            else
            {
                _logger.LogInformation("Application is up to date: {Current}", updateResult.Current);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _logger.LogDebug("Update check was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
        }
    }

    private async Task NotifyUpdateAvailable(UpdateCheckResult updateResult)
    {
        // This method could be enhanced to show a notification to the user
        // For now, we just log the information
        _logger.LogInformation("New version {Version} is available. Current version: {Current}",
            updateResult.Latest, updateResult.Current);

        // In the future, this could:
        // - Show a system notification
        // - Update a property that the UI binds to
        // - Trigger an event that the main window subscribes to

        await Task.CompletedTask;
    }
}