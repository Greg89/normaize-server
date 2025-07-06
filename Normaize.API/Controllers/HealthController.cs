using Microsoft.AspNetCore.Mvc;
using Normaize.API.Services;

namespace Normaize.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly IStructuredLoggingService _loggingService;

    public HealthController(IStructuredLoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _loggingService.LogUserAction("Health check requested");
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Normaize API"
        });
    }

    [HttpGet("basic")]
    public IActionResult GetBasic()
    {
        _loggingService.LogUserAction("Basic health check requested");
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Normaize API",
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }
} 