using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController(IStructuredLoggingService loggingService) : BaseApiController(loggingService)
{

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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