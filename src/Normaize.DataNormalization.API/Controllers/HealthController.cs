using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Normaize.DataNormalization.API.Controllers;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Controller for health check endpoints using ASP.NET Core Health Checks
/// </summary>
[Route("api/health")]
public class HealthController : BaseApiController
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get overall health status with detailed information
    /// </summary>
    /// <returns>Comprehensive health check results</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(503)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetHealthStatus()
    {
        try
        {
            _logger.LogDebug("Performing comprehensive health check");

            var healthReport = await _healthCheckService.CheckHealthAsync();

            var response = new
            {
                Status = healthReport.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Duration = healthReport.TotalDuration,
                Results = healthReport.Entries.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        Status = kvp.Value.Status.ToString(),
                        Description = kvp.Value.Description,
                        Duration = kvp.Value.Duration,
                        Exception = kvp.Value.Exception?.Message,
                        Data = kvp.Value.Data
                    })
            };

            var statusCode = healthReport.Status switch
            {
                HealthStatus.Healthy => 200,
                HealthStatus.Degraded => 200,
                HealthStatus.Unhealthy => 503,
                _ => 500
            };

            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check");
            return StatusCode(500, new
            {
                Status = "Error",
                Timestamp = DateTime.UtcNow,
                Message = "Health check failed with exception",
                Exception = ex.Message
            });
        }
    }

    /// <summary>
    /// Get readiness status (Kubernetes readiness probe)
    /// </summary>
    /// <returns>Readiness status for container orchestration</returns>
    [HttpGet("ready")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> GetReadinessStatus()
    {
        try
        {
            _logger.LogDebug("Performing readiness check");

            // Run only readiness-tagged health checks
            var healthReport = await _healthCheckService.CheckHealthAsync(check =>
                check.Tags.Contains("ready"));

            var response = new
            {
                Status = healthReport.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Ready = healthReport.Status == HealthStatus.Healthy
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing readiness check");
            return StatusCode(503, new
            {
                Status = "NotReady",
                Timestamp = DateTime.UtcNow,
                Ready = false,
                Exception = ex.Message
            });
        }
    }

    /// <summary>
    /// Get liveness status (Kubernetes liveness probe)
    /// </summary>
    /// <returns>Liveness status for container orchestration</returns>
    [HttpGet("live")]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult GetLivenessStatus()
    {
        try
        {
            _logger.LogDebug("Performing liveness check");

            // Simple liveness check - if we can respond, we're alive
            var response = new
            {
                Status = "Alive",
                Timestamp = DateTime.UtcNow,
                Alive = true,
                Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing liveness check");
            return StatusCode(500, new
            {
                Status = "Error",
                Timestamp = DateTime.UtcNow,
                Alive = false,
                Exception = ex.Message
            });
        }
    }

    /// <summary>
    /// Get database health status specifically
    /// </summary>
    /// <returns>Database connectivity status</returns>
    [HttpGet("database")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> GetDatabaseHealth()
    {
        try
        {
            _logger.LogDebug("Performing database health check");

            var healthReport = await _healthCheckService.CheckHealthAsync(check =>
                check.Tags.Contains("database"));

            var databaseCheck = healthReport.Entries.FirstOrDefault();

            var response = new
            {
                Status = healthReport.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Database = databaseCheck.Key ?? "Unknown",
                Healthy = healthReport.Status == HealthStatus.Healthy,
                Duration = databaseCheck.Value.Duration,
                Description = databaseCheck.Value.Description,
                Data = databaseCheck.Value.Data
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing database health check");
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Database = "Unknown",
                Healthy = false,
                Exception = ex.Message
            });
        }
    }

    /// <summary>
    /// Get storage health status specifically
    /// </summary>
    /// <returns>Storage connectivity status</returns>
    [HttpGet("storage")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> GetStorageHealth()
    {
        try
        {
            _logger.LogDebug("Performing storage health check");

            var healthReport = await _healthCheckService.CheckHealthAsync(check =>
                check.Tags.Contains("storage"));

            var storageCheck = healthReport.Entries.FirstOrDefault();

            var response = new
            {
                Status = healthReport.Status.ToString(),
                Timestamp = DateTime.UtcNow,
                Storage = storageCheck.Key ?? "Unknown",
                Healthy = healthReport.Status == HealthStatus.Healthy,
                Duration = storageCheck.Value.Duration,
                Description = storageCheck.Value.Description,
                Data = storageCheck.Value.Data
            };

            var statusCode = healthReport.Status == HealthStatus.Healthy ? 200 : 503;
            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing storage health check");
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Storage = "Unknown",
                Healthy = false,
                Exception = ex.Message
            });
        }
    }
}