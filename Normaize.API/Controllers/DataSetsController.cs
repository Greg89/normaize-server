using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Extensions;
using System.Linq;

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
public class DataSetsController(
    IDataProcessingService dataProcessingService,
    IDataSetLifecycleService dataSetLifecycleService,
    IDataSetQueryService dataSetQueryService,
    IDataSetPreviewService dataSetPreviewService,
    IStructuredLoggingService loggingService)
    : BaseApiController(loggingService)
{
    private readonly IDataProcessingService _dataProcessingService = dataProcessingService;
    private readonly IDataSetLifecycleService _dataSetLifecycleService = dataSetLifecycleService;
    private readonly IDataSetQueryService _dataSetQueryService = dataSetQueryService;
    private readonly IDataSetPreviewService _dataSetPreviewService = dataSetPreviewService;

    /// <summary>
    /// Gets the current user ID from the authenticated user claims
    /// </summary>
    /// <returns>The user ID as a string</returns>
    private string GetCurrentUserId()
    {
        return User.GetUserId();
    }

    /// <summary>
    /// Retrieves datasets for the authenticated user
    /// </summary>
    /// <param name="includeDeleted">Optional. When true, returns both active and deleted datasets; default is false (active only)</param>
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
    public async Task<ActionResult<ApiResponse<List<DataSetDto>>>> GetDataSets([FromQuery] bool includeDeleted = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dataSets = await _dataSetQueryService.GetDataSetsByUserAsync(userId, 1, 20, includeDeleted);
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
    /// Updates an existing dataset
    /// </summary>
    /// <param name="id">The unique identifier of the dataset to update</param>
    /// <param name="updateDto">The update data containing new values</param>
    /// <returns>
    /// The updated dataset with current values
    /// </returns>
    /// <remarks>
    /// This endpoint allows updating dataset metadata such as name and description.
    /// The file content cannot be modified through this endpoint. To replace the file
    /// content, use the upload endpoint with a new file.
    /// 
    /// Updateable fields:
    /// - Name: The display name of the dataset
    /// - Description: Detailed description of the dataset
    /// 
    /// The endpoint validates that the dataset belongs to the authenticated user
    /// and that all required fields are provided.
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
            var userId = GetCurrentUserId();

            // Validate file
            if (uploadDto.File == null || uploadDto.File.Length == 0)
                return BadRequest<DataSetUploadResponse>("No file provided");

            // Create file request
            var fileRequest = new FileUploadRequest
            {
                FileName = uploadDto.File.FileName,
                ContentType = uploadDto.File.ContentType,
                FileSize = uploadDto.File.Length,
                FileStream = uploadDto.File.OpenReadStream()
            };

            // Create dataset DTO
            var createDto = new CreateDataSetDto
            {
                Name = uploadDto.Name,
                Description = uploadDto.Description,
                UserId = userId
            };

            var result = await _dataProcessingService.UploadDataSetAsync(fileRequest, createDto);
            return Success(result);
        }
        catch (Exception ex)
        {
            return HandleException<DataSetUploadResponse>(ex, "UploadDataSet");
        }
    }

    /// <summary>
    /// Retrieves preview data for a specific dataset
    /// </summary>
    /// <param name="id">The unique identifier of the dataset</param>
    /// <param name="rows">Number of rows to return in the preview (default: 10, max: 1000)</param>
    /// <returns>
    /// Preview data containing sample rows and column information
    /// </returns>
    /// <remarks>
    /// This endpoint provides a preview of the dataset content, showing a limited number
    /// of rows to help users understand the data structure and content. The preview
    /// includes both the actual data rows and metadata about the columns.
    /// 
    /// Preview features:
    /// - Configurable number of rows (1-1000)
    /// - Column names and types
    /// - Sample data values
    /// - Data structure information
    /// 
    /// This is useful for:
    /// - Understanding dataset structure before analysis
    /// - Validating data quality and format
    /// - Quick data exploration
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
            var preview = await _dataSetPreviewService.GetDataSetPreviewAsync(id, rows, userId);
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
            var schema = await _dataSetPreviewService.GetDataSetSchemaAsync(id, userId);
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

            return Success<string?>("Dataset deleted successfully");
        }
        catch (Exception ex)
        {
            return HandleException<string?>(ex, $"DeleteDataSet({id})");
        }
    }

    /// <summary>
    /// Restores a soft-deleted dataset
    /// </summary>
    /// <param name="id">The unique identifier of the dataset to restore</param>
    /// <returns>
    /// Success message indicating the dataset was restored
    /// </returns>
    /// <remarks>
    /// This endpoint restores a previously soft-deleted dataset, making it available
    /// again for normal operations. The dataset will reappear in queries and can be
    /// accessed normally.
    /// 
    /// Restore behavior:
    /// - Dataset is marked as active in the database
    /// - All data and metadata is preserved
    /// - Dataset becomes visible in normal queries
    /// - Processing status and preview data are maintained
    /// </remarks>
    /// <response code="200">Dataset restored successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during restore</response>
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
            var result = await _dataSetLifecycleService.RestoreDataSetAsync(id, userId);
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
    /// Resets a dataset to its original state
    /// </summary>
    /// <param name="id">The unique identifier of the dataset to reset</param>
    /// <param name="resetDto">Reset configuration options</param>
    /// <returns>
    /// Detailed operation result with reset information
    /// </returns>
    /// <remarks>
    /// This endpoint performs a "factory reset" on a dataset, allowing you to start
    /// fresh with the original data. Different reset types are available:
    /// 
    /// Reset Types:
    /// - Reprocess: Downloads and reprocesses the original file from S3
    /// - Restore: Restores deleted dataset without changing processing status
    /// 
    /// Reprocess reset will:
    /// - Download the original file from S3 (if still available)
    /// - Reprocess the file to regenerate preview and schema data
    /// - Update the dataset with fresh processing results
    /// 
    /// Restore reset will:
    /// - Restore the dataset if it was deleted
    /// - Keep all processing data and status intact
    /// - Dataset becomes immediately usable
    /// 
    /// If the original file is no longer available (due to retention policies),
    /// the operation will fail gracefully with appropriate error information.
    /// </remarks>
    /// <response code="200">Dataset reset successfully</response>
    /// <response code="400">Invalid reset configuration</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="409">Original file no longer available</response>
    /// <response code="500">Internal server error during reset</response>
    [HttpPost("{id}/reset")]
    [ProducesResponseType(typeof(ApiResponse<string?>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<string?>>> ResetDataSet(int id, [FromBody] DataSetResetDto resetDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _dataSetLifecycleService.ResetDataSetAsync(id, resetDto, userId);

            if (!result.Success)
            {
                if (result.Message.Contains("no longer available") || result.Message.Contains("not found"))
                    return Conflict(result.Message);
                return BadRequest<string?>(result.Message);
            }

            return Success<string?>(result.Message);
        }
        catch (Exception ex)
        {
            return HandleException<string?>(ex, $"ResetDataSet({id})");
        }
    }

    /// <summary>
    /// Permanently deletes a dataset and its associated file
    /// </summary>
    /// <param name="id">The unique identifier of the dataset to permanently delete</param>
    /// <returns>
    /// Success message indicating the dataset was permanently deleted
    /// </returns>
    /// <remarks>
    /// This endpoint performs a hard delete operation that permanently removes the dataset
    /// from the database and deletes the associated file from storage. This operation
    /// cannot be undone and should be used with extreme caution.
    /// 
    /// Hard delete behavior:
    /// - Dataset is permanently removed from the database
    /// - Associated file is deleted from storage (S3)
    /// - All metadata and processing data is lost
    /// - Operation cannot be reversed
    /// 
    /// Warning: This operation is irreversible. Consider using soft delete first
    /// to allow for recovery in case of accidental deletion.
    /// </remarks>
    /// <response code="200">Dataset permanently deleted successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during deletion</response>
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
            var result = await _dataSetLifecycleService.HardDeleteDataSetAsync(id, userId);
            if (!result)
                return NotFound<string?>();

            return Success<string?>("Dataset permanently deleted successfully");
        }
        catch (Exception ex)
        {
            return HandleException<string?>(ex, $"HardDeleteDataSet({id})");
        }
    }

    /// <summary>
    /// Updates the retention policy for a dataset
    /// </summary>
    /// <param name="id">The unique identifier of the dataset</param>
    /// <param name="retentionDto">Retention policy configuration</param>
    /// <returns>
    /// Success message with updated retention information
    /// </returns>
    /// <remarks>
    /// This endpoint allows users to set custom data retention periods for their datasets.
    /// The retention policy determines how long the original file will be kept in S3
    /// before automatic cleanup.
    /// 
    /// Retention features:
    /// - Custom retention period in days
    /// - Automatic expiry date calculation
    /// - Graceful handling of expired files
    /// - User-defined data lifecycle management
    /// 
    /// When a file expires:
    /// - The original file is automatically removed from S3
    /// - Dataset metadata and processed data remain in the database
    /// - File-based reset operations will fail gracefully
    /// - Database-only operations continue to work normally
    /// </remarks>
    /// <response code="200">Retention policy updated successfully</response>
    /// <response code="400">Invalid retention configuration</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during update</response>
    [HttpPut("{id}/retention")]
    [ProducesResponseType(typeof(ApiResponse<string?>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<string?>>> UpdateRetentionPolicy(int id, [FromBody] DataSetRetentionDto retentionDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _dataSetLifecycleService.UpdateRetentionPolicyAsync(id, retentionDto, userId);

            if (!result.Success)
                return BadRequest<string?>(result.Message);

            return Success<string?>(result.Message);
        }
        catch (Exception ex)
        {
            return HandleException<string?>(ex, $"UpdateRetentionPolicy({id})");
        }
    }

    /// <summary>
    /// Retrieves retention status for a dataset
    /// </summary>
    /// <param name="id">The unique identifier of the dataset</param>
    /// <returns>
    /// Detailed retention status information
    /// </returns>
    /// <remarks>
    /// This endpoint provides comprehensive information about a dataset's retention policy
    /// and current status. It helps users understand when their data will expire and
    /// manage their data lifecycle effectively.
    /// 
    /// Status information includes:
    /// - Current retention policy (days)
    /// - Expiry date calculation
    /// - Whether retention has expired
    /// - Days remaining (if not expired)
    /// - File availability status
    /// 
    /// This is useful for:
    /// - Data lifecycle management
    /// - Compliance monitoring
    /// - Storage cost optimization
    /// - Proactive data management
    /// </remarks>
    /// <response code="200">Retention status retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="404">Dataset not found or access denied</response>
    /// <response code="500">Internal server error during status retrieval</response>
    [HttpGet("{id}/retention-status")]
    [ProducesResponseType(typeof(ApiResponse<DataSetRetentionStatusDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ApiResponse<DataSetRetentionStatusDto>>> GetRetentionStatus(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var status = await _dataSetLifecycleService.GetRetentionStatusAsync(id, userId);
            if (status == null)
                return NotFound<DataSetRetentionStatusDto>();

            return Success(status);
        }
        catch (Exception ex)
        {
            return HandleException<DataSetRetentionStatusDto>(ex, $"GetRetentionStatus({id})");
        }
    }

    /// <summary>
    /// Retrieves all soft-deleted datasets for the authenticated user
    /// </summary>
    /// <returns>
    /// A list of soft-deleted datasets owned by the current user
    /// </returns>
    /// <remarks>
    /// This endpoint returns all datasets that have been soft-deleted by the authenticated user.
    /// Soft-deleted datasets are marked as deleted but their data is preserved, allowing
    /// for potential restoration.
    /// 
    /// Deleted datasets include:
    /// - All metadata and file information
    /// - Deletion timestamp
    /// - Original processing status
    /// - File availability status
    /// 
    /// This endpoint is useful for:
    /// - Reviewing deleted datasets
    /// - Restoring accidentally deleted data
    /// - Data recovery operations
    /// - Audit and compliance purposes
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
            var dataSets = await _dataSetQueryService.GetDeletedDataSetsAsync(userId);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, "GetDeletedDataSets");
        }
    }

    /// <summary>
    /// Searches datasets by term with pagination support
    /// </summary>
    /// <param name="q">Search term to match against dataset names, descriptions, and file names</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>
    /// Paginated list of datasets matching the search criteria
    /// </returns>
    /// <remarks>
    /// This endpoint provides full-text search capabilities across dataset metadata.
    /// The search is case-insensitive and matches against multiple fields including
    /// dataset names, descriptions, and file names.
    /// 
    /// Search features:
    /// - Case-insensitive text matching
    /// - Multi-field search (name, description, filename)
    /// - Pagination support for large result sets
    /// - User-specific dataset isolation
    /// - Real-time search results
    /// 
    /// Search behavior:
    /// - Matches partial strings in any field
    /// - Returns datasets owned by the authenticated user only
    /// - Excludes soft-deleted datasets from results
    /// - Supports pagination for performance
    /// </remarks>
    /// <response code="200">Search results retrieved successfully</response>
    /// <response code="400">Invalid search parameters</response>
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
                return BadRequest<List<DataSetDto>>("Search term is required");

            var userId = GetCurrentUserId();
            var dataSets = await _dataSetQueryService.SearchDataSetsAsync(q, userId, page, pageSize);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, "SearchDataSets");
        }
    }

    /// <summary>
    /// Retrieves datasets filtered by file type with pagination support
    /// </summary>
    /// <param name="fileType">The file type to filter by</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>
    /// Paginated list of datasets with the specified file type
    /// </returns>
    /// <remarks>
    /// This endpoint filters datasets by their file type, allowing users to focus on
    /// specific data formats. Supported file types include CSV, Excel, JSON, and XML.
    /// 
    /// File type filtering:
    /// - CSV: Comma-separated values files
    /// - Excel: Microsoft Excel files (.xlsx, .xls)
    /// - JSON: JavaScript Object Notation files
    /// - XML: Extensible Markup Language files
    /// - Unknown: Unsupported or unrecognized file types
    /// 
    /// This is useful for:
    /// - Working with specific data formats
    /// - Data format analysis
    /// - Format-specific processing workflows
    /// - Data organization and management
    /// </remarks>
    /// <response code="200">Filtered datasets retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during retrieval</response>
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
            var dataSets = await _dataSetQueryService.GetDataSetsByFileTypeAsync(fileType, userId, page, pageSize);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, $"GetDataSetsByFileType({fileType})");
        }
    }

    /// <summary>
    /// Retrieves datasets within a specified date range with pagination support
    /// </summary>
    /// <param name="startDate">Start date for the range (inclusive)</param>
    /// <param name="endDate">End date for the range (inclusive)</param>
    /// <param name="page">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 20, max: 100)</param>
    /// <returns>
    /// Paginated list of datasets uploaded within the specified date range
    /// </returns>
    /// <remarks>
    /// This endpoint filters datasets by their upload date, allowing users to find
    /// datasets created within a specific time period. The date range is inclusive,
    /// meaning datasets uploaded on the start or end date are included.
    /// 
    /// Date range features:
    /// - Inclusive date range filtering
    /// - Based on dataset upload timestamp
    /// - Pagination support for large result sets
    /// - User-specific dataset isolation
    /// 
    /// This is useful for:
    /// - Finding recently uploaded datasets
    /// - Historical data analysis
    /// - Data lifecycle management
    /// - Audit and compliance reporting
    /// </remarks>
    /// <response code="200">Date range filtered datasets retrieved successfully</response>
    /// <response code="401">Unauthorized - Authentication required</response>
    /// <response code="500">Internal server error during retrieval</response>
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
            var dataSets = await _dataSetQueryService.GetDataSetsByDateRangeAsync(startDate, endDate, userId, page, pageSize);
            return Success(dataSets?.ToList() ?? []);
        }
        catch (Exception ex)
        {
            return HandleException<List<DataSetDto>>(ex, "GetDataSetsByDateRange");
        }
    }

    /// <summary>
    /// Retrieves comprehensive statistics for the authenticated user's datasets
    /// </summary>
    /// <returns>
    /// Detailed statistics including counts, file sizes, and breakdowns
    /// </returns>
    /// <remarks>
    /// This endpoint provides comprehensive analytics and statistics about the user's
    /// datasets, including counts, file sizes, type breakdowns, and processing status.
    /// This information is useful for understanding data usage patterns and storage
    /// requirements.
    /// 
    /// Statistics include:
    /// - Total dataset count (active and deleted)
    /// - Total and average file sizes
    /// - File type distribution
    /// - Processing status breakdown
    /// - Recent upload activity
    /// - Storage utilization metrics
    /// 
    /// This is useful for:
    /// - Data usage analysis
    /// - Storage planning and optimization
    /// - Performance monitoring
    /// - User activity insights
    /// - Capacity planning
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
            var statistics = await _dataSetQueryService.GetDataSetStatisticsAsync(userId);
            return Success(statistics);
        }
        catch (Exception ex)
        {
            return HandleException<DataSetStatisticsDto>(ex, "GetDataSetStatistics");
        }
    }
}