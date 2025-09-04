using Lazarus.App.Shared.Models;

namespace Lazarus.App.Data.Repositories;

/// <summary>
/// Repository interface for managing LLM assets
/// </summary>
public interface ILlmAssetRepository
{
    /// <summary>
    /// Gets all LLM assets
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of all LLM assets</returns>
    Task<IEnumerable<LlmAsset>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an LLM asset by its unique identifier
    /// </summary>
    /// <param name="id">The asset identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The LLM asset if found; otherwise null</returns>
    Task<LlmAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an LLM asset by its file path
    /// </summary>
    /// <param name="filePath">The file path</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The LLM asset if found; otherwise null</returns>
    Task<LlmAsset?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an LLM asset by its file hash
    /// </summary>
    /// <param name="fileHash">The file hash</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The LLM asset if found; otherwise null</returns>
    Task<LlmAsset?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets LLM assets by type
    /// </summary>
    /// <param name="assetType">The asset type</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of LLM assets of the specified type</returns>
    Task<IEnumerable<LlmAsset>> GetByTypeAsync(LlmAssetType assetType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets LLM assets by status
    /// </summary>
    /// <param name="status">The asset status</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of LLM assets with the specified status</returns>
    Task<IEnumerable<LlmAsset>> GetByStatusAsync(LlmAssetStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets LLM assets compatible with a specific runner
    /// </summary>
    /// <param name="runnerId">The runner identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of compatible LLM assets</returns>
    Task<IEnumerable<LlmAsset>> GetCompatibleWithRunnerAsync(string runnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active asset for a specific runner
    /// </summary>
    /// <param name="runnerId">The runner identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The active LLM asset for the runner if found; otherwise null</returns>
    Task<LlmAsset?> GetActiveAssetForRunnerAsync(string runnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for LLM assets by name or description
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A collection of matching LLM assets</returns>
    Task<IEnumerable<LlmAsset>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new LLM asset
    /// </summary>
    /// <param name="asset">The LLM asset to add</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The added LLM asset</returns>
    Task<LlmAsset> AddAsync(LlmAsset asset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing LLM asset
    /// </summary>
    /// <param name="asset">The LLM asset to update</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The updated LLM asset</returns>
    Task<LlmAsset> UpdateAsync(LlmAsset asset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an LLM asset (soft delete)
    /// </summary>
    /// <param name="id">The asset identifier</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if the asset was deleted; otherwise false</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an LLM asset
    /// </summary>
    /// <param name="id">The asset identifier</param>
    /// <param name="status">The new status</param>
    /// <param name="activeRunnerId">The runner ID if status is Active; otherwise null</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>True if the status was updated; otherwise false</returns>
    Task<bool> UpdateStatusAsync(Guid id, LlmAssetStatus status, string? activeRunnerId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total count of assets by type
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Dictionary with asset types as keys and counts as values</returns>
    Task<Dictionary<LlmAssetType, int>> GetAssetCountsByTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges all asset entries from the database (hard delete for phantom elimination)
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Number of entries purged</returns>
    Task<int> PurgeAllAssetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes assets that reference non-existent files (orphan cleanup)
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>Number of orphaned entries removed</returns>
    Task<int> RemoveOrphanedAssetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all registered assets still exist on disk
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>List of assets with missing files</returns>
    Task<IEnumerable<LlmAsset>> ValidateAssetFilesExistAsync(CancellationToken cancellationToken = default);
}