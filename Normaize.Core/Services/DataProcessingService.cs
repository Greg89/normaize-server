using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Mapping;
using System.Diagnostics;

namespace Normaize.Core.Services;

/// <summary>
/// Core service for dataset CRUD operations and file processing.
/// Implements industry-standard error handling and distributed tracing.
/// </summary>
public class DataProcessingService : IDataProcessingService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IAuditService _auditService;
    private readonly IUserSettingsService _userSettingsService;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public DataProcessingService(
        IDataSetRepository dataSetRepository,
        IFileUploadService fileUploadService,
        IAuditService auditService,
        IUserSettingsService userSettingsService,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(dataSetRepository);
        ArgumentNullException.ThrowIfNull(fileUploadService);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(userSettingsService);
        ArgumentNullException.ThrowIfNull(infrastructure);
        _dataSetRepository = dataSetRepository;
        _fileUploadService = fileUploadService;
        _auditService = auditService;
        _userSettingsService = userSettingsService;
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
                [AppConstants.FileProcessing.FILE_NAME_KEY] = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN
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
                _infrastructure.QuickTimeout))
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
                _infrastructure.DefaultTimeout);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_SAVED, new Dictionary<string, object>
            {
                [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath
            });

            // Process file and create dataset
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_PROCESSING_STARTED);
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest!.FileName)),
                _infrastructure.DefaultTimeout);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.FILE_PROCESSED, new Dictionary<string, object>
            {
                ["RowCount"] = dataSet.RowCount,
                ["ColumnCount"] = dataSet.ColumnCount
            });

            // Get user settings for retention policy
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.USER_SETTINGS_RETRIEVAL_STARTED);
            var userSettings = await ExecuteWithTimeoutAsync(
                () => _userSettingsService.GetUserSettingsAsync(createDto!.UserId),
                _infrastructure.QuickTimeout);

            // Set retention expiry date based on user settings
            var retentionDays = userSettings?.RetentionDays ?? 365; // Default to 1 year if no settings
            dataSet.RetentionExpiryDate = DateTime.UtcNow.AddDays(retentionDays);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.RETENTION_POLICY_SET, new Dictionary<string, object>
            {
                ["RetentionDays"] = retentionDays,
                ["RetentionExpiryDate"] = dataSet.RetentionExpiryDate
            });

            // Set user-specific properties
            dataSet.UserId = createDto!.UserId;
            dataSet.Name = createDto.Name;
            dataSet.Description = createDto.Description;
            dataSet.FileName = fileRequest!.FileName;
            dataSet.FilePath = filePath;
            dataSet.FileSize = fileRequest.FileSize;
            dataSet.FileType = Path.GetExtension(fileRequest.FileName).ToLowerInvariant() switch
            {
                AppConstants.DataProcessing.CSV_EXTENSION => FileType.CSV,
                AppConstants.DataProcessing.JSON_EXTENSION => FileType.JSON,
                AppConstants.DataProcessing.XML_EXTENSION => FileType.XML,
                AppConstants.DataProcessing.XLSX_EXTENSION => FileType.EXCEL,
                _ => FileType.UNKNOWN
            };
            dataSet.StorageProvider = StorageProvider.S3;
            dataSet.UploadedAt = DateTime.UtcNow;
            dataSet.IsProcessed = true;
            dataSet.ProcessedAt = DateTime.UtcNow;

            // Save to database
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATABASE_SAVE_STARTED);
            var savedDataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.AddAsync(dataSet),
                _infrastructure.DefaultTimeout);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATABASE_SAVED, new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = savedDataSet.Id
            });

            // Log audit action
            await _auditService.LogDataSetActionAsync(savedDataSet.Id, createDto.UserId, AppConstants.DataProcessing.AUDIT_ACTION_UPLOAD_DATA_SET, new Dictionary<string, object>
            {
                ["FileName"] = fileRequest.FileName,
                ["FileSize"] = fileRequest.FileSize,
                [AppConstants.DataStructures.CORRELATION_ID] = correlationId
            });

            _infrastructure.StructuredLogging.LogSummary(context, true, AppConstants.DataProcessing.UPLOAD_SUCCESSFUL);
            return new DataSetUploadResponse
            {
                Success = true,
                Message = AppConstants.DataProcessing.UPLOAD_SUCCESSFUL,
                DataSetId = savedDataSet.Id
            };
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogException(ex, AppConstants.DataProcessing.UPLOAD_FAILED);
            return new DataSetUploadResponse
            {
                Success = false,
                Message = $"{AppConstants.DataProcessing.UPLOAD_FAILED}: {ex.Message}"
            };
        }
    }

    public async Task<DataSetDto?> GetDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataProcessing.GET_DATA_SET,
            userId,
            new Dictionary<string, object> { [AppConstants.DataStructures.DATASETID] = id },
            () => ValidateGetDataSetInputs(id, userId),
            async (context) =>
            {
                var dataSet = await _dataSetRepository.GetByIdAsync(id);

                if (dataSet == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATASET_NOT_FOUND);
                    return null;
                }

                if (dataSet.UserId != userId)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER);
                    throw new UnauthorizedAccessException($"{AppConstants.DataProcessing.ACCESS_DENIED_TO_DATASET} {id}");
                }

                // Log audit action
                await _auditService.LogDataSetActionAsync(id, userId, AppConstants.DataProcessing.AUDIT_ACTION_VIEWED, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.CORRELATION_ID] = context.CorrelationId
                });

                return dataSet.ToDto();
            });
    }

    public async Task<DataSetDto?> UpdateDataSetAsync(int id, UpdateDataSetDto updateDto, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataProcessing.UPDATE_DATA_SET,
            userId,
            new Dictionary<string, object> { [AppConstants.DataStructures.DATASETID] = id },
            () => ValidateUpdateDataSetInputs(id, updateDto, userId),
            async (context) =>
            {
                var dataSet = await _dataSetRepository.GetByIdAsync(id);

                if (dataSet == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATASET_NOT_FOUND);
                    return null;
                }

                if (dataSet.UserId != userId)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER);
                    throw new UnauthorizedAccessException($"{AppConstants.DataProcessing.ACCESS_DENIED_TO_DATASET} {id}");
                }

                // Update properties using the mapper
                var updatedDataSet = updateDto.ToEntity(dataSet);

                if (updatedDataSet == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Failed to map update DTO to entity");
                    throw new InvalidOperationException("Failed to update dataset");
                }

                // Log retention expiry date update if provided
                if (updateDto.RetentionExpiryDate.HasValue)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Retention expiry date updated", new Dictionary<string, object>
                    {
                        ["OldExpiryDate"] = dataSet.RetentionExpiryDate?.ToString() ?? "Not set",
                        ["NewExpiryDate"] = updateDto.RetentionExpiryDate.Value
                    });
                }

                var result = await _dataSetRepository.UpdateAsync(updatedDataSet);

                if (result == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Repository update returned null");
                    throw new InvalidOperationException("Failed to update dataset in repository");
                }

                // Log audit action
                await _auditService.LogDataSetActionAsync(id, userId, AppConstants.DataProcessing.AUDIT_ACTION_UPDATE_DATA_SET, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.CORRELATION_ID] = context.CorrelationId
                });

                return result.ToDto();
            });
    }

    public async Task<bool> DeleteDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            AppConstants.DataProcessing.DELETE_DATA_SET,
            userId,
            new Dictionary<string, object> { [AppConstants.DataStructures.DATASETID] = id },
            () => ValidateDeleteInputs(id, userId),
            async (context) =>
            {
                var dataSet = await _dataSetRepository.GetByIdAsync(id);

                if (dataSet == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATASET_NOT_FOUND);
                    return false;
                }

                if (dataSet.UserId != userId)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER);
                    throw new UnauthorizedAccessException($"{AppConstants.DataProcessing.ACCESS_DENIED_TO_DATASET} {id}");
                }

                if (dataSet.IsDeleted)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATASET_IS_ALREADY_DELETED);
                    return true;
                }

                // Soft delete
                dataSet.IsDeleted = true;
                dataSet.DeletedAt = DateTime.UtcNow;

                await _dataSetRepository.UpdateAsync(dataSet);

                // Log audit action
                await _auditService.LogDataSetActionAsync(id, userId, AppConstants.DataProcessing.AUDIT_ACTION_DELETE_DATA_SET, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.CORRELATION_ID] = context.CorrelationId
                });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataProcessing.DATASET_SOFT_DELETED_SUCCESSFULLY);
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
            additionalMetadata);

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

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _infrastructure.StructuredLogging.LogWarning(AppConstants.DataProcessing.OPERATION_TIMED_OUT, new Dictionary<string, object>
            {
                ["TimeoutMs"] = timeout.TotalMilliseconds
            });
            throw new TimeoutException($"{AppConstants.DataProcessing.OPERATION_TIMED_OUT} after {timeout.TotalMilliseconds}ms");
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateUploadInputs(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        ArgumentNullException.ThrowIfNull(fileRequest);
        ArgumentNullException.ThrowIfNull(createDto);
        if (string.IsNullOrWhiteSpace(createDto.UserId)) throw new ArgumentException(AppConstants.DataProcessing.USER_ID_CANNOT_BE_NULL_OR_EMPTY, nameof(createDto));
        if (string.IsNullOrWhiteSpace(createDto.Name)) throw new ArgumentException(AppConstants.DataProcessing.NAME_CANNOT_BE_NULL_OR_EMPTY, nameof(createDto));
        if (string.IsNullOrWhiteSpace(fileRequest.FileName)) throw new ArgumentException(AppConstants.DataProcessing.FILE_NAME_CANNOT_BE_NULL_OR_EMPTY, nameof(fileRequest));
        if (fileRequest.FileSize <= 0) throw new ArgumentException(AppConstants.DataProcessing.FILE_SIZE_MUST_BE_POSITIVE, nameof(fileRequest));

        // Validate file name for security (prevent path traversal attacks)
        if (fileRequest.FileName.Contains("..") || fileRequest.FileName.Contains('/') || fileRequest.FileName.Contains('\\'))
            throw new ArgumentException(AppConstants.DataProcessing.INVALID_FILE_NAME, nameof(fileRequest));
    }

    private static void ValidateGetDataSetInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateUpdateDataSetInputs(int id, UpdateDataSetDto updateDto, string userId)
    {
        ValidateDataSetIdAndUserId(id, userId);
        ArgumentNullException.ThrowIfNull(updateDto);
        if (string.IsNullOrWhiteSpace(updateDto.Name)) throw new ArgumentException(AppConstants.DataProcessing.NAME_CANNOT_BE_NULL_OR_EMPTY, nameof(updateDto));
    }

    private static void ValidateDeleteInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateDataSetIdAndUserId(int id, string userId)
    {
        if (id <= 0) throw new ArgumentException(AppConstants.DataProcessing.DATASET_ID_MUST_BE_POSITIVE, nameof(id));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException(AppConstants.DataProcessing.USER_ID_CANNOT_BE_NULL_OR_EMPTY, nameof(userId));
    }

    #endregion
}