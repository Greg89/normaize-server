using System.Text.Json;
using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;

namespace Normaize.Core.Services.DataNormalization;

/// <summary>
/// Main service for data normalization operations
/// </summary>
public class DataNormalizationService : IDataNormalizationService
{
    private readonly IJobQueueService _jobQueueService;
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IDuplicateRowRemovalProcessor _duplicateRowRemovalProcessor;
    private readonly ILogger<DataNormalizationService> _logger;

    public DataNormalizationService(
        IJobQueueService jobQueueService,
        IDataSetRepository dataSetRepository,
        IDuplicateRowRemovalProcessor duplicateRowRemovalProcessor,
        ILogger<DataNormalizationService> logger)
    {
        ArgumentNullException.ThrowIfNull(jobQueueService);
        ArgumentNullException.ThrowIfNull(dataSetRepository);
        ArgumentNullException.ThrowIfNull(duplicateRowRemovalProcessor);
        ArgumentNullException.ThrowIfNull(logger);

        _jobQueueService = jobQueueService;
        _dataSetRepository = dataSetRepository;
        _duplicateRowRemovalProcessor = duplicateRowRemovalProcessor;
        _logger = logger;
    }

    public async Task<NormalizationJobResponse> SubmitDuplicateRowRemovalJobAsync(
        int dataSetId,
        RemoveDuplicateRowsRequest request,
        string userId,
        string? correlationId = null)
    {
        try
        {
            _logger.LogInformation("Submitting duplicate row removal job for dataset {DataSetId} by user {UserId}", dataSetId, userId);

            // Validate dataset exists and user has access
            var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
            if (dataSet == null)
            {
                throw new InvalidOperationException(DataNormalizationConstants.DataNormalization.DATASET_NOT_FOUND);
            }

            if (dataSet.UserId != userId)
            {
                throw new UnauthorizedAccessException(DataNormalizationConstants.DataNormalization.ACCESS_DENIED);
            }

            // Validate the request
            var validationResult = await _duplicateRowRemovalProcessor.ValidateRequestAsync(dataSet, request);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.ErrorMessage);
            }

            // Create the normalization job
            var job = new DataNormalizationJob
            {
                Id = Guid.NewGuid().ToString(),
                DataSetId = dataSetId,
                UserId = userId,
                OperationType = DataNormalizationConstants.DataNormalization.REMOVE_DUPLICATE_ROWS,
                OperationParameters = JsonSerializer.Serialize(request),
                Status = NormalizationJobStatus.Queued,
                Priority = 1,
                SubmittedAt = DateTime.UtcNow,
                CorrelationId = correlationId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedBy = userId
            };

            // Enqueue the job
            var enqueued = await _jobQueueService.EnqueueJobAsync(job);
            if (!enqueued)
            {
                throw new InvalidOperationException("Failed to enqueue normalization job");
            }

            // Estimate processing time and memory usage
            var estimatedTime = await _duplicateRowRemovalProcessor.EstimateProcessingTimeAsync(dataSet, request);
            var estimatedMemory = await _duplicateRowRemovalProcessor.EstimateMemoryUsageAsync(dataSet, request);

            var response = new NormalizationJobResponse
            {
                JobId = job.Id,
                Status = NormalizationJobStatus.Queued,
                Message = DataNormalizationConstants.DataNormalization.JOB_QUEUED,
                SubmittedAt = job.SubmittedAt,
                EstimatedCompletionAt = DateTime.UtcNow.AddMilliseconds(estimatedTime),
                ProgressPercentage = 0,
                Success = true
            };

            _logger.LogInformation("Duplicate row removal job {JobId} submitted successfully for dataset {DataSetId}. Estimated time: {EstimatedTime}ms, Memory: {EstimatedMemory:F2}MB",
                job.Id, dataSetId, estimatedTime, estimatedMemory);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit duplicate row removal job for dataset {DataSetId}", dataSetId);
            throw new InvalidOperationException($"Failed to submit duplicate row removal job for dataset {dataSetId}", ex);
        }
    }

    public async Task<NormalizationJobStatusResponse> GetJobStatusAsync(string jobId, string userId)
    {
        try
        {
            var job = await FindJobByIdAsync(jobId);

            if (job == null)
            {
                throw new InvalidOperationException("Job not found");
            }

            // Verify user has access to this job
            if (job.UserId != userId)
            {
                throw new UnauthorizedAccessException(DataNormalizationConstants.DataNormalization.ACCESS_DENIED);
            }

            var response = new NormalizationJobStatusResponse
            {
                JobId = job.Id,
                Status = job.Status,
                Message = GetStatusMessage(job.Status),
                SubmittedAt = job.SubmittedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ProgressPercentage = job.ProgressPercentage,
                ErrorMessage = job.ErrorMessage
            };

            // Parse results if available
            if (!string.IsNullOrEmpty(job.Results))
            {
                try
                {
                    var results = JsonSerializer.Deserialize<NormalizationResults>(job.Results);
                    response.Results = results;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize results for job {JobId}", jobId);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for job {JobId}", jobId);
            throw new InvalidOperationException($"Failed to get status for job {jobId}", ex);
        }
    }

    private async Task<DataNormalizationJob?> FindJobByIdAsync(string jobId)
    {
        var statuses = new[]
        {
            NormalizationJobStatus.Queued,
            NormalizationJobStatus.Processing,
            NormalizationJobStatus.Completed,
            NormalizationJobStatus.Failed,
            NormalizationJobStatus.Cancelled
        };

        foreach (var status in statuses)
        {
            var jobs = await _jobQueueService.GetJobsByPriorityAsync(status, int.MaxValue, 1000);
            var job = jobs.FirstOrDefault(j => j.Id == jobId);
            if (job != null)
            {
                return job;
            }
        }

        return null;
    }

    public async Task<bool> CancelJobAsync(string jobId, string userId)
    {
        try
        {
            // Get job to verify ownership
            var jobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Queued, int.MaxValue, 1000);
            var job = jobs.FirstOrDefault(j => j.Id == jobId);

            if (job == null)
            {
                var processingJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Processing, int.MaxValue, 1000);
                job = processingJobs.FirstOrDefault(j => j.Id == jobId);
            }

            if (job == null)
            {
                throw new InvalidOperationException("Job not found or cannot be cancelled");
            }

            // Verify user has access to this job
            if (job.UserId != userId)
            {
                throw new UnauthorizedAccessException(DataNormalizationConstants.DataNormalization.ACCESS_DENIED);
            }

            // Only allow cancellation of queued or processing jobs
            if (job.Status != NormalizationJobStatus.Queued && job.Status != NormalizationJobStatus.Processing)
            {
                throw new InvalidOperationException("Job cannot be cancelled in its current state");
            }

            var cancelled = await _jobQueueService.MarkJobAsCancelledAsync(jobId);
            if (cancelled)
            {
                _logger.LogInformation("Job {JobId} cancelled by user {UserId}", jobId, userId);
            }

            return cancelled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
            throw new InvalidOperationException($"Failed to cancel job {jobId}", ex);
        }
    }

    public async Task<IEnumerable<NormalizationJobStatusResponse>> GetUserJobsAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        bool includeCompleted = false)
    {
        try
        {
            var allJobs = new List<DataNormalizationJob>();

            // Get queued jobs
            var queuedJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Queued, int.MaxValue, 1000);
            allJobs.AddRange(queuedJobs.Where(j => j.UserId == userId));

            // Get processing jobs
            var processingJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Processing, int.MaxValue, 1000);
            allJobs.AddRange(processingJobs.Where(j => j.UserId == userId));

            if (includeCompleted)
            {
                // Get completed jobs
                var completedJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Completed, int.MaxValue, 1000);
                allJobs.AddRange(completedJobs.Where(j => j.UserId == userId));

                // Get failed jobs
                var failedJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Failed, int.MaxValue, 1000);
                allJobs.AddRange(failedJobs.Where(j => j.UserId == userId));

                // Get cancelled jobs
                var cancelledJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Cancelled, int.MaxValue, 1000);
                allJobs.AddRange(cancelledJobs.Where(j => j.UserId == userId));
            }

            // Apply pagination
            var paginatedJobs = allJobs
                .OrderByDescending(j => j.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return paginatedJobs.Select(job => new NormalizationJobStatusResponse
            {
                JobId = job.Id,
                Status = job.Status,
                Message = GetStatusMessage(job.Status),
                SubmittedAt = job.SubmittedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                ProgressPercentage = job.ProgressPercentage,
                ErrorMessage = job.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get jobs for user {UserId}", userId);
            throw new InvalidOperationException($"Failed to get jobs for user {userId}", ex);
        }
    }

    public async Task<IEnumerable<NormalizationJobStatusResponse>> GetDataSetJobsAsync(int dataSetId, string userId)
    {
        try
        {
            // Verify user has access to the dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
            if (dataSet == null)
            {
                throw new InvalidOperationException(DataNormalizationConstants.DataNormalization.DATASET_NOT_FOUND);
            }

            if (dataSet.UserId != userId)
            {
                throw new UnauthorizedAccessException(DataNormalizationConstants.DataNormalization.ACCESS_DENIED);
            }

            var allJobs = new List<DataNormalizationJob>();

            // Get all jobs for this dataset
            var queuedJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Queued, int.MaxValue, 1000);
            allJobs.AddRange(queuedJobs.Where(j => j.DataSetId == dataSetId));

            var processingJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Processing, int.MaxValue, 1000);
            allJobs.AddRange(processingJobs.Where(j => j.DataSetId == dataSetId));

            var completedJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Completed, int.MaxValue, 1000);
            allJobs.AddRange(completedJobs.Where(j => j.DataSetId == dataSetId));

            var failedJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Failed, int.MaxValue, 1000);
            allJobs.AddRange(failedJobs.Where(j => j.DataSetId == dataSetId));

            var cancelledJobs = await _jobQueueService.GetJobsByPriorityAsync(NormalizationJobStatus.Cancelled, int.MaxValue, 1000);
            allJobs.AddRange(cancelledJobs.Where(j => j.DataSetId == dataSetId));

            return allJobs
                .OrderByDescending(j => j.SubmittedAt)
                .Select(job => new NormalizationJobStatusResponse
                {
                    JobId = job.Id,
                    Status = job.Status,
                    Message = GetStatusMessage(job.Status),
                    SubmittedAt = job.SubmittedAt,
                    StartedAt = job.StartedAt,
                    CompletedAt = job.CompletedAt,
                    ProgressPercentage = job.ProgressPercentage,
                    ErrorMessage = job.ErrorMessage
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get jobs for dataset {DataSetId}", dataSetId);
            throw new InvalidOperationException($"Failed to get jobs for dataset {dataSetId}", ex);
        }
    }

    private static string GetStatusMessage(NormalizationJobStatus status)
    {
        return status switch
        {
            NormalizationJobStatus.Queued => DataNormalizationConstants.DataNormalization.JOB_QUEUED,
            NormalizationJobStatus.Processing => DataNormalizationConstants.DataNormalization.JOB_STARTED_PROCESSING,
            NormalizationJobStatus.Completed => DataNormalizationConstants.DataNormalization.JOB_COMPLETED_SUCCESSFULLY,
            NormalizationJobStatus.Failed => DataNormalizationConstants.DataNormalization.JOB_FAILED,
            NormalizationJobStatus.Cancelled => DataNormalizationConstants.DataNormalization.JOB_CANCELLED,
            _ => "Unknown status"
        };
    }
}
