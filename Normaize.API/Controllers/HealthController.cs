using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

/// <summary>
/// Controller for basic health check functionality
/// </summary>
/// <remarks>
/// This controller provides a simple health check endpoint that returns basic application status
/// information. It is designed for lightweight health monitoring and load balancer health checks.
/// Unlike the HealthMonitoringController which provides detailed component health information,
/// this controller focuses on basic availability and status reporting.
/// 
/// Key features:
/// - Simple health status check
/// - Basic application information
/// - Environment and version details
/// - Lightweight and fast response
/// - Suitable for frequent polling
/// 
/// This controller is typically used for:
/// - Basic load balancer health checks
/// - Simple availability monitoring
/// - Quick status verification
/// - Development and testing health checks
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class HealthController : BaseApiController
{
    public HealthController(IStructuredLoggingService loggingService) : base(loggingService)
    {
    }

    /// <summary>
    /// Performs a basic health check and returns application status information
    /// </summary>
    /// <returns>
    /// Basic health status including application status, timestamp, service information,
    /// version details, and environment information
    /// </returns>
    /// <remarks>
    /// This endpoint performs a lightweight health check to verify that the application
    /// is running and responding to requests. It returns basic status information
    /// without performing deep component health checks or external service validation.
    /// 
    /// The health check provides:
    /// - Current application status (always "healthy" if responding)
    /// - Timestamp of the health check
    /// - Service identification information
    /// - Application version details
    /// - Environment configuration
    /// 
    /// This endpoint is designed to be:
    /// - Fast and lightweight
    /// - Non-intrusive to system performance
    /// - Suitable for frequent polling
    /// - Reliable for basic availability checks
    /// 
    /// This endpoint is typically used for:
    /// - Load balancer health checks
    /// - Basic availability monitoring
    /// - Quick status verification
    /// - Development and testing
    /// </remarks>
    /// <response code="200">Application is healthy with status information</response>
    /// <response code="500">Internal server error during health check</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthResponseDto>), 200)]
    [ProducesResponseType(500)]
    public ActionResult<ApiResponse<HealthResponseDto>> Get()
    {
        _loggingService?.LogUserAction("Health check requested", null);

        var healthResponse = new HealthResponseDto
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Normaize API",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        return Success(healthResponse);
    }
}