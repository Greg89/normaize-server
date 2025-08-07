using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Mapping;
using System.Text.Json;
using System.Diagnostics;

namespace Normaize.Core.Services;

/// <summary>
/// Service for managing dataset lifecycle operations including restore, reset, and retention policies.
/// </summary>
public class DataSetLifecycleService : IDataSetLifecycleService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IAuditService _auditService;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public DataSetLifecycleService(
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

    public async Task<OperationResultDto> RestoreDataSetEnhancedAsync(int id, DataSetRestoreDto restoreDto, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataSetLifecycle.RESTORE_DATA_SET_ENHANCED,
            userId,
            new Dictionary<string, object> { ["DataSetId"] = id, ["RestoreType"] = restoreDto?.RestoreType.ToString() ?? "null" },
            () => ValidateRestoreEnhancedInputs(id, restoreDto!, userId),
            async (context) =>
            {
                // Chaos engineering: Simulate restore operation delay
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("RestoreOperationDelay", context.CorrelationId!, context.OperationName!, async () =>
                {
                    var delayMs = new Random().Next(AppConstants.ChaosEngineering.RESTORE_OPERATION_DELAY_MIN_MS, AppConstants.ChaosEngineering.RESTORE_OPERATION_DELAY_MAX_MS);
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating restore operation delay", new Dictionary<string, object>
                    {
                        ["DelayMs"] = delayMs,
                        ["ChaosType"] = "RestoreOperationDelay"
                    });
                    await Task.Delay(delayMs);
                    return Task.CompletedTask;
                }, new Dictionary<string, object> { ["UserId"] = userId, ["DataSetId"] = id });

                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                return await PerformRestoreOperationAsync(dataSet!, restoreDto!, context);
            });
    }

    public async Task<OperationResultDto> ResetDataSetAsync(int id, DataSetResetDto resetDto, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataSetLifecycle.RESET_DATA_SET,
            userId,
            new Dictionary<string, object> { ["DataSetId"] = id, ["ResetType"] = resetDto.ResetType.ToString() },
            () => ValidateResetInputs(id, resetDto, userId),
            async (context) =>
            {
                // Chaos engineering: Simulate file processing failure during reset
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("FileProcessingFailure", context.CorrelationId!, context.OperationName!, () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating file processing failure during reset", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "FileProcessingFailure",
                        ["ResetType"] = resetDto.ResetType.ToString()
                    });
                    throw new InvalidOperationException("Simulated file processing failure during dataset reset");
                }, new Dictionary<string, object> { ["UserId"] = userId, ["DataSetId"] = id, ["ResetType"] = resetDto.ResetType.ToString() });

                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                return await PerformResetOperationAsync(dataSet!, resetDto, context);
            });
    }

    public async Task<OperationResultDto> UpdateRetentionPolicyAsync(int id, DataSetRetentionDto retentionDto, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataSetLifecycle.UPDATE_RETENTION_POLICY,
            userId,
            new Dictionary<string, object> { ["DataSetId"] = id, ["RetentionDays"] = retentionDto?.RetentionDays ?? 0 },
            () => ValidateRetentionInputs(id, retentionDto!, userId),
            async (context) =>
            {
                // Chaos engineering: Simulate database timeout during retention policy update
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("DatabaseTimeout", context.CorrelationId!, context.OperationName!, async () =>
                {
                    var delayMs = new Random().Next(AppConstants.ChaosEngineering.RETENTION_POLICY_TIMEOUT_MIN_MS, AppConstants.ChaosEngineering.RETENTION_POLICY_TIMEOUT_MAX_MS);
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating database timeout during retention policy update", new Dictionary<string, object>
                    {
                        ["DelayMs"] = delayMs,
                        ["ChaosType"] = "DatabaseTimeout"
                    });
                    await Task.Delay(delayMs);
                }, new Dictionary<string, object> { ["UserId"] = userId, ["DataSetId"] = id, ["RetentionDays"] = retentionDto?.RetentionDays ?? 0 });

                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) throw new InvalidOperationException($"Dataset with ID {id} not found");

                // Calculate new expiry date
                var expiryDate = DateTime.UtcNow.AddDays(retentionDto?.RetentionDays ?? 0);

                // Update retention policy
                dataSet!.RetentionDays = retentionDto?.RetentionDays ?? 0;
                dataSet!.RetentionExpiryDate = expiryDate;

                await _dataSetRepository.UpdateAsync(dataSet);

                // Log audit action
                await LogAuditActionAsync(id, userId, "UpdateRetentionPolicy", context, new
                {
                    OldRetentionDays = dataSet!.RetentionDays,
                    NewRetentionDays = retentionDto?.RetentionDays ?? 0,
                    NewExpiryDate = expiryDate
                });

                return new OperationResultDto
                {
                    Success = true,
                    Message = string.Format(AppConstants.DataSetLifecycle.RETENTION_POLICY_UPDATED_SUCCESSFULLY, retentionDto?.RetentionDays ?? 0),
                    Data = new
                    {
                        DataSetId = id,
                        RetentionDays = retentionDto?.RetentionDays ?? 0,
                        ExpiryDate = expiryDate,
                        IsExpired = false
                    }
                };
            });
    }

    public async Task<DataSetRetentionStatusDto?> GetRetentionStatusAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataSetLifecycle.GET_RETENTION_STATUS,
            userId,
            new Dictionary<string, object> { ["DataSetId"] = id },
            () => ValidateDataSetIdAndUserId(id, userId),
            async (context) =>
            {
                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) throw new InvalidOperationException($"Dataset with ID {id} not found");

                var isExpired = dataSet!.RetentionExpiryDate.HasValue &&
                               dataSet.RetentionExpiryDate.Value < DateTime.UtcNow;

                var daysRemaining = dataSet!.RetentionExpiryDate.HasValue && !isExpired
                    ? (int)(dataSet.RetentionExpiryDate.Value - DateTime.UtcNow).TotalDays
                    : 0;

                return new DataSetRetentionStatusDto
                {
                    DataSetId = id,
                    RetentionDays = dataSet!.RetentionDays,
                    RetentionExpiryDate = dataSet.RetentionExpiryDate,
                    IsRetentionExpired = isExpired,
                    DaysUntilExpiry = daysRemaining
                };
            });
    }

    public async Task<bool> RestoreDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataSetLifecycle.RESTORE_DATA_SET,
            userId,
            new Dictionary<string, object> { ["DataSetId"] = id },
            () => ValidateRestoreInputs(id, userId),
            async (context) =>
            {
                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);
                if (dataSet == null) throw new InvalidOperationException($"Dataset with ID {id} not found");

                if (!dataSet!.IsDeleted)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Dataset is not deleted, no action needed");
                    return true;
                }

                dataSet!.IsDeleted = false;
                dataSet.DeletedAt = null;

                await _dataSetRepository.UpdateAsync(dataSet);
                await HandleSuccessfulRestoreAsync(id, userId, context);

                return true;
            });
    }

    public async Task<bool> HardDeleteDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataSetLifecycle.HARD_DELETE_DATA_SET,
            userId,
            new Dictionary<string, object> { ["DataSetId"] = id },
            () => ValidateHardDeleteInputs(id, userId),
            async (context) =>
            {
                // Chaos engineering: Simulate storage service failure during hard delete
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("StorageFailure", context.CorrelationId!, context.OperationName!, () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating storage service failure during hard delete", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "StorageFailure",
                        ["Operation"] = "HardDelete"
                    });
                    throw new InvalidOperationException("Simulated storage service failure during hard delete");
                }, new Dictionary<string, object> { ["UserId"] = userId, ["DataSetId"] = id });

                var dataSet = await RetrieveDataSetWithAccessControlAsync(id, userId, context);

                // Delete the file from storage
                await DeleteDataSetFileAsync(dataSet!, context);

                // Remove from database
                await _dataSetRepository.DeleteAsync(dataSet!.Id);

                await HandleSuccessfulHardDeleteAsync(id, userId, dataSet, context);

                return true;
            });
    }

    #region Private Helper Methods

    private async Task<T> ExecuteDataSetOperationAsync<T>(
        string operationName,
        string userId,
        Dictionary<string, object>? additionalMetadata,
        Action validation,
        Func<IOperationContext, Task<T>> operation)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(
            operationName,
            correlationId,
            userId,
            additionalMetadata)!;

        try
        {
            validation();

            _infrastructure.StructuredLogging.LogStep(context, $"{operationName} started");

            var result = await operation(context);

            _infrastructure.StructuredLogging.LogSummary(context, true, $"{operationName} completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogException(ex, $"{operationName} failed");
            throw;
        }
    }

    private async Task<DataSet?> RetrieveDataSetWithAccessControlAsync(int id, string userId, IOperationContext context)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(id);

        if (dataSet == null)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetLifecycle.DATASET_NOT_FOUND);
            throw new InvalidOperationException($"Dataset with ID {id} not found");
        }

        if (dataSet.UserId != userId)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetLifecycle.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER);
            throw new UnauthorizedAccessException($"{AppConstants.DataSetLifecycle.ACCESS_DENIED_TO_DATASET} {id}");
        }

        return dataSet;
    }

    private async Task DeleteDataSetFileAsync(DataSet dataSet, IOperationContext context)
    {
        if (!string.IsNullOrEmpty(dataSet.FilePath))
        {
            try
            {
                await _fileUploadService.DeleteFileAsync(dataSet.FilePath);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetLifecycle.FILE_DELETED_FROM_STORAGE, new Dictionary<string, object>
                {
                    ["FilePath"] = dataSet.FilePath
                });
            }
            catch (Exception ex)
            {
                _infrastructure.StructuredLogging.LogException(ex, AppConstants.DataSetLifecycle.FAILED_TO_DELETE_FILE_FROM_STORAGE);
                // Continue with database deletion even if file deletion fails
            }
        }
    }

    private async Task HandleSuccessfulRestoreAsync(int id, string userId, IOperationContext context)
    {
        await LogAuditActionAsync(id, userId, "RestoreDataSet", context);
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetLifecycle.DATASET_RESTORED_SUCCESSFULLY);
    }

    private async Task HandleSuccessfulHardDeleteAsync(int id, string userId, DataSet dataSet, IOperationContext context)
    {
        await LogAuditActionAsync(id, userId, "HardDeleteDataSet", context, new
        {
            FileName = dataSet.FileName,
            FilePath = dataSet.FilePath
        });
        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataSetLifecycle.DATASET_PERMANENTLY_DELETED);
    }

    private async Task LogAuditActionAsync(int id, string userId, string action, IOperationContext context, object? additionalData = null)
    {
        var auditData = new Dictionary<string, object>
        {
            ["DataSetId"] = id,
            ["CorrelationId"] = context.CorrelationId
        };

        if (additionalData != null)
        {
            var additionalDict = JsonSerializer.SerializeToNode(additionalData)?.AsObject()?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            if (additionalDict != null)
            {
                foreach (var kvp in additionalDict)
                {
                    auditData[kvp.Key] = kvp.Value!;
                }
            }
        }

        await _auditService.LogDataSetActionAsync(id, userId, action, auditData);
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateDataSetIdAndUserId(int id, string userId)
    {
        if (id <= 0) throw new ArgumentException(AppConstants.DataSetLifecycle.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(AppConstants.DataSetLifecycle.USER_ID_CANNOT_BE_NULL_OR_EMPTY, nameof(userId));
    }

    private static void ValidateRestoreInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateHardDeleteInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateRestoreEnhancedInputs(int id, DataSetRestoreDto restoreDto, string userId)
    {
        ValidateDataSetIdAndUserId(id, userId);
        if (restoreDto == null) throw new ArgumentNullException(nameof(restoreDto));
    }

    private static void ValidateResetInputs(int id, DataSetResetDto resetDto, string userId)
    {
        ValidateDataSetIdAndUserId(id, userId);
        if (resetDto == null) throw new ArgumentNullException(nameof(resetDto));
    }

    private static void ValidateRetentionInputs(int id, DataSetRetentionDto retentionDto, string userId)
    {
        ValidateDataSetIdAndUserId(id, userId);
        if (retentionDto == null) throw new ArgumentNullException(nameof(retentionDto));
        if (retentionDto.RetentionDays <= 0) throw new ArgumentException(AppConstants.DataSetLifecycle.RETENTION_DAYS_MUST_BE_POSITIVE, nameof(retentionDto.RetentionDays));
    }

    #endregion

    #region Operation Implementation Methods

    private async Task<FileAvailabilityResult> CheckFileAvailabilityAsync(DataSet dataSet, IOperationContext context)
    {
        if (string.IsNullOrEmpty(dataSet.FilePath))
        {
            return new FileAvailabilityResult
            {
                IsAvailable = false,
                Reason = AppConstants.DataSetLifecycle.NO_FILE_PATH_ASSOCIATED_WITH_DATASET,
                ErrorCode = AppConstants.DataSetLifecycle.NO_FILE_PATH
            };
        }

        try
        {
            var fileExists = await _fileUploadService.FileExistsAsync(dataSet.FilePath);

            if (!fileExists)
            {
                return new FileAvailabilityResult
                {
                    IsAvailable = false,
                    Reason = AppConstants.DataSetLifecycle.ORIGINAL_FILE_NO_LONGER_EXISTS_IN_STORAGE,
                    ErrorCode = AppConstants.DataSetLifecycle.FILE_NOT_FOUND
                };
            }

            return new FileAvailabilityResult
            {
                IsAvailable = true,
                Reason = AppConstants.DataSetLifecycle.FILE_IS_AVAILABLE_FOR_PROCESSING
            };
        }
        catch (Exception ex)
        {
            return new FileAvailabilityResult
            {
                IsAvailable = false,
                Reason = $"{AppConstants.DataSetLifecycle.ERROR_CHECKING_FILE_AVAILABILITY}: {ex.Message}",
                ErrorCode = AppConstants.DataSetLifecycle.CHECK_ERROR
            };
        }
    }

    private async Task<OperationResultDto> PerformRestoreOperationAsync(DataSet dataSet, DataSetRestoreDto restoreDto, IOperationContext context)
    {
        if (!dataSet.IsDeleted)
        {
            return new OperationResultDto
            {
                Success = true,
                Message = AppConstants.DataSetLifecycle.DATASET_IS_NOT_DELETED_NO_RESTORE_ACTION_NEEDED,
                Data = new { DataSetId = dataSet.Id, IsDeleted = false }
            };
        }

        switch (restoreDto.RestoreType)
        {
            case RestoreType.Simple:
                return await PerformSimpleRestoreAsync(dataSet, context);

            case RestoreType.WithReset:
                return await PerformFullRestoreAsync(dataSet, context);

            default:
                throw new ArgumentException($"Unsupported restore type: {restoreDto.RestoreType}");
        }
    }

    private async Task<OperationResultDto> PerformSimpleRestoreAsync(DataSet dataSet, IOperationContext context)
    {
        dataSet.IsDeleted = false;
        dataSet.DeletedAt = null;

        await _dataSetRepository.UpdateAsync(dataSet);
        await LogAuditActionAsync(dataSet.Id, dataSet.UserId, AppConstants.DataSetLifecycle.RESTORE_DATA_SET_SIMPLE, context);

        return new OperationResultDto
        {
            Success = true,
            Message = AppConstants.DataSetLifecycle.DATASET_RESTORED_SUCCESSFULLY_SIMPLE_RESTORE,
            Data = new { DataSetId = dataSet.Id, RestoreType = AppConstants.DataSetLifecycle.RESTORE_TYPE_SIMPLE }
        };
    }

    private async Task<OperationResultDto> PerformFullRestoreAsync(DataSet dataSet, IOperationContext context)
    {
        dataSet.IsDeleted = false;
        dataSet.DeletedAt = null;
        dataSet.IsProcessed = false;
        dataSet.ProcessedAt = null;
        dataSet.PreviewData = null;
        dataSet.Schema = null;
        dataSet.RowCount = 0;
        dataSet.ColumnCount = 0;

        await _dataSetRepository.UpdateAsync(dataSet);
        await LogAuditActionAsync(dataSet.Id, dataSet.UserId, AppConstants.DataSetLifecycle.RESTORE_DATA_SET_FULL, context);

        return new OperationResultDto
        {
            Success = true,
            Message = AppConstants.DataSetLifecycle.DATASET_RESTORED_SUCCESSFULLY_FULL_RESTORE,
            Data = new { DataSetId = dataSet.Id, RestoreType = AppConstants.DataSetLifecycle.RESTORE_TYPE_FULL }
        };
    }

    private async Task<OperationResultDto> PerformResetOperationAsync(DataSet dataSet, DataSetResetDto resetDto, IOperationContext context)
    {
        switch (resetDto.ResetType)
        {
            case ResetType.OriginalFile:
                return await PerformFileResetAsync(dataSet, context);

            case ResetType.Database:
                return await PerformDatabaseResetAsync(dataSet, context);

            default:
                throw new ArgumentException($"Unsupported reset type: {resetDto.ResetType}");
        }
    }

    private async Task<OperationResultDto> PerformFileResetAsync(DataSet dataSet, IOperationContext context)
    {
        // Check if original file is still available
        var fileAvailability = await CheckFileAvailabilityAsync(dataSet, context);

        if (!fileAvailability.IsAvailable)
        {
            return new OperationResultDto
            {
                Success = false,
                Message = $"{AppConstants.DataSetLifecycle.CANNOT_RESET_DATASET}: {fileAvailability.Reason}",
                Data = new
                {
                    DataSetId = dataSet.Id,
                    FileAvailable = false,
                    ErrorCode = fileAvailability.ErrorCode,
                    Reason = fileAvailability.Reason
                }
            };
        }

        try
        {
            // If dataset was deleted, restore it first
            if (dataSet.IsDeleted)
            {
                dataSet.IsDeleted = false;
                dataSet.DeletedAt = null;
            }

            // Reset processing status
            dataSet.IsProcessed = false;
            dataSet.ProcessedAt = null;
            dataSet.PreviewData = null;
            dataSet.Schema = null;
            dataSet.RowCount = 0;
            dataSet.ColumnCount = 0;

            // Reprocess the original file
            var processedDataSet = await _fileUploadService.ProcessFileAsync(dataSet.FilePath, Path.GetExtension(dataSet.FileName));

            // Update with reprocessed data
            dataSet.PreviewData = processedDataSet.PreviewData;
            dataSet.Schema = processedDataSet.Schema;
            dataSet.RowCount = processedDataSet.RowCount;
            dataSet.ColumnCount = processedDataSet.ColumnCount;
            dataSet.IsProcessed = true;
            dataSet.ProcessedAt = DateTime.UtcNow;
            dataSet.DataHash = processedDataSet.DataHash;

            await _dataSetRepository.UpdateAsync(dataSet);
            await LogAuditActionAsync(dataSet.Id, dataSet.UserId, AppConstants.DataSetLifecycle.RESET_DATA_SET_FILE_BASED, context, new
            {
                ResetType = AppConstants.DataSetLifecycle.RESET_TYPE_FILE_BASED,
                FilePath = dataSet.FilePath
            });

            return new OperationResultDto
            {
                Success = true,
                Message = AppConstants.DataSetLifecycle.DATASET_RESET_SUCCESSFULLY_USING_ORIGINAL_FILE,
                Data = new
                {
                    DataSetId = dataSet.Id,
                    ResetType = AppConstants.DataSetLifecycle.RESET_TYPE_FILE_BASED,
                    FileAvailable = true,
                    Reprocessed = true
                }
            };
        }
        catch (Exception ex)
        {
            return new OperationResultDto
            {
                Success = false,
                Message = $"{AppConstants.DataSetLifecycle.FAILED_TO_RESET_DATASET}: {ex.Message}",
                Data = new
                {
                    DataSetId = dataSet.Id,
                    ResetType = AppConstants.DataSetLifecycle.RESET_TYPE_FILE_BASED,
                    FileAvailable = true,
                    Error = ex.Message
                }
            };
        }
    }

    private async Task<OperationResultDto> PerformDatabaseResetAsync(DataSet dataSet, IOperationContext context)
    {
        // If dataset was deleted, restore it
        if (dataSet.IsDeleted)
        {
            dataSet.IsDeleted = false;
            dataSet.DeletedAt = null;
        }

        // Reset processing status
        dataSet.IsProcessed = false;
        dataSet.ProcessedAt = null;
        dataSet.PreviewData = null;
        dataSet.Schema = null;
        dataSet.RowCount = 0;
        dataSet.ColumnCount = 0;

        await _dataSetRepository.UpdateAsync(dataSet);
        await LogAuditActionAsync(dataSet.Id, dataSet.UserId, AppConstants.DataSetLifecycle.RESET_DATA_SET_DATABASE_ONLY, context, new
        {
            ResetType = AppConstants.DataSetLifecycle.RESET_TYPE_DATABASE_ONLY
        });

        return new OperationResultDto
        {
            Success = true,
            Message = AppConstants.DataSetLifecycle.DATASET_RESET_SUCCESSFULLY_DATABASE_ONLY,
            Data = new
            {
                DataSetId = dataSet.Id,
                ResetType = AppConstants.DataSetLifecycle.RESET_TYPE_DATABASE_ONLY
            }
        };
    }

    private class FileAvailabilityResult
    {
        public bool IsAvailable { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }
    }

    #endregion
}