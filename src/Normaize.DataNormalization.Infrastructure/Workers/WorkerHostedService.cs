using System;
using System.Threading;
using System.Threading.Tasks;
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
        _logger.LogInformation("Data Normalization Worker started");

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
}