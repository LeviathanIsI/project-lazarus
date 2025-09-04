using Lazarus.App.Shared.Models;

namespace Lazarus.App.Data.Services;

/// <summary>
/// Service interface for the Asset Keeper - manages LLM assets with registry integration
/// </summary>
public interface IAssetKeeperService
{
    /// <summary>
    /// Scans and registers models from a directory
    /// </summary>
    /// <param name="directoryPath">The directory path to scan</param>
    /// <param name="includeSubdirectories">Whether to scan subdirectories recursively</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of new assets discovered and registered</returns>
    Task<int> ScanAndRegisterModelsAsync(string directoryPath, bool includeSubdirectories = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a single model file
    /// </summary>
    /// <param name="filePath">The path to the model file</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The registered LLM asset if successful; otherwise null</returns>
    Task<LlmAsset?> RegisterModelAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered assets
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of all registered LLM assets</returns>
    Task<IEnumerable<LlmAsset>> GetAllAssetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assets by type
    /// </summary>
    /// <param name="assetType">The asset type to filter by</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of assets of the specified type</returns>
    Task<IEnumerable<LlmAsset>> GetAssetsByTypeAsync(LlmAssetType assetType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assets by status
    /// </summary>
    /// <param name="status">The status to filter by</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of assets with the specified status</returns>
    Task<IEnumerable<LlmAsset>> GetAssetsByStatusAsync(LlmAssetStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for assets by name, description, or architecture
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of matching assets</returns>
    Task<IEnumerable<LlmAsset>> SearchAssetsAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an asset file and updates its validation status
    /// </summary>
    /// <param name="assetId">The asset identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if validation passed; otherwise false</returns>
    Task<bool> ValidateAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an asset
    /// </summary>
    /// <param name="assetId">The asset identifier</param>
    /// <param name="status">The new status</param>
    /// <param name="activeRunnerId">The runner ID if status is Active; otherwise null</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if the status was updated successfully; otherwise false</returns>
    Task<bool> UpdateAssetStatusAsync(Guid assetId, LlmAssetStatus status, string? activeRunnerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an asset from the registry (soft delete)
    /// </summary>
    /// <param name="assetId">The asset identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if the asset was removed successfully; otherwise false</returns>
    Task<bool> RemoveAssetAsync(Guid assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets asset summary statistics
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Dictionary with asset counts by type and status</returns>
    Task<AssetSummary> GetAssetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets assets compatible with a specific runner
    /// </summary>
    /// <param name="runnerId">The runner identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of compatible assets</returns>
    Task<IEnumerable<LlmAsset>> GetCompatibleAssetsAsync(string runnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-scans and updates metadata for an existing asset
    /// </summary>
    /// <param name="assetId">The asset identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The updated asset if successful; otherwise null</returns>
    Task<LlmAsset?> RefreshAssetMetadataAsync(Guid assetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for missing files and updates asset status accordingly
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of assets marked as missing</returns>
    Task<int> ValidateAssetFilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces aggressive initialization and scanning of all AppData directories
    /// </summary>
    /// <param name="directoryPaths">The directory paths to scan for each asset type</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The total number of assets discovered and registered</returns>
    Task<int> ForceInitializeAppDataDirectoriesAsync(IDictionary<string, string> directoryPaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manual debugging method to force complete filesystem diagnostics - outputs comprehensive logging
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Detailed diagnostic report as string</returns>
    Task<string> ForceFilesystemDiagnosticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a summary of asset statistics
/// </summary>
public class AssetSummary
{
    /// <summary>
    /// Gets or sets the total number of assets
    /// </summary>
    public int TotalAssets { get; set; }

    /// <summary>
    /// Gets or sets the asset counts by type
    /// </summary>
    public Dictionary<LlmAssetType, int> CountsByType { get; set; } = new();

    /// <summary>
    /// Gets or sets the asset counts by status
    /// </summary>
    public Dictionary<LlmAssetStatus, int> CountsByStatus { get; set; } = new();

    /// <summary>
    /// Gets or sets the total storage size in bytes
    /// </summary>
    public long TotalStorageBytes { get; set; }

    /// <summary>
    /// Gets or sets the estimated total VRAM requirement in GB
    /// </summary>
    public decimal EstimatedTotalVramGb { get; set; }
}