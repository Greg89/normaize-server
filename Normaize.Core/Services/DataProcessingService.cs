using AutoMapper;
using Microsoft.Extensions.Logging;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Services;

public class DataProcessingService : IDataProcessingService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<DataProcessingService> _logger;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public DataProcessingService(
        IDataSetRepository dataSetRepository,
        IFileUploadService fileUploadService,
        IAuditService auditService,
        IMapper mapper,
        ILogger<DataProcessingService> logger,
        IMemoryCache cache)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _fileUploadService = fileUploadService ?? throw new ArgumentNullException(nameof(fileUploadService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        try
        {
            _logger.LogInformation("Starting file upload process for {FileName} by user {UserId}", 
                fileRequest.FileName, createDto.UserId);

            // Validate inputs
            ValidateUploadInputs(fileRequest, createDto);

            // Validate file
            _logger.LogInformation("Validating file {FileName}", fileRequest.FileName);
            if (!await _fileUploadService.ValidateFileAsync(fileRequest))
            {
                _logger.LogWarning("File validation failed for {FileName}", fileRequest.FileName);
                return new DataSetUploadResponse
                {
                    Success = false,
                    Message = "Invalid file format or size"
                };
            }
            _logger.LogInformation("File validation passed for {FileName}", fileRequest.FileName);

            // Save file
            _logger.LogInformation("Saving file {FileName} to storage", fileRequest.FileName);
            var filePath = await _fileUploadService.SaveFileAsync(fileRequest);
            _logger.LogInformation("File saved successfully to {FilePath}", filePath);

            // Process file and create dataset
            _logger.LogInformation("Processing file {FilePath}", filePath);
            var dataSet = await _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest.FileName));
            _logger.LogInformation("File processing completed. Rows: {RowCount}, Columns: {ColumnCount}, FileSize: {FileSize}", 
                dataSet.RowCount, dataSet.ColumnCount, dataSet.FileSize);
            
            // Update with user-provided information
            dataSet.Name = createDto.Name;
            dataSet.Description = createDto.Description;
            dataSet.UserId = createDto.UserId;

            // Save to database
            _logger.LogInformation("Saving dataset to database");
            var savedDataSet = await _dataSetRepository.AddAsync(dataSet);
            _logger.LogInformation("Dataset saved to database with ID {DataSetId}", savedDataSet.Id);

            // Clear cache for this user
            _cache.Remove($"stats_{createDto.UserId}");

            // Log audit trail
            await _auditService.LogDataSetActionAsync(
                savedDataSet.Id,
                createDto.UserId,
                "Created",
                new { 
                    fileName = fileRequest.FileName,
                    fileSize = dataSet.FileSize,
                    rowCount = dataSet.RowCount,
                    columnCount = dataSet.ColumnCount,
                    filePath = filePath
                }
            );

            _logger.LogInformation("File upload completed successfully. Dataset ID: {DataSetId}, File: {FilePath}", 
                savedDataSet.Id, filePath);

            return new DataSetUploadResponse
            {
                DataSetId = savedDataSet.Id,
                Success = true,
                Message = "Dataset uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading dataset {FileName} for user {UserId}", 
                fileRequest.FileName, createDto.UserId);
            return new DataSetUploadResponse
            {
                Success = false,
                Message = "Error uploading dataset: " + ex.Message
            };
        }
    }

    public async Task<DataSetDto?> GetDataSetAsync(int id, string userId)
    {
        try
        {
            ValidateGetDataSetInputs(id, userId);

            _logger.LogDebug("Retrieving dataset with ID: {DataSetId} for user: {UserId}", id, userId);

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet?.UserId != userId)
            {
                _logger.LogWarning("Dataset {DataSetId} not found or access denied for user {UserId}", id, userId);
                return null;
            }

            // Log audit trail for data access
            await _auditService.LogDataSetActionAsync(id, userId, "Viewed");
            
            _logger.LogDebug("Successfully retrieved dataset {DataSetId} for user {UserId}", id, userId);
            return _mapper.Map<DataSetDto>(dataSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dataset {DataSetId} for user {UserId}", id, userId);
            throw;
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByUserAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);

            _logger.LogDebug("Retrieving datasets for user: {UserId}, page: {Page}, pageSize: {PageSize}", 
                userId, page, pageSize);

            var dataSets = await _dataSetRepository.GetByUserIdAsync(userId);
            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogDebug("Retrieved {Count} datasets for user {UserId} (page {Page})", 
                pagedDataSets.Count, userId, page);

            return _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving datasets for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteDataSetAsync(int id, string userId)
    {
        try
        {
            ValidateDeleteInputs(id, userId);

            _logger.LogInformation("Attempting to delete dataset {DataSetId} for user {UserId}", id, userId);

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId)
            {
                _logger.LogWarning("Dataset {DataSetId} not found or access denied for user {UserId}", id, userId);
                return false;
            }

            // Delete the file
            if (!string.IsNullOrEmpty(dataSet.FilePath))
            {
                try
                {
                    await _fileUploadService.DeleteFileAsync(dataSet.FilePath);
                    _logger.LogDebug("File deleted successfully: {FilePath}", dataSet.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FilePath}, continuing with database deletion", dataSet.FilePath);
                }
            }

            var result = await _dataSetRepository.DeleteAsync(id);
            
            if (result)
            {
                // Clear cache for this user
                _cache.Remove($"stats_{userId}");

                // Log audit trail for soft delete
                await _auditService.LogDataSetActionAsync(
                    id, 
                    userId, 
                    "Deleted",
                    new { 
                        fileName = dataSet.FileName,
                        fileSize = dataSet.FileSize,
                        rowCount = dataSet.RowCount
                    }
                );

                _logger.LogInformation("Successfully deleted dataset {DataSetId} for user {UserId}", id, userId);
            }
            else
            {
                _logger.LogWarning("Failed to delete dataset {DataSetId} for user {UserId}", id, userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dataset {DataSetId} for user {UserId}", id, userId);
            throw;
        }
    }

    public async Task<bool> RestoreDataSetAsync(int id, string userId)
    {
        try
        {
            ValidateRestoreInputs(id, userId);

            _logger.LogInformation("Attempting to restore dataset {DataSetId} for user {UserId}", id, userId);

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId)
            {
                _logger.LogWarning("Dataset {DataSetId} not found or access denied for user {UserId}", id, userId);
                return false;
            }

            var result = await _dataSetRepository.RestoreAsync(id);
            
            if (result)
            {
                // Clear cache for this user
                _cache.Remove($"stats_{userId}");

                // Log audit trail for restore
                await _auditService.LogDataSetActionAsync(id, userId, "Restored");

                _logger.LogInformation("Successfully restored dataset {DataSetId} for user {UserId}", id, userId);
            }
            else
            {
                _logger.LogWarning("Failed to restore dataset {DataSetId} for user {UserId}", id, userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring dataset {DataSetId} for user {UserId}", id, userId);
            throw;
        }
    }

    public async Task<bool> HardDeleteDataSetAsync(int id, string userId)
    {
        try
        {
            ValidateHardDeleteInputs(id, userId);

            _logger.LogInformation("Attempting hard delete of dataset {DataSetId} for user {UserId}", id, userId);

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId)
            {
                _logger.LogWarning("Dataset {DataSetId} not found or access denied for user {UserId}", id, userId);
                return false;
            }

            // Delete the file
            if (!string.IsNullOrEmpty(dataSet.FilePath))
            {
                try
                {
                    await _fileUploadService.DeleteFileAsync(dataSet.FilePath);
                    _logger.LogDebug("File deleted successfully: {FilePath}", dataSet.FilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FilePath}, continuing with database deletion", dataSet.FilePath);
                }
            }

            var result = await _dataSetRepository.HardDeleteAsync(id);
            
            if (result)
            {
                // Clear cache for this user
                _cache.Remove($"stats_{userId}");

                // Log audit trail for hard delete
                await _auditService.LogDataSetActionAsync(
                    id, 
                    userId, 
                    "HardDeleted",
                    new { 
                        fileName = dataSet.FileName,
                        fileSize = dataSet.FileSize,
                        rowCount = dataSet.RowCount
                    }
                );

                _logger.LogInformation("Successfully hard deleted dataset {DataSetId} for user {UserId}", id, userId);
            }
            else
            {
                _logger.LogWarning("Failed to hard delete dataset {DataSetId} for user {UserId}", id, userId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hard deleting dataset {DataSetId} for user {UserId}", id, userId);
            throw;
        }
    }

    public async Task<string?> GetDataSetPreviewAsync(int id, int rows, string userId)
    {
        try
        {
            ValidatePreviewInputs(id, rows, userId);

            _logger.LogDebug("Retrieving preview for dataset {DataSetId}, rows: {Rows}, user: {UserId}", id, rows, userId);

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId || string.IsNullOrEmpty(dataSet.PreviewData))
            {
                _logger.LogWarning("Dataset {DataSetId} not found, access denied, or no preview data for user {UserId}", id, userId);
                return null;
            }

            // Log audit trail for preview access
            await _auditService.LogDataSetActionAsync(id, userId, "Previewed", new { rows });

            _logger.LogDebug("Successfully retrieved preview for dataset {DataSetId}", id);
            return dataSet.PreviewData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving preview for dataset {DataSetId} for user {UserId}", id, userId);
            throw;
        }
    }

    public async Task<object?> GetDataSetSchemaAsync(int id, string userId)
    {
        try
        {
            ValidateSchemaInputs(id, userId);

            _logger.LogDebug("Retrieving schema for dataset {DataSetId} for user {UserId}", id, userId);

            var dataSet = await _dataSetRepository.GetByIdAsync(id);
            if (dataSet == null || dataSet.UserId != userId || string.IsNullOrEmpty(dataSet.Schema))
            {
                _logger.LogWarning("Dataset {DataSetId} not found, access denied, or no schema for user {UserId}", id, userId);
                return null;
            }

            try
            {
                var schema = JsonSerializer.Deserialize<object>(dataSet.Schema);
                _logger.LogDebug("Successfully retrieved schema for dataset {DataSetId}", id);
                return schema;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogWarning(jsonEx, "Failed to deserialize schema for dataset {DataSetId}", id);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schema for dataset {DataSetId} for user {UserId}", id, userId);
            throw;
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDeletedDataSetsAsync(string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);

            _logger.LogDebug("Retrieving deleted datasets for user: {UserId}, page: {Page}, pageSize: {PageSize}", 
                userId, page, pageSize);

            var dataSets = await _dataSetRepository.GetByUserIdAsync(userId, includeDeleted: true);
            var deletedDataSets = dataSets
                .Where(d => d.IsDeleted)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogDebug("Retrieved {Count} deleted datasets for user {UserId} (page {Page})", 
                deletedDataSets.Count, userId, page);

            return _mapper.Map<IEnumerable<DataSetDto>>(deletedDataSets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deleted datasets for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<DataSetDto>> SearchDataSetsAsync(string searchTerm, string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            ValidateSearchInputs(searchTerm, userId, page, pageSize);

            _logger.LogDebug("Searching datasets for user: {UserId}, term: '{SearchTerm}', page: {Page}, pageSize: {PageSize}", 
                userId, searchTerm, page, pageSize);

            var dataSets = await _dataSetRepository.SearchAsync(searchTerm, userId);
            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogDebug("Found {Count} datasets matching '{SearchTerm}' for user {UserId} (page {Page})", 
                pagedDataSets.Count, searchTerm, userId, page);

            return _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching datasets for user {UserId} with term '{SearchTerm}'", userId, searchTerm);
            throw;
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByFileTypeAsync(FileType fileType, string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);

            _logger.LogDebug("Retrieving datasets with file type {FileType} for user: {UserId}, page: {Page}, pageSize: {PageSize}", 
                fileType, userId, page, pageSize);

            var dataSets = await _dataSetRepository.GetByFileTypeAsync(fileType, userId);
            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogDebug("Retrieved {Count} datasets with file type {FileType} for user {UserId} (page {Page})", 
                pagedDataSets.Count, fileType, userId, page);

            return _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving datasets with file type {FileType} for user {UserId}", fileType, userId);
            throw;
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate, string userId, int page = 1, int pageSize = 20)
    {
        try
        {
            ValidateDateRangeInputs(startDate, endDate, userId, page, pageSize);

            _logger.LogDebug("Retrieving datasets in date range {StartDate} to {EndDate} for user: {UserId}, page: {Page}, pageSize: {PageSize}", 
                startDate, endDate, userId, page, pageSize);

            var dataSets = await _dataSetRepository.GetByDateRangeAsync(startDate, endDate, userId);
            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogDebug("Retrieved {Count} datasets in date range for user {UserId} (page {Page})", 
                pagedDataSets.Count, userId, page);

            return _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving datasets in date range for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DataSetStatisticsDto> GetDataSetStatisticsAsync(string userId)
    {
        try
        {
            ValidateUserId(userId);

            var cacheKey = $"stats_{userId}";
            
            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out DataSetStatisticsDto? cachedStats))
            {
                _logger.LogDebug("Retrieved statistics from cache for user {UserId}", userId);
                return cachedStats!;
            }

            _logger.LogDebug("Calculating statistics for user {UserId}", userId);

            var totalCount = await _dataSetRepository.GetTotalCountAsync(userId);
            var totalSize = await _dataSetRepository.GetTotalSizeAsync(userId);
            var recentlyModified = await _dataSetRepository.GetRecentlyModifiedAsync(userId, 5);

            var statistics = new DataSetStatisticsDto
            {
                TotalCount = totalCount,
                TotalSize = totalSize,
                RecentlyModified = _mapper.Map<IEnumerable<DataSetDto>>(recentlyModified)
            };

            // Cache the results
            _cache.Set(cacheKey, statistics, _cacheExpiration);

            _logger.LogDebug("Calculated statistics for user {UserId}: {TotalCount} datasets, {TotalSize} bytes", 
                userId, totalCount, totalSize);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for user {UserId}", userId);
            throw;
        }
    }

    #region Validation Methods

    private void ValidateUploadInputs(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        if (fileRequest == null)
            throw new ArgumentNullException(nameof(fileRequest));
        
        if (createDto == null)
            throw new ArgumentNullException(nameof(createDto));

        if (string.IsNullOrWhiteSpace(fileRequest.FileName))
            throw new ArgumentException("File name is required", nameof(fileRequest.FileName));

        if (string.IsNullOrWhiteSpace(createDto.Name))
            throw new ArgumentException("Dataset name is required", nameof(createDto.Name));

        if (string.IsNullOrWhiteSpace(createDto.UserId))
            throw new ArgumentException("User ID is required", nameof(createDto.UserId));

        // Security: Validate file path to prevent directory traversal
        if (fileRequest.FileName.Contains("..") || fileRequest.FileName.Contains("/") || fileRequest.FileName.Contains("\\"))
            throw new ArgumentException("Invalid file name", nameof(fileRequest.FileName));
    }

    private void ValidateGetDataSetInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException("Dataset ID must be positive", nameof(id));
        
        ValidateUserId(userId);
    }

    private void ValidateDeleteInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException("Dataset ID must be positive", nameof(id));
        
        ValidateUserId(userId);
    }

    private void ValidateRestoreInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException("Dataset ID must be positive", nameof(id));
        
        ValidateUserId(userId);
    }

    private void ValidateHardDeleteInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException("Dataset ID must be positive", nameof(id));
        
        ValidateUserId(userId);
    }

    private void ValidatePreviewInputs(int id, int rows, string userId)
    {
        if (id <= 0)
            throw new ArgumentException("Dataset ID must be positive", nameof(id));
        
        if (rows <= 0 || rows > 1000)
            throw new ArgumentException("Rows must be between 1 and 1000", nameof(rows));
        
        ValidateUserId(userId);
    }

    private void ValidateSchemaInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException("Dataset ID must be positive", nameof(id));
        
        ValidateUserId(userId);
    }

    private void ValidateSearchInputs(string searchTerm, string userId, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term is required", nameof(searchTerm));
        
        if (searchTerm.Length > 100)
            throw new ArgumentException("Search term cannot exceed 100 characters", nameof(searchTerm));
        
        ValidateUserId(userId);
        ValidatePaginationInputs(page, pageSize);
    }

    private void ValidateDateRangeInputs(DateTime startDate, DateTime endDate, string userId, int page, int pageSize)
    {
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");
        
        if (endDate > DateTime.UtcNow.AddDays(1))
            throw new ArgumentException("End date cannot be in the future");
        
        if (startDate < DateTime.UtcNow.AddYears(-10))
            throw new ArgumentException("Start date cannot be more than 10 years ago");
        
        ValidateUserId(userId);
        ValidatePaginationInputs(page, pageSize);
    }

    private void ValidatePaginationInputs(int page, int pageSize)
    {
        if (page <= 0)
            throw new ArgumentException("Page must be positive", nameof(page));
        
        if (pageSize <= 0 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
    }

    private void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        
        if (userId.Length > 100)
            throw new ArgumentException("User ID cannot exceed 100 characters", nameof(userId));
    }

    #endregion
} 