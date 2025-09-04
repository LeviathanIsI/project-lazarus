using Microsoft.AspNetCore.Mvc;

namespace Lazarus.App.Orchestrator.Controllers;

/// <summary>
/// Controller for health check endpoints
/// </summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class
    /// </summary>
    /// <param name="logger">The logger</param>
    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the health status of the orchestrator service
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), 200)]
    public ActionResult<HealthResponse> GetHealth()
    {
        try
        {
            var health = new HealthResponse
            {
                Status = "healthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Uptime = Environment.TickCount64 / 1000.0, // Convert to seconds
                Service = "orchestrator"
            };

            _logger.LogDebug("Health check requested - Status: {Status}", health.Status);
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
            var errorHealth = new HealthResponse
            {
                Status = "unhealthy",
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Uptime = Environment.TickCount64 / 1000.0,
                Service = "orchestrator",
                Error = ex.Message
            };
            return StatusCode(500, errorHealth);
        }
    }
}

/// <summary>
/// Health response model
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Gets or sets the health status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp of the health check
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the service version
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service uptime in seconds
    /// </summary>
    public double Uptime { get; set; }

    /// <summary>
    /// Gets or sets the service name
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any error message if unhealthy
    /// </summary>
    public string? Error { get; set; }
}