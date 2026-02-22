using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.Application.Commands.DataSets;
using Normaize.DataNormalization.Application.Queries.DataSets;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Commands.DataSetLifecycle;
using Normaize.DataNormalization.Application.Queries.DataSetLifecycle;
using LifecycleUpdateRetentionPolicyCommand = Normaize.DataNormalization.Application.Commands.DataSetLifecycle.UpdateRetentionPolicyCommand;
using LifecycleResetDataSetCommand = Normaize.DataNormalization.Application.Commands.DataSetLifecycle.ResetDataSetCommand;
using LifecycleResetType = Normaize.DataNormalization.Application.Commands.DataSetLifecycle.ResetType;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Controller for dataset management operations using clean DDD architecture with CQRS
/// </summary>
[Authorize]
public class DataSetsController(
    IMediator mediator,
    IDataSetDataLoader dataLoader,
    ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid> submitDuplicateRemovalHandler,
    ILogger<DataSetsController> logger) : BaseApiController
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IDataSetDataLoader _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
    private readonly ICommandHandler<SubmitDuplicateRemovalJobCommand, Guid> _submitDuplicateRemovalHandler = submitDuplicateRemovalHandler ?? throw new ArgumentNullException(nameof(submitDuplicateRemovalHandler));
    private readonly ILogger<DataSetsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Get all datasets for the authenticated user
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="includeDeleted">Include soft-deleted datasets (default: false)</param>
    /// <returns>Paginated list of datasets</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedApiResponse<List<DataSetResponse>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDataSets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false)
    {
        try
        {
            if (page < 1)
            {
                return Error("Page number must be greater than 0", "INVALID_PAGE");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return Error("Page size must be between 1 and 100", "INVALID_PAGE_SIZE");
            }

            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting datasets, page {Page}, pageSize {PageSize}",
                userId, page, pageSize);

            var query = new GetDataSetsByUserQuery(userId, page, pageSize, includeDeleted);
            var result = await _mediator.Send(query);

            var responses = result.Items.Select(MapFromDto).ToList();
            var totalItems = result.TotalItems;

            return SuccessPaginated(responses, page, pageSize, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting datasets for user");
            return HandleException(ex, nameof(GetDataSets));
        }
    }

    /// <summary>
    /// Get a specific dataset by ID
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <returns>Dataset information</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DataSetResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDataSet(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting dataset {DataSetId}", userId, id);

            var query = new GetDataSetByIdQuery(id, userId);
            var dataSet = await _mediator.Send(query);

            if (dataSet == null)
            {
                return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
            }

            var response = MapFromDto(dataSet);
            return Success(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dataset {DataSetId}", id);
            return HandleException(ex, nameof(GetDataSet));
        }
    }

    /// <summary>
    /// Update a dataset's metadata
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <param name="request">Update request</param>
    /// <returns>Updated dataset information</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DataSetResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateDataSet(Guid id, [FromBody] UpdateDataSetRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} updating dataset {DataSetId}", userId, id);

            var command = new UpdateDataSetCommand(id, userId, request.Name, request.Description, request.RetentionExpiryDate, userId);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return Error(result.Message, "UPDATE_FAILED", 400);
            }

            // Fetch updated dataset
            var query = new GetDataSetByIdQuery(id, userId);
            var dataSet = await _mediator.Send(query);

            var response = MapFromDto(dataSet!);
            return Success(response, "Dataset updated successfully");
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dataset {DataSetId}", id);
            return HandleException(ex, nameof(UpdateDataSet));
        }
    }

    /// <summary>
    /// Soft delete a dataset
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <returns>Success confirmation</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteDataSet(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} deleting dataset {DataSetId}", userId, id);

            var command = new DeleteDataSetCommand(id, userId, userId);
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return Error(result.Message, "DELETE_FAILED", result.Error != null ? 404 : 400);
            }

            return Success("Dataset deleted successfully");
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to delete it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dataset {DataSetId}", id);
            return HandleException(ex, nameof(DeleteDataSet));
        }
    }

    /// <summary>
    /// Get a preview of dataset data
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <param name="rows">Number of rows to include in preview (default: 10, max: 100)</param>
    /// <returns>Dataset preview with sample data</returns>
    [HttpGet("{id:guid}/preview")]
    [ProducesResponseType(typeof(ApiResponse<DataSetPreviewResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDataSetPreview(Guid id, [FromQuery] int rows = 10)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting preview for dataset {DataSetId}", userId, id);

            // Validate row count
            rows = Math.Min(Math.Max(rows, 1), 100);

            var query = new GetDataSetByIdQuery(id, userId);
            var dataSet = await _mediator.Send(query);

            if (dataSet == null)
            {
                return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
            }

            var dataSetData = await _dataLoader.LoadDataSetSampleAsync(id, rows);

            var response = new DataSetPreviewResponse
            {
                DataSetId = id,
                Columns = dataSetData.Columns.Select(c => new ColumnInfo
                {
                    Name = c.Name,
                    DataType = c.DataType,
                    Index = c.Index,
                    AllowNull = c.AllowNull
                }).ToList(),
                Rows = dataSetData.Rows.Select(r => r.Values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)).ToList(),
                TotalRows = dataSet.RowCount,
                PreviewRows = dataSetData.Rows.Count
            };

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preview for dataset {DataSetId}", id);
            return HandleException(ex, nameof(GetDataSetPreview));
        }
    }

    /// <summary>
    /// Get column information for a dataset
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <returns>Column schema information</returns>
    [HttpGet("{id:guid}/columns")]
    [ProducesResponseType(typeof(ApiResponse<List<ColumnInfo>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDataSetColumns(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting columns for dataset {DataSetId}", userId, id);

            var query = new GetDataSetByIdQuery(id, userId);
            var dataSet = await _mediator.Send(query);

            if (dataSet == null)
            {
                return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
            }

            var columns = await _dataLoader.GetDataSetColumnsAsync(id);
            var response = columns.Select(c => new ColumnInfo
            {
                Name = c.Name,
                DataType = c.DataType,
                Index = c.Index,
                AllowNull = c.AllowNull
            }).ToList();

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting columns for dataset {DataSetId}", id);
            return HandleException(ex, nameof(GetDataSetColumns));
        }
    }

    /// <summary>
    /// Create a new dataset by uploading a file
    /// </summary>
    /// <param name="request">Upload request with file and metadata</param>
    /// <returns>Created dataset information</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<DataSetResponse>), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UploadDataSet([FromForm] CreateDataSetRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("📤 User {UserId} starting dataset upload - Name: {Name}, File: {FileName}",
                userId, request.Name, request.File?.FileName);

            if (request.File == null || request.File.Length == 0)
            {
                _logger.LogWarning("Upload failed - No file provided");
                return Error("File is required", "FILE_REQUIRED", 400);
            }

            _logger.LogInformation("File details - Size: {Size} bytes, ContentType: {ContentType}",
                request.File.Length, request.File.ContentType);

            // Create upload command
            var command = new UploadDataSetCommand(
                request.Name,
                request.Description,
                userId,
                request.File.FileName,
                string.Empty, // FilePath will be set by the handler
                request.File.Length,
                request.File.OpenReadStream(),
                request.RetentionDays);

            _logger.LogInformation("Sending upload command to handler...");
            var result = await _mediator.Send(command);
            _logger.LogInformation("Upload command completed - Success: {Success}, DataSetId: {DataSetId}",
                result.Success, result.DataSetId);

            if (!result.Success)
            {
                _logger.LogWarning("Upload failed - {Message}", result.Message);
                return Error(result.Message, "UPLOAD_FAILED", 400);
            }

            // Fetch created dataset
            _logger.LogInformation("Fetching created dataset {DataSetId}...", result.DataSetId);
            var query = new GetDataSetByIdQuery(result.DataSetId!.Value, userId);
            var dataSet = await _mediator.Send(query);

            if (dataSet == null)
            {
                _logger.LogError("Dataset {DataSetId} not found after creation", result.DataSetId);
                return Error("Dataset created but not found", "DATASET_NOT_FOUND", 500);
            }

            var response = MapFromDto(dataSet);
            _logger.LogInformation("✅ Dataset {DataSetId} uploaded successfully", result.DataSetId);

            // Include processing job ID if async processing
            var successMessage = result.ProcessingJobId.HasValue
                ? "Dataset uploaded successfully. Processing in background..."
                : "Dataset uploaded and processed successfully";

            var createdResponse = new
            {
                success = true,
                message = successMessage,
                data = response,
                processingJobId = result.ProcessingJobId,
                isAsyncProcessing = result.ProcessingJobId.HasValue
            };

            return StatusCode(201, createdResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error uploading dataset - {Message}", ex.Message);
            return HandleException(ex, nameof(UploadDataSet));
        }
    }

    /// <summary>
    /// Search datasets by name or description
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <returns>Paginated search results</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PaginatedApiResponse<List<DataSetResponse>>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> SearchDataSets(
        [FromQuery] string query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Error("Search query is required", "INVALID_QUERY", 400);
            }

            if (page < 1)
            {
                return Error("Page number must be greater than 0", "INVALID_PAGE");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return Error("Page size must be between 1 and 100", "INVALID_PAGE_SIZE");
            }

            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} searching datasets with query: {Query}", userId, query);

            var searchQuery = new SearchDataSetsQuery(query, userId, page, pageSize);
            var result = await _mediator.Send(searchQuery);

            var responses = result.Items.Select(MapFromDto).ToList();
            var totalItems = result.TotalItems;

            return SuccessPaginated(responses, page, pageSize, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching datasets");
            return HandleException(ex, nameof(SearchDataSets));
        }
    }

    /// <summary>
    /// Get deleted datasets for the authenticated user
    /// </summary>
    /// <returns>List of soft-deleted datasets</returns>
    [HttpGet("deleted")]
    [ProducesResponseType(typeof(ApiResponse<List<DataSetResponse>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDeletedDataSets()
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting deleted datasets", userId);

            var query = new GetDataSetsByUserQuery(userId, 1, 1000, IncludeDeleted: true);
            var allDataSets = (await _mediator.Send(query)).Items;

            // Filter to only deleted datasets
            var deletedDataSets = allDataSets.Where(ds => ds.IsDeleted).ToList();
            var responses = deletedDataSets.Select(MapFromDto).ToList();

            return Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting deleted datasets");
            return HandleException(ex, nameof(GetDeletedDataSets));
        }
    }

    /// <summary>
    /// Reset a dataset to its original state
    /// Matches legacy endpoint: POST /api/datasets/{id}/reset
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <param name="request">Reset configuration options</param>
    /// <returns>Updated dataset information</returns>
    [HttpPost("{id:guid}/reset")]
    [ProducesResponseType(typeof(ApiResponse<DataSetResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ResetDataSet(Guid id, [FromBody] ResetDataSetRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} resetting dataset {DataSetId} with type {ResetType}",
                userId, id, request.ResetType);

            // Map client ResetType (RESTORE/REPROCESS) to domain ResetType enum
            if (!Enum.TryParse<LifecycleResetType>(request.ResetType, ignoreCase: true, out var resetType))
            {
                return Error($"Invalid reset type: {request.ResetType}. Valid values are RESTORE or REPROCESS",
                    "INVALID_RESET_TYPE", 400);
            }

            var command = new LifecycleResetDataSetCommand
            {
                DataSetId = id,
                UserId = userId,
                ResetType = resetType,
                Reason = request.Reason
            };

            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                // Check if dataset not found or access denied (404)
                if (result.Error != null &&
                    (result.Error.Contains("not found") || result.Error.Contains("Access denied")))
                {
                    return Error(result.Error, "DATASET_NOT_FOUND", 404);
                }

                // Check if it's a file availability issue (409 Conflict)
                if (result.ErrorCode is "FILE_NOT_AVAILABLE" or "FILE_NOT_FOUND" or "FILE_NOT_AVAILABLE" ||
                    (result.Error != null && result.Error.Contains("no longer available")))
                {
                    return Error(result.Error ?? result.Message, result.ErrorCode ?? "FILE_NOT_AVAILABLE", 409);
                }

                // Other errors (400 Bad Request)
                return Error(result.Error ?? result.Message, "RESET_FAILED", 400);
            }

            // Fetch updated dataset to return
            var query = new GetDataSetByIdQuery(id, userId);
            var dataSet = await _mediator.Send(query);

            if (dataSet == null)
            {
                return Error("Dataset reset successfully but could not retrieve updated dataset",
                    "RETRIEVAL_FAILED", 500);
            }

            var response = MapFromDto(dataSet);
            return Success(response, result.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to reset it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting dataset {DataSetId}", id);
            return HandleException(ex, nameof(ResetDataSet));
        }
    }

    /// <summary>
    /// Restore a soft-deleted dataset
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <returns>Success confirmation</returns>
    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RestoreDataSet(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} restoring dataset {DataSetId}", userId, id);

            var command = new RestoreDataSetCommand { DataSetId = id, UserId = userId };
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return Error(result.Message, "RESTORE_FAILED", result.Error != null ? 404 : 400);
            }

            return Success("Dataset restored successfully");
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to restore it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring dataset {DataSetId}", id);
            return HandleException(ex, nameof(RestoreDataSet));
        }
    }

    /// <summary>
    /// Permanently delete a dataset and its data
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <returns>Success confirmation</returns>
    [HttpDelete("{id:guid}/hard-delete")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> HardDeleteDataSet(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogWarning("User {UserId} performing hard delete of dataset {DataSetId}", userId, id);

            var command = new HardDeleteDataSetCommand { DataSetId = id, UserId = userId };
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return Error(result.Message, "HARD_DELETE_FAILED", result.Error != null ? 404 : 400);
            }

            return Success("Dataset permanently deleted");
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to delete it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hard deleting dataset {DataSetId}", id);
            return HandleException(ex, nameof(HardDeleteDataSet));
        }
    }

    /// <summary>
    /// Get retention status for a dataset
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <returns>Retention status information</returns>
    [HttpGet("{id:guid}/retention-status")]
    [ProducesResponseType(typeof(ApiResponse<RetentionStatusResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetRetentionStatus(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting retention status for dataset {DataSetId}", userId, id);

            var query = new GetRetentionStatusQuery { DataSetId = id, UserId = userId };
            var status = await _mediator.Send(query);

            if (!status.Success)
            {
                var statusCode = status.Error?.Contains("Access denied") == true ? 404 : 404;
                return Error(status.Error ?? "Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", statusCode);
            }

            var response = new RetentionStatusResponse
            {
                DataSetId = status.DataSetId ?? Guid.Empty,
                RetentionDays = status.RetentionDays ?? 0,
                CreatedAt = status.UploadedAt ?? DateTime.MinValue,
                ExpiryDate = status.RetentionExpiryDate ?? DateTime.MinValue,
                DaysRemaining = status.DaysUntilExpiry,
                IsExpired = status.IsRetentionExpired,
                CanExtend = status.CanExtend,
                FileExists = status.FileExists
            };

            return Success(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting retention status for dataset {DataSetId}", id);
            return HandleException(ex, nameof(GetRetentionStatus));
        }
    }

    /// <summary>
    /// Update retention policy for a dataset
    /// </summary>
    /// <param name="id">Dataset ID</param>
    /// <param name="request">Retention policy update request</param>
    /// <returns>Success confirmation</returns>
    [HttpPut("{id:guid}/retention-policy")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateRetentionPolicy(Guid id, [FromBody] UpdateRetentionPolicyRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} updating retention policy for dataset {DataSetId} to {Days} days",
                userId, id, request.RetentionDays);

            var command = new LifecycleUpdateRetentionPolicyCommand { DataSetId = id, RetentionDays = request.RetentionDays, UserId = userId };
            var result = await _mediator.Send(command);

            if (!result.Success)
            {
                return Error(result.Message, "UPDATE_RETENTION_FAILED", result.Error != null ? 404 : 400);
            }

            return Success($"Retention policy updated to {request.RetentionDays} days");
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to update it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating retention policy for dataset {DataSetId}", id);
            return HandleException(ex, nameof(UpdateRetentionPolicy));
        }
    }

    /// <summary>
    /// Remove duplicate rows from a dataset
    /// Matches legacy endpoint: POST /api/datasets/{dataSetId}/remove-duplicates
    /// This endpoint provides backward compatibility with client expectations (path-parameter based route)
    /// </summary>
    /// <param name="dataSetId">Dataset ID from path</param>
    /// <param name="request">Duplicate removal configuration (client format)</param>
    /// <returns>Job submission response</returns>
    [HttpPost("{dataSetId:guid}/remove-duplicates")]
    [ProducesResponseType(typeof(ApiResponse<JobSubmissionResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> RemoveDuplicates(Guid dataSetId, [FromBody] RemoveDuplicateRowsRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} requesting duplicate removal for dataset {DataSetId} via path-parameter route",
                userId, dataSetId);

            // Map client format to domain value objects
            var caseSensitivity = request.CaseSensitive
                ? CaseSensitivity.Sensitive
                : CaseSensitivity.Insensitive;

            // Map KeepFirstOccurrence boolean to Strategy string
            var strategy = request.KeepFirstOccurrence ? "KeepFirst" : "KeepLast";

            var options = strategy.ToLower() switch
            {
                "keeplast" => DuplicateRemovalOptions.KeepLast(request.ColumnNames, caseSensitivity),
                _ => DuplicateRemovalOptions.KeepFirst(request.ColumnNames, caseSensitivity)
            };

            // Create command with domain value object
            var command = new SubmitDuplicateRemovalJobCommand(dataSetId, options);
            var jobId = await _submitDuplicateRemovalHandler.HandleAsync(command);

            var response = new JobSubmissionResponse
            {
                JobId = jobId,
                Status = "Submitted",
                Message = "Duplicate removal job submitted successfully",
                SubmittedAt = DateTime.UtcNow
            };

            return Success(response, "Duplicate removal job submitted successfully");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Error($"Dataset with ID {dataSetId} not found", "DATASET_NOT_FOUND", 404);
        }
        catch (UnauthorizedAccessException)
        {
            return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting duplicate removal job for dataset {DataSetId}", dataSetId);
            return HandleException(ex, nameof(RemoveDuplicates));
        }
    }

    /// <summary>
    /// Maps a domain entity to API response DTO
    /// </summary>
    private static DataSetResponse MapToResponse(Domain.Entities.DataSet dataSet)
    {
        return new DataSetResponse
        {
            Id = dataSet.Id,
            Name = dataSet.Name,
            Description = dataSet.Description ?? string.Empty,
            CreatedBy = dataSet.UserId,
            CreatedAt = dataSet.UploadedAt,
            UpdatedAt = dataSet.LastModifiedAt,
            IsProcessed = !string.IsNullOrEmpty(dataSet.ProcessedData),
            IsDeleted = dataSet.IsDeleted,
            RetentionExpiryDate = dataSet.RetentionExpiryDate,
            FileMetadata = dataSet.FileInfo != null ? new FileMetadataResponse
            {
                OriginalFileName = dataSet.FileInfo.FileName,
                StoragePath = dataSet.FileInfo.FilePath,
                FileType = dataSet.FileInfo.FileType.ToString(),
                SizeInBytes = dataSet.FileInfo.FileSize,
                Checksum = dataSet.FileInfo.DataHash ?? string.Empty,
                StorageProvider = dataSet.FileInfo.StorageProvider.ToString()
            } : null,
            Statistics = dataSet.Statistics != null ? new DatasetStatisticsResponse
            {
                RowCount = dataSet.Statistics.RowCount,
                ColumnCount = dataSet.Statistics.ColumnCount,
                FileSizeBytes = dataSet.FileInfo?.FileSize ?? 0, // Use file size as stats don't have FileSizeBytes
                LastProcessedAt = dataSet.LastModifiedAt
            } : new DatasetStatisticsResponse
            {
                RowCount = 0,
                ColumnCount = 0,
                FileSizeBytes = dataSet.FileInfo?.FileSize ?? 0,
                LastProcessedAt = dataSet.LastModifiedAt
            }
        };
    }

    /// <summary>
    /// Maps an Application DTO to API response DTO
    /// </summary>
    private static DataSetResponse MapFromDto(Application.DTOs.DataSetDto dataSet)
    {
        return new DataSetResponse
        {
            Id = dataSet.Id,
            Name = dataSet.Name,
            Description = dataSet.Description ?? string.Empty,
            CreatedBy = dataSet.UserId,
            CreatedAt = dataSet.UploadedAt,
            UpdatedAt = dataSet.LastModifiedAt,
            IsProcessed = dataSet.IsProcessed,
            IsDeleted = dataSet.IsDeleted,
            RetentionExpiryDate = dataSet.RetentionExpiryDate,
            FileMetadata = new FileMetadataResponse
            {
                OriginalFileName = dataSet.FileName,
                StoragePath = dataSet.FilePath,
                FileType = dataSet.FileType,
                SizeInBytes = dataSet.FileSize,
                Checksum = string.Empty, // Checksum not available in DTO
                StorageProvider = dataSet.StorageProvider
            },
            Statistics = new DatasetStatisticsResponse
            {
                RowCount = dataSet.RowCount,
                ColumnCount = dataSet.ColumnCount,
                FileSizeBytes = dataSet.FileSize,
                LastProcessedAt = dataSet.ProcessedAt ?? dataSet.LastModifiedAt
            }
        };
    }
}