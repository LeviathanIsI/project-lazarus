using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lazarus.Data.Repositories;

/// <summary>
/// Generic repository implementation for data access operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    private readonly LazarusDbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    private readonly ILogger<Repository<TEntity>>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Repository{TEntity}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="logger">Optional logger for repository operations.</param>
    public Repository(LazarusDbContext context, ILogger<Repository<TEntity>>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
        _logger = logger;
    }

    public async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get entity by ID: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get all entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to find entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get first entity of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check if any entity exists of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbSet.CountAsync(predicate, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to count entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public void Add(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            _dbSet.Add(entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to add entity of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public void AddRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        try
        {
            _dbSet.AddRange(entities);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to add range of entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            _dbSet.Update(entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to update entity of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        try
        {
            _dbSet.Remove(entity);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove entity of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        try
        {
            _dbSet.RemoveRange(entities);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove range of entities of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            var inner = ex.GetBaseException().Message;
            _logger?.LogError(ex, "DbUpdateException in repository {EntityType}. Inner={Inner}", typeof(TEntity).Name, inner);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save changes for repository of type {EntityType}", typeof(TEntity).Name);
            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            // Context will be disposed by DI container
            _disposed = true;
        }
        return ValueTask.CompletedTask;
    }
}