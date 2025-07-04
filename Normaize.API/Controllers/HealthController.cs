using Microsoft.AspNetCore.Mvc;

namespace Normaize.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("Health check requested at {Timestamp}", DateTime.UtcNow);
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Normaize API"
        });
    }
} 