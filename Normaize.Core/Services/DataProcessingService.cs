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
    private readonly IDataProcessingInfrastructure _infrastructure;

    public DataProcessingService(
        IDataSetRepository dataSetRepository,
        IFileUploadService fileUploadService,
        IAuditService auditService,
        IMapper mapper,
        IDataProcessingInfrastructure infrastructure)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _fileUploadService = fileUploadService ?? throw new ArgumentNullException(nameof(fileUploadService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _infrastructure = infrastructure ?? throw new ArgumentNullException(nameof(infrastructure));
    }

    public async Task<DataSetUploadResponse> UploadDataSetAsync(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(
            nameof(UploadDataSetAsync), 
            correlationId, 
            createDto?.UserId,
            new Dictionary<string, object>
            {
                ["FileName"] = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN
            });

        // Validate inputs first (before try-catch so exceptions are thrown)
        ValidateUploadInputs(fileRequest!, createDto!);
        
        try
        {
            // Chaos engineering: Simulate processing delay
            await _infrastructure.ChaosEngineering.ExecuteChaosAsync("ProcessingDelay", correlationId, context.OperationName, async () =>
            {
                var delayMs = new Random().Next(1000, 5000);
                _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering delay", new Dictionary<string, object>
                {
                    ["DelayMs"] = delayMs
                });
                await Task.Delay(delayMs);
            }, new Dictionary<string, object> { ["UserId"] = createDto?.UserId ?? AppConstants.Messages.UNKNOWN });

            // Validate file
            _infrastructure.StructuredLogging.LogStep(context, "File validation started");
            if (!await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ValidateFileAsync(fileRequest!),
                _infrastructure.QuickTimeout,
                correlationId,
                $"{context.OperationName}_ValidateFile"))
            {
                _infrastructure.StructuredLogging.LogStep(context, "File validation failed");
                _infrastructure.StructuredLogging.LogSummary(context, false, "Invalid file format or size");
                return new DataSetUploadResponse
                {
                    Success = false,
                    Message = "Invalid file format or size"
                };
            }
            _infrastructure.StructuredLogging.LogStep(context, "File validation passed");

            // Save file
            _infrastructure.StructuredLogging.LogStep(context, "File save started");
            var filePath = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.SaveFileAsync(fileRequest!),
                _infrastructure.DefaultTimeout,
                correlationId,
                $"{context.OperationName}_SaveFile");
            _infrastructure.StructuredLogging.LogStep(context, "File saved", new Dictionary<string, object>
            {
                ["FilePath"] = filePath
            });

            // Process file and create dataset
            _infrastructure.StructuredLogging.LogStep(context, "File processing started");
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest!.FileName)),
                _infrastructure.DefaultTimeout,
                correlationId,
                $"{context.OperationName}_ProcessFile");
            _infrastructure.StructuredLogging.LogStep(context, "File processing completed", new Dictionary<string, object>
            {
                ["RowCount"] = dataSet.RowCount,
                ["ColumnCount"] = dataSet.ColumnCount,
                ["FileSize"] = dataSet.FileSize
            });
            
            // Update with user-provided information
            dataSet.Name = createDto!.Name ?? "Unnamed Dataset";
            dataSet.Description = createDto.Description ?? string.Empty;
            dataSet.UserId = createDto.UserId ?? AppConstants.Messages.UNKNOWN;

            // Save to database
            _infrastructure.StructuredLogging.LogStep(context, "Database save started");
            var savedDataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.AddAsync(dataSet),
                _infrastructure.QuickTimeout,
                correlationId,
                $"{context.OperationName}_SaveToDatabase");
            context.SetMetadata(AppConstants.DataStructures.DATASET_ID, savedDataSet.Id);
            _infrastructure.StructuredLogging.LogStep(context, "Database save completed");

            // Clear cache for this user
            _infrastructure.Cache.Remove($"stats_{createDto.UserId}");

            // Log audit trail
            await ExecuteWithTimeoutAsync(
                () => _auditService.LogDataSetActionAsync(
                    savedDataSet.Id,
                    createDto.UserId ?? AppConstants.Messages.UNKNOWN,
                    "Created",
                    new { 
                        fileName = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN,
                        fileSize = dataSet.FileSize,
                        rowCount = dataSet.RowCount,
                        columnCount = dataSet.ColumnCount,
                        filePath = filePath
                    }),
                _infrastructure.QuickTimeout,
                correlationId,
                $"{context.OperationName}_AuditLog");

            _infrastructure.StructuredLogging.LogSummary(context, true);

            return new DataSetUploadResponse
            {
                DataSetId = savedDataSet.Id,
                Success = true,
                Message = "Dataset uploaded successfully"
            };
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogSummary(context, false, ex.Message);
            return new DataSetUploadResponse
            {
                Success = false,
                Message = "Error uploading dataset: " + ex.Message
            };
        }
    }

    public async Task<DataSetDto?> GetDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(GetDataSetAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.DATASET_ID] = id },
            validation: () => ValidateGetDataSetInputs(id, userId),
            operation: async (context) =>
            {
                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) return null;

                await LogAuditActionAsync(id, userId, "Viewed", context);
                return _mapper.Map<DataSetDto>(dataSet);
            });
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByUserAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(GetDataSetsByUserAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.DataStructures.PAGE] = page,
                [AppConstants.DataStructures.PAGE_SIZE] = pageSize
            },
            validation: () => 
            {
                ValidatePaginationInputs(page, pageSize);
                ValidateUserId(userId);
            },
            operation: async (context) =>
            {
                            var dataSets = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.GetByUserIdAsync(userId, false),
                _infrastructure.QuickTimeout,
                GetCorrelationId(),
                context.OperationName);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.TOTAL_DATASETS] = dataSets.Count()
                });

                var pagedDataSets = ApplyPagination(dataSets, page, pageSize, context);
                return _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            });
    }

    public async Task<bool> DeleteDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(DeleteDataSetAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.DATASET_ID] = id },
            validation: () => ValidateDeleteInputs(id, userId),
            operation: async (context) =>
            {
                            // Chaos engineering: Simulate deletion failure
            await _infrastructure.ChaosEngineering.ExecuteChaosAsync("DeletionFailure", () =>
            {
                _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating deletion failure", new Dictionary<string, object>
                {
                    ["ChaosType"] = "DeletionFailure"
                });
                throw new InvalidOperationException("Simulated deletion failure (chaos engineering)");
            }, new Dictionary<string, object> { ["UserId"] = userId });

                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) return false;

                await DeleteDataSetFileAsync(dataSet, context);
                
                var result = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.DeleteAsync(id),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_DeleteFromDatabase");

                if (result)
                {
                    await HandleSuccessfulDeletionAsync(id, userId, dataSet, context);
                }

                return result;
            });
    }

    public async Task<bool> RestoreDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(RestoreDataSetAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.DATASET_ID] = id },
            validation: () => ValidateRestoreInputs(id, userId),
            operation: async (context) =>
            {
                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) return false;

                var result = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.RestoreAsync(id),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_RestoreFromDatabase");

                if (result)
                {
                    await HandleSuccessfulRestoreAsync(id, userId, context);
                }

                return result;
            });
    }

    public async Task<bool> HardDeleteDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(HardDeleteDataSetAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.DATASET_ID] = id },
            validation: () => ValidateHardDeleteInputs(id, userId),
            operation: async (context) =>
            {
                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) return false;

                await DeleteDataSetFileAsync(dataSet, context);

                var result = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.HardDeleteAsync(id),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_HardDeleteFromDatabase");

                if (result)
                {
                    await HandleSuccessfulHardDeleteAsync(id, userId, dataSet, context);
                }

                return result;
            });
    }

    public async Task<string?> GetDataSetPreviewAsync(int id, int rows, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(GetDataSetPreviewAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASET_ID] = id,
                ["RequestedRows"] = rows
            },
            validation: () => ValidatePreviewInputs(id, rows, userId),
            operation: async (context) =>
            {
                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null || string.IsNullOrEmpty(dataSet.PreviewData))
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Access denied or no preview data", new Dictionary<string, object>
                    {
                        ["HasPreviewData"] = !string.IsNullOrEmpty(dataSet?.PreviewData)
                    });
                    return null;
                }

                await LogAuditActionAsync(id, userId, "Previewed", context, new { rows });
                return dataSet.PreviewData;
            });
    }

    public async Task<object?> GetDataSetSchemaAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(GetDataSetSchemaAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object> { [AppConstants.DataStructures.DATASET_ID] = id },
            validation: () => ValidateSchemaInputs(id, userId),
            operation: async (context) =>
            {
                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null || string.IsNullOrEmpty(dataSet.Schema))
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Access denied or no schema", new Dictionary<string, object>
                    {
                        ["HasSchema"] = !string.IsNullOrEmpty(dataSet?.Schema)
                    });
                    return null;
                }

                var schema = await DeserializeSchemaSafelyAsync(dataSet.Schema, id, GetCorrelationId());
                return schema;
            });
    }

    public async Task<IEnumerable<DataSetDto>> GetDeletedDataSetsAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(GetDeletedDataSetsAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.DataStructures.PAGE] = page,
                [AppConstants.DataStructures.PAGE_SIZE] = pageSize
            },
            validation: () => 
            {
                ValidatePaginationInputs(page, pageSize);
                ValidateUserId(userId);
            },
            operation: async (context) =>
            {
                var dataSets = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.GetByUserIdAsync(userId, includeDeleted: true),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    context.OperationName);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.TOTAL_DATASETS] = dataSets.Count()
                });

                var deletedDataSets = dataSets
                    .Where(d => d.IsDeleted)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _infrastructure.StructuredLogging.LogStep(context, "Filtering deleted datasets completed", new Dictionary<string, object>
                {
                    ["DeletedDataSets"] = deletedDataSets.Count
                });

                return _mapper.Map<IEnumerable<DataSetDto>>(deletedDataSets);
            });
    }

    public async Task<IEnumerable<DataSetDto>> SearchDataSetsAsync(string searchTerm, string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(SearchDataSetsAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.DataStructures.SEARCH_TERM] = searchTerm,
                [AppConstants.DataStructures.PAGE] = page,
                [AppConstants.DataStructures.PAGE_SIZE] = pageSize
            },
            validation: () => ValidateSearchInputs(searchTerm, userId, page, pageSize),
            operation: async (context) =>
            {
                var dataSets = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.SearchAsync(searchTerm, userId),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    context.OperationName);

                _infrastructure.StructuredLogging.LogStep(context, "Search operation completed", new Dictionary<string, object>
                {
                    ["TotalMatches"] = dataSets.Count()
                });

                var pagedDataSets = ApplyPagination(dataSets, page, pageSize, context);
                return _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            });
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByFileTypeAsync(FileType fileType, string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(GetDataSetsByFileTypeAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.DataStructures.FILE_TYPE] = fileType.ToString(),
                [AppConstants.DataStructures.PAGE] = page,
                [AppConstants.DataStructures.PAGE_SIZE] = pageSize
            },
            validation: () => 
            {
                ValidatePaginationInputs(page, pageSize);
                ValidateUserId(userId);
            },
            operation: async (context) =>
            {
                var dataSets = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.GetByFileTypeAsync(fileType, userId),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    context.OperationName);

                _infrastructure.StructuredLogging.LogStep(context, "Database retrieval by file type completed", new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.TOTAL_DATASETS] = dataSets.Count()
                });

                var pagedDataSets = ApplyPagination(dataSets, page, pageSize, context);
                return _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            });
    }

    public async Task<IEnumerable<DataSetDto>> GetDataSetsByDateRangeAsync(DateTime startDate, DateTime endDate, string userId, int page = 1, int pageSize = 20)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(GetDataSetsByDateRangeAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.DataStructures.START_DATE] = startDate.ToString("yyyy-MM-dd"),
                [AppConstants.DataStructures.END_DATE] = endDate.ToString("yyyy-MM-dd"),
                [AppConstants.DataStructures.PAGE] = page,
                [AppConstants.DataStructures.PAGE_SIZE] = pageSize
            },
            validation: () => ValidateDateRangeInputs(startDate, endDate, userId, page, pageSize),
            operation: async (context) =>
            {
                var dataSets = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.GetByDateRangeAsync(startDate, endDate, userId),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    context.OperationName);

                _infrastructure.StructuredLogging.LogStep(context, "Database retrieval by date range completed", new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.TOTAL_DATASETS] = dataSets.Count()
                });

                var pagedDataSets = ApplyPagination(dataSets, page, pageSize, context);
                return _mapper.Map<IEnumerable<DataSetDto>>(pagedDataSets);
            });
    }

    public async Task<DataSetStatisticsDto> GetDataSetStatisticsAsync(string userId)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(GetDataSetStatisticsAsync),
            userId: userId,
            additionalMetadata: null,
            validation: () => ValidateUserId(userId),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate cache corruption
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("CacheFailure", () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating cache corruption", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "CacheCorruption"
                    });
                    _infrastructure.Cache.Remove($"stats_{userId}");
                    return Task.CompletedTask;
                }, new Dictionary<string, object> { ["UserId"] = userId });

                var cacheKey = $"stats_{userId}";
                
                _infrastructure.StructuredLogging.LogStep(context, "Cache lookup started");
                if (_infrastructure.Cache.TryGetValue(cacheKey, out DataSetStatisticsDto? cachedStats))
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Statistics retrieved from cache");
                    return cachedStats!;
                }
                _infrastructure.StructuredLogging.LogStep(context, "Cache miss - calculating statistics");

                var totalCount = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.GetTotalCountAsync(userId),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_GetTotalCount");

                var totalSize = await ExecuteWithTimeoutAsync<long>(
                    () => _dataSetRepository.GetTotalSizeAsync(userId),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_GetTotalSize");

                var recentlyModified = await ExecuteWithTimeoutAsync<IEnumerable<DataSet>>(
                    () => _dataSetRepository.GetRecentlyModifiedAsync(userId, 5),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_GetRecentlyModified");

                var statistics = new DataSetStatisticsDto
                {
                    TotalCount = totalCount,
                    TotalSize = totalSize,
                    RecentlyModified = _mapper.Map<IEnumerable<DataSetDto>>(recentlyModified)
                };

                _infrastructure.StructuredLogging.LogStep(context, "Cache storage started");
                _infrastructure.Cache.Set(cacheKey, statistics, _infrastructure.CacheExpiration);
                _infrastructure.StructuredLogging.LogStep(context, "Cache storage completed");

                return statistics;
            });
    }

    #region Private Methods

    private async Task<T> ExecuteDataSetOperationAsync<T>(
        string operationName,
        string userId,
        Dictionary<string, object>? additionalMetadata,
        Action validation,
        Func<IOperationContext, Task<T>> operation)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(operationName, correlationId, userId, additionalMetadata);

        try
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            validation();
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            var result = await operation(context);
            _infrastructure.StructuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogSummary(context, false, ex.Message);
            throw new InvalidOperationException($"Failed to complete {operationName} for user {userId}", ex);
        }
    }

    private async Task<DataSet?> RetrieveDataSetWithAccessControlAsync(int id, string userId, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Dataset retrieval started");
        var dataSet = await ExecuteWithTimeoutAsync(
            () => _dataSetRepository.GetByIdAsync(id),
            _infrastructure.QuickTimeout,
            GetCorrelationId(),
            $"{context.OperationName}_GetDataSet");
        _infrastructure.StructuredLogging.LogStep(context, "Dataset retrieval completed");

        if (dataSet?.UserId != userId)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Access denied - user mismatch", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.EXPECTED_USER_ID] = userId,
                [AppConstants.DataStructures.ACTUAL_USER_ID] = dataSet?.UserId ?? "null"
            });
            _infrastructure.StructuredLogging.LogSummary(context, false, AppConstants.ValidationMessages.DATASET_NOT_FOUND_OR_ACCESS_DENIED);
            return null;
        }

        return dataSet;
    }

    private async Task DeleteDataSetFileAsync(DataSet dataSet, IOperationContext context)
    {
        if (!string.IsNullOrEmpty(dataSet.FilePath))
        {
            _infrastructure.StructuredLogging.LogStep(context, "File deletion started", new Dictionary<string, object>
            {
                ["FilePath"] = dataSet.FilePath
            });
            try
            {
                await ExecuteWithTimeoutAsync(
                    () => _fileUploadService.DeleteFileAsync(dataSet.FilePath),
                    _infrastructure.QuickTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_DeleteFile");
                _infrastructure.StructuredLogging.LogStep(context, "File deletion completed successfully");
            }
            catch (Exception ex)
            {
                _infrastructure.StructuredLogging.LogStep(context, "File deletion failed, continuing with database deletion", new Dictionary<string, object>
                {
                    ["FileDeletionError"] = ex.Message
                });
            }
        }
        else
        {
            _infrastructure.StructuredLogging.LogStep(context, "No file path to delete");
        }
    }

    private async Task HandleSuccessfulDeletionAsync(int id, string userId, DataSet dataSet, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Cache clearing started");
        _infrastructure.Cache.Remove($"stats_{userId}");
        _infrastructure.StructuredLogging.LogStep(context, "Cache clearing completed");

        await LogAuditActionAsync(id, userId, "Deleted", context, new { 
            fileName = dataSet.FileName,
            fileSize = dataSet.FileSize,
            rowCount = dataSet.RowCount
        });
    }

    private async Task HandleSuccessfulRestoreAsync(int id, string userId, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Cache clearing started");
        _infrastructure.Cache.Remove($"stats_{userId}");
        _infrastructure.StructuredLogging.LogStep(context, "Cache clearing completed");

        await LogAuditActionAsync(id, userId, "Restored", context);
    }

    private async Task HandleSuccessfulHardDeleteAsync(int id, string userId, DataSet dataSet, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, "Cache clearing started");
        _infrastructure.Cache.Remove($"stats_{userId}");
        _infrastructure.StructuredLogging.LogStep(context, "Cache clearing completed");

        await LogAuditActionAsync(id, userId, "HardDeleted", context, new { 
            fileName = dataSet.FileName,
            fileSize = dataSet.FileSize,
            rowCount = dataSet.RowCount
        });
    }

    private async Task LogAuditActionAsync(int id, string userId, string action, IOperationContext context, object? additionalData = null)
    {
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.AUDIT_LOGGING_STARTED);
        await ExecuteWithTimeoutAsync(
            () => _auditService.LogDataSetActionAsync(id, userId, action, additionalData),
            _infrastructure.QuickTimeout,
            GetCorrelationId(),
            $"{context.OperationName}_AuditLog");
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.AUDIT_LOGGING_COMPLETED);
    }

    private List<DataSet> ApplyPagination(IEnumerable<DataSet> dataSets, int page, int pageSize, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.PAGINATION_STARTED);
        var pagedDataSets = dataSets
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.PAGINATION_COMPLETED, new Dictionary<string, object>
        {
            ["PagedDataSets"] = pagedDataSets.Count
        });
        return pagedDataSets;
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _infrastructure.Logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
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
            _infrastructure.Logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
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
            _infrastructure.Logger.LogWarning(jsonEx, "Failed to deserialize schema for dataset {DataSetId}. CorrelationId: {CorrelationId}", 
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
        if (fileRequest.FileName.Contains("..") || fileRequest.FileName.Contains('/') || fileRequest.FileName.Contains('\\'))
            throw new ArgumentException("Invalid file name", nameof(fileRequest));
    }

    private static void ValidateDataSetIdAndUserId(int id, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        ValidateUserId(userId);
    }

    private static void ValidateGetDataSetInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateDeleteInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateRestoreInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateHardDeleteInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidatePreviewInputs(int id, int rows, string userId)
    {
        if (id <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        
        if (rows <= 0 || rows > 1000)
            throw new ArgumentException("Rows must be between 1 and 1000", nameof(rows));
        
        ValidateUserId(userId);
    }

    private static void ValidateSchemaInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

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