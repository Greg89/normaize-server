using System;
using System.Threading;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Normaize.DataNormalization.Infrastructure.Workers;

/// <summary>
/// Background worker interface for processing normalization jobs
/// </summary>
public interface IBackgroundWorker
{
    Task ProcessJobsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Background worker that processes normalization jobs from the queue with improved error handling and service scoping
/// </summary>
public class NormalizationWorker : IBackgroundWorker
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NormalizationWorker> _logger;
    private readonly TimeSpan _pollingInterval;
    private readonly TimeSpan _errorDelay;

    public NormalizationWorker(
        IServiceProvider serviceProvider,
        ILogger<NormalizationWorker> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pollingInterval = TimeSpan.FromSeconds(5); // Poll every 5 seconds
        _errorDelay = TimeSpan.FromSeconds(30); // Wait 30 seconds after errors
    }

    public async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Normalization worker started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessSingleJobAsync(cancellationToken);
                
                // Wait before polling for the next job
                await Task.Delay(_pollingInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Normalization worker shutdown requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in normalization worker. Will retry after delay.");
                
                try
                {
                    await Task.Delay(_errorDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Normalization worker stopped");
    }

    private async Task ProcessSingleJobAsync(CancellationToken cancellationToken)
    {
        // Use a new scope for each job to ensure proper resource disposal
        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var jobQueue = services.GetRequiredService<IJobQueue>();
        var jobProgress = services.GetRequiredService<IJobProgress>();
        var jobRouter = services.GetRequiredService<INormalizationJobRouter>();

        // Try to dequeue a job
        var job = await jobQueue.DequeueAsync();
        if (job == null)
        {
            _logger.LogTrace("No jobs available in queue");
            return;
        }

        _logger.LogInformation("Processing job {JobId} of type {OperationType} for dataset {DataSetId}", 
            job.Id, job.OperationType, job.DataSetId);

        try
        {
            // Report that we've started processing
            await jobProgress.StartedAsync(job.Id);

            // Route the job to the appropriate handler
            await jobRouter.HandleAsync(job, jobProgress);

            // Acknowledge successful completion
            await jobQueue.AckAsync(job.Id);

            _logger.LogInformation("Successfully processed job {JobId}", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing job {JobId}: {Error}", job.Id, ex.Message);

            try
            {
                // Negative acknowledge with the error
                await jobQueue.NackAsync(job.Id, ex.Message);
            }
            catch (Exception nackEx)
            {
                _logger.LogError(nackEx, "Error negative acknowledging job {JobId}", job.Id);
            }
        }
    }
}