using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Mapping;
using Normaize.Core.Configuration;
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
    private readonly IDataProcessingInfrastructure _infrastructure;

    public DataProcessingService(
        IDataSetRepository dataSetRepository,
        IFileUploadService fileUploadService,
        IAuditService auditService,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(dataSetRepository);
        ArgumentNullException.ThrowIfNull(fileUploadService);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(infrastructure);
        _dataSetRepository = dataSetRepository;
        _fileUploadService = fileUploadService;
        _auditService = auditService;
        _infrastructure = infrastructure;
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
            await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.PROCESSING_DELAY, correlationId, context.OperationName, async () =>
            {
                var delayMs = new Random().Next(AppConstants.ChaosEngineering.MIN_PROCESSING_DELAY_MS, AppConstants.ChaosEngineering.MAX_PROCESSING_DELAY_MS);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CHAOS_ENGINEERING_DELAY, new Dictionary<string, object>
                {
                    ["DelayMs"] = delayMs
                });
                await Task.Delay(delayMs);
            }, new Dictionary<string, object> { ["UserId"] = createDto?.UserId ?? AppConstants.Messages.UNKNOWN });

            // Validate file
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_VALIDATION_STARTED);
            if (!await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ValidateFileAsync(fileRequest!),
                _infrastructure.QuickTimeout,
                context))
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_VALIDATION_FAILED);
                _infrastructure.StructuredLogging.LogSummary(context, false, AppConstants.DataProcessing.INVALID_FILE_FORMAT_OR_SIZE);
                return new DataSetUploadResponse
                {
                    Success = false,
                    Message = AppConstants.DataProcessing.INVALID_FILE_FORMAT_OR_SIZE
                };
            }
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_VALIDATION_PASSED);

            // Save file
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_SAVE_STARTED);
            var filePath = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.SaveFileAsync(fileRequest!),
                _infrastructure.DefaultTimeout,
                context);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_SAVED, new Dictionary<string, object>
            {
                ["FilePath"] = filePath
            });

            // Process file and create dataset
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_PROCESSING_STARTED);
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest!.FileName)),
                _infrastructure.DefaultTimeout,
                context);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_PROCESSING_COMPLETED, new Dictionary<string, object>
            {
                ["RowCount"] = dataSet.RowCount,
                ["ColumnCount"] = dataSet.ColumnCount,
                ["FileSize"] = dataSet.FileSize
            });

            // Update with user-provided information
            dataSet.Name = createDto!.Name ?? AppConstants.DataProcessing.UNNAMED_DATASET;
            dataSet.Description = createDto.Description ?? string.Empty;
            dataSet.UserId = createDto.UserId ?? AppConstants.Messages.UNKNOWN;

            // Save to database
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATABASE_SAVE_STARTED);
            var savedDataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.AddAsync(dataSet),
                _infrastructure.QuickTimeout,
                context);
            context.SetMetadata(AppConstants.DataStructures.DATASET_ID, savedDataSet.Id);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATABASE_SAVE_COMPLETED);

            // Clear cache for this user
            _infrastructure.Cache.Remove($"{AppConstants.DataProcessing.STATS_CACHE_KEY_PREFIX}{createDto.UserId}");

            // Log audit trail
            await ExecuteWithTimeoutAsync(
                () => _auditService.LogDataSetActionAsync(
                    savedDataSet.Id,
                    createDto.UserId ?? AppConstants.Messages.UNKNOWN,
                    "Created",
                    new
                    {
                        fileName = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN,
                        fileSize = dataSet.FileSize,
                        rowCount = dataSet.RowCount,
                        columnCount = dataSet.ColumnCount,
                        filePath
                    }),
                _infrastructure.QuickTimeout,
                context);

            _infrastructure.StructuredLogging.LogSummary(context, true);

            return new DataSetUploadResponse
            {
                DataSetId = savedDataSet.Id,
                Success = true,
                Message = AppConstants.DataProcessing.DATASET_UPLOADED_SUCCESSFULLY
            };
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogSummary(context, false, ex.Message);
            return new DataSetUploadResponse
            {
                Success = false,
                Message = AppConstants.DataProcessing.ERROR_UPLOADING_DATASET + ex.Message
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
                return dataSet.ToDto();
            });
    }

    public async Task<DataSetDto?> UpdateDataSetAsync(int id, UpdateDataSetDto updateDto, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            operationName: nameof(UpdateDataSetAsync),
            userId: userId,
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASET_ID] = id,
                ["UpdateData"] = new { updateDto.Name, updateDto.Description }
            },
            validation: () => ValidateUpdateDataSetInputs(id, updateDto, userId),
            operation: async (context) =>
            {
                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) return null;

                // Store original values for audit logging
                var originalName = dataSet.Name;
                var originalDescription = dataSet.Description;

                // Update the dataset
                dataSet.Name = updateDto.Name;
                dataSet.Description = updateDto.Description;
                dataSet.LastModifiedAt = DateTime.UtcNow;
                dataSet.LastModifiedBy = userId;

                var updatedDataSet = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.UpdateAsync(dataSet),
                    _infrastructure.QuickTimeout,
                    context);

                if (updatedDataSet != null)
                {
                    // Log audit action with changes
                    var changes = new
                    {
                        Name = new { From = originalName, To = updateDto.Name },
                        Description = new { From = originalDescription, To = updateDto.Description }
                    };
                    await LogAuditActionAsync(id, userId, "Updated", context, changes);

                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATASET_UPDATED_SUCCESSFULLY, new Dictionary<string, object>
                    {
                        ["OriginalName"] = originalName,
                        ["NewName"] = updateDto.Name,
                        ["OriginalDescription"] = originalDescription ?? "null",
                        ["NewDescription"] = updateDto.Description ?? "null"
                    });
                }

                return updatedDataSet?.ToDto();
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
                    context);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.TOTAL_DATASETS] = dataSets.Count()
                });

                var pagedDataSets = ApplyPagination(dataSets, page, pageSize, context);
                return pagedDataSets.ToDtoCollection();
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
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.DataProcessing.DELETION_FAILURE, () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CHAOS_ENGINEERING_SIMULATING_DELETION_FAILURE, new Dictionary<string, object>
                    {
                        ["ChaosType"] = AppConstants.DataProcessing.DELETION_FAILURE
                    });
                    throw new InvalidOperationException(AppConstants.DataProcessing.SIMULATED_DELETION_FAILURE_MESSAGE);
                }, new Dictionary<string, object> { ["UserId"] = userId });

                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) return false;

                var result = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.DeleteAsync(id),
                    _infrastructure.QuickTimeout,
                    context);

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
                    context);

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
                    context);

                if (result)
                {
                    await HandleSuccessfulHardDeleteAsync(id, userId, dataSet, context);
                }

                return result;
            });
    }

    public async Task<DataSetPreviewDto?> GetDataSetPreviewAsync(int id, int rows, string userId)
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
                if (dataSet == null || string.IsNullOrEmpty(dataSet.PreviewData) || string.IsNullOrEmpty(dataSet.Schema))
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.ACCESS_DENIED_OR_NO_PREVIEW_DATA, new Dictionary<string, object>
                    {
                        ["HasPreviewData"] = !string.IsNullOrEmpty(dataSet?.PreviewData),
                        ["HasSchema"] = !string.IsNullOrEmpty(dataSet?.Schema)
                    });
                    return null;
                }

                // Parse the schema (column headers)
                var columns = JsonConfiguration.Deserialize<List<string>>(dataSet.Schema);
                if (columns == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Failed to deserialize schema", new Dictionary<string, object>
                    {
                        ["Schema"] = dataSet.Schema
                    });
                    return null;
                }

                // Parse the preview data
                var previewRows = JsonConfiguration.Deserialize<List<Dictionary<string, object>>>(dataSet.PreviewData);
                if (previewRows == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Failed to deserialize preview data", new Dictionary<string, object>
                    {
                        ["PreviewData"] = dataSet.PreviewData
                    });
                    return null;
                }

                // Limit rows to requested amount
                var limitedRows = previewRows.Take(rows).ToList();

                await LogAuditActionAsync(id, userId, "Previewed", context, new { rows, actualRows = limitedRows.Count });

                return new DataSetPreviewDto
                {
                    Columns = columns,
                    Rows = limitedRows,
                    TotalRows = dataSet.RowCount,
                    PreviewRowCount = limitedRows.Count,
                    MaxPreviewRows = previewRows.Count
                };
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
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.ACCESS_DENIED_OR_NO_SCHEMA, new Dictionary<string, object>
                    {
                        ["HasSchema"] = !string.IsNullOrEmpty(dataSet?.Schema)
                    });
                    return null;
                }

                var schema = await DeserializeSchemaSafelyAsync(dataSet.Schema, id, context);
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
                    context);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.DATABASE_RETRIEVAL_COMPLETED, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.TOTAL_DATASETS] = dataSets.Count()
                });

                var deletedDataSets = dataSets
                    .Where(d => d.IsDeleted)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILTERING_DELETED_DATASETS_COMPLETED, new Dictionary<string, object>
                {
                    ["DeletedDataSets"] = deletedDataSets.Count
                });

                return deletedDataSets.ToDtoCollection();
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
                    context);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.SEARCH_OPERATION_COMPLETED, new Dictionary<string, object>
                {
                    ["TotalMatches"] = dataSets.Count()
                });

                var pagedDataSets = ApplyPagination(dataSets, page, pageSize, context);
                return pagedDataSets.ToDtoCollection();
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
                    context);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATABASE_RETRIEVAL_BY_FILE_TYPE_COMPLETED, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.TOTAL_DATASETS] = dataSets.Count()
                });

                var pagedDataSets = ApplyPagination(dataSets, page, pageSize, context);
                return pagedDataSets.ToDtoCollection();
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
                    context);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATABASE_RETRIEVAL_BY_DATE_RANGE_COMPLETED, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.TOTAL_DATASETS] = dataSets.Count()
                });

                var pagedDataSets = ApplyPagination(dataSets, page, pageSize, context);
                return pagedDataSets.ToDtoCollection();
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
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(AppConstants.ChaosEngineering.CACHE_FAILURE, () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CHAOS_ENGINEERING_SIMULATING_CACHE_CORRUPTION, new Dictionary<string, object>
                    {
                        ["ChaosType"] = AppConstants.DataProcessing.CACHE_CORRUPTION
                    });
                    _infrastructure.Cache.Remove($"{AppConstants.DataProcessing.STATS_CACHE_KEY_PREFIX}{userId}");
                    return Task.CompletedTask;
                }, new Dictionary<string, object> { ["UserId"] = userId });

                var cacheKey = $"{AppConstants.DataProcessing.STATS_CACHE_KEY_PREFIX}{userId}";

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_LOOKUP_STARTED);
                if (_infrastructure.Cache.TryGetValue(cacheKey, out DataSetStatisticsDto? cachedStats))
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.STATISTICS_RETRIEVED_FROM_CACHE);
                    return cachedStats!;
                }
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_MISS_CALCULATING_STATISTICS);

                var totalCount = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.GetTotalCountAsync(userId),
                    _infrastructure.QuickTimeout,
                    context);

                var totalSize = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.GetTotalSizeAsync(userId),
                    _infrastructure.QuickTimeout,
                    context);

                var recentlyModified = await ExecuteWithTimeoutAsync(
                    () => _dataSetRepository.GetRecentlyModifiedAsync(userId, 5),
                    _infrastructure.QuickTimeout,
                    context);

                var statistics = new DataSetStatisticsDto
                {
                    TotalCount = totalCount,
                    TotalSize = totalSize,
                    RecentlyModified = recentlyModified.ToDtoCollection()
                };

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_STORAGE_STARTED);
                _infrastructure.Cache.Set(cacheKey, statistics, _infrastructure.CacheExpiration);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_STORAGE_COMPLETED);

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
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATASET_RETRIEVAL_STARTED);
        var dataSet = await ExecuteWithTimeoutAsync(
            () => _dataSetRepository.GetByIdAsync(id),
            _infrastructure.QuickTimeout,
            context);
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATASET_RETRIEVAL_COMPLETED);

        if (dataSet?.UserId != userId)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.ACCESS_DENIED_USER_MISMATCH, new Dictionary<string, object>
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
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_DELETION_STARTED, new Dictionary<string, object>
            {
                ["FilePath"] = dataSet.FilePath
            });
            try
            {
                await ExecuteWithTimeoutAsync(
                    () => _fileUploadService.DeleteFileAsync(dataSet.FilePath),
                    _infrastructure.QuickTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_DELETION_COMPLETED_SUCCESSFULLY);
            }
            catch (Exception ex)
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_DELETION_FAILED_CONTINUING, new Dictionary<string, object>
                {
                    ["FileDeletionError"] = ex.Message
                });
            }
        }
        else
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.NO_FILE_PATH_TO_DELETE);
        }
    }

    private async Task HandleSuccessfulDeletionAsync(int id, string userId, DataSet dataSet, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_CLEARING_STARTED);
        _infrastructure.Cache.Remove($"{AppConstants.DataProcessing.STATS_CACHE_KEY_PREFIX}{userId}");
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_CLEARING_COMPLETED);

        await LogAuditActionAsync(id, userId, "Deleted", context, new
        {
            fileName = dataSet.FileName,
            fileSize = dataSet.FileSize,
            rowCount = dataSet.RowCount
        });
    }

    private async Task HandleSuccessfulRestoreAsync(int id, string userId, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_CLEARING_STARTED);
        _infrastructure.Cache.Remove($"{AppConstants.DataProcessing.STATS_CACHE_KEY_PREFIX}{userId}");
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_CLEARING_COMPLETED);

        await LogAuditActionAsync(id, userId, "Restored", context);
    }

    private async Task HandleSuccessfulHardDeleteAsync(int id, string userId, DataSet dataSet, IOperationContext context)
    {
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_CLEARING_STARTED);
        _infrastructure.Cache.Remove($"{AppConstants.DataProcessing.STATS_CACHE_KEY_PREFIX}{userId}");
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.CACHE_CLEARING_COMPLETED);

        await LogAuditActionAsync(id, userId, "HardDeleted", context, new
        {
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
            context);
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

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, IOperationContext context)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Operation timed out", new Dictionary<string, object>
            {
                ["Timeout"] = timeout.ToString(),
                ["OperationName"] = context.OperationName,
                ["ErrorMessage"] = ex.Message
            });
            throw new TimeoutException($"Operation {context.OperationName} timed out after {timeout}");
        }
    }

    private async Task ExecuteWithTimeoutAsync(Func<Task> operation, TimeSpan timeout, IOperationContext context)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Operation timed out", new Dictionary<string, object>
            {
                ["Timeout"] = timeout.ToString(),
                ["OperationName"] = context.OperationName,
                ["ErrorMessage"] = ex.Message
            });
            throw new TimeoutException($"Operation {context.OperationName} timed out after {timeout}");
        }
    }

    private async Task<object?> DeserializeSchemaSafelyAsync(string schema, int dataSetId, IOperationContext context)
    {
        try
        {
            return await Task.Run(() => JsonSerializer.Deserialize<object>(schema));
        }
        catch (JsonException jsonEx)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Failed to deserialize schema", new Dictionary<string, object>
            {
                ["DataSetId"] = dataSetId,
                ["ErrorMessage"] = jsonEx.Message
            });
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

    private static void ValidateUpdateDataSetInputs(int id, UpdateDataSetDto updateDto, string userId)
    {
        ValidateDataSetIdAndUserId(id, userId);

        if (updateDto == null)
            throw new ArgumentNullException(nameof(updateDto), "Update data cannot be null");

        if (string.IsNullOrWhiteSpace(updateDto.Name))
            throw new ArgumentException("Dataset name cannot be null or empty", nameof(updateDto.Name));

        if (updateDto.Name.Length > 255)
            throw new ArgumentException("Dataset name cannot exceed 255 characters", nameof(updateDto.Name));

        if (updateDto.Description != null && updateDto.Description.Length > 1000)
            throw new ArgumentException("Dataset description cannot exceed 1000 characters", nameof(updateDto.Description));
    }

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