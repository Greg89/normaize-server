using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Interfaces;
using System.Diagnostics;

namespace Normaize.Data.Services;

public class HealthCheckService(
    NormaizeContext context,
    ILogger<HealthCheckService> logger,
    IOptions<HealthCheckConfiguration> config) : IHealthCheckService
{
    private readonly NormaizeContext _context = context;
    private readonly ILogger<HealthCheckService> _logger = logger;
    private readonly HealthCheckConfiguration _config = config.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting comprehensive health check. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var components = await CheckAllComponentsAsync(correlationId, cancellationToken);
            
            var result = CreateHealthResult(components, "healthy", "unhealthy", correlationId);
            result.Duration = stopwatch.Elapsed;

            LogHealthCheckResult(result, correlationId);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Health check was cancelled. CorrelationId: {CorrelationId}", correlationId);
            
            return CreateErrorResult("Health check was cancelled", stopwatch.Elapsed, correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during health check. CorrelationId: {CorrelationId}", correlationId);
            
            return CreateErrorResult(
                _config.IncludeDetailedErrors ? ex.Message : "An unexpected error occurred during health check",
                stopwatch.Elapsed,
                correlationId);
        }
    }

    public async Task<HealthCheckResult> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting liveness check. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var appHealth = await CheckApplicationHealthAsync(correlationId, cancellationToken);
            
            var result = new HealthCheckResult
            {
                IsHealthy = appHealth.IsHealthy,
                Status = appHealth.IsHealthy ? "alive" : "not_alive",
                Components = new Dictionary<string, ComponentHealth>
                {
                    [_config.ComponentNames.Application] = appHealth
                },
                Duration = stopwatch.Elapsed,
                CorrelationId = correlationId
            };

            if (!appHealth.IsHealthy)
            {
                result.Issues.Add($"{_config.ComponentNames.Application}: {appHealth.ErrorMessage}");
            }

            LogHealthCheckResult(result, correlationId);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Liveness check was cancelled. CorrelationId: {CorrelationId}", correlationId);
            
            return CreateErrorResult("Liveness check was cancelled", stopwatch.Elapsed, correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during liveness check. CorrelationId: {CorrelationId}", correlationId);
            
            return CreateErrorResult(
                _config.IncludeDetailedErrors ? ex.Message : "An unexpected error occurred during liveness check",
                stopwatch.Elapsed,
                correlationId);
        }
    }

    public async Task<HealthCheckResult> CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting readiness check. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var components = new Dictionary<string, ComponentHealth>();

            if (!_config.SkipDatabaseCheck)
            {
                var dbHealth = await CheckDatabaseHealthAsync(correlationId, cancellationToken);
                components[_config.ComponentNames.Database] = dbHealth;
            }

            var appHealth = await CheckApplicationHealthAsync(correlationId, cancellationToken);
            components[_config.ComponentNames.Application] = appHealth;

            var result = CreateHealthResult(components, "ready", "not_ready", correlationId);
            result.Duration = stopwatch.Elapsed;

            LogHealthCheckResult(result, correlationId);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Readiness check was cancelled. CorrelationId: {CorrelationId}", correlationId);
            
            return CreateErrorResult("Readiness check was cancelled", stopwatch.Elapsed, correlationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during readiness check. CorrelationId: {CorrelationId}", correlationId);
            
            return CreateErrorResult(
                _config.IncludeDetailedErrors ? ex.Message : "An unexpected error occurred during readiness check",
                stopwatch.Elapsed,
                correlationId);
        }
    }

    private async Task<Dictionary<string, ComponentHealth>> CheckAllComponentsAsync(string correlationId, CancellationToken cancellationToken)
    {
        var components = new Dictionary<string, ComponentHealth>();

        if (!_config.SkipDatabaseCheck)
        {
            var dbHealth = await CheckDatabaseHealthAsync(correlationId, cancellationToken);
            components[_config.ComponentNames.Database] = dbHealth;
        }

        var appHealth = await CheckApplicationHealthAsync(correlationId, cancellationToken);
        components[_config.ComponentNames.Application] = appHealth;

        return components;
    }

    private async Task<ComponentHealth> CheckDatabaseHealthAsync(string correlationId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogDebug("Checking database health. CorrelationId: {CorrelationId}", correlationId);
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.DatabaseTimeoutSeconds));

            var canConnect = await _context.Database.CanConnectAsync(cts.Token);
            
            stopwatch.Stop();
            
            var health = new ComponentHealth
            {
                IsHealthy = canConnect,
                Status = canConnect ? "healthy" : "unhealthy",
                ErrorMessage = canConnect ? null : "Cannot connect to database",
                Details = new Dictionary<string, object>
                {
                    ["canConnect"] = canConnect,
                    ["timeoutSeconds"] = _config.DatabaseTimeoutSeconds
                },
                Duration = stopwatch.Elapsed,
                CorrelationId = correlationId
            };

            _logger.LogDebug("Database health check completed. IsHealthy: {IsHealthy}, Duration: {Duration}ms. CorrelationId: {CorrelationId}", 
                health.IsHealthy, health.Duration.TotalMilliseconds, correlationId);

            return health;
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Database health check timed out after {TimeoutSeconds}s. CorrelationId: {CorrelationId}", 
                _config.DatabaseTimeoutSeconds, correlationId);
            
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "timeout",
                ErrorMessage = $"Database health check timed out after {_config.DatabaseTimeoutSeconds} seconds",
                Duration = stopwatch.Elapsed,
                CorrelationId = correlationId
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during database health check. CorrelationId: {CorrelationId}", correlationId);
            
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = _config.IncludeDetailedErrors ? ex.Message : "Database health check failed",
                Duration = stopwatch.Elapsed,
                CorrelationId = correlationId
            };
        }
    }

    private async Task<ComponentHealth> CheckApplicationHealthAsync(string correlationId, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogDebug("Checking application health. CorrelationId: {CorrelationId}", correlationId);
        
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.ApplicationTimeoutSeconds));

            // If using in-memory provider, skip relational checks and always return healthy
            var providerName = _context.Database.ProviderName;
            if (providerName != null && providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                stopwatch.Stop();
                var health = new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    ErrorMessage = null,
                    Details = new Dictionary<string, object>
                    {
                        ["provider"] = providerName,
                        ["note"] = "In-memory provider: skipping relational checks"
                    },
                    Duration = stopwatch.Elapsed,
                    CorrelationId = correlationId
                };

                _logger.LogDebug("Application health check completed (in-memory). IsHealthy: {IsHealthy}, Duration: {Duration}ms. CorrelationId: {CorrelationId}", 
                    health.IsHealthy, health.Duration.TotalMilliseconds, correlationId);

                return health;
            }

            // Check if application is responsive
            var canConnect = await _context.Database.CanConnectAsync(cts.Token);
            
            // Check for pending migrations (only if not skipped)
            var pendingMigrations = new List<string>();
            if (!_config.SkipMigrationsCheck)
            {
                pendingMigrations = [.. await _context.Database.GetPendingMigrationsAsync(cts.Token)];
            }

            stopwatch.Stop();
            
            var isHealthy = canConnect && pendingMigrations.Count == 0;
            string? errorMessage = null;
            if (!canConnect)
            {
                errorMessage = "Cannot connect to database";
            }
            else if (pendingMigrations.Count > 0)
            {
                errorMessage = $"Pending migrations: {string.Join(", ", pendingMigrations)}";
            }

            var appHealth = new ComponentHealth
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "healthy" : "unhealthy",
                ErrorMessage = errorMessage,
                Details = new Dictionary<string, object>
                {
                    ["pendingMigrations"] = pendingMigrations,
                    ["canConnect"] = canConnect,
                    ["skipMigrationsCheck"] = _config.SkipMigrationsCheck,
                    ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    ["timeoutSeconds"] = _config.ApplicationTimeoutSeconds
                },
                Duration = stopwatch.Elapsed,
                CorrelationId = correlationId
            };

            _logger.LogDebug("Application health check completed. IsHealthy: {IsHealthy}, Duration: {Duration}ms. CorrelationId: {CorrelationId}", 
                appHealth.IsHealthy, appHealth.Duration.TotalMilliseconds, correlationId);

            return appHealth;
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Application health check timed out after {TimeoutSeconds}s. CorrelationId: {CorrelationId}", 
                _config.ApplicationTimeoutSeconds, correlationId);
            
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "timeout",
                ErrorMessage = $"Application health check timed out after {_config.ApplicationTimeoutSeconds} seconds",
                Duration = stopwatch.Elapsed,
                CorrelationId = correlationId
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during application health check. CorrelationId: {CorrelationId}", correlationId);
            
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = _config.IncludeDetailedErrors ? ex.Message : "Application health check failed",
                Duration = stopwatch.Elapsed,
                CorrelationId = correlationId
            };
        }
    }

    private static HealthCheckResult CreateHealthResult(
        Dictionary<string, ComponentHealth> components, 
        string healthyStatus, 
        string unhealthyStatus,
        string correlationId)
    {
        var isHealthy = components.All(c => c.Value.IsHealthy);
        var issues = components.Where(c => !c.Value.IsHealthy)
                              .Select(c => $"{c.Key}: {c.Value.ErrorMessage}")
                              .ToList();

        return new HealthCheckResult
        {
            IsHealthy = isHealthy,
            Status = isHealthy ? healthyStatus : unhealthyStatus,
            Components = components,
            Issues = issues,
            CorrelationId = correlationId
        };
    }

    private static HealthCheckResult CreateErrorResult(string errorMessage, TimeSpan duration, string correlationId)
    {
        return new HealthCheckResult
        {
            IsHealthy = false,
            Status = "error",
            Issues = [errorMessage],
            Duration = duration,
            CorrelationId = correlationId
        };
    }

    private void LogHealthCheckResult(HealthCheckResult result, string correlationId)
    {
        if (result.IsHealthy)
        {
            _logger.LogInformation("Health check completed successfully. Status: {Status}, Duration: {Duration}ms, CorrelationId: {CorrelationId}", 
                result.Status, result.Duration.TotalMilliseconds, correlationId);
        }
        else
        {
            _logger.LogWarning("Health check failed. Status: {Status}, Issues: {IssueCount}, Duration: {Duration}ms, CorrelationId: {CorrelationId}", 
                result.Status, result.Issues.Count, result.Duration.TotalMilliseconds, correlationId);
        }
    }
} 