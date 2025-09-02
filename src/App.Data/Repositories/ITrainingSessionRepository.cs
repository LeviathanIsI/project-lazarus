using Lazarus.App.Shared.Models;

namespace Lazarus.App.Data.Repositories;

/// <summary>
/// Repository interface for managing TrainingSession entities
/// </summary>
public interface ITrainingSessionRepository
{
    /// <summary>
    /// Gets all training sessions asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of training sessions</returns>
    Task<IEnumerable<TrainingSession>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a training session by ID asynchronously
    /// </summary>
    /// <param name="id">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The training session if found, null otherwise</returns>
    Task<TrainingSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets training sessions by status asynchronously
    /// </summary>
    /// <param name="status">The training status to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of training sessions with the specified status</returns>
    Task<IEnumerable<TrainingSession>> GetByStatusAsync(TrainingStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new training session asynchronously
    /// </summary>
    /// <param name="entity">The training session to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added training session</returns>
    Task<TrainingSession> AddAsync(TrainingSession entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing training session asynchronously
    /// </summary>
    /// <param name="entity">The training session to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated training session</returns>
    Task<TrainingSession> UpdateAsync(TrainingSession entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes a training session asynchronously
    /// </summary>
    /// <param name="id">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a training session exists asynchronously
    /// </summary>
    /// <param name="id">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the session exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}