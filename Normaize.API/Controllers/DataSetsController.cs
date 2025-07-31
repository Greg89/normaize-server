using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Extensions;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataSetsController(IDataProcessingService dataProcessingService, IStructuredLoggingService loggingService)
    : BaseApiController(loggingService)
{
    private readonly IDataProcessingService _dataProcessingService = dataProcessingService;

    private string GetCurrentUserId()
    {
        return User.GetUserId();
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<DataSetDto>>>> GetDataSets()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.GetDataSetsByUserAsync(userId);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, "GetDataSets");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DataSetDto>>> GetDataSet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSet = await _dataProcessingService.GetDataSetAsync(id, userId);
            if (dataSet == null)
                return NotFound<DataSetDto>();

            return Success(dataSet);
        }
        catch (Exception ex)
        {
            return HandleException<DataSetDto>(ex, $"GetDataSet({id})");
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<DataSetUploadResponse>>> UploadDataSet([FromForm] FileUploadDto uploadDto)
    {
        try
        {
            if (uploadDto.File == null || uploadDto.File.Length == 0)
                return BadRequest<DataSetUploadResponse>("No file provided");

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
                return BadRequest<DataSetUploadResponse>(result.Message);

            return Success(result, "Dataset uploaded successfully");
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService?.LogException(ex, "UploadDataSet");
            return StatusCode(500, "Error uploading dataset");
        }
    }

    [HttpGet("{id}/preview")]
    public async Task<ActionResult<ApiResponse<string>>> GetDataSetPreview(int id, [FromQuery] int rows = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var preview = await _dataProcessingService.GetDataSetPreviewAsync(id, rows, userId);
            if (preview == null)
                return NotFound<string>();

            return Success<string>(preview ?? string.Empty);
        }
        catch (Exception ex)
        {
            return HandleException<string>(ex, $"GetDataSetPreview({id})");
        }
    }

    [HttpGet("{id}/schema")]
    public async Task<ActionResult<ApiResponse<object>>> GetDataSetSchema(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var schema = await _dataProcessingService.GetDataSetSchemaAsync(id, userId);
            if (schema == null)
                return NotFound<object>();

            return Success(schema);
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, $"GetDataSetSchema({id})");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string?>>> DeleteDataSet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _dataProcessingService.DeleteDataSetAsync(id, userId);
            if (!result)
                return NotFound<string?>();

            return Success<string?>(null, "Dataset deleted successfully");
        }
        catch (Exception ex)
        {
            return HandleException<string?>(ex, $"DeleteDataSet({id})");
        }
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult<ApiResponse<string?>>> RestoreDataSet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _dataProcessingService.RestoreDataSetAsync(id, userId);
            if (!result)
                return NotFound<string?>();

            return Success<string?>(null, "Dataset restored successfully");
        }
        catch (Exception ex)
        {
            return HandleException<string?>(ex, $"RestoreDataSet({id})");
        }
    }

    [HttpDelete("{id}/permanent")]
    public async Task<ActionResult<ApiResponse<string?>>> HardDeleteDataSet(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _dataProcessingService.HardDeleteDataSetAsync(id, userId);
            if (!result)
                return NotFound<string?>();

            return Success<string?>(null, "Dataset permanently deleted");
        }
        catch (Exception ex)
        {
            return HandleException<string?>(ex, $"HardDeleteDataSet({id})");
        }
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<ApiResponse<List<DataSetDto>>>> GetDeletedDataSets()
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.GetDeletedDataSetsAsync(userId);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, "GetDeletedDataSets");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<DataSetDto>>>> SearchDataSets(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest<List<DataSetDto>>("Search query is required");

            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.SearchDataSetsAsync(q, userId, page, pageSize);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, $"SearchDataSets({q})");
        }
    }

    [HttpGet("filetype/{fileType}")]
    public async Task<ActionResult<ApiResponse<List<DataSetDto>>>> GetDataSetsByFileType(
        FileType fileType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.GetDataSetsByFileTypeAsync(fileType, userId, page, pageSize);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, $"GetDataSetsByFileType({fileType})");
        }
    }

    [HttpGet("date-range")]
    public async Task<ActionResult<ApiResponse<List<DataSetDto>>>> GetDataSetsByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataProcessingService.GetDataSetsByDateRangeAsync(startDate, endDate, userId, page, pageSize);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, $"GetDataSetsByDateRange({startDate}, {endDate})");
        }
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<DataSetStatisticsDto>>> GetDataSetStatistics()
    {
        try
        {
            var userId = GetCurrentUserId();
            var statistics = await _dataProcessingService.GetDataSetStatisticsAsync(userId);
            return Success(statistics);
        }
        catch (Exception ex)
        {
            return HandleException<DataSetStatisticsDto>(ex, "GetDataSetStatistics");
        }
    }

}