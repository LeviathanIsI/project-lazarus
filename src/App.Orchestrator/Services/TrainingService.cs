using Lazarus.App.Data.Repositories;
using Lazarus.App.Shared.Contracts;
using Lazarus.App.Shared.Models;

namespace Lazarus.App.Orchestrator.Services;

/// <summary>
/// Service implementation for managing training operations
/// </summary>
public class TrainingService : ITrainingService
{
    private readonly ITrainingSessionRepository _repository;
    private readonly ILogger<TrainingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingService"/> class
    /// </summary>
    /// <param name="repository">The training session repository</param>
    /// <param name="logger">The logger</param>
    public TrainingService(ITrainingSessionRepository repository, ILogger<TrainingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TrainingSession>> GetAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all training sessions");
        return await _repository.GetAllAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TrainingSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            _logger.LogWarning("Invalid session ID provided: {SessionId}", sessionId);
            return null;
        }

        _logger.LogInformation("Retrieving training session {SessionId}", sessionId);
        return await _repository.GetByIdAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TrainingSession> CreateSessionAsync(TrainingSession session, CancellationToken cancellationToken = default)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        _logger.LogInformation("Creating training session {SessionName}", session.Name);

        // Validate the session
        ValidateSession(session);

        // Set initial state
        session.Id = Guid.NewGuid();
        session.Status = TrainingStatus.Pending;
        session.Progress = 0;
        session.CreatedAt = DateTimeOffset.UtcNow;

        var createdSession = await _repository.AddAsync(session, cancellationToken);
        _logger.LogInformation("Training session {SessionId} created successfully", createdSession.Id);

        return createdSession;
    }

    /// <inheritdoc />
    public async Task<TrainingSession> UpdateSessionAsync(TrainingSession session, CancellationToken cancellationToken = default)
    {
        if (session == null)
            throw new ArgumentNullException(nameof(session));

        _logger.LogInformation("Updating training session {SessionId}", session.Id);

        // Validate the session
        ValidateSession(session);

        // Check if session exists
        var existingSession = await _repository.GetByIdAsync(session.Id, cancellationToken);
        if (existingSession == null)
        {
            _logger.LogWarning("Training session {SessionId} not found for update", session.Id);
            throw new InvalidOperationException($"Training session {session.Id} not found");
        }

        // Update timestamp
        session.UpdatedAt = DateTimeOffset.UtcNow;

        var updatedSession = await _repository.UpdateAsync(session, cancellationToken);
        _logger.LogInformation("Training session {SessionId} updated successfully", session.Id);

        return updatedSession;
    }

    /// <inheritdoc />
    public async Task<bool> StartSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            _logger.LogWarning("Invalid session ID provided for start: {SessionId}", sessionId);
            return false;
        }

        _logger.LogInformation("Starting training session {SessionId}", sessionId);

        var session = await _repository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Training session {SessionId} not found for start", sessionId);
            return false;
        }

        // Check if session can be started
        if (session.Status != TrainingStatus.Pending && session.Status != TrainingStatus.Failed)
        {
            _logger.LogWarning("Training session {SessionId} cannot be started. Current status: {Status}", 
                sessionId, session.Status);
            return false;
        }

        // Update session status
        session.Status = TrainingStatus.Running;
        session.StartedAt = DateTimeOffset.UtcNow;
        session.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(session, cancellationToken);
        _logger.LogInformation("Training session {SessionId} started successfully", sessionId);

        // TODO: Start actual training process here
        
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> StopSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            _logger.LogWarning("Invalid session ID provided for stop: {SessionId}", sessionId);
            return false;
        }

        _logger.LogInformation("Stopping training session {SessionId}", sessionId);

        var session = await _repository.GetByIdAsync(sessionId, cancellationToken);
        if (session == null)
        {
            _logger.LogWarning("Training session {SessionId} not found for stop", sessionId);
            return false;
        }

        // Check if session can be stopped
        if (session.Status != TrainingStatus.Running)
        {
            _logger.LogWarning("Training session {SessionId} cannot be stopped. Current status: {Status}", 
                sessionId, session.Status);
            return false;
        }

        // Update session status
        session.Status = TrainingStatus.Cancelled;
        session.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(session, cancellationToken);
        _logger.LogInformation("Training session {SessionId} stopped successfully", sessionId);

        // TODO: Stop actual training process here
        
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            _logger.LogWarning("Invalid session ID provided for deletion: {SessionId}", sessionId);
            return false;
        }

        _logger.LogInformation("Deleting training session {SessionId}", sessionId);

        var exists = await _repository.ExistsAsync(sessionId, cancellationToken);
        if (!exists)
        {
            _logger.LogWarning("Training session {SessionId} not found for deletion", sessionId);
            return false;
        }

        var success = await _repository.DeleteAsync(sessionId, cancellationToken);
        
        if (success)
        {
            _logger.LogInformation("Training session {SessionId} deleted successfully", sessionId);
        }
        else
        {
            _logger.LogError("Failed to delete training session {SessionId}", sessionId);
        }

        return success;
    }

    /// <summary>
    /// Validates a training session
    /// </summary>
    /// <param name="session">The session to validate</param>
    /// <exception cref="ArgumentException">Thrown when the session is invalid</exception>
    private static void ValidateSession(TrainingSession session)
    {
        if (string.IsNullOrWhiteSpace(session.Name))
            throw new ArgumentException("Session name is required", nameof(session));

        if (session.Name.Length > 256)
            throw new ArgumentException("Session name cannot exceed 256 characters", nameof(session));

        if (!string.IsNullOrEmpty(session.Description) && session.Description.Length > 1024)
            throw new ArgumentException("Session description cannot exceed 1024 characters", nameof(session));

        if (session.Progress < 0 || session.Progress > 100)
            throw new ArgumentException("Progress must be between 0 and 100", nameof(session));
    }
}