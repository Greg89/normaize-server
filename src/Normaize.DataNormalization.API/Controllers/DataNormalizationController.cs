using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Controller for data normalization operations using clean DDD architecture
/// </summary>
[Route("api/normalization")]
[Authorize]
public class DataNormalizationController : BaseApiController
{
    private readonly ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid> _submitJobHandler;
    private readonly ICommandHandler<RetryJobCommand> _retryJobHandler;
    private readonly ICommandHandler<CancelJobCommand> _cancelJobHandler;
    private readonly IQueryHandler<GetJobStatusQuery, JobStatusDto?> _getJobStatusHandler;
    private readonly ILogger<DataNormalizationController> _logger;

    public DataNormalizationController(
        ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid> submitJobHandler,
        ICommandHandler<RetryJobCommand> retryJobHandler,
        ICommandHandler<CancelJobCommand> cancelJobHandler,
        IQueryHandler<GetJobStatusQuery, JobStatusDto?> getJobStatusHandler,
        ILogger<DataNormalizationController> logger)
    {
        _submitJobHandler = submitJobHandler ?? throw new ArgumentNullException(nameof(submitJobHandler));
        _retryJobHandler = retryJobHandler ?? throw new ArgumentNullException(nameof(retryJobHandler));
        _cancelJobHandler = cancelJobHandler ?? throw new ArgumentNullException(nameof(cancelJobHandler));
        _getJobStatusHandler = getJobStatusHandler ?? throw new ArgumentNullException(nameof(getJobStatusHandler));
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

            var query = new GetJobStatusQuery(jobId);
            var jobStatusDto = await _getJobStatusHandler.HandleAsync(query);

            if (jobStatusDto == null)
            {
                return NotFound($"Job with ID {jobId} not found");
            }

            // Map from Application DTO to API DTO
            var response = new JobStatusResponse
            {
                JobId = jobStatusDto.Id,
                DataSetId = jobStatusDto.DataSetId,
                JobType = jobStatusDto.OperationType,
                Status = jobStatusDto.Status,
                StatusMessage = jobStatusDto.ErrorMessage ?? jobStatusDto.ProgressMessage,
                ProgressPercentage = jobStatusDto.ProgressPercentage,
                SubmittedAt = jobStatusDto.CreatedAt,
                StartedAt = jobStatusDto.StartedAt,
                CompletedAt = jobStatusDto.CompletedAt,
                SubmittedBy = userId, // TODO: Store actual submitter in domain model
                Parameters = ParseParametersFromJson(jobStatusDto.OperationParameters),
                Results = CreateJobResults(jobStatusDto)
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
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("User {UserId} cancelling job {JobId}", userId, jobId);

            var command = new CancelJobCommand(jobId);
            await _cancelJobHandler.HandleAsync(command);

            _logger.LogInformation("Successfully cancelled job {JobId} for user {UserId}", jobId, userId);
            return Success(true, "Job cancelled successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Job {JobId} not found for cancellation by user {UserId}", jobId, userId);
            return NotFound($"Job with ID {jobId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
            return HandleException(ex, nameof(CancelJob));
        }
    }

    /// <summary>
    /// Retry a failed normalization job
    /// </summary>
    /// <param name="jobId">ID of the job to retry</param>
    /// <returns>Whether the retry was successful</returns>
    [HttpPost("jobs/{jobId:guid}/retry")]
    [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RetryJob(Guid jobId)
    {
        var userId = GetCurrentUserId();

        try
        {
            _logger.LogInformation("User {UserId} retrying job {JobId}", userId, jobId);

            var command = new RetryJobCommand(jobId);
            await _retryJobHandler.HandleAsync(command);

            _logger.LogInformation("Successfully scheduled retry for job {JobId} by user {UserId}", jobId, userId);
            return Success(true, "Job retry scheduled successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning("Job {JobId} not found for retry by user {UserId}", jobId, userId);
            return NotFound($"Job with ID {jobId} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying job {JobId}", jobId);
            return HandleException(ex, nameof(RetryJob));
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

    /// <summary>
    /// Parse operation parameters from JSON string
    /// </summary>
    private Dictionary<string, object> ParseParametersFromJson(string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            return new Dictionary<string, object>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson)
                   ?? new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse operation parameters: {Parameters}", parametersJson);
            return new Dictionary<string, object> { { "raw", parametersJson } };
        }
    }

    /// <summary>
    /// Create job results response from job status DTO
    /// </summary>
    private JobResultsResponse? CreateJobResults(JobStatusDto jobStatus)
    {
        // Only create results if job is completed and has result data
        if (jobStatus.Status != "Completed" || string.IsNullOrWhiteSpace(jobStatus.Result))
        {
            return null;
        }

        try
        {
            // Try to parse result JSON into meaningful statistics
            var resultData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jobStatus.Result);

            return new JobResultsResponse
            {
                ProcessedRows = GetIntValue(resultData, "processedRows", 0),
                RowsRemoved = GetIntValue(resultData, "rowsRemoved", 0),
                RowsModified = GetIntValue(resultData, "rowsModified", 0),
                ProcessingTime = TimeSpan.FromMilliseconds(GetIntValue(resultData, "processingTimeMs", 0)),
                Statistics = resultData ?? new Dictionary<string, object>(),
                Warnings = GetStringList(resultData, "warnings")
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse job result: {Result}", jobStatus.Result);
            return new JobResultsResponse
            {
                Statistics = new Dictionary<string, object> { { "raw", jobStatus.Result } }
            };
        }
    }

    private static int GetIntValue(Dictionary<string, object>? data, string key, int defaultValue)
    {
        if (data?.TryGetValue(key, out var value) == true)
        {
            return value switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                double doubleValue => (int)doubleValue,
                string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
                _ => defaultValue
            };
        }
        return defaultValue;
    }

    private static List<string> GetStringList(Dictionary<string, object>? data, string key)
    {
        if (data?.TryGetValue(key, out var value) == true && value is System.Text.Json.JsonElement jsonElement)
        {
            try
            {
                return jsonElement.EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
            catch
            {
                // Fall through to return empty list
            }
        }
        return new List<string>();
    }
}