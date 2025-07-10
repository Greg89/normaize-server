using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Data;
using System.Diagnostics;

namespace Normaize.Data.Services;

public class HealthCheckService : IHealthCheckService
{
    private readonly NormaizeContext _context;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        NormaizeContext context,
        ILogger<HealthCheckService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult();

        try
        {
            _logger.LogInformation("Starting health check...");

            // Check all components
            var components = new Dictionary<string, ComponentHealth>();

            // 1. Database Health
            var dbHealth = await CheckDatabaseHealthAsync();
            components["database"] = dbHealth;

            // 2. Application Health
            var appHealth = await CheckApplicationHealthAsync();
            components["application"] = appHealth;

            // Determine overall health
            result.IsHealthy = components.All(c => c.Value.IsHealthy);
            result.Status = result.IsHealthy ? "healthy" : "unhealthy";
            result.Components = components;
            result.Issues = components.Where(c => !c.Value.IsHealthy)
                                    .Select(c => $"{c.Key}: {c.Value.ErrorMessage}")
                                    .ToList();

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            if (result.IsHealthy)
            {
                _logger.LogInformation("Health check completed successfully in {Duration}ms", result.Duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogWarning("Health check failed with {IssueCount} issues in {Duration}ms", result.Issues.Count, result.Duration.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during health check");
            
            result.IsHealthy = false;
            result.Status = "error";
            result.Issues.Add($"Health check error: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            
            return result;
        }
    }

    public async Task<HealthCheckResult> CheckLivenessAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult();

        try
        {
            // Liveness check - just verify the application is responsive
            var appHealth = await CheckApplicationHealthAsync();
            
            result.IsHealthy = appHealth.IsHealthy;
            result.Status = appHealth.IsHealthy ? "alive" : "not_alive";
            result.Components = new Dictionary<string, ComponentHealth>
            {
                ["application"] = appHealth
            };
            
            if (!appHealth.IsHealthy)
            {
                result.Issues.Add($"Application: {appHealth.ErrorMessage}");
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.Status = "error";
            result.Issues.Add($"Liveness check error: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    public async Task<HealthCheckResult> CheckReadinessAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new HealthCheckResult();

        try
        {
            // Readiness check - verify the app is ready to serve traffic
            var components = new Dictionary<string, ComponentHealth>();

            // Database connectivity
            var dbHealth = await CheckDatabaseHealthAsync();
            components["database"] = dbHealth;

            // Application health
            var appHealth = await CheckApplicationHealthAsync();
            components["application"] = appHealth;

            var issues = new List<string>();
            if (!dbHealth.IsHealthy) issues.Add($"Database: {dbHealth.ErrorMessage}");
            if (!appHealth.IsHealthy) issues.Add($"Application: {appHealth.ErrorMessage}");

            result.IsHealthy = components.All(c => c.Value.IsHealthy);
            result.Status = result.IsHealthy ? "ready" : "not_ready";
            result.Components = components;
            result.Issues = issues;

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsHealthy = false;
            result.Status = "error";
            result.Issues.Add($"Readiness check error: {ex.Message}");
            result.Duration = stopwatch.Elapsed;
            return result;
        }
    }

    private async Task<ComponentHealth> CheckDatabaseHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            
            stopwatch.Stop();
            
            return new ComponentHealth
            {
                IsHealthy = canConnect,
                Status = canConnect ? "healthy" : "unhealthy",
                ErrorMessage = canConnect ? null : "Cannot connect to database",
                Details = new Dictionary<string, object>
                {
                    ["canConnect"] = canConnect
                },
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<ComponentHealth> CheckApplicationHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // If using in-memory provider, skip relational checks and always return healthy
            var providerName = _context.Database.ProviderName;
            if (providerName != null && providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                stopwatch.Stop();
                return new ComponentHealth
                {
                    IsHealthy = true,
                    Status = "healthy",
                    ErrorMessage = null,
                    Details = new Dictionary<string, object>
                    {
                        ["provider"] = providerName,
                        ["note"] = "In-memory provider: skipping relational checks"
                    },
                    Duration = stopwatch.Elapsed
                };
            }

            // Check if application is responsive
            var canConnect = await _context.Database.CanConnectAsync();
            // Check for pending migrations
            var pendingMigrations = _context.Database.GetPendingMigrations().ToList();
            stopwatch.Stop();
            var isHealthy = canConnect && !pendingMigrations.Any();
            return new ComponentHealth
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "healthy" : "unhealthy",
                ErrorMessage = !canConnect ? "Cannot connect to database" : 
                              pendingMigrations.Any() ? $"Pending migrations: {string.Join(", ", pendingMigrations)}" : null,
                Details = new Dictionary<string, object>
                {
                    ["pendingMigrations"] = pendingMigrations,
                    ["canConnect"] = canConnect,
                    ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
                },
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ComponentHealth
            {
                IsHealthy = false,
                Status = "error",
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }
} 