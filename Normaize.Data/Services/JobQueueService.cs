using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Normaize.Data.Services;

/// <summary>
/// Database-backed job queue service for processing normalization jobs
/// </summary>
public class JobQueueService : IJobQueueService, IDisposable
{
    private readonly NormaizeContext _context;
    private readonly ILogger<JobQueueService> _logger;
    private readonly JobQueueOptions _options;
    private readonly SemaphoreSlim _processingSemaphore;
    private readonly ConcurrentDictionary<string, DateTime> _processingJobs;
    private readonly Timer _cleanupTimer;
    private readonly Timer _retryTimer;
    private bool _disposed;

    public JobQueueService(
        NormaizeContext context,
        ILogger<JobQueueService> logger,
        IOptions<JobQueueOptions> options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);

        _context = context;
        _logger = logger;
        _options = options.Value;
        _processingSemaphore = new SemaphoreSlim(_options.MaxConcurrentJobs, _options.MaxConcurrentJobs);
        _processingJobs = new ConcurrentDictionary<string, DateTime>();

        // Start cleanup timer
        _cleanupTimer = new Timer(CleanupOldJobs, null, _options.CleanupInterval, _options.CleanupInterval);

        // Start retry timer
        _retryTimer = new Timer(ProcessRetryJobs, null, _options.RetryCheckInterval, _options.RetryCheckInterval);

        _logger.LogInformation("JobQueueService initialized with max concurrent jobs: {MaxConcurrentJobs}, cleanup interval: {CleanupInterval}, retry interval: {RetryInterval}",
            _options.MaxConcurrentJobs, _options.CleanupInterval, _options.RetryCheckInterval);
    }

    public async Task<bool> EnqueueJobAsync(DataNormalizationJob job)
    {
        try
        {
            _logger.LogDebug("Enqueueing job {JobId} for dataset {DataSetId}", job.Id, job.DataSetId);

            job.Status = NormalizationJobStatus.Queued;
            job.SubmittedAt = DateTime.UtcNow;
            job.LastModifiedAt = DateTime.UtcNow;
            job.LastModifiedBy = job.UserId;

            _context.DataNormalizationJobs.Add(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Job {JobId} enqueued successfully", job.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue job {JobId}", job.Id);
            return false;
        }
    }

    public async Task<DataNormalizationJob?> DequeueJobAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _processingSemaphore.WaitAsync(cancellationToken);

            // Get the highest priority job that's queued
            var job = await _context.DataNormalizationJobs
                .Where(j => j.Status == NormalizationJobStatus.Queued && !j.IsDeleted)
                .OrderByDescending(j => j.Priority)
                .ThenBy(j => j.SubmittedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (job != null)
            {
                job.Status = NormalizationJobStatus.Processing;
                job.StartedAt = DateTime.UtcNow;
                job.LastModifiedAt = DateTime.UtcNow;
                job.LastModifiedBy = job.UserId;

                await _context.SaveChangesAsync(cancellationToken);

                _processingJobs.TryAdd(job.Id, DateTime.UtcNow);
                _logger.LogDebug("Job {JobId} dequeued and marked as processing", job.Id);
            }

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dequeuing job");
            return null;
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    public async Task<int> GetQueueLengthAsync()
    {
        try
        {
            return await _context.DataNormalizationJobs
                .CountAsync(j => j.Status == NormalizationJobStatus.Queued && !j.IsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue length");
            return 0;
        }
    }

    public async Task<IEnumerable<DataNormalizationJob>> GetJobsByPriorityAsync(
        NormalizationJobStatus status,
        int maxPriority = int.MaxValue,
        int limit = 100)
    {
        try
        {
            return await _context.DataNormalizationJobs
                .Where(j => j.Status == status && j.Priority <= maxPriority && !j.IsDeleted)
                .OrderByDescending(j => j.Priority)
                .ThenBy(j => j.SubmittedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs by priority");
            return Enumerable.Empty<DataNormalizationJob>();
        }
    }

    public async Task<bool> MarkJobAsStartedAsync(string jobId)
    {
        try
        {
            var job = await _context.DataNormalizationJobs.FindAsync(jobId);
            if (job == null) return false;

            job.Status = NormalizationJobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;
            job.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job {JobId} as started", jobId);
            return false;
        }
    }

    public async Task<bool> UpdateJobProgressAsync(string jobId, int progressPercentage, string message)
    {
        try
        {
            var job = await _context.DataNormalizationJobs.FindAsync(jobId);
            if (job == null) return false;

            job.ProgressPercentage = Math.Clamp(progressPercentage, 0, 100);
            job.LastModifiedAt = DateTime.UtcNow;

            // Add audit log entry
            var auditLog = new DataNormalizationAuditLog
            {
                NormalizationJobId = jobId,
                UserId = job.UserId,
                Action = "Progress",
                Changes = JsonSerializer.Serialize(new { ProgressPercentage = progressPercentage, Message = message }),
                Timestamp = DateTime.UtcNow
            };

            _context.DataNormalizationAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating progress for job {JobId}", jobId);
            return false;
        }
    }

    public async Task<bool> MarkJobAsCompletedAsync(string jobId, string results)
    {
        try
        {
            var job = await _context.DataNormalizationJobs.FindAsync(jobId);
            if (job == null) return false;

            job.Status = NormalizationJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ProgressPercentage = 100;
            job.Results = results;
            job.LastModifiedAt = DateTime.UtcNow;

            // Add audit log entry
            var auditLog = new DataNormalizationAuditLog
            {
                NormalizationJobId = jobId,
                UserId = job.UserId,
                Action = "Completed",
                Changes = JsonSerializer.Serialize(new { Results = results }),
                Timestamp = DateTime.UtcNow
            };

            _context.DataNormalizationAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _processingJobs.TryRemove(jobId, out _);
            _logger.LogInformation("Job {JobId} marked as completed", jobId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job {JobId} as completed", jobId);
            return false;
        }
    }

    public async Task<bool> MarkJobAsFailedAsync(string jobId, string errorMessage)
    {
        try
        {
            var job = await _context.DataNormalizationJobs.FindAsync(jobId);
            if (job == null) return false;

            job.Status = NormalizationJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = errorMessage;
            job.LastModifiedAt = DateTime.UtcNow;

            // Add audit log entry
            var auditLog = new DataNormalizationAuditLog
            {
                NormalizationJobId = jobId,
                UserId = job.UserId,
                Action = "Failed",
                Changes = JsonSerializer.Serialize(new { ErrorMessage = errorMessage }),
                Timestamp = DateTime.UtcNow
            };

            _context.DataNormalizationAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _processingJobs.TryRemove(jobId, out _);
            _logger.LogWarning("Job {JobId} marked as failed: {ErrorMessage}", jobId, errorMessage);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job {JobId} as failed", jobId);
            return false;
        }
    }

    public async Task<bool> MarkJobAsCancelledAsync(string jobId)
    {
        try
        {
            var job = await _context.DataNormalizationJobs.FindAsync(jobId);
            if (job == null) return false;

            job.Status = NormalizationJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
            job.LastModifiedAt = DateTime.UtcNow;

            // Add audit log entry
            var auditLog = new DataNormalizationAuditLog
            {
                NormalizationJobId = jobId,
                UserId = job.UserId,
                Action = "Cancelled",
                Timestamp = DateTime.UtcNow
            };

            _context.DataNormalizationAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _processingJobs.TryRemove(jobId, out _);
            _logger.LogInformation("Job {JobId} marked as cancelled", jobId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking job {JobId} as cancelled", jobId);
            return false;
        }
    }

    public async Task<bool> RetryJobAsync(string jobId, DateTime nextRetryAt)
    {
        try
        {
            var job = await _context.DataNormalizationJobs.FindAsync(jobId);
            if (job == null) return false;

            if (job.RetryCount >= job.MaxRetries)
            {
                _logger.LogWarning("Job {JobId} has exceeded maximum retry attempts ({MaxRetries})", jobId, job.MaxRetries);
                return false;
            }

            job.Status = NormalizationJobStatus.Queued;
            job.RetryCount++;
            job.NextRetryAt = nextRetryAt;
            job.ProgressPercentage = 0;
            job.ErrorMessage = null;
            job.LastModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Job {JobId} scheduled for retry {RetryCount}/{MaxRetries} at {NextRetryAt}",
                jobId, job.RetryCount, job.MaxRetries, nextRetryAt);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling retry for job {JobId}", jobId);
            return false;
        }
    }

    public async Task<IEnumerable<DataNormalizationJob>> GetJobsReadyForRetryAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            return await _context.DataNormalizationJobs
                .Where(j => j.Status == NormalizationJobStatus.Failed &&
                           j.RetryCount < j.MaxRetries &&
                           j.NextRetryAt <= now &&
                           !j.IsDeleted)
                .OrderBy(j => j.NextRetryAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs ready for retry");
            return Enumerable.Empty<DataNormalizationJob>();
        }
    }

    public async Task<int> CleanupOldJobsAsync(DateTime olderThan)
    {
        try
        {
            var jobsToDelete = await _context.DataNormalizationJobs
                .Where(j => j.Status == NormalizationJobStatus.Completed &&
                           j.CompletedAt < olderThan &&
                           !j.IsDeleted)
                .ToListAsync();

            foreach (var job in jobsToDelete)
            {
                job.IsDeleted = true;
                job.DeletedAt = DateTime.UtcNow;
                job.DeletedBy = "System";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} old completed jobs", jobsToDelete.Count);
            return jobsToDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up old jobs");
            return 0;
        }
    }

    private async void CleanupOldJobs(object? state)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_options.JobRetentionDays);
            await CleanupOldJobsAsync(cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cleanup timer");
        }
    }

    private async void ProcessRetryJobs(object? state)
    {
        try
        {
            var retryJobs = await GetJobsReadyForRetryAsync();
            foreach (var job in retryJobs)
            {
                _logger.LogInformation("Processing retry for job {JobId}", job.Id);
                // The actual retry logic will be handled by the background service
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in retry timer");
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _cleanupTimer?.Dispose();
            _retryTimer?.Dispose();
            _processingSemaphore?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Configuration options for the job queue service
/// </summary>
public class JobQueueOptions
{
    /// <summary>
    /// Maximum number of concurrent jobs that can be processed
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 5;

    /// <summary>
    /// How often to clean up old completed jobs
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How often to check for jobs that need retrying
    /// </summary>
    public TimeSpan RetryCheckInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How many days to retain completed jobs before cleanup
    /// </summary>
    public int JobRetentionDays { get; set; } = 30;
}
