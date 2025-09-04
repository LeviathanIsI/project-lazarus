using Lazarus.App.Shared.Models;

namespace Lazarus.App.Data.Services;

/// <summary>
/// Service interface for purifying the asset registry by eliminating phantom entries
/// and maintaining strict filesystem-first validation
/// </summary>
public interface IAssetRegistryPurificationService
{
    /// <summary>
    /// Performs complete database exorcism by eliminating all phantom asset entries
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Purification results with phantom count eliminated</returns>
    Task<AssetRegistryPurificationResult> PurgeAllPhantomEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates all existing assets against filesystem and removes orphaned entries
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Cleanup results with orphan count removed</returns>
    Task<AssetRegistryCleanupResult> CleanupOrphanedAssetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a single asset file exists on disk before allowing database registration
    /// </summary>
    /// <param name="filePath">The absolute file path to validate</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if file exists and is accessible; otherwise false</returns>
    Task<bool> ValidateAssetFileExistsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an asset with strict filesystem-first validation
    /// Only registers assets that physically exist on disk
    /// </summary>
    /// <param name="filePath">The absolute file path</param>
    /// <param name="assetType">The type of asset</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The registered asset if successful; otherwise null</returns>
    Task<LlmAsset?> RegisterAssetWithValidationAsync(string filePath, LlmAssetType assetType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs automatic registry hygiene by removing entries for files that no longer exist
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Registry hygiene results</returns>
    Task<AssetRegistryHygieneResult> PerformRegistryHygieneAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans specified directories for actual asset files and reconciles with database
    /// </summary>
    /// <param name="directoriesToScan">List of directories to scan for assets</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Discovery and reconciliation results</returns>
    Task<AssetDiscoveryResult> DiscoverAndReconcileAssetsAsync(IEnumerable<string> directoriesToScan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Results from database purification operation
/// </summary>
public class AssetRegistryPurificationResult
{
    /// <summary>
    /// Gets or sets the number of phantom entries eliminated
    /// </summary>
    public int PhantomsEliminated { get; set; }

    /// <summary>
    /// Gets or sets whether the purification was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets any error messages from the purification
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the duration of the purification operation
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Results from orphaned asset cleanup operation
/// </summary>
public class AssetRegistryCleanupResult
{
    /// <summary>
    /// Gets or sets the number of orphaned entries removed
    /// </summary>
    public int OrphansRemoved { get; set; }

    /// <summary>
    /// Gets or sets the number of valid assets retained
    /// </summary>
    public int ValidAssetsRetained { get; set; }

    /// <summary>
    /// Gets or sets whether the cleanup was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets any error messages from the cleanup
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the list of removed asset paths
    /// </summary>
    public List<string> RemovedAssetPaths { get; set; } = new();
}

/// <summary>
/// Results from registry hygiene operation
/// </summary>
public class AssetRegistryHygieneResult
{
    /// <summary>
    /// Gets or sets the number of missing files detected
    /// </summary>
    public int MissingFilesDetected { get; set; }

    /// <summary>
    /// Gets or sets the number of entries cleaned up
    /// </summary>
    public int EntriesCleanedUp { get; set; }

    /// <summary>
    /// Gets or sets whether the hygiene operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets any error messages
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Results from asset discovery and reconciliation operation
/// </summary>
public class AssetDiscoveryResult
{
    /// <summary>
    /// Gets or sets the number of files discovered on disk
    /// </summary>
    public int FilesDiscovered { get; set; }

    /// <summary>
    /// Gets or sets the number of new assets registered
    /// </summary>
    public int NewAssetsRegistered { get; set; }

    /// <summary>
    /// Gets or sets the number of existing assets updated
    /// </summary>
    public int ExistingAssetsUpdated { get; set; }

    /// <summary>
    /// Gets or sets whether the discovery was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets any error messages
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the list of discovered asset paths
    /// </summary>
    public List<string> DiscoveredAssetPaths { get; set; } = new();
}