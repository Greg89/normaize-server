using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Infrastructure.Workers;

/// <summary>
/// Hosted service that runs the normalization background worker
/// </summary>
public class WorkerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WorkerHostedService> _logger;

    public WorkerHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<WorkerHostedService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Normalization Worker starting - waiting for database to be ready...");

        // Wait for database migrations to complete before starting worker
        await WaitForDatabaseAsync(stoppingToken);

        _logger.LogInformation("Database ready - Data Normalization Worker started");

        using var scope = _serviceScopeFactory.CreateScope();
        var worker = scope.ServiceProvider.GetRequiredService<IBackgroundWorker>();

        try
        {
            await worker.ProcessJobsAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Data Normalization Worker cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Data Normalization Worker failed with unhandled exception");
            throw; // Re-throw to crash the service and trigger restart
        }
        finally
        {
            _logger.LogInformation("Data Normalization Worker stopped");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data Normalization Worker stop requested");
        await base.StopAsync(cancellationToken);
    }

    private async Task WaitForDatabaseAsync(CancellationToken stoppingToken)
    {
        const int maxRetries = 30;
        const int delaySeconds = 2;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<Data.DataNormalizationDbContext>();

                if (dbContext != null)
                {
                    // Check if migrations are complete by querying a table
                    var canConnect = await dbContext.Database.CanConnectAsync(stoppingToken);
                    if (canConnect)
                    {
                        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(stoppingToken);
                        if (!pendingMigrations.Any())
                        {
                            _logger.LogInformation("Database is ready with all migrations applied");
                            return;
                        }

                        _logger.LogInformation("Waiting for {Count} pending migration(s) to complete...", pendingMigrations.Count());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Database not ready yet (attempt {Attempt}/{Max}): {Message}",
                    i + 1, maxRetries, ex.Message);
            }

            if (i < maxRetries - 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
        }

        _logger.LogWarning("Database did not become ready within timeout - worker will start anyway");
    }
}