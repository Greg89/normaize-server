using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.Infrastructure.HealthChecks;

/// <summary>
/// Health check for database connectivity and configuration.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly DataNormalizationDbContext _dbContext;

    public DatabaseHealthCheck(DataNormalizationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if database can be connected
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to database",
                    data: new Dictionary<string, object>
                    {
                        ["provider"] = _dbContext.Database.ProviderName ?? "Unknown",
                        ["timestamp"] = DateTime.UtcNow
                    });
            }

            // Check pending migrations
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var hasPendingMigrations = pendingMigrations.Any();

            var data = new Dictionary<string, object>
            {
                ["provider"] = _dbContext.Database.ProviderName ?? "Unknown",
                ["canConnect"] = true,
                ["hasPendingMigrations"] = hasPendingMigrations,
                ["pendingMigrationCount"] = pendingMigrations.Count(),
                ["timestamp"] = DateTime.UtcNow
            };

            if (hasPendingMigrations)
            {
                data["pendingMigrations"] = pendingMigrations.ToList();

                return HealthCheckResult.Degraded(
                    $"Database is accessible but has {pendingMigrations.Count()} pending migrations",
                    data: data);
            }

            return HealthCheckResult.Healthy(
                "Database is healthy and all migrations are applied",
                data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["provider"] = _dbContext.Database.ProviderName ?? "Unknown",
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                });
        }
    }
}
