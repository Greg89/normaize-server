using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataSetsController : ControllerBase
{
    private readonly IDataProcessingService _dataProcessingService;
    private readonly ILogger<DataSetsController> _logger;

    public DataSetsController(IDataProcessingService dataProcessingService, ILogger<DataSetsController> logger)
    {
        _dataProcessingService = dataProcessingService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DataSetDto>>> GetDataSets()
    {
        try
        {
            var dataSets = await _dataProcessingService.GetAllDataSetsAsync();
            return Ok(dataSets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving datasets");
            return StatusCode(500, "Error retrieving datasets");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DataSetDto>> GetDataSet(int id)
    {
        try
        {
            var dataSet = await _dataProcessingService.GetDataSetAsync(id);
            if (dataSet == null)
                return NotFound();

            return Ok(dataSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dataset {Id}", id);
            return StatusCode(500, "Error retrieving dataset");
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<DataSetUploadResponse>> UploadDataSet([FromForm] IFormFile file, [FromForm] string name, [FromForm] string? description)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            var createDto = new CreateDataSetDto
            {
                Name = name,
                Description = description
            };

            // Convert IFormFile to FileUploadRequest (abstraction)
            var fileRequest = new FileUploadRequest
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileStream = file.OpenReadStream()
            };

            var result = await _dataProcessingService.UploadDataSetAsync(fileRequest, createDto);
            
            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading dataset");
            return StatusCode(500, "Error uploading dataset");
        }
    }

    [HttpGet("{id}/preview")]
    public async Task<ActionResult<string>> GetDataSetPreview(int id, [FromQuery] int rows = 10)
    {
        try
        {
            var preview = await _dataProcessingService.GetDataSetPreviewAsync(id, rows);
            if (preview == null)
                return NotFound();

            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dataset preview {Id}", id);
            return StatusCode(500, "Error retrieving dataset preview");
        }
    }

    [HttpGet("{id}/schema")]
    public async Task<ActionResult<object>> GetDataSetSchema(int id)
    {
        try
        {
            var schema = await _dataProcessingService.GetDataSetSchemaAsync(id);
            if (schema == null)
                return NotFound();

            return Ok(schema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dataset schema {Id}", id);
            return StatusCode(500, "Error retrieving dataset schema");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDataSet(int id)
    {
        try
        {
            var result = await _dataProcessingService.DeleteDataSetAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dataset {Id}", id);
            return StatusCode(500, "Error deleting dataset");
        }
    }
} 