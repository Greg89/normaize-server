using Normaize.Core.Models;
using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for managing background job processing queue
/// </summary>
public interface IJobQueueService
{
    /// <summary>
    /// Enqueues a job for processing
    /// </summary>
    /// <param name="job">Job to enqueue</param>
    /// <returns>Whether the job was successfully enqueued</returns>
    Task<bool> EnqueueJobAsync(DataNormalizationJob job);

    /// <summary>
    /// Dequeues the next job for processing
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next job to process, or null if no jobs available</returns>
    Task<DataNormalizationJob?> DequeueJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current queue length
    /// </summary>
    /// <returns>Number of jobs in the queue</returns>
    Task<int> GetQueueLengthAsync();

    /// <summary>
    /// Gets jobs by priority and status
    /// </summary>
    /// <param name="status">Status to filter by</param>
    /// <param name="maxPriority">Maximum priority to include</param>
    /// <param name="limit">Maximum number of jobs to return</param>
    /// <returns>List of jobs matching the criteria</returns>
    Task<IEnumerable<DataNormalizationJob>> GetJobsByPriorityAsync(
        NormalizationJobStatus status,
        int maxPriority = int.MaxValue,
        int limit = 100);

    /// <summary>
    /// Marks a job as started
    /// </summary>
    /// <param name="jobId">ID of the job</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> MarkJobAsStartedAsync(string jobId);

    /// <summary>
    /// Updates job progress
    /// </summary>
    /// <param name="jobId">ID of the job</param>
    /// <param name="progressPercentage">Progress percentage (0-100)</param>
    /// <param name="message">Status message</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> UpdateJobProgressAsync(string jobId, int progressPercentage, string message);

    /// <summary>
    /// Marks a job as completed
    /// </summary>
    /// <param name="jobId">ID of the job</param>
    /// <param name="results">Results of the operation</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> MarkJobAsCompletedAsync(string jobId, string results);

    /// <summary>
    /// Marks a job as failed
    /// </summary>
    /// <param name="jobId">ID of the job</param>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> MarkJobAsFailedAsync(string jobId, string errorMessage);

    /// <summary>
    /// Marks a job as cancelled
    /// </summary>
    /// <param name="jobId">ID of the job</param>
    /// <returns>Whether the update was successful</returns>
    Task<bool> MarkJobAsCancelledAsync(string jobId);

    /// <summary>
    /// Retries a failed job
    /// </summary>
    /// <param name="jobId">ID of the job to retry</param>
    /// <param name="nextRetryAt">When to retry the job</param>
    /// <returns>Whether the retry was scheduled</returns>
    Task<bool> RetryJobAsync(string jobId, DateTime nextRetryAt);

    /// <summary>
    /// Gets failed jobs that are ready for retry
    /// </summary>
    /// <returns>List of jobs ready for retry</returns>
    Task<IEnumerable<DataNormalizationJob>> GetJobsReadyForRetryAsync();

    /// <summary>
    /// Cleans up old completed jobs
    /// </summary>
    /// <param name="olderThan">Remove jobs older than this date</param>
    /// <returns>Number of jobs cleaned up</returns>
    Task<int> CleanupOldJobsAsync(DateTime olderThan);
}
