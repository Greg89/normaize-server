using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Extensions;

namespace Normaize.API.Controllers;

/// <summary>
/// Controller for managing datasets and file upload operations
/// </summary>
/// <remarks>
/// This controller provides comprehensive dataset management functionality including CRUD operations,
/// file uploads, dataset previews, schema analysis, search capabilities, and statistics. All endpoints
/// require authentication and support user-specific dataset isolation. The controller handles various
/// file types (CSV, Excel, JSON) and provides both soft delete and hard delete capabilities.
/// 
/// Key features:
/// - Dataset CRUD operations with user isolation
/// - File upload with validation and processing
/// - Dataset preview and schema analysis
/// - Search and filtering capabilities
/// - Soft delete and restore functionality
/// - Dataset statistics and analytics
/// - Pagination support for large datasets
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DataSetsController(IDataProcessingService dataProcessingService, IStructuredLoggingService loggingService)
    : BaseApiController(loggingService)
{
    private readonly IDataProcessingService _dataProcessingService = dataProcessingService;

    /// <summary>
    /// Gets the current user ID from the authenticated user claims
    /// </summary>
    /// <returns>The user ID as a string</returns>
    private string GetCurrentUserId()
    {
        return User.GetUserId();
    }

    /// <summary>
    /// Retrieves all datasets for the authenticated user
    /// </summary>
    /// <returns>
    /// A list of datasets owned by the current user with pagination support
    /// </returns>
    /// <remarks>
    /// This endpoint returns all datasets that belong to the authenticated user.
    /// The response includes dataset metadata, file information, and processing status.
    /// Results are automatically paginated for performance with large datasets.
    /// </remarks>
    /// <response code="200">Datasets retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during dataset retrieval</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DataSetDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Retrieves a specific dataset by ID for the authenticated user
    /// </summary>
    /// <param name="id">The unique identifier of the dataset</param>
    /// <returns>
    /// The requested dataset if found and owned by the current user
    /// </returns>
    /// <remarks>
    /// This endpoint retrieves a specific dataset by its ID. The dataset must belong
    /// to the authenticated user. If the dataset is not found or doesn't belong to
    /// the user, a 404 Not Found response is returned.
    /// </remarks>
    /// <response code="200">Dataset retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during dataset retrieval</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DataSetDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Updates a dataset's name and description for the authenticated user
    /// </summary>
    /// <param name="id">The unique identifier of the dataset</param>
    /// <param name="updateDto">The update data containing name and description</param>
    /// <returns>
    /// The updated dataset if found and owned by the current user
    /// </returns>
    /// <remarks>
    /// This endpoint allows updating the name and description of an existing dataset.
    /// The dataset must belong to the authenticated user. Only the name and description
    /// fields can be updated to maintain data integrity. All changes are logged in the
    /// audit trail for compliance and tracking purposes.
    /// 
    /// Update capabilities:
    /// - Dataset name (required, max 255 characters)
    /// - Dataset description (optional, max 1000 characters)
    /// - Automatic audit trail logging
    /// - User access control validation
    /// </remarks>
    /// <response code="200">Dataset updated successfully</response>
    /// <response code="400">Invalid update data provided</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during update</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DataSetDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<DataSetDto>>> UpdateDataSet(int id, [FromBody] UpdateDataSetDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSet = await _dataProcessingService.UpdateDataSetAsync(id, updateDto, userId);
            if (dataSet == null)
                return NotFound<DataSetDto>();

            return Success(dataSet, "Dataset updated successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            _loggingService?.LogException(ex, $"UpdateDataSet({id}) - Unauthorized");
            return Unauthorized<DataSetDto>("Authentication failed");
        }
        catch (Exception ex)
        {
            return HandleException<DataSetDto>(ex, $"UpdateDataSet({id})");
        }
    }

    /// <summary>
    /// Uploads a new dataset file with metadata
    /// </summary>
    /// <param name="uploadDto">The file upload data transfer object containing file and metadata</param>
    /// <returns>
    /// Upload response with dataset ID and processing status
    /// </returns>
    /// <remarks>
    /// This endpoint handles file uploads for new datasets. The uploaded file is validated,
    /// processed, and stored according to the configured storage provider. The endpoint
    /// supports various file formats including CSV, Excel, and JSON files.
    /// 
    /// The upload process includes:
    /// - File validation (size, format, content)
    /// - Metadata extraction and validation
    /// - File storage and processing
    /// - Dataset record creation
    /// - Initial schema analysis
    /// </remarks>
    /// <response code="200">Dataset uploaded and processed successfully</response>
    /// <response code="400">Invalid file or metadata provided</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during upload processing</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<DataSetUploadResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Retrieves a preview of dataset content with specified number of rows
    /// </summary>
    /// <param name="id">The unique identifier of the dataset</param>
    /// <param name="rows">The number of rows to include in the preview (default: 10, max: 100)</param>
    /// <returns>
    /// A preview of the dataset content as formatted text
    /// </returns>
    /// <remarks>
    /// This endpoint provides a preview of the dataset content by returning the first
    /// N rows in a formatted text representation. This is useful for quickly examining
    /// dataset structure and content without loading the entire dataset.
    /// 
    /// The preview includes:
    /// - Column headers
    /// - Sample data rows
    /// - Formatted output for readability
    /// </remarks>
    /// <response code="200">Dataset preview retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during preview generation</response>
    [HttpGet("{id}/preview")]
    [ProducesResponseType(typeof(ApiResponse<DataSetPreviewDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<DataSetPreviewDto>>> GetDataSetPreview(int id, [FromQuery] int rows = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            var preview = await _dataProcessingService.GetDataSetPreviewAsync(id, rows, userId);
            if (preview == null)
                return NotFound<DataSetPreviewDto>();

            return Success(preview);
        }
        catch (Exception ex)
        {
            return HandleException<DataSetPreviewDto>(ex, $"GetDataSetPreview({id})");
        }
    }

    /// <summary>
    /// Retrieves the schema information for a specific dataset
    /// </summary>
    /// <param name="id">The unique identifier of the dataset</param>
    /// <returns>
    /// Schema information including column names, types, and metadata
    /// </returns>
    /// <remarks>
    /// This endpoint provides detailed schema information for a dataset, including
    /// column names, data types, constraints, and statistical metadata. This information
    /// is useful for understanding dataset structure and for data analysis operations.
    /// 
    /// Schema information includes:
    /// - Column names and types
    /// - Data constraints and validation rules
    /// - Statistical summaries (min, max, average, etc.)
    /// - Data quality indicators
    /// </remarks>
    /// <response code="200">Dataset schema retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during schema analysis</response>
    [HttpGet("{id}/schema")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Soft deletes a dataset (marks as deleted but preserves data)
    /// </summary>
    /// <param name="id">The unique identifier of the dataset to delete</param>
    /// <returns>
    /// Success message indicating the dataset was deleted
    /// </returns>
    /// <remarks>
    /// This endpoint performs a soft delete operation on the dataset, which marks it
    /// as deleted but preserves the actual data. Soft deleted datasets can be restored
    /// using the restore endpoint. This approach provides data safety and allows for
    /// accidental deletion recovery.
    /// 
    /// Soft delete behavior:
    /// - Dataset is marked as deleted in the database
    /// - File data is preserved in storage
    /// - Dataset is hidden from normal queries
    /// - Can be restored using the restore endpoint
    /// </remarks>
    /// <response code="200">Dataset soft deleted successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during deletion</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Restores a previously soft-deleted dataset
    /// </summary>
    /// <param name="id">The unique identifier of the dataset to restore</param>
    /// <returns>
    /// Success message indicating the dataset was restored
    /// </returns>
    /// <remarks>
    /// This endpoint restores a dataset that was previously soft deleted. The dataset
    /// becomes visible again in normal queries and can be accessed normally. This
    /// operation is only available for datasets that were soft deleted, not hard deleted.
    /// 
    /// Restore behavior:
    /// - Removes the deleted flag from the dataset
    /// - Makes the dataset visible in normal queries
    /// - Preserves all original data and metadata
    /// - Maintains the original dataset ID
    /// </remarks>
    /// <response code="200">Dataset restored successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during restoration</response>
    [HttpPost("{id}/restore")]
    [ProducesResponseType(typeof(ApiResponse<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Permanently deletes a dataset and all associated data
    /// </summary>
    /// <param name="id">The unique identifier of the dataset to permanently delete</param>
    /// <returns>
    /// Success message indicating the dataset was permanently deleted
    /// </returns>
    /// <remarks>
    /// This endpoint performs a hard delete operation that permanently removes the dataset
    /// and all associated data from both the database and storage. This operation is
    /// irreversible and should be used with caution.
    /// 
    /// Hard delete behavior:
    /// - Permanently removes dataset record from database
    /// - Deletes associated file data from storage
    /// - Removes all related metadata and statistics
    /// - Operation is irreversible
    /// </remarks>
    /// <response code="200">Dataset permanently deleted successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during permanent deletion</response>
    [HttpDelete("{id}/permanent")]
    [ProducesResponseType(typeof(ApiResponse<string?>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Retrieves all soft-deleted datasets for the authenticated user
    /// </summary>
    /// <returns>
    /// A list of soft-deleted datasets that can be restored
    /// </returns>
    /// <remarks>
    /// This endpoint returns all datasets that have been soft deleted by the authenticated
    /// user. These datasets can be restored using the restore endpoint. The response
    /// includes dataset metadata and deletion information.
    /// 
    /// Deleted datasets include:
    /// - Original dataset metadata
    /// - Deletion timestamp
    /// - Deletion reason (if available)
    /// - Restoration eligibility status
    /// </remarks>
    /// <response code="200">Deleted datasets retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during retrieval</response>
    [HttpGet("deleted")]
    [ProducesResponseType(typeof(ApiResponse<List<DataSetDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Searches datasets by name and description for the authenticated user
    /// </summary>
    /// <param name="q">The search query string</param>
    /// <param name="page">The page number for pagination (default: 1)</param>
    /// <param name="pageSize">The number of items per page (default: 20, max: 100)</param>
    /// <returns>
    /// A paginated list of datasets matching the search criteria
    /// </returns>
    /// <remarks>
    /// This endpoint performs a text-based search across dataset names and descriptions.
    /// The search is case-insensitive and supports partial matching. Results are
    /// paginated for performance with large result sets.
    /// 
    /// Search capabilities:
    /// - Case-insensitive text matching
    /// - Partial word matching
    /// - Search across name and description fields
    /// - Paginated results for performance
    /// - Relevance-based result ordering
    /// </remarks>
    /// <response code="200">Search results retrieved successfully</response>
    /// <response code="400">Invalid search query provided</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during search</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<DataSetDto>>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Retrieves datasets filtered by file type for the authenticated user
    /// </summary>
    /// <param name="fileType">The file type to filter by</param>
    /// <param name="page">The page number for pagination (default: 1)</param>
    /// <param name="pageSize">The number of items per page (default: 20, max: 100)</param>
    /// <returns>
    /// A paginated list of datasets with the specified file type
    /// </returns>
    /// <remarks>
    /// This endpoint filters datasets by their file type (CSV, Excel, JSON, etc.).
    /// This is useful for finding datasets of a specific format or for bulk operations
    /// on datasets with similar characteristics.
    /// 
    /// Supported file types:
    /// - CSV (Comma-separated values)
    /// - Excel (XLSX, XLS)
    /// - JSON (JavaScript Object Notation)
    /// - XML (Extensible Markup Language)
    /// - Other supported formats
    /// </remarks>
    /// <response code="200">Filtered datasets retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during filtering</response>
    [HttpGet("filetype/{fileType}")]
    [ProducesResponseType(typeof(ApiResponse<List<DataSetDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Retrieves datasets created within a specified date range for the authenticated user
    /// </summary>
    /// <param name="startDate">The start date for the range (inclusive)</param>
    /// <param name="endDate">The end date for the range (inclusive)</param>
    /// <param name="page">The page number for pagination (default: 1)</param>
    /// <param name="pageSize">The number of items per page (default: 20, max: 100)</param>
    /// <returns>
    /// A paginated list of datasets created within the specified date range
    /// </returns>
    /// <remarks>
    /// This endpoint filters datasets by their creation date within a specified range.
    /// This is useful for finding recently created datasets or for time-based analysis
    /// and reporting.
    /// 
    /// Date range filtering:
    /// - Inclusive start and end dates
    /// - Based on dataset creation timestamp
    /// - Supports various date formats
    /// - Results ordered by creation date (newest first)
    /// </remarks>
    /// <response code="200">Date range filtered datasets retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during filtering</response>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(ApiResponse<List<DataSetDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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

    /// <summary>
    /// Retrieves comprehensive statistics about the user's datasets
    /// </summary>
    /// <returns>
    /// Dataset statistics including counts, sizes, and recent activity
    /// </returns>
    /// <remarks>
    /// This endpoint provides comprehensive statistics about all datasets owned by
    /// the authenticated user. The statistics include counts, total sizes, and
    /// information about recently modified datasets.
    /// 
    /// Statistics include:
    /// - Total number of datasets
    /// - Total size of all datasets
    /// - Recently modified datasets
    /// - File type distribution
    /// - Storage usage metrics
    /// </remarks>
    /// <response code="200">Dataset statistics retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during statistics calculation</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<DataSetStatisticsDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
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