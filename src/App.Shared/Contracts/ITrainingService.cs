using Lazarus.App.Shared.Models;

namespace Lazarus.App.Shared.Contracts;

/// <summary>
/// Service contract for managing training operations
/// </summary>
public interface ITrainingService
{
    /// <summary>
    /// Gets all training sessions asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of training sessions</returns>
    Task<IEnumerable<TrainingSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific training session by ID asynchronously
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The training session if found, null otherwise</returns>
    Task<TrainingSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new training session asynchronously
    /// </summary>
    /// <param name="session">The training session to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created training session</returns>
    Task<TrainingSession> CreateSessionAsync(TrainingSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing training session asynchronously
    /// </summary>
    /// <param name="session">The training session to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated training session</returns>
    Task<TrainingSession> UpdateSessionAsync(TrainingSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a training session asynchronously
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if started successfully, false otherwise</returns>
    Task<bool> StartSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a training session asynchronously
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if stopped successfully, false otherwise</returns>
    Task<bool> StopSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a training session asynchronously
    /// </summary>
    /// <param name="sessionId">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}