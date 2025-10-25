using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Job progress reporting service that updates job status and publishes domain events
/// </summary>
public class JobProgressService : IJobProgress
{
    private readonly INormalizationJobRepository _jobRepository;
    private readonly ILogger<JobProgressService> _logger;

    public JobProgressService(
        INormalizationJobRepository jobRepository,
        ILogger<JobProgressService> logger)
    {
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartedAsync(Guid jobId)
    {
        try
        {
            _logger.LogDebug("Marking job {JobId} as started", jobId);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Cannot mark job {JobId} as started - job not found", jobId);
                return;
            }

            if (job.Status != JobStatus.Processing)
            {
                _logger.LogWarning("Job {JobId} is not in processing status, current status: {Status}",
                    jobId, job.Status);
                return;
            }

            // Job.Start() is typically called during dequeue, but this ensures consistency
            if (job.StartedAt == null)
            {
                job.Start();
                await _jobRepository.UpdateAsync(job);
            }

            _logger.LogInformation("Job {JobId} marked as started", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job {JobId} as started", jobId);
            throw;
        }
    }

    public async Task ReportAsync(Guid jobId, int percent, string message)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentException("Percent must be between 0 and 100", nameof(percent));

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be null or empty", nameof(message));

        try
        {
            _logger.LogDebug("Reporting progress for job {JobId}: {Percent}% - {Message}",
                jobId, percent, message);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Cannot report progress for job {JobId} - job not found", jobId);
                return;
            }

            if (job.Status != JobStatus.Processing)
            {
                _logger.LogWarning("Cannot report progress for job {JobId} - job is not in processing status (current: {Status})",
                    jobId, job.Status);
                return;
            }

            // Update progress
            job.UpdateProgress(percent, message);
            await _jobRepository.UpdateAsync(job);

            _logger.LogDebug("Progress updated for job {JobId}: {Percent}% - {Message}",
                jobId, percent, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting progress for job {JobId}", jobId);
            throw;
        }
    }

    public async Task SucceededAsync(Guid jobId, object? result)
    {
        try
        {
            _logger.LogDebug("Marking job {JobId} as succeeded", jobId);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Cannot mark job {JobId} as succeeded - job not found", jobId);
                return;
            }

            if (job.Status != JobStatus.Processing)
            {
                _logger.LogWarning("Cannot mark job {JobId} as succeeded - job is not in processing status (current: {Status})",
                    jobId, job.Status);
                return;
            }

            // Convert result to string for storage
            var resultString = result switch
            {
                null => null,
                string s => s,
                _ => System.Text.Json.JsonSerializer.Serialize(result)
            };

            // Mark job as completed
            job.Complete(resultString);
            await _jobRepository.UpdateAsync(job);

            _logger.LogInformation("Job {JobId} marked as succeeded", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job {JobId} as succeeded", jobId);
            throw;
        }
    }

    public async Task FailedAsync(Guid jobId, string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        try
        {
            _logger.LogDebug("Marking job {JobId} as failed with error: {Error}", jobId, error);

            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job == null)
            {
                _logger.LogWarning("Cannot mark job {JobId} as failed - job not found", jobId);
                return;
            }

            if (job.Status != JobStatus.Processing)
            {
                _logger.LogWarning("Cannot mark job {JobId} as failed - job is not in processing status (current: {Status})",
                    jobId, job.Status);
                return;
            }

            // Check if we should retry or fail permanently
            if (job.RetryCount < job.MaxRetries)
            {
                // First mark as failed, then schedule retry
                job.Fail(error);
                job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5)); // Schedule retry in 5 minutes
                await _jobRepository.UpdateAsync(job);

                _logger.LogInformation("Job {JobId} scheduled for retry ({RetryCount}/{MaxRetries}) due to error: {Error}",
                    jobId, job.RetryCount, job.MaxRetries, error);
            }
            else
            {
                // Fail permanently - just call Fail, don't schedule retry
                job.Fail(error);
                await _jobRepository.UpdateAsync(job);

                _logger.LogWarning("Job {JobId} failed permanently after {RetryCount} retries. Error: {Error}",
                    jobId, job.RetryCount, error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job {JobId} as failed", jobId);
            throw;
        }
    }
}