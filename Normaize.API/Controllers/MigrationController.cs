using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;

namespace Normaize.API.Controllers;

/// <summary>
/// Controller for handling data migration operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MigrationController(
    IDataMigrationService dataMigrationService,
    IStructuredLoggingService structuredLogging) : ControllerBase
{
    private readonly IDataMigrationService _dataMigrationService = dataMigrationService;
    private readonly IStructuredLoggingService _structuredLogging = structuredLogging;

    /// <summary>
    /// Standardizes the PreviewData format for all datasets
    /// </summary>
    /// <returns>Number of datasets that were standardized</returns>
    [HttpPost("standardize-preview-data")]
    public async Task<ActionResult<ApiResponse<int>>> StandardizePreviewDataFormat()
    {
        try
        {
            var standardizedCount = await _dataMigrationService.StandardizePreviewDataFormatAsync();

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = standardizedCount,
                Message = $"Successfully standardized PreviewData format for {standardizedCount} datasets"
            });
        }
        catch (Exception ex)
        {
            _structuredLogging.LogException(ex, "Failed to standardize PreviewData format");

            return StatusCode(500, ApiResponse<int>.ErrorResponse("Failed to standardize PreviewData format", "MIGRATION_FAILED"));
        }
    }
}