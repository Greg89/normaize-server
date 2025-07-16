using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Constants;
using System.Security.Claims;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataSetsController : ControllerBase
{
    private readonly IDataProcessingService _dataProcessingService;
    private readonly IStructuredLoggingService _loggingService;

    public DataSetsController(IDataProcessingService dataProcessingService, IStructuredLoggingService loggingService)
    {
        _dataProcessingService = dataProcessingService;
        _loggingService = loggingService;
    }

    private string GetCurrentUserId()
    {
        // Get user ID from JWT token (Auth0 sub claim)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                    ?? User.FindFirst("sub")?.Value 
                    ?? throw new UnauthorizedAccessException("User ID not found in token");
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DataSetDto>>> GetDataSets()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.GetDataSetsByUserAsync(userId);
            return Ok(dataSets);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetDataSets");
            return StatusCode(500, "Error retrieving datasets");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DataSetDto>> GetDataSet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSet = await _dataProcessingService.GetDataSetAsync(id, userId);
            if (dataSet == null)
                return NotFound();

            return Ok(dataSet);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetDataSet({id})");
            return StatusCode(500, "Error retrieving dataset");
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<DataSetUploadResponse>> UploadDataSet([FromForm] FileUploadDto uploadDto)
    {
        try
        {
            if (uploadDto.File == null || uploadDto.File.Length == 0)
                return BadRequest("No file provided");

            var userId = GetCurrentUserId();
            var createDto = new CreateDataSetDto
            {
                Name = uploadDto.Name,
                Description = uploadDto.Description,
                UserId = userId
            };

            // Convert IFormFile to FileUploadRequest (abstraction)
            var fileRequest = new FileUploadRequest
            {
                FileName = uploadDto.File.FileName,
                ContentType = uploadDto.File.ContentType,
                FileSize = uploadDto.File.Length,
                FileStream = uploadDto.File.OpenReadStream()
            };

            var result = await _dataProcessingService.UploadDataSetAsync(fileRequest, createDto);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "UploadDataSet");
            return StatusCode(500, "Error uploading dataset");
        }
    }

    [HttpGet("{id}/preview")]
    public async Task<ActionResult<string>> GetDataSetPreview(int id, [FromQuery] int rows = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var preview = await _dataProcessingService.GetDataSetPreviewAsync(id, rows, userId);
            if (preview == null)
                return NotFound();

            return Ok(preview);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetDataSetPreview({id})");
            return StatusCode(500, "Error retrieving dataset preview");
        }
    }

    [HttpGet("{id}/schema")]
    public async Task<ActionResult<object>> GetDataSetSchema(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var schema = await _dataProcessingService.GetDataSetSchemaAsync(id, userId);
            if (schema == null)
                return NotFound();

            return Ok(schema);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetDataSetSchema({id})");
            return StatusCode(500, "Error retrieving dataset schema");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDataSet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _dataProcessingService.DeleteDataSetAsync(id, userId);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"DeleteDataSet({id})");
            return StatusCode(500, "Error deleting dataset");
        }
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult> RestoreDataSet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _dataProcessingService.RestoreDataSetAsync(id, userId);
            if (!result)
                return NotFound();

            return Ok(new { message = "Dataset restored successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"RestoreDataSet({id})");
            return StatusCode(500, "Error restoring dataset");
        }
    }

    [HttpDelete("{id}/permanent")]
    public async Task<ActionResult> HardDeleteDataSet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _dataProcessingService.HardDeleteDataSetAsync(id, userId);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"HardDeleteDataSet({id})");
            return StatusCode(500, "Error permanently deleting dataset");
        }
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<IEnumerable<DataSetDto>>> GetDeletedDataSets()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.GetDeletedDataSetsAsync(userId);
            return Ok(dataSets);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetDeletedDataSets");
            return StatusCode(500, "Error retrieving deleted datasets");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<DataSetDto>>> SearchDataSets([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Search query is required");

            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.SearchDataSetsAsync(q, userId);
            return Ok(dataSets);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"SearchDataSets({q})");
            return StatusCode(500, "Error searching datasets");
        }
    }

    [HttpGet("filetype/{fileType}")]
    public async Task<ActionResult<IEnumerable<DataSetDto>>> GetDataSetsByFileType(string fileType)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.GetDataSetsByFileTypeAsync(fileType, userId);
            return Ok(dataSets);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetDataSetsByFileType({fileType})");
            return StatusCode(500, "Error retrieving datasets by file type");
        }
    }

    [HttpGet("date-range")]
    public async Task<ActionResult<IEnumerable<DataSetDto>>> GetDataSetsByDateRange(
        [FromQuery] DateTime startDate, 
        [FromQuery] DateTime endDate)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.GetDataSetsByDateRangeAsync(startDate, endDate, userId);
            return Ok(dataSets);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetDataSetsByDateRange({startDate}, {endDate})");
            return StatusCode(500, "Error retrieving datasets by date range");
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<DataSetStatisticsDto>> GetDataSetStatistics()
    {
        try
        {
            var userId = GetCurrentUserId();
            var statistics = await _dataProcessingService.GetDataSetStatisticsAsync(userId);
            return Ok(statistics);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetDataSetStatistics");
            return StatusCode(500, "Error retrieving dataset statistics");
        }
    }

} 