using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lazarus.App.Data.Services;

/// <summary>
/// Background service that performs database exorcism on application startup
/// to eliminate all phantom asset consciousness
/// </summary>
public class DatabaseExorcismStartupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseExorcismStartupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseExorcismStartupService"/> class
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="logger">The logger</param>
    public DatabaseExorcismStartupService(
        IServiceProvider serviceProvider,
        ILogger<DatabaseExorcismStartupService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("DATABASE EXORCISM SERVICE: Initiating phantom elimination on startup");

            // Wait a moment for the application to fully start up
            await Task.Delay(2000, stoppingToken).ConfigureAwait(false);

            // Create async scope for proper disposal in background service
            await using var scope = _serviceProvider.CreateAsyncScope();
            var purificationService = scope.ServiceProvider.GetRequiredService<IAssetRegistryPurificationService>();

            // NUCLEAR OPTION: Complete phantom purge on startup
            var purificationResult = await purificationService.PurgeAllPhantomEntriesAsync(stoppingToken).ConfigureAwait(false);

            if (purificationResult.Success)
            {
                _logger.LogInformation("DATABASE EXORCISM COMPLETE: Eliminated {PhantomCount} phantom entries in {Duration:F2}s",
                    purificationResult.PhantomsEliminated, purificationResult.Duration.TotalSeconds);

                // Optionally perform initial asset discovery from common directories
                var commonAssetDirectories = GetCommonAssetDirectories();
                if (commonAssetDirectories.Any())
                {
                    _logger.LogInformation("Performing initial asset discovery in {DirectoryCount} directories", 
                        commonAssetDirectories.Count());

                    var discoveryResult = await purificationService.DiscoverAndReconcileAssetsAsync(
                        commonAssetDirectories, stoppingToken).ConfigureAwait(false);

                    if (discoveryResult.Success)
                    {
                        _logger.LogInformation("Asset discovery complete: {NewAssets} new assets registered from {FilesFound} files",
                            discoveryResult.NewAssetsRegistered, discoveryResult.FilesDiscovered);
                    }
                    else
                    {
                        _logger.LogWarning("Asset discovery failed: {ErrorMessage}", discoveryResult.ErrorMessage);
                    }
                }
            }
            else
            {
                _logger.LogError("DATABASE EXORCISM FAILED: {ErrorMessage}", purificationResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical failure during database exorcism startup operation");
        }
    }

    /// <summary>
    /// Gets list of common directories where LLM assets might be stored
    /// </summary>
    /// <returns>List of directories to scan</returns>
    private IEnumerable<string> GetCommonAssetDirectories()
    {
        var directories = new List<string>();
        
        try
        {
            // Common model storage locations
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Potential model directories
            var candidates = new[]
            {
                Path.Combine(userProfile, "AppData", "Local", "LM Studio", "models"),
                Path.Combine(appData, "LM Studio", "models"),
                Path.Combine(localAppData, "LM Studio", "models"),
                Path.Combine(userProfile, "Downloads"), // Many users download models here
                Path.Combine(userProfile, "models"),
                Path.Combine(userProfile, "llm-models"),
                "D:\\models", // Common dedicated drive location
                "C:\\models"
            };

            // Only add directories that actually exist
            directories.AddRange(candidates.Where(Directory.Exists));

            _logger.LogDebug("Found {DirectoryCount} existing asset directories to scan", directories.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect common asset directories");
        }

        return directories;
    }
}