using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Controller for job status endpoints
/// Provides backward compatibility route matching client expectations: /api/jobs/{jobId}/status
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : BaseApiController
{
    private readonly IQueryHandler<GetJobStatusQuery, JobStatusDto?> _getJobStatusHandler;
    private readonly ILogger<JobsController> _logger;

    public JobsController(
        IQueryHandler<GetJobStatusQuery, JobStatusDto?> getJobStatusHandler,
        ILogger<JobsController> logger)
    {
        _getJobStatusHandler = getJobStatusHandler ?? throw new ArgumentNullException(nameof(getJobStatusHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the status of a job
    /// Client expects: GET /api/jobs/{jobId}/status
    /// This endpoint provides backward compatibility with client expectations
    /// </summary>
    /// <param name="jobId">ID of the job to check (can be string GUID)</param>
    /// <returns>Current job status</returns>
    [HttpGet("{jobId}/status")]
    [ProducesResponseType(typeof(ApiResponse<JobStatusResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetJobStatus(string jobId)
    {
        try
        {
            // Parse jobId (support both string and Guid)
            if (!Guid.TryParse(jobId, out var guidJobId))
            {
                return Error("Invalid job ID format", "INVALID_JOB_ID", 400);
            }

            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting status for job {JobId}", userId, guidJobId);

            var query = new GetJobStatusQuery(guidJobId);
            var jobStatusDto = await _getJobStatusHandler.HandleAsync(query);

            if (jobStatusDto == null)
            {
                return Error($"Job with ID {jobId} not found", "JOB_NOT_FOUND", 404);
            }

            // Map from Application DTO to API DTO matching client expectations
            // Note: JSON serialization will convert Guid to string automatically
            var response = new JobStatusResponse
            {
                JobId = jobStatusDto.Id, // Guid - will serialize to string in JSON
                DataSetId = jobStatusDto.DataSetId, // Guid - will serialize to string in JSON
                JobType = jobStatusDto.OperationType,
                Status = jobStatusDto.Status,
                StatusMessage = jobStatusDto.ErrorMessage ?? jobStatusDto.ProgressMessage ?? "Job is processing",
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
        var isCompletedStatus = jobStatus.Status is "Completed" or "Succeeded";
        if (!isCompletedStatus || string.IsNullOrWhiteSpace(jobStatus.Result))
        {
            return null;
        }

        try
        {
            // Try to parse result JSON into meaningful statistics
            // Use JsonDocument to properly parse JsonElements
            using var document = System.Text.Json.JsonDocument.Parse(jobStatus.Result);
            var root = document.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                throw new InvalidOperationException("Result is not a JSON object");
            }

            var resultData = new Dictionary<string, object>();
            foreach (var property in root.EnumerateObject())
            {
                resultData[property.Name] = property.Value.Clone(); // Clone JsonElement
            }

            return new JobResultsResponse
            {
                ProcessedRows = GetIntValue(resultData, "processedRows") ?? GetIntValue(resultData, "ProcessedRows") ?? 0,
                RowsRemoved = GetIntValue(resultData, "rowsRemoved") ?? GetIntValue(resultData, "RowsRemoved") ?? 0,
                RowsModified = GetIntValue(resultData, "rowsModified") ?? GetIntValue(resultData, "RowsModified") ?? 0,
                ProcessingTime = TimeSpan.FromMilliseconds(GetIntValue(resultData, "processingTimeMs") ?? GetIntValue(resultData, "ProcessingTimeMs") ?? 0),
                Statistics = resultData,
                Warnings = GetStringList(resultData, "warnings") ?? GetStringList(resultData, "Warnings") ?? new List<string>()
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

    private static int? GetIntValue(Dictionary<string, object>? data, string key)
    {
        if (data?.TryGetValue(key, out var value) == true)
        {
            return value switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                double doubleValue => (int)doubleValue,
                string stringValue when int.TryParse(stringValue, out var parsed) => parsed,
                System.Text.Json.JsonElement jsonElement when jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number => jsonElement.GetInt32(),
                _ => null
            };
        }
        return null;
    }

    private static int GetIntValue(Dictionary<string, object>? data, string key, int defaultValue)
    {
        return GetIntValue(data, key) ?? defaultValue;
    }

    private static List<string>? GetStringList(Dictionary<string, object>? data, string key)
    {
        if (data?.TryGetValue(key, out var value) == true)
        {
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                try
                {
                    if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        return jsonElement.EnumerateArray()
                            .Select(item => item.GetString() ?? string.Empty)
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();
                    }
                }
                catch
                {
                    // Fall through to return null
                }
            }
            else if (value is List<string> stringList)
            {
                return stringList;
            }
        }
        return null;
    }
}

