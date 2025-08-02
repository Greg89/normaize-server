using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

/// <summary>
/// Controller for comprehensive health monitoring and system status checks
/// </summary>
/// <remarks>
/// This controller provides detailed health monitoring endpoints for Kubernetes and container orchestration
/// systems. It includes liveness probes, readiness probes, and comprehensive health checks that verify
/// the application's ability to serve traffic and maintain system health. These endpoints are critical
/// for container orchestration, load balancing, and automated health monitoring.
/// 
/// Key features:
/// - Liveness probe for basic application availability
/// - Readiness probe for traffic serving capability
/// - Comprehensive health check with component details
/// - Detailed component health information
/// - Performance metrics and timing data
/// - Correlation ID support for tracing
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class HealthMonitoringController(IHealthCheckService healthCheckService) : BaseApiController()
{
    /// <summary>
    /// Performs a comprehensive health check of all system components
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>
    /// Comprehensive health status including all system components, performance metrics,
    /// and detailed health information
    /// </returns>
    /// <remarks>
    /// This endpoint performs a thorough health check of all system components including
    /// database connectivity, storage services, external dependencies, and application
    /// health. It provides detailed information about each component's status, performance
    /// metrics, and any issues encountered.
    /// 
    /// The health check includes:
    /// - Database connectivity and performance
    /// - Storage service availability
    /// - External service dependencies
    /// - Application memory and CPU usage
    /// - Configuration validation
    /// - Performance timing metrics
    /// 
    /// This endpoint is typically used for:
    /// - Comprehensive system monitoring
    /// - Detailed health dashboards
    /// - Troubleshooting system issues
    /// - Performance analysis and optimization
    /// </remarks>
    /// <response code="200">All systems healthy with detailed component information</response>
    /// <response code="503">One or more system components are unhealthy</response>
    /// <response code="500">Internal server error during health check</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(503)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<object>>> GetHealth(CancellationToken cancellationToken)
    {
        var result = await healthCheckService.CheckHealthAsync(cancellationToken);

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

    /// <summary>
    /// Performs a liveness check to verify the application is running
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>
    /// Basic liveness status indicating if the application is alive and responding
    /// </returns>
    /// <remarks>
    /// This endpoint performs a lightweight liveness check to verify that the application
    /// is running and can respond to requests. This is typically used by container
    /// orchestration systems (like Kubernetes) to determine if a container should be
    /// restarted.
    /// 
    /// The liveness check is designed to be:
    /// - Fast and lightweight
    /// - Non-intrusive to system performance
    /// - Focused on basic application availability
    /// - Suitable for frequent polling
    /// 
    /// This endpoint is typically used for:
    /// - Container health monitoring
    /// - Automatic restart decisions
    /// - Basic availability checks
    /// - Load balancer health checks
    /// </remarks>
    /// <response code="200">Application is alive and responding</response>
    /// <response code="503">Application is not responding or unhealthy</response>
    /// <response code="500">Internal server error during liveness check</response>
    [HttpGet("liveness")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(503)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<object>>> GetLiveness(CancellationToken cancellationToken)
    {
        var result = await healthCheckService.CheckLivenessAsync(cancellationToken);

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

    /// <summary>
    /// Performs a readiness check to verify the application can serve traffic
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>
    /// Readiness status indicating if the application is ready to serve traffic
    /// </returns>
    /// <remarks>
    /// This endpoint performs a readiness check to verify that the application is ready
    /// to serve traffic. This includes checking database connectivity, storage services,
    /// and other critical dependencies. This is typically used by load balancers and
    /// container orchestration systems to determine if traffic should be routed to
    /// this instance.
    /// 
    /// The readiness check verifies:
    /// - Database connectivity and responsiveness
    /// - Storage service availability
    /// - Configuration validation
    /// - Critical service dependencies
    /// - Application initialization status
    /// 
    /// This endpoint is typically used for:
    /// - Load balancer health checks
    /// - Traffic routing decisions
    /// - Service discovery health validation
    /// - Deployment readiness verification
    /// </remarks>
    /// <response code="200">Application is ready to serve traffic</response>
    /// <response code="503">Application is not ready to serve traffic</response>
    /// <response code="500">Internal server error during readiness check</response>
    [HttpGet("readiness")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(503)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<object>>> GetReadiness(CancellationToken cancellationToken)
    {
        var result = await healthCheckService.CheckReadinessAsync(cancellationToken);

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