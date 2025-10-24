using System;
using System.Threading;
using System.Threading.Tasks;
using Normaize.DataNormalization.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Normaize.DataNormalization.Infrastructure.Workers;

/// <summary>
/// Background worker interface for processing normalization jobs
/// </summary>
public interface IBackgroundWorker
{
    Task ProcessJobsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Background worker that processes normalization jobs from the queue
/// </summary>
public class NormalizationWorker : IBackgroundWorker
{
    private readonly IJobQueue _jobQueue;
    private readonly INormalizationJobRouter _jobRouter;
    private readonly IJobProgress _jobProgress;
    private readonly ILogger<NormalizationWorker> _logger;

    public NormalizationWorker(
        IJobQueue jobQueue,
        INormalizationJobRouter jobRouter,
        IJobProgress jobProgress,
        ILogger<NormalizationWorker> logger)
    {
        _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
        _jobRouter = jobRouter ?? throw new ArgumentNullException(nameof(jobRouter));
        _jobProgress = jobProgress ?? throw new ArgumentNullException(nameof(jobProgress));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var job = await _jobQueue.DequeueAsync();
                if (job == null)
                {
                    // No jobs available, wait before checking again
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    continue;
                }

                _logger.LogInformation("Processing job {JobId} of type {OperationType}", job.Id, job.OperationType);

                try
                {
                    // Process the job using the router
                    await _jobRouter.HandleAsync(job, _jobProgress);
                    
                    // Acknowledge successful processing
                    await _jobQueue.AckAsync(job.Id);
                    
                    _logger.LogInformation("Successfully processed job {JobId}", job.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process job {JobId}", job.Id);
                    
                    // Negative acknowledge - will retry or move to dead letter
                    await _jobQueue.NackAsync(job.Id, ex.Message, TimeSpan.FromMinutes(5));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background worker main loop");
                // Continue processing after a delay to avoid tight error loops
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }
}