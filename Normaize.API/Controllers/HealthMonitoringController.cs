using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

[ApiController]
[Route("health")]
public class HealthMonitoringController(IHealthCheckService _healthCheckService) : BaseApiController()
{

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<object>>> GetHealth(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckHealthAsync(cancellationToken);

        if (!result.IsHealthy)
        {
            return Error<object>("Health check failed", "HEALTH_CHECK_FAILED", 503);
        }

        return Success((object)new
        {
            status = result.Status,
            components = result.Components,
            timestamp = result.Timestamp,
            duration = result.Duration.TotalMilliseconds,
            message = "All systems healthy",
            correlationId = result.CorrelationId
        });
    }

    [HttpGet("liveness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<object>>> GetLiveness(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckLivenessAsync(cancellationToken);

        if (!result.IsHealthy)
        {
            return Error<object>("Liveness check failed", "LIVENESS_CHECK_FAILED", 503);
        }

        return Success((object)new
        {
            status = result.Status,
            timestamp = result.Timestamp,
            duration = result.Duration.TotalMilliseconds,
            message = "Application is alive",
            correlationId = result.CorrelationId
        });
    }

    [HttpGet("readiness")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<object>>> GetReadiness(CancellationToken cancellationToken)
    {
        var result = await _healthCheckService.CheckReadinessAsync(cancellationToken);

        if (!result.IsHealthy)
        {
            return Error<object>("Readiness check failed", "READINESS_CHECK_FAILED", 503);
        }

        return Success((object)new
        {
            status = result.Status,
            components = result.Components,
            timestamp = result.Timestamp,
            duration = result.Duration.TotalMilliseconds,
            message = "Application is ready to serve traffic",
            correlationId = result.CorrelationId
        });
    }
}