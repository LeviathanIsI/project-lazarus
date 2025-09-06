using Lazarus.Data.Entities;
using Lazarus.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Repository implementation for model-specific data operations.
/// </summary>
public class ModelRepository : Repository<Model>, IModelRepository
{
    private readonly LazarusDbContext _context;
    private readonly ILogger<ModelRepository>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for repository operations.</param>
    public ModelRepository(LazarusDbContext context, ILogger<ModelRepository>? logger = null)
        : base(context, logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Model>> GetActiveModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Models
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get active models");
            throw;
        }
    }

    public async Task<IEnumerable<Model>> GetModelsByRunnerTypeAsync(RunnerType runnerType, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Models.Where(m => m.RunnerType == runnerType);

            if (activeOnly)
            {
                query = query.Where(m => m.IsActive);
            }

            return await query
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get models by runner type {RunnerType} (activeOnly: {ActiveOnly})", runnerType, activeOnly);
            throw;
        }
    }

    public async Task<Model?> GetModelByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name);

        try
        {
            return await _context.Models
                .FirstOrDefaultAsync(m => m.Name == name, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get model by name '{Name}'", name);
            throw;
        }
    }

    public async Task<bool> PathExistsAsync(string path, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);

        try
        {
            var query = _context.Models.Where(m => m.Path == path);

            if (excludeId.HasValue)
            {
                query = query.Where(m => m.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check if path '{Path}' exists (excludeId: {ExcludeId})", path, excludeId);
            throw;
        }
    }

    public async Task<int> DeactivateAllModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Database
                .ExecuteSqlRawAsync("UPDATE Models SET IsActive = 0, LastModified = {0}", [DateTime.UtcNow.ToString("O")], cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to deactivate all models");
            throw;
        }
    }

    public async Task<bool> SetActiveModelAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Deactivate all models
            await DeactivateAllModelsAsync(cancellationToken).ConfigureAwait(false);

            // Activate the specified model
            var rowsAffected = await _context.Database
                .ExecuteSqlRawAsync(
                    "UPDATE Models SET IsActive = 1, LastModified = {0} WHERE Id = {1}",
                    [DateTime.UtcNow.ToString("O"), modelId.ToString()],
                    cancellationToken)
                .ConfigureAwait(false);

            if (rowsAffected > 0)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger?.LogError(ex, "Failed to set active model {ModelId}", modelId);
            throw;
        }
    }
}