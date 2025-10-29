using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Database-backed job queue implementation using the NormalizationJob repository
/// </summary>
public class JobQueueService : IJobQueue
{
    private readonly INormalizationJobRepository _jobRepository;
    private readonly ILogger<JobQueueService> _logger;

    public JobQueueService(
        INormalizationJobRepository jobRepository,
        ILogger<JobQueueService> logger)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnqueueAsync(NormalizationJob job)
    {
        if (job == null) throw new ArgumentNullException(nameof(job));

        try
        {
            _logger.LogDebug("Enqueuing job {JobId} for dataset {DataSetId}", job.Id, job.DataSetId);

            // Job should already be in Queued status when created
            if (job.Status != JobStatus.Queued)
            {
                throw new InvalidOperationException($"Job {job.Id} must be in Queued status to be enqueued, but was {job.Status}");
            }

            // Job should already be saved by the command handler
            // Just log the enqueue operation
            _logger.LogInformation("Successfully enqueued job {JobId} of type {OperationType} for dataset {DataSetId}",
                job.Id, job.OperationType, job.DataSetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueuing job {JobId}", job.Id);
            throw;
        }
    }

    public async Task<NormalizationJob?> DequeueAsync()
    {
        try
        {
            _logger.LogDebug("Attempting to dequeue next available job");

            // Get the oldest queued job
            var job = await _jobRepository.GetNextQueuedJobAsync();

            if (job == null)
            {
                _logger.LogDebug("No queued jobs available for processing");
                return null;
            }

            // Mark the job as processing to prevent other workers from picking it up
            job.Start();

            // Update the job status in the database
            await _jobRepository.UpdateAsync(job);

            _logger.LogInformation("Successfully dequeued job {JobId} of type {OperationType} for dataset {DataSetId}",
                job.Id, job.OperationType, job.DataSetId);

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dequeuing job from queue");
            throw;
        }
    }

    public async Task AckAsync(Guid jobId)
    {
        try
        {
            _logger.LogDebug("Acknowledging successful completion of job {JobId}", jobId);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Cannot acknowledge job {JobId} - job not found", jobId);
                return;
            }

            if (job.Status != JobStatus.Processing)
            {
                _logger.LogWarning("Cannot acknowledge job {JobId} - job is not in processing status (current: {Status})",
                    jobId, job.Status);
                return;
            }

            // The job completion should already be handled by the operation handlers
            // This method is mainly for explicit acknowledgment if needed
            _logger.LogInformation("Job {JobId} acknowledged successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging job {JobId}", jobId);
            throw;
        }
    }

    public async Task NackAsync(Guid jobId, string reason, TimeSpan? delay = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty", nameof(reason));

        try
        {
            _logger.LogDebug("Negative acknowledging job {JobId} with reason: {Reason}", jobId, reason);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Cannot nack job {JobId} - job not found", jobId);
                return;
            }

            if (job.Status != JobStatus.Processing)
            {
                _logger.LogWarning("Cannot nack job {JobId} - job is not in processing status (current: {Status})",
                    jobId, job.Status);
                return;
            }

            // Determine if we should retry or move to dead letter
            if (job.RetryCount < job.MaxRetries)
            {
                // First mark as failed, then schedule retry
                job.Fail(reason);
                job.ScheduleRetry(DateTime.UtcNow.Add(delay ?? TimeSpan.FromMinutes(5)));
                await _jobRepository.UpdateAsync(job);

                _logger.LogInformation("Job {JobId} scheduled for retry ({RetryCount}/{MaxRetries}) due to: {Reason}",
                    jobId, job.RetryCount, job.MaxRetries, reason);
            }
            else
            {
                // Move to dead letter queue - no need to call Fail first since MoveToDeadLetter handles it
                job.MoveToDeadLetter(reason);
                await _jobRepository.UpdateAsync(job);

                _logger.LogWarning("Job {JobId} moved to dead letter queue after {RetryCount} retries. Final reason: {Reason}",
                    jobId, job.RetryCount, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error negative acknowledging job {JobId}", jobId);
            throw;
        }
    }
}