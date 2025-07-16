using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;

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
    [ProducesResponseType(typeof(HealthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Get()
    {
        _loggingService.LogUserAction("Health check requested", null);
        
        return Ok(new HealthResponseDto
        {
            Status = "healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Normaize API",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }
} 