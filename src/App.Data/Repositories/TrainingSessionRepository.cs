using Lazarus.App.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Lazarus.App.Data.Repositories;

/// <summary>
/// Repository implementation for managing TrainingSession entities
/// </summary>
public class TrainingSessionRepository : ITrainingSessionRepository
{
    private readonly LazarusDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingSessionRepository"/> class
    /// </summary>
    /// <param name="context">The database context</param>
    public TrainingSessionRepository(LazarusDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TrainingSession>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSessions
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSessions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TrainingSession>> GetByStatusAsync(TrainingStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSessions
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TrainingSession> AddAsync(TrainingSession entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _context.TrainingSessions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public async Task<TrainingSession> UpdateAsync(TrainingSession entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        _context.TrainingSessions.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        // Soft delete
        entity.IsDeleted = true;
        await UpdateAsync(entity, cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TrainingSessions
            .AnyAsync(x => x.Id == id, cancellationToken);
    }
}