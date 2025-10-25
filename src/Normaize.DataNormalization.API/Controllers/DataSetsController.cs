using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Controller for dataset management operations using clean DDD architecture
/// </summary>
public class DataSetsController : BaseApiController
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IDataSetDataLoader _dataLoader;
    private readonly ILogger<DataSetsController> _logger;

    public DataSetsController(
        IDataSetRepository dataSetRepository,
        IDataSetDataLoader dataLoader,
        ILogger<DataSetsController> logger)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} requesting datasets, page {Page}, pageSize {PageSize}", 
                userId, page, pageSize);

            var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
            var filteredDataSets = includeDeleted 
                ? dataSets 
                : dataSets.Where(ds => !ds.IsDeleted);

            var totalItems = filteredDataSets.Count();
            var pagedDataSets = filteredDataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToResponse)
                .ToList();

            return SuccessPaginated(pagedDataSets, page, pageSize, totalItems);
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

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId)
            {
                return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
            }

            var response = MapToResponse(dataSet);
            return Success(response);
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

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId)
            {
                return Error("Dataset not found or you don't have permission to access it", "DATASET_NOT_FOUND", 404);
            }

            // TODO: Implement UpdateDetails method on DataSet entity
            // dataSet.UpdateDetails(request.Name, request.Description, userId);
            // await _dataSetRepository.UpdateAsync(dataSet);

            var response = MapToResponse(dataSet);
            return Success(response, "Dataset updated successfully");
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

            var result = await _dataSetRepository.DeleteAsync(id);
            if (!result)
            {
                return Error("Dataset not found or you don't have permission to delete it", "DATASET_NOT_FOUND", 404);
            }

            return Success("Dataset deleted successfully");
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

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId)
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
                TotalRows = dataSet.Statistics.RowCount,
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

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId)
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
            _logger.LogInformation("User {UserId} uploading dataset {Name}", userId, request.Name);

            // TODO: Implement file upload and dataset creation
            // This would involve:
            // 1. Validate file
            // 2. Store file to storage provider
            // 3. Create dataset entity
            // 4. Process file for preview/statistics
            // 5. Save to repository

            return Error("Dataset upload is not yet implemented", "NOT_IMPLEMENTED", 501);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading dataset");
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

            var userId = GetCurrentUserId();
            _logger.LogDebug("User {UserId} searching datasets with query: {Query}", userId, query);

            var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
            var filteredDataSets = dataSets
                .Where(ds => !ds.IsDeleted)
                .Where(ds => ds.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            (ds.Description != null && ds.Description.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var totalItems = filteredDataSets.Count;
            var pagedDataSets = filteredDataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToResponse)
                .ToList();

            return SuccessPaginated(pagedDataSets, page, pageSize, totalItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching datasets");
            return HandleException(ex, nameof(SearchDataSets));
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
            FileMetadata = dataSet.FileInfo != null ? new FileMetadataResponse
            {
                OriginalFileName = dataSet.FileInfo.FileName,
                StoragePath = dataSet.FileInfo.FilePath,
                FileType = dataSet.FileInfo.FileType.ToString(),
                SizeInBytes = dataSet.FileInfo.FileSize,
                Checksum = dataSet.FileInfo.DataHash ?? string.Empty,
                StorageProvider = dataSet.FileInfo.StorageProvider.ToString()
            } : null,
            Statistics = new DatasetStatisticsResponse
            {
                RowCount = dataSet.Statistics.RowCount,
                ColumnCount = dataSet.Statistics.ColumnCount,
                FileSizeBytes = dataSet.FileInfo?.FileSize ?? 0, // Use file size as stats don't have FileSizeBytes
                LastProcessedAt = dataSet.LastModifiedAt
            }
        };
    }
}