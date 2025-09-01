using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using System.Security.Claims;

namespace Normaize.API.Controllers;

/// <summary>
/// Controller for data normalization operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataNormalizationController : BaseApiController
{
    private readonly IDataNormalizationService _normalizationService;
    private readonly ILogger<DataNormalizationController> _logger;

    public DataNormalizationController(
        IDataNormalizationService normalizationService,
        ILogger<DataNormalizationController> logger)
    {
        ArgumentNullException.ThrowIfNull(normalizationService);
        ArgumentNullException.ThrowIfNull(logger);

        _normalizationService = normalizationService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a duplicate row removal job for a dataset
    /// </summary>
    /// <param name="dataSetId">ID of the dataset to normalize</param>
    /// <param name="request">Duplicate removal configuration</param>
    /// <returns>Job submission response</returns>
    [HttpPost("datasets/{dataSetId}/remove-duplicates")]
    public async Task<ActionResult<NormalizationJobResponse>> RemoveDuplicateRows(
        int dataSetId,
        [FromBody] RemoveDuplicateRowsRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("User {UserId} requesting duplicate row removal for dataset {DataSetId}", userId, dataSetId);

            var response = await _normalizationService.SubmitDuplicateRowRemovalJobAsync(
                dataSetId,
                request,
                userId,
                GetCorrelationId());

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting duplicate row removal job for dataset {DataSetId}", dataSetId);
            return StatusCode(500, new { error = "An error occurred while submitting the job" });
        }
    }

    /// <summary>
    /// Get the status of a normalization job
    /// </summary>
    /// <param name="jobId">ID of the job to check</param>
    /// <returns>Current job status</returns>
    [HttpGet("jobs/{jobId}")]
    public async Task<ActionResult<NormalizationJobStatusResponse>> GetJobStatus(string jobId)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var response = await _normalizationService.GetJobStatusAsync(jobId, userId);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for job {JobId}", jobId);
            return StatusCode(500, new { error = "An error occurred while getting job status" });
        }
    }

    /// <summary>
    /// Cancel a normalization job
    /// </summary>
    /// <param name="jobId">ID of the job to cancel</param>
    /// <returns>Whether the cancellation was successful</returns>
    [HttpPost("jobs/{jobId}/cancel")]
    public async Task<ActionResult<bool>> CancelJob(string jobId)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var cancelled = await _normalizationService.CancelJobAsync(jobId, userId);
            return Ok(cancelled);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
            return StatusCode(500, new { error = "An error occurred while cancelling the job" });
        }
    }

    /// <summary>
    /// Get all normalization jobs for the current user
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="includeCompleted">Whether to include completed jobs (default: false)</param>
    /// <returns>Paginated list of jobs</returns>
    [HttpGet("jobs")]
    public async Task<ActionResult<IEnumerable<NormalizationJobStatusResponse>>> GetUserJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeCompleted = false)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Validate pagination parameters
            if (page < 1)
            {
                return BadRequest(new { error = "Page must be greater than 0" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { error = "Page size must be between 1 and 100" });
            }

            var jobs = await _normalizationService.GetUserJobsAsync(userId, page, pageSize, includeCompleted);
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for user");
            return StatusCode(500, new { error = "An error occurred while getting jobs" });
        }
    }

    /// <summary>
    /// Get all normalization jobs for a specific dataset
    /// </summary>
    /// <param name="dataSetId">ID of the dataset</param>
    /// <returns>List of jobs for the dataset</returns>
    [HttpGet("datasets/{dataSetId}/jobs")]
    public async Task<ActionResult<IEnumerable<NormalizationJobStatusResponse>>> GetDataSetJobs(int dataSetId)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var jobs = await _normalizationService.GetDataSetJobsAsync(dataSetId, userId);
            return Ok(jobs);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting jobs for dataset {DataSetId}", dataSetId);
            return StatusCode(500, new { error = "An error occurred while getting dataset jobs" });
        }
    }

    private string? GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetCorrelationId([FromHeader(Name = "X-Correlation-ID")] string? correlationId = null)
    {
        return correlationId ?? HttpContext.TraceIdentifier;
    }
}
