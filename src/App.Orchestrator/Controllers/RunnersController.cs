using Lazarus.App.Shared.Contracts;
using Lazarus.App.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lazarus.App.Orchestrator.Controllers;

/// <summary>
/// Controller for managing runner instances
/// </summary>
[ApiController]
[Route("api/runners")]
[Produces("application/json")]
public class RunnersController : ControllerBase
{
    private readonly IRunnerService _runnerService;
    private readonly ILogger<RunnersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnersController"/> class
    /// </summary>
    /// <param name="runnerService">The runner service</param>
    /// <param name="logger">The logger</param>
    public RunnersController(IRunnerService runnerService, ILogger<RunnersController> logger)
    {
        _runnerService = runnerService ?? throw new ArgumentNullException(nameof(runnerService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all registered runners and their current status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of runner statuses</returns>
    [HttpGet]
    [ProducesResponseType(typeof(RunnerApiResponse), 200)]
    public async Task<ActionResult<RunnerApiResponse>> GetRunners(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Retrieving all runner statuses");
            var runners = await _runnerService.GetAllRunnersAsync(cancellationToken);
            
            var response = new RunnerApiResponse
            {
                Runners = runners.ToList()
            };

            _logger.LogDebug("Retrieved {Count} runners", runners.Count());
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving runner statuses");
            return StatusCode(500, new RunnerApiResponse 
            { 
                Runners = new List<RunnerStatus>(),
                Error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Gets a specific runner by ID
    /// </summary>
    /// <param name="id">The runner identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The runner status if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RunnerStatus), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<RunnerStatus>> GetRunner(string id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Retrieving runner status for {RunnerId}", id);
            var runner = await _runnerService.GetRunnerAsync(id, cancellationToken);
            
            if (runner == null)
            {
                _logger.LogWarning("Runner {RunnerId} not found", id);
                return NotFound();
            }

            return Ok(runner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving runner status for {RunnerId}", id);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Starts a specific runner
    /// </summary>
    /// <param name="id">The runner identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if started successfully</returns>
    [HttpPost("{id}/start")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<ActionResult> StartRunner(string id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting runner {RunnerId}", id);
            var success = await _runnerService.StartRunnerAsync(id, cancellationToken);
            
            if (!success)
            {
                _logger.LogWarning("Failed to start runner {RunnerId} - runner may not exist or already be running", id);
                return Conflict("Runner could not be started - it may not exist or already be running");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting runner {RunnerId}", id);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Stops a specific runner
    /// </summary>
    /// <param name="id">The runner identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response if stopped successfully</returns>
    [HttpPost("{id}/stop")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> StopRunner(string id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Stopping runner {RunnerId}", id);
            var success = await _runnerService.StopRunnerAsync(id, cancellationToken);
            
            if (!success)
            {
                _logger.LogWarning("Failed to stop runner {RunnerId} - runner may not exist", id);
                return NotFound();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping runner {RunnerId}", id);
            return StatusCode(500);
        }
    }
}