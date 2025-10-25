using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Controller for data normalization operations using clean DDD architecture
/// </summary>
public class DataNormalizationController : BaseApiController
{
    private readonly ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid> _submitJobHandler;
    private readonly ILogger<DataNormalizationController> _logger;

    public DataNormalizationController(
        ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid> submitJobHandler,
        ILogger<DataNormalizationController> logger)
    {
        _submitJobHandler = submitJobHandler ?? throw new ArgumentNullException(nameof(submitJobHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Submit a duplicate removal job for a dataset
    /// </summary>
    /// <param name="request">Duplicate removal configuration</param>
    /// <returns>Job submission response</returns>
    [HttpPost("remove-duplicates")]
    [ProducesResponseType(typeof(ApiResponse<JobSubmissionResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RemoveDuplicates([FromBody] RemoveDuplicatesRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requesting duplicate removal for dataset {DataSetId}", 
                userId, request.DataSetId);

            // Map to domain value object
            var caseSensitivity = request.CaseSensitive 
                ? CaseSensitivity.Sensitive 
                : CaseSensitivity.Insensitive;

            var options = request.Strategy.ToLower() switch
            {
                "keeplast" => DuplicateRemovalOptions.KeepLast(request.ComparisonColumns, caseSensitivity),
                _ => DuplicateRemovalOptions.KeepFirst(request.ComparisonColumns, caseSensitivity)
            };

            // Create command with domain value object
            var command = new SubmitDuplicateRemovalJobCommand(request.DataSetId, options);
            var jobId = await _submitJobHandler.HandleAsync(command);

            var response = new JobSubmissionResponse
            {
                JobId = jobId,
                Status = "Submitted",
                Message = "Duplicate removal job submitted successfully",
                SubmittedAt = DateTime.UtcNow
            };

            return Success(response, "Duplicate removal job submitted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting duplicate removal job for dataset {DataSetId}", 
                request.DataSetId);
            return HandleException(ex, nameof(RemoveDuplicates));
        }
    }

    /// <summary>
    /// Submit a generic normalization job
    /// </summary>
    /// <param name="request">Job submission request</param>
    /// <returns>Job submission response</returns>
    [HttpPost("submit-job")]
    [ProducesResponseType(typeof(ApiResponse<JobSubmissionResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SubmitJob([FromBody] SubmitJobRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} submitting {JobType} job for dataset {DataSetId}", 
                userId, request.JobType, request.DataSetId);

            // For now, only support duplicate removal jobs
            if (request.JobType != "RemoveDuplicates")
            {
                return Error($"Job type '{request.JobType}' is not supported. Only 'RemoveDuplicates' is currently supported.", "UNSUPPORTED_JOB_TYPE", 400);
            }

            // Create basic duplicate removal options for generic job submission
            var options = DuplicateRemovalOptions.KeepFirst(new List<string>(), CaseSensitivity.Insensitive);
            var command = new SubmitDuplicateRemovalJobCommand(request.DataSetId, options);
            var jobId = await _submitJobHandler.HandleAsync(command);

            var response = new JobSubmissionResponse
            {
                JobId = jobId,
                Status = "Submitted",
                Message = "Job submitted successfully",
                SubmittedAt = DateTime.UtcNow
            };

            return Success(response, "Job submitted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting {JobType} job for dataset {DataSetId}", 
                request.JobType, request.DataSetId);
            return HandleException(ex, nameof(SubmitJob));
        }
    }

    /// <summary>
    /// Get the status of a normalization job
    /// </summary>
    /// <param name="jobId">ID of the job to check</param>
    /// <returns>Current job status</returns>
    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetJobStatus(Guid jobId)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting status for job {JobId}", userId, jobId);

            // TODO: Implement GetJobStatusQuery when available
            var response = new JobStatusResponse
            {
                JobId = jobId,
                DataSetId = Guid.NewGuid(),
                JobType = "Unknown",
                Status = "NotImplemented",
                StatusMessage = "Job status retrieval not yet implemented",
                ProgressPercentage = 0,
                SubmittedAt = DateTime.UtcNow,
                SubmittedBy = userId,
                Parameters = new Dictionary<string, object>()
            };

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for job {JobId}", jobId);
            return HandleException(ex, nameof(GetJobStatus));
        }
    }

    /// <summary>
    /// Cancel a normalization job
    /// </summary>
    /// <param name="jobId">ID of the job to cancel</param>
    /// <param name="request">Cancellation request</param>
    /// <returns>Whether the cancellation was successful</returns>
    [HttpPost("jobs/{jobId:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CancelJob(Guid jobId, [FromBody] CancelJobRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} cancelling job {JobId}", userId, jobId);

            // TODO: Implement CancelJobCommand when needed
            return Error("Job cancellation is not yet implemented", "NOT_IMPLEMENTED", 501);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
            return HandleException(ex, nameof(CancelJob));
        }
    }

    /// <summary>
    /// Get all normalization jobs for the current user
    /// </summary>
    /// <param name="filter">Job filtering parameters</param>
    /// <returns>Paginated list of jobs</returns>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(PaginatedApiResponse<JobListResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetUserJobs([FromQuery] JobFilterRequest filter)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting job list", userId);

            // TODO: Implement GetUserJobsQuery when needed
            var response = new JobListResponse
            {
                Jobs = new List<JobStatusResponse>(),
                TotalJobs = 0
            };

            return SuccessPaginated(response, filter.Page, filter.PageSize, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for user");
            return HandleException(ex, nameof(GetUserJobs));
        }
    }

    /// <summary>
    /// Get all normalization jobs for a specific dataset
    /// </summary>
    /// <param name="dataSetId">ID of the dataset</param>
    /// <param name="filter">Job filtering parameters</param>
    /// <returns>List of jobs for the dataset</returns>
    [HttpGet("datasets/{dataSetId:guid}/jobs")]
    [ProducesResponseType(typeof(ApiResponse<JobListResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDataSetJobs(Guid dataSetId, [FromQuery] JobFilterRequest filter)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting jobs for dataset {DataSetId}", userId, dataSetId);

            // TODO: Implement GetDataSetJobsQuery when needed
            var response = new JobListResponse
            {
                Jobs = new List<JobStatusResponse>(),
                TotalJobs = 0
            };

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for dataset {DataSetId}", dataSetId);
            return HandleException(ex, nameof(GetDataSetJobs));
        }
    }
}