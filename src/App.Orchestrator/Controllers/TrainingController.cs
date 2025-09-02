using Lazarus.App.Shared.Contracts;
using Lazarus.App.Shared.DTOs;
using Lazarus.App.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lazarus.App.Orchestrator.Controllers;

/// <summary>
/// Controller for managing training sessions
/// </summary>
[ApiController]
[Route("api/training")]
[Produces("application/json")]
public class TrainingController : ControllerBase
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<TrainingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrainingController"/> class
    /// </summary>
    /// <param name="trainingService">The training service</param>
    /// <param name="logger">The logger</param>
    public TrainingController(ITrainingService trainingService, ILogger<TrainingController> logger)
    {
        _trainingService = trainingService ?? throw new ArgumentNullException(nameof(trainingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all training sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of training sessions</returns>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TrainingSession>>), 200)]
    public async Task<ActionResult<ApiResponse<IEnumerable<TrainingSession>>>> GetAllSessions(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving all training sessions");
            var sessions = await _trainingService.GetAllSessionsAsync(cancellationToken);
            return Ok(ApiResponse<IEnumerable<TrainingSession>>.SuccessResult(sessions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training sessions");
            return StatusCode(500, ApiResponse<IEnumerable<TrainingSession>>.ErrorResult("Internal server error", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Gets a specific training session by ID
    /// </summary>
    /// <param name="id">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The training session if found</returns>
    [HttpGet("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrainingSession>), 200)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSession>), 404)]
    public async Task<ActionResult<ApiResponse<TrainingSession>>> GetSession(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving training session {SessionId}", id);
            var session = await _trainingService.GetSessionAsync(id, cancellationToken);
            
            if (session == null)
            {
                _logger.LogWarning("Training session {SessionId} not found", id);
                return NotFound(ApiResponse<TrainingSession>.ErrorResult("Training session not found", "NOT_FOUND"));
            }

            return Ok(ApiResponse<TrainingSession>.SuccessResult(session));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving training session {SessionId}", id);
            return StatusCode(500, ApiResponse<TrainingSession>.ErrorResult("Internal server error", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Creates a new training session
    /// </summary>
    /// <param name="session">The training session to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created training session</returns>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ApiResponse<TrainingSession>), 201)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSession>), 400)]
    public async Task<ActionResult<ApiResponse<TrainingSession>>> CreateSession([FromBody] TrainingSession session, CancellationToken cancellationToken)
    {
        if (session == null)
        {
            return BadRequest(ApiResponse<TrainingSession>.ErrorResult("Session data is required", "INVALID_DATA"));
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(ApiResponse<TrainingSession>.ErrorResult($"Validation failed: {errors}", "VALIDATION_ERROR"));
        }

        try
        {
            _logger.LogInformation("Creating training session {SessionName}", session.Name);
            var createdSession = await _trainingService.CreateSessionAsync(session, cancellationToken);
            
            return CreatedAtAction(
                nameof(GetSession),
                new { id = createdSession.Id },
                ApiResponse<TrainingSession>.SuccessResult(createdSession));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating training session {SessionName}", session.Name);
            return StatusCode(500, ApiResponse<TrainingSession>.ErrorResult("Internal server error", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Updates an existing training session
    /// </summary>
    /// <param name="id">The session identifier</param>
    /// <param name="session">The updated session data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated training session</returns>
    [HttpPut("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TrainingSession>), 200)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSession>), 400)]
    [ProducesResponseType(typeof(ApiResponse<TrainingSession>), 404)]
    public async Task<ActionResult<ApiResponse<TrainingSession>>> UpdateSession(Guid id, [FromBody] TrainingSession session, CancellationToken cancellationToken)
    {
        if (session == null)
        {
            return BadRequest(ApiResponse<TrainingSession>.ErrorResult("Session data is required", "INVALID_DATA"));
        }

        if (id != session.Id)
        {
            return BadRequest(ApiResponse<TrainingSession>.ErrorResult("Session ID mismatch", "ID_MISMATCH"));
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return BadRequest(ApiResponse<TrainingSession>.ErrorResult($"Validation failed: {errors}", "VALIDATION_ERROR"));
        }

        try
        {
            _logger.LogInformation("Updating training session {SessionId}", id);
            
            // Check if session exists
            var existingSession = await _trainingService.GetSessionAsync(id, cancellationToken);
            if (existingSession == null)
            {
                _logger.LogWarning("Training session {SessionId} not found for update", id);
                return NotFound(ApiResponse<TrainingSession>.ErrorResult("Training session not found", "NOT_FOUND"));
            }

            var updatedSession = await _trainingService.UpdateSessionAsync(session, cancellationToken);
            return Ok(ApiResponse<TrainingSession>.SuccessResult(updatedSession));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating training session {SessionId}", id);
            return StatusCode(500, ApiResponse<TrainingSession>.ErrorResult("Internal server error", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Starts a training session
    /// </summary>
    /// <param name="id">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if started successfully</returns>
    [HttpPost("sessions/{id:guid}/start")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse>> StartSession(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting training session {SessionId}", id);
            var success = await _trainingService.StartSessionAsync(id, cancellationToken);
            
            if (!success)
            {
                _logger.LogWarning("Failed to start training session {SessionId}", id);
                return NotFound(ApiResponse.ErrorResult("Training session not found or cannot be started", "START_FAILED"));
            }

            return Ok(ApiResponse.SuccessResult());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting training session {SessionId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Stops a training session
    /// </summary>
    /// <param name="id">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if stopped successfully</returns>
    [HttpPost("sessions/{id:guid}/stop")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse>> StopSession(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Stopping training session {SessionId}", id);
            var success = await _trainingService.StopSessionAsync(id, cancellationToken);
            
            if (!success)
            {
                _logger.LogWarning("Failed to stop training session {SessionId}", id);
                return NotFound(ApiResponse.ErrorResult("Training session not found or cannot be stopped", "STOP_FAILED"));
            }

            return Ok(ApiResponse.SuccessResult());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping training session {SessionId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Deletes a training session
    /// </summary>
    /// <param name="id">The session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if deleted successfully</returns>
    [HttpDelete("sessions/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse>> DeleteSession(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting training session {SessionId}", id);
            var success = await _trainingService.DeleteSessionAsync(id, cancellationToken);
            
            if (!success)
            {
                _logger.LogWarning("Training session {SessionId} not found for deletion", id);
                return NotFound(ApiResponse.ErrorResult("Training session not found", "NOT_FOUND"));
            }

            return Ok(ApiResponse.SuccessResult());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting training session {SessionId}", id);
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", "INTERNAL_ERROR"));
        }
    }
}