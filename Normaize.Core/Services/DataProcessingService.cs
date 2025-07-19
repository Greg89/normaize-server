using AutoMapper;
using Microsoft.Extensions.Logging;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

namespace Normaize.Core.Services;

/// <summary>
/// Service for managing data processing operations with chaos engineering resilience.
/// Implements industry-standard error handling and distributed tracing.
/// </summary>
public class DataProcessingService : IDataProcessingService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IAuditService _auditService;
    private readonly IMapper _mapper;
    private readonly ILogger<DataProcessingService> _logger;
    private readonly IMemoryCache _cache;
    private readonly Random _chaosRandom;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(10);
    private readonly TimeSpan _quickTimeout = TimeSpan.FromSeconds(30);

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
        _chaosRandom = new Random();
    }

    public async Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(UploadDataSetAsync);
        
        _logger.LogInformation(AppConstants.LogMessages.STARTING_OPERATION_WITH_FILE,
            operationName, fileRequest?.FileName, createDto?.UserId, correlationId);

        // Validate inputs first (before try-catch so exceptions are thrown)
        ValidateUploadInputs(fileRequest!, createDto!);
        
        try
        {
            // Chaos engineering: Simulate processing delay
            if (_chaosRandom.NextDouble() < 0.001) // 0.1% probability
            {
                _logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
                await Task.Delay(_chaosRandom.Next(1000, 5000)); // 1-5 second delay
            }

            // Validate file
            _logger.LogInformation("Validating file {FileName}. CorrelationId: {CorrelationId}", fileRequest!.FileName, correlationId);
            if (!await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ValidateFileAsync(fileRequest),
                _quickTimeout,
                correlationId,
                $"{operationName}_ValidateFile"))
            {
                _logger.LogWarning("File validation failed for {FileName}. CorrelationId: {CorrelationId}", 
                    fileRequest.FileName, correlationId);
                return new DataSetUploadResponse
                {
                    Success = false,
                    Message = "Invalid file format or size"
                };
            }
            _logger.LogInformation("File validation passed for {FileName}. CorrelationId: {CorrelationId}", 
                fileRequest.FileName, correlationId);

            // Save file
            _logger.LogInformation("Saving file {FileName} to storage. CorrelationId: {CorrelationId}", 
                fileRequest.FileName, correlationId);
            var filePath = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.SaveFileAsync(fileRequest),
                _defaultTimeout,
                correlationId,
                $"{operationName}_SaveFile");
            _logger.LogInformation("File saved successfully to {FilePath}. CorrelationId: {CorrelationId}", 
                filePath, correlationId);

            // Process file and create dataset
            _logger.LogInformation("Processing file {FilePath}. CorrelationId: {CorrelationId}", filePath, correlationId);
            var dataSet = await ExecuteWithTimeoutAsync<DataSet>(
                () => _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest.FileName)),
                _defaultTimeout,
                correlationId,
                $"{operationName}_ProcessFile");
            _logger.LogInformation("File processing completed. Rows: {RowCount}, Columns: {ColumnCount}, FileSize: {FileSize}. CorrelationId: {CorrelationId}", 
                dataSet.RowCount, dataSet.ColumnCount, dataSet.FileSize, correlationId);
            
            // Update with user-provided information
            dataSet.Name = createDto!.Name;
            dataSet.Description = createDto.Description;
            dataSet.UserId = createDto.UserId;

            // Save to database
            _logger.LogInformation("Saving dataset to database. CorrelationId: {CorrelationId}", correlationId);
            var savedDataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.AddAsync(dataSet),
                _quickTimeout,
                correlationId,
                $"{operationName}_SaveToDatabase");
            _logger.LogInformation("Dataset saved to database with ID {DataSetId}. CorrelationId: {CorrelationId}", 
                savedDataSet.Id, correlationId);

            // Clear cache for this user
            _cache.Remove($"stats_{createDto.UserId}");

            // Log audit trail
            await ExecuteWithTimeoutAsync(
                () => _auditService.LogDataSetActionAsync(
                    savedDataSet.Id,
                    createDto.UserId,
                    "Created",
                    new { 
                        fileName = fileRequest.FileName,
                        fileSize = dataSet.FileSize,
                        rowCount = dataSet.RowCount,
                        columnCount = dataSet.ColumnCount,
                        filePath = filePath
                    }),
                _quickTimeout,
                correlationId,
                $"{operationName}_AuditLog");

            _logger.LogInformation("File upload completed successfully. Dataset ID: {DataSetId}, File: {FilePath}. CorrelationId: {CorrelationId}", 
                savedDataSet.Id, filePath, correlationId);

            return new DataSetUploadResponse
            {
                DataSetId = savedDataSet.Id,
                Success = true,
                Message = "Dataset uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                operationName, fileRequest?.FileName, createDto?.UserId, correlationId);
            return new DataSetUploadResponse
            {
                Success = false,
                Message = "Error uploading dataset: " + ex.Message
            };
        }
    }

    public async Task<DataSetDto?> GetDataSetAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(GetDataSetAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_WITH_USER,
            operationName, id, userId, correlationId);

        try
        {
            ValidateGetDataSetInputs(id, userId);

            var dataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                operationName);

            if (dataSet?.UserId != userId)
            {
                _logger.LogWarning("Dataset {DataSetId} not found or access denied for user {UserId}. CorrelationId: {CorrelationId}", 
                    id, userId, correlationId);
                return null;
            }

            // Log audit trail for data access
            await ExecuteWithTimeoutAsync(
                () => _auditService.LogDataSetActionAsync(id, userId, "Viewed"),
                _quickTimeout,
                correlationId,
                $"{operationName}_AuditLog");
            
            var result = _mapper.Map<DataSetDto>(dataSet);
            
            _logger.LogDebug("Successfully completed {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}", 
                operationName, id, userId, correlationId);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                operationName, id, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for dataset ID {id}", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByUserAsync(string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(GetDataSetsByUserAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_WITH_PAGINATION,
            operationName, userId, page, pageSize, correlationId);

        try
        {
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);

            var dataSets = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByUserIdAsync(userId, false),
                _quickTimeout,
                correlationId,
                operationName);

            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            
            _logger.LogDebug("Successfully completed {Operation} for user: {UserId}, retrieved {Count} datasets (page {Page}). CorrelationId: {CorrelationId}", 
                operationName, userId, pagedDataSets.Count, page, correlationId);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete {Operation} for user {UserId}. CorrelationId: {CorrelationId}", 
                operationName, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for user {userId}", ex);
        }
    }

    public async Task<bool> DeleteDataSetAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(DeleteDataSetAsync);
        
        _logger.LogInformation(AppConstants.LogMessages.STARTING_OPERATION_WITH_USER,
            operationName, id, userId, correlationId);

        try
        {
            ValidateDeleteInputs(id, userId);

            // Chaos engineering: Simulate deletion failure
            if (_chaosRandom.NextDouble() < 0.0003) // 0.03% probability
            {
                _logger.LogWarning("Chaos engineering: Simulating deletion failure. CorrelationId: {CorrelationId}", correlationId);
                throw new InvalidOperationException("Simulated deletion failure (chaos engineering)");
            }

            var dataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{operationName}_GetDataSet");

            if (dataSet == null || dataSet.UserId != userId)
            {
                _logger.LogWarning("Dataset {DataSetId} not found or access denied for user {UserId}. CorrelationId: {CorrelationId}", 
                    id, userId, correlationId);
                return false;
            }

            // Delete the file
            if (!string.IsNullOrEmpty(dataSet.FilePath))
            {
                try
                {
                    await ExecuteWithTimeoutAsync(
                        () => _fileUploadService.DeleteFileAsync(dataSet.FilePath),
                        _quickTimeout,
                        correlationId,
                        $"{operationName}_DeleteFile");
                    _logger.LogDebug("File deleted successfully: {FilePath}. CorrelationId: {CorrelationId}", 
                        dataSet.FilePath, correlationId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FilePath}, continuing with database deletion. CorrelationId: {CorrelationId}", 
                        dataSet.FilePath, correlationId);
                }
            }

            var result = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.DeleteAsync(id),
                _quickTimeout,
                correlationId,
                $"{operationName}_DeleteFromDatabase");
            
            if (result)
            {
                // Clear cache for this user
                _cache.Remove($"stats_{userId}");

                // Log audit trail for soft delete
                await ExecuteWithTimeoutAsync(
                    () => _auditService.LogDataSetActionAsync(
                        id, 
                        userId, 
                        "Deleted",
                        new { 
                            fileName = dataSet.FileName,
                            fileSize = dataSet.FileSize,
                            rowCount = dataSet.RowCount
                        }),
                    _quickTimeout,
                    correlationId,
                    $"{operationName}_AuditLog");

                _logger.LogInformation("Successfully completed {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}", 
                    operationName, id, userId, correlationId);
            }
            else
            {
                _logger.LogWarning(AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                    operationName, id, userId, correlationId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                operationName, id, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for dataset ID {id}", ex);
        }
    }

    public async Task<bool> RestoreDataSetAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(RestoreDataSetAsync);
        
        _logger.LogInformation(AppConstants.LogMessages.STARTING_OPERATION_WITH_USER,
            operationName, id, userId, correlationId);

        try
        {
            ValidateRestoreInputs(id, userId);

            var dataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{operationName}_GetDataSet");

            if (dataSet == null || dataSet.UserId != userId)
            {
                _logger.LogWarning("Dataset {DataSetId} not found or access denied for user {UserId}. CorrelationId: {CorrelationId}", 
                    id, userId, correlationId);
                return false;
            }

            var result = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.RestoreAsync(id),
                _quickTimeout,
                correlationId,
                $"{operationName}_RestoreFromDatabase");
            
            if (result)
            {
                // Clear cache for this user
                _cache.Remove($"stats_{userId}");

                // Log audit trail for restore
                await ExecuteWithTimeoutAsync(
                    () => _auditService.LogDataSetActionAsync(id, userId, "Restored"),
                    _quickTimeout,
                    correlationId,
                    $"{operationName}_AuditLog");

                _logger.LogInformation("Successfully completed {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}", 
                    operationName, id, userId, correlationId);
            }
            else
            {
                _logger.LogWarning(AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                    operationName, id, userId, correlationId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                operationName, id, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for dataset ID {id}", ex);
        }
    }

    public async Task<bool> HardDeleteDataSetAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(HardDeleteDataSetAsync);
        
        _logger.LogInformation(AppConstants.LogMessages.STARTING_OPERATION_WITH_USER,
            operationName, id, userId, correlationId);

        try
        {
            ValidateHardDeleteInputs(id, userId);

            var dataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{operationName}_GetDataSet");

            if (dataSet == null || dataSet.UserId != userId)
            {
                _logger.LogWarning("Dataset {DataSetId} not found or access denied for user {UserId}. CorrelationId: {CorrelationId}", 
                    id, userId, correlationId);
                return false;
            }

            // Delete the file
            if (!string.IsNullOrEmpty(dataSet.FilePath))
            {
                try
                {
                    await ExecuteWithTimeoutAsync(
                        () => _fileUploadService.DeleteFileAsync(dataSet.FilePath),
                        _quickTimeout,
                        correlationId,
                        $"{operationName}_DeleteFile");
                    _logger.LogDebug("File deleted successfully: {FilePath}. CorrelationId: {CorrelationId}", 
                        dataSet.FilePath, correlationId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FilePath}, continuing with database deletion. CorrelationId: {CorrelationId}", 
                        dataSet.FilePath, correlationId);
                }
            }

            var result = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.HardDeleteAsync(id),
                _quickTimeout,
                correlationId,
                $"{operationName}_HardDeleteFromDatabase");
            
            if (result)
            {
                // Clear cache for this user
                _cache.Remove($"stats_{userId}");

                // Log audit trail for hard delete
                await ExecuteWithTimeoutAsync(
                    () => _auditService.LogDataSetActionAsync(
                        id, 
                        userId, 
                        "HardDeleted",
                        new { 
                            fileName = dataSet.FileName,
                            fileSize = dataSet.FileSize,
                            rowCount = dataSet.RowCount
                        }),
                    _quickTimeout,
                    correlationId,
                    $"{operationName}_AuditLog");

                _logger.LogInformation("Successfully completed {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}", 
                    operationName, id, userId, correlationId);
            }
            else
            {
                _logger.LogWarning(AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                    operationName, id, userId, correlationId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                operationName, id, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for dataset ID {id}", ex);
        }
    }

    public async Task<string?> GetDataSetPreviewAsync(int id, int rows, string userId)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(GetDataSetPreviewAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_WITH_ROWS,
            operationName, id, rows, userId, correlationId);

        try
        {
            ValidatePreviewInputs(id, rows, userId);

            var dataSet = await ExecuteWithTimeoutAsync<DataSet?>(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{operationName}_GetDataSet");

            if (dataSet == null || dataSet.UserId != userId || string.IsNullOrEmpty(dataSet.PreviewData))
            {
                _logger.LogWarning("Dataset {DataSetId} not found, access denied, or no preview data for user {UserId}. CorrelationId: {CorrelationId}", 
                    id, userId, correlationId);
                return null;
            }

            // Log audit trail for preview access
            await ExecuteWithTimeoutAsync(
                () => _auditService.LogDataSetActionAsync(id, userId, "Previewed", new { rows }),
                _quickTimeout,
                correlationId,
                $"{operationName}_AuditLog");

            _logger.LogDebug("Successfully completed {Operation} for ID: {DataSetId}. CorrelationId: {CorrelationId}", 
                operationName, id, correlationId);
            
            return dataSet.PreviewData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                operationName, id, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for dataset ID {id}", ex);
        }
    }

    public async Task<object?> GetDataSetSchemaAsync(int id, string userId)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(GetDataSetSchemaAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_WITH_USER,
            operationName, id, userId, correlationId);

        try
        {
            ValidateSchemaInputs(id, userId);

            var dataSet = await ExecuteWithTimeoutAsync<DataSet?>(
                () => _dataSetRepository.GetByIdAsync(id),
                _quickTimeout,
                correlationId,
                $"{operationName}_GetDataSet");

            if (dataSet == null || dataSet.UserId != userId || string.IsNullOrEmpty(dataSet.Schema))
            {
                _logger.LogWarning("Dataset {DataSetId} not found, access denied, or no schema for user {UserId}. CorrelationId: {CorrelationId}", 
                    id, userId, correlationId);
                return null;
            }

            var schema = await DeserializeSchemaSafelyAsync(dataSet.Schema, id, correlationId);
            
            _logger.LogDebug("Successfully completed {Operation} for ID: {DataSetId}. CorrelationId: {CorrelationId}", 
                operationName, id, correlationId);
            
            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, AppConstants.LogMessages.OPERATION_FAILED_WITH_USER, 
                operationName, id, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for dataset ID {id}", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDeletedDataSetsAsync(string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(GetDeletedDataSetsAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_WITH_PAGINATION,
            operationName, userId, page, pageSize, correlationId);

        try
        {
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);

            var dataSets = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.GetByUserIdAsync(userId, includeDeleted: true),
                _quickTimeout,
                correlationId,
                operationName);

            var deletedDataSets = dataSets
                .Where(d => d.IsDeleted)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = _mapper.Map<IEnumerable<DataSetDto>>(deletedDataSets);
            
            _logger.LogDebug("Successfully completed {Operation} for user: {UserId}, retrieved {Count} deleted datasets (page {Page}). CorrelationId: {CorrelationId}", 
                operationName, userId, deletedDataSets.Count, page, correlationId);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete {Operation} for user {UserId}. CorrelationId: {CorrelationId}", 
                operationName, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for user {userId}", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> SearchDataSetsAsync(string searchTerm, string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(SearchDataSetsAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_WITH_SEARCH,
            operationName, userId, searchTerm, page, pageSize, correlationId);

        try
        {
            ValidateSearchInputs(searchTerm, userId, page, pageSize);

            var dataSets = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.SearchAsync(searchTerm, userId),
                _quickTimeout,
                correlationId,
                operationName);

            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            
            _logger.LogDebug("Successfully completed {Operation} for user: {UserId}, found {Count} datasets matching '{SearchTerm}' (page {Page}). CorrelationId: {CorrelationId}", 
                operationName, userId, pagedDataSets.Count, searchTerm, page, correlationId);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete {Operation} for user {UserId} with term '{SearchTerm}'. CorrelationId: {CorrelationId}", 
                operationName, userId, searchTerm, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for user {userId} with search term '{searchTerm}'", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByFileTypeAsync(FileType fileType, string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(GetDataSetsByFileTypeAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_WITH_FILETYPE,
            operationName, fileType, userId, page, pageSize, correlationId);

        try
        {
            ValidatePaginationInputs(page, pageSize);
            ValidateUserId(userId);

            var dataSets = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.GetByFileTypeAsync(fileType, userId),
                _quickTimeout,
                correlationId,
                operationName);

            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            
            _logger.LogDebug("Successfully completed {Operation} for file type {FileType}, user: {UserId}, retrieved {Count} datasets (page {Page}). CorrelationId: {CorrelationId}", 
                operationName, fileType, userId, pagedDataSets.Count, page, correlationId);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete {Operation} for file type {FileType}, user {UserId}. CorrelationId: {CorrelationId}", 
                operationName, fileType, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for file type {fileType}, user {userId}", ex);
        }
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate, string userId, int page = 1, int pageSize = 20)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(GetDataSetsByDateRangeAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_WITH_DATERANGE,
            operationName, startDate, endDate, userId, page, pageSize, correlationId);

        try
        {
            ValidateDateRangeInputs(startDate, endDate, userId, page, pageSize);

            var dataSets = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.GetByDateRangeAsync(startDate, endDate, userId),
                _quickTimeout,
                correlationId,
                operationName);

            var pagedDataSets = dataSets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            
            _logger.LogDebug("Successfully completed {Operation} for date range, user: {UserId}, retrieved {Count} datasets (page {Page}). CorrelationId: {CorrelationId}", 
                operationName, userId, pagedDataSets.Count, page, correlationId);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete {Operation} for date range, user {UserId}. CorrelationId: {CorrelationId}", 
                operationName, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for date range, user {userId}", ex);
        }
    }

    public async Task<DataSetStatisticsDto> GetDataSetStatisticsAsync(string userId)
    {
        var correlationId = GetCorrelationId();
        var operationName = nameof(GetDataSetStatisticsAsync);
        
        _logger.LogDebug(AppConstants.LogMessages.STARTING_OPERATION_FOR_STATISTICS,
            operationName, userId, correlationId);

        try
        {
            ValidateUserId(userId);

            // Chaos engineering: Simulate cache corruption
            if (_chaosRandom.NextDouble() < 0.0005) // 0.05% probability
            {
                _logger.LogWarning("Chaos engineering: Simulating cache corruption. CorrelationId: {CorrelationId}", correlationId);
                _cache.Remove($"stats_{userId}");
            }

            var cacheKey = $"stats_{userId}";
            
            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out DataSetStatisticsDto? cachedStats))
            {
                _logger.LogDebug("Retrieved statistics from cache for user {UserId}. CorrelationId: {CorrelationId}", 
                    userId, correlationId);
                return cachedStats!;
            }

            _logger.LogDebug("Calculating statistics for user {UserId}. CorrelationId: {CorrelationId}", 
                userId, correlationId);

            var totalCount = await ExecuteWithTimeoutAsync<int>(
                () => _dataSetRepository.GetTotalCountAsync(userId),
                _quickTimeout,
                correlationId,
                $"{operationName}_GetTotalCount");

            var totalSize = await ExecuteWithTimeoutAsync<long>(
                () => _dataSetRepository.GetTotalSizeAsync(userId),
                _quickTimeout,
                correlationId,
                $"{operationName}_GetTotalSize");

            var recentlyModified = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                () => _dataSetRepository.GetRecentlyModifiedAsync(userId, 5),
                _quickTimeout,
                correlationId,
                $"{operationName}_GetRecentlyModified");

            var statistics = new DataSetStatisticsDto
            {
                TotalCount = totalCount,
                TotalSize = totalSize,
                RecentlyModified = _mapper.Map<IEnumerable<DataSetDto>>(recentlyModified)
            };

            // Cache the results
            _cache.Set(cacheKey, statistics, _cacheExpiration);

            _logger.LogDebug("Successfully completed {Operation} for user {UserId}: {TotalCount} datasets, {TotalSize} bytes. CorrelationId: {CorrelationId}", 
                operationName, userId, totalCount, totalSize, correlationId);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete {Operation} for user {UserId}. CorrelationId: {CorrelationId}", 
                operationName, userId, correlationId);
            throw new InvalidOperationException($"Failed to complete {operationName} for user {userId}", ex);
        }
    }

    #region Private Methods

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private async Task ExecuteWithTimeoutAsync(Func<Task> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private async Task<object?> DeserializeSchemaSafelyAsync(string schema, int dataSetId, string correlationId)
    {
        try
        {
            return await Task.Run(() => JsonSerializer.Deserialize<object>(schema));
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx, "Failed to deserialize schema for dataset {DataSetId}. CorrelationId: {CorrelationId}", 
                dataSetId, correlationId);
            return null;
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateUploadInputs(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        ArgumentNullException.ThrowIfNull(fileRequest);
        ArgumentNullException.ThrowIfNull(createDto);

        if (string.IsNullOrWhiteSpace(fileRequest.FileName))
            throw new ArgumentException("File name is required", nameof(fileRequest));

        if (string.IsNullOrWhiteSpace(createDto.Name))
            throw new ArgumentException("Dataset name is required", nameof(createDto));

        if (string.IsNullOrWhiteSpace(createDto.UserId))
            throw new ArgumentException("User ID is required", nameof(createDto));

        // Security: Validate file path to prevent directory traversal
        if (fileRequest.FileName.Contains("..") || fileRequest.FileName.Contains("/") || fileRequest.FileName.Contains("\\"))
            throw new ArgumentException("Invalid file name", nameof(fileRequest));
    }

    private static void ValidateGetDataSetInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateDeleteInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateRestoreInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateHardDeleteInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidatePreviewInputs(int id, int rows, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        if (rows <= 0 || rows > 1000)
            throw new ArgumentException("Rows must be between 1 and 1000", nameof(rows));
        
        ValidateUserId(userId);
    }

    private static void ValidateSchemaInputs(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateSearchInputs(string searchTerm, string userId, int page, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term is required", nameof(searchTerm));
        
        if (searchTerm.Length > 100)
            throw new ArgumentException("Search term cannot exceed 100 characters", nameof(searchTerm));
        
        ValidateUserId(userId);
        ValidatePaginationInputs(page, pageSize);
    }

    private static void ValidateDateRangeInputs(DateTime startDate, DateTime endDate, string userId, int page, int pageSize)
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

    private static void ValidatePaginationInputs(int page, int pageSize)
    {
        if (page <= 0)
            throw new ArgumentException("Page must be positive", nameof(page));
        
        if (pageSize <= 0 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));
    }

    private static void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        
        if (userId.Length > 100)
            throw new ArgumentException("User ID cannot exceed 100 characters", nameof(userId));
    }

    #endregion
} 