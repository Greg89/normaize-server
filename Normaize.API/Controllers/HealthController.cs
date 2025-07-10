using Microsoft.AspNetCore.Mvc;
using Normaize.API.Services;
using Normaize.Core.Interfaces;

namespace Normaize.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IStructuredLoggingService _loggingService;
    private readonly IHealthCheckService _healthCheckService;

    public HealthController(IStructuredLoggingService loggingService, IHealthCheckService healthCheckService)
    {
        _loggingService = loggingService;
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _loggingService.LogUserAction("Health check requested");
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Normaize API",
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        var result = await _healthCheckService.CheckHealthAsync();
        
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
    public async Task<IActionResult> GetLiveness()
    {
        var result = await _healthCheckService.CheckLivenessAsync();
        
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
    public async Task<IActionResult> GetReadiness()
    {
        var result = await _healthCheckService.CheckReadinessAsync();
        
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