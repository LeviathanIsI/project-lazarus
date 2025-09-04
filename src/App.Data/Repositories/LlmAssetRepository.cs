using Lazarus.App.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lazarus.App.Data.Repositories;

/// <summary>
/// Repository implementation for managing LLM assets
/// </summary>
public class LlmAssetRepository : ILlmAssetRepository
{
    private readonly LazarusDbContext _context;
    private readonly ILogger<LlmAssetRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmAssetRepository"/> class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger</param>
    public LlmAssetRepository(LazarusDbContext context, ILogger<LlmAssetRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all LLM assets with navigation persistence");

        // CRITICAL FIX: AsNoTracking() prevents phantom entity spawning during navigation transitions
        // Entities returned are detached and won't cause tracking conflicts across navigation boundaries
        return await _context.LlmAssets
            .AsNoTracking() // PHANTOM PREVENTION: Detached entities prevent navigation-induced duplicates
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LlmAsset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting LLM asset by ID: {AssetId}", id);

        return await _context.LlmAssets
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LlmAsset?> GetByFilePathAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
        }

        _logger.LogDebug("Getting LLM asset by file path: {FilePath}", filePath);

        return await _context.LlmAssets
            .FirstOrDefaultAsync(a => a.FilePath == filePath, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LlmAsset?> GetByFileHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileHash))
        {
            throw new ArgumentException("File hash cannot be null or empty", nameof(fileHash));
        }

        _logger.LogDebug("Getting LLM asset by file hash: {FileHash}", fileHash);

        return await _context.LlmAssets
            .FirstOrDefaultAsync(a => a.FileHash == fileHash, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> GetByTypeAsync(LlmAssetType assetType, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting LLM assets by type with navigation persistence: {AssetType}", assetType);

        return await _context.LlmAssets
            .AsNoTracking() // PHANTOM PREVENTION: Navigation-safe entity retrieval
            .Where(a => a.AssetType == assetType)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> GetByStatusAsync(LlmAssetStatus status, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting LLM assets by status with navigation persistence: {Status}", status);

        return await _context.LlmAssets
            .AsNoTracking() // PHANTOM PREVENTION: Navigation-safe entity retrieval
            .Where(a => a.Status == status)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> GetCompatibleWithRunnerAsync(string runnerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner ID cannot be null or empty", nameof(runnerId));
        }

        _logger.LogDebug("Getting LLM assets compatible with runner with navigation persistence: {RunnerId}", runnerId);

        return await _context.LlmAssets
            .AsNoTracking() // PHANTOM PREVENTION: Navigation-safe entity retrieval
            .Where(a => a.CompatibleRunners.Contains(runnerId))
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LlmAsset?> GetActiveAssetForRunnerAsync(string runnerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            throw new ArgumentException("Runner ID cannot be null or empty", nameof(runnerId));
        }

        _logger.LogDebug("Getting active LLM asset for runner: {RunnerId}", runnerId);

        return await _context.LlmAssets
            .FirstOrDefaultAsync(a => a.ActiveRunnerId == runnerId && a.Status == LlmAssetStatus.Active, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogDebug("Searching LLM assets with navigation persistence for term: {SearchTerm}", searchTerm);

        var lowerSearchTerm = searchTerm.ToLowerInvariant();

        return await _context.LlmAssets
            .AsNoTracking() // PHANTOM PREVENTION: Navigation-safe entity retrieval
            .Where(a => a.Name.ToLower().Contains(lowerSearchTerm) ||
                       (a.Description != null && a.Description.ToLower().Contains(lowerSearchTerm)) ||
                       (a.Architecture != null && a.Architecture.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LlmAsset> AddAsync(LlmAsset asset, CancellationToken cancellationToken = default)
    {
        if (asset == null)
        {
            throw new ArgumentNullException(nameof(asset));
        }

        _logger.LogDebug("Adding LLM asset: {AssetName} at {FilePath}", asset.Name, asset.FilePath);

        _context.LlmAssets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully added LLM asset: {AssetId} - {AssetName}", asset.Id, asset.Name);

        return asset;
    }

    /// <inheritdoc />
    public async Task<LlmAsset> UpdateAsync(LlmAsset asset, CancellationToken cancellationToken = default)
    {
        if (asset == null)
        {
            throw new ArgumentNullException(nameof(asset));
        }

        _logger.LogDebug("Updating LLM asset: {AssetId} - {AssetName}", asset.Id, asset.Name);

        _context.LlmAssets.Update(asset);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully updated LLM asset: {AssetId} - {AssetName}", asset.Id, asset.Name);

        return asset;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting LLM asset: {AssetId}", id);

        var asset = await _context.LlmAssets
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (asset == null)
        {
            _logger.LogWarning("LLM asset not found for deletion: {AssetId}", id);
            return false;
        }

        // Soft delete
        asset.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully deleted LLM asset: {AssetId} - {AssetName}", asset.Id, asset.Name);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStatusAsync(Guid id, LlmAssetStatus status, string? activeRunnerId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating status of LLM asset {AssetId} to {Status} with runner {RunnerId}", id, status, activeRunnerId);

        var asset = await _context.LlmAssets
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (asset == null)
        {
            _logger.LogWarning("LLM asset not found for status update: {AssetId}", id);
            return false;
        }

        asset.Status = status;
        asset.ActiveRunnerId = status == LlmAssetStatus.Active ? activeRunnerId : null;

        if (status == LlmAssetStatus.Active)
        {
            asset.LastLoadedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Successfully updated status of LLM asset: {AssetId} to {Status}", asset.Id, status);

        return true;
    }

    /// <inheritdoc />
    public async Task<Dictionary<LlmAssetType, int>> GetAssetCountsByTypeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting asset counts by type");

        var result = await _context.LlmAssets
            .GroupBy(a => a.AssetType)
            .Select(g => new { AssetType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.ToDictionary(x => x.AssetType, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<int> PurgeAllAssetsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("PURGING ALL ASSETS - Nuclear database cleansing operation initiated");

        var totalCount = await _context.LlmAssets.CountAsync(cancellationToken).ConfigureAwait(false);
        
        if (totalCount > 0)
        {
            // Hard delete all entries - no soft delete for phantom elimination
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM LlmAssets", cancellationToken).ConfigureAwait(false);
            
            // Reset identity seed
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name='LlmAssets'", cancellationToken).ConfigureAwait(false);
            
            _logger.LogWarning("PURGE COMPLETE: {TotalCount} phantom asset entries eliminated from database", totalCount);
        }

        return totalCount;
    }

    /// <inheritdoc />
    public async Task<int> RemoveOrphanedAssetsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Identifying and removing orphaned asset entries");

        var allAssets = await _context.LlmAssets.ToListAsync(cancellationToken).ConfigureAwait(false);
        var orphansToRemove = new List<LlmAsset>();

        // Use Task.Run to move file system checks off the DB context thread
        await Task.Run(() =>
        {
            foreach (var asset in allAssets)
            {
                if (!File.Exists(asset.FilePath))
                {
                    _logger.LogWarning("Found orphaned asset: {AssetName} - File missing at {FilePath}", asset.Name, asset.FilePath);
                    orphansToRemove.Add(asset);
                }
            }
        }, cancellationToken).ConfigureAwait(false);

        if (orphansToRemove.Count > 0)
        {
            _context.LlmAssets.RemoveRange(orphansToRemove);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            
            _logger.LogInformation("Removed {OrphanCount} orphaned asset entries", orphansToRemove.Count);
        }

        return orphansToRemove.Count;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LlmAsset>> ValidateAssetFilesExistAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating that all registered asset files exist on disk");

        var allAssets = await _context.LlmAssets.ToListAsync(cancellationToken).ConfigureAwait(false);
        var assetsWithMissingFiles = new List<LlmAsset>();

        await Task.Run(() =>
        {
            foreach (var asset in allAssets)
            {
                if (!File.Exists(asset.FilePath))
                {
                    _logger.LogWarning("Asset file missing: {AssetName} at {FilePath}", asset.Name, asset.FilePath);
                    assetsWithMissingFiles.Add(asset);
                }
            }
        }, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("File validation complete: {MissingCount} assets have missing files out of {TotalCount} registered", 
            assetsWithMissingFiles.Count, allAssets.Count);

        return assetsWithMissingFiles;
    }
}