using Lazarus.App.Shared.Contracts;
using Lazarus.App.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lazarus.App.Orchestrator.Controllers;

/// <summary>
/// Controller for performance and system metrics
/// </summary>
[ApiController]
[Route("api/metrics")]
[Produces("application/json")]
public class MetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;
    private readonly ILogger<MetricsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsController"/> class
    /// </summary>
    /// <param name="metricsService">The metrics service</param>
    /// <param name="logger">The logger</param>
    public MetricsController(IMetricsService metricsService, ILogger<MetricsController> logger)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets current performance metrics for the orchestrator and runners
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance metrics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(MetricsApiResponse), 200)]
    public async Task<ActionResult<MetricsApiResponse>> GetMetrics(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Retrieving system performance metrics");
            var metrics = await _metricsService.GetCurrentMetricsAsync(cancellationToken);
            
            _logger.LogDebug("Retrieved metrics - Latency: {Latency}ms, TPS: {TPS}", 
                metrics.AverageInferenceLatencyMs, metrics.TokensPerSecond);
            
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics");
            return StatusCode(500, new MetricsApiResponse
            {
                AverageInferenceLatencyMs = 0,
                TokensPerSecond = 0,
                TotalRequests = 0,
                FailedRequests = 0,
                Error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Gets historical metrics over a specified time period
    /// </summary>
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Historical metrics</returns>
    [HttpGet("historical")]
    [ProducesResponseType(typeof(HistoricalMetricsResponse), 200)]
    public async Task<ActionResult<HistoricalMetricsResponse>> GetHistoricalMetrics(
        [FromQuery] int hours = 24, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving historical metrics for {Hours} hours", hours);
            var historicalMetrics = await _metricsService.GetHistoricalMetricsAsync(hours, cancellationToken);
            
            return Ok(historicalMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving historical metrics");
            return StatusCode(500, new HistoricalMetricsResponse
            {
                TimeRange = TimeSpan.FromHours(hours),
                DataPoints = new List<MetricsDataPoint>(),
                Error = "Internal server error"
            });
        }
    }

    /// <summary>
    /// Resets accumulated metrics (useful for testing)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("reset")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> ResetMetrics(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Resetting performance metrics");
            await _metricsService.ResetMetricsAsync(cancellationToken);
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting performance metrics");
            return StatusCode(500);
        }
    }
}

