using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Interfaces;

namespace Normaize.API.Controllers;

[ApiController]
[Route("health")]
public class HealthMonitoringController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;

    public HealthMonitoringController(IHealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckHealthAsync(cancellationToken);
        
        if (!result.IsHealthy)
        {
            return StatusCode(503, new
            {
                status = result.Status,
                components = result.Components,
                issues = result.Issues,
                timestamp = result.Timestamp,
                duration = result.Duration.TotalMilliseconds
            });
        }

        return Ok(new
        {
            status = result.Status,
            components = result.Components,
            timestamp = result.Timestamp,
            duration = result.Duration.TotalMilliseconds,
            message = "All systems healthy"
        });
    }

    [HttpGet("liveness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetLiveness(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckLivenessAsync(cancellationToken);
        
        if (!result.IsHealthy)
        {
            return StatusCode(503, new
            {
                status = result.Status,
                issues = result.Issues,
                timestamp = result.Timestamp,
                duration = result.Duration.TotalMilliseconds
            });
        }

        return Ok(new
        {
            status = result.Status,
            timestamp = result.Timestamp,
            duration = result.Duration.TotalMilliseconds,
            message = "Application is alive"
        });
    }

    [HttpGet("readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReadiness(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckReadinessAsync(cancellationToken);
        
        if (!result.IsHealthy)
        {
            return StatusCode(503, new
            {
                status = result.Status,
                components = result.Components,
                issues = result.Issues,
                timestamp = result.Timestamp,
                duration = result.Duration.TotalMilliseconds
            });
        }

        return Ok(new
        {
            status = result.Status,
            components = result.Components,
            timestamp = result.Timestamp,
            duration = result.Duration.TotalMilliseconds,
            message = "Application is ready to serve traffic"
        });
    }
} 