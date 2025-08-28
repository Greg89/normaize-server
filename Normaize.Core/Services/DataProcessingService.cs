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
                [FileProcessingConstants.FileProcessing.FILE_NAME_KEY] = fileRequest?.FileName ?? SharedConstants.Messages.UNKNOWN
            });

        // Validate inputs first (before try-catch so exceptions are thrown)
        ValidateUploadInputs(fileRequest!, createDto!);

        try
        {
            // Chaos engineering: Simulate processing delay
            await _infrastructure.ChaosEngineering.ExecuteChaosAsync(ChaosEngineeringConstants.ChaosEngineering.PROCESSING_DELAY, correlationId, context.OperationName, async () =>
            {
                var delayMs = new Random().Next(ChaosEngineeringConstants.ChaosEngineering.MIN_PROCESSING_DELAY_MS, ChaosEngineeringConstants.ChaosEngineering.MAX_PROCESSING_DELAY_MS);
                _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Processing delay", new Dictionary<string, object>
                {
                    ["DelayMs"] = delayMs
                });
                await Task.Delay(delayMs);
            }, new Dictionary<string, object> { ["UserId"] = createDto?.UserId ?? SharedConstants.Messages.UNKNOWN });

            // Validate file
            _infrastructure.StructuredLogging.LogStep(context, FileProcessingConstants.FileUploadMessages.FILE_VALIDATION_STARTED);
            if (!await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ValidateFileAsync(fileRequest!),
                _infrastructure.QuickTimeout))
            {
                _infrastructure.StructuredLogging.LogStep(context, FileProcessingConstants.FileUploadMessages.FILE_VALIDATION_FAILED);
                _infrastructure.StructuredLogging.LogSummary(context, false, FileProcessingConstants.FileUploadMessages.FILE_VALIDATION_FAILED);
                return new DataSetUploadResponse
                {
                    Success = false,
                    Message = FileProcessingConstants.FileUploadMessages.FILE_VALIDATION_FAILED
                };
            }
            _infrastructure.StructuredLogging.LogStep(context, FileProcessingConstants.FileUploadMessages.FILE_VALIDATION_PASSED);

            // Save file
            _infrastructure.StructuredLogging.LogStep(context, "File save started");
            var filePath = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.SaveFileAsync(fileRequest!),
                _infrastructure.DefaultTimeout);
            _infrastructure.StructuredLogging.LogStep(context, "File saved", new Dictionary<string, object>
            {
                [FileProcessingConstants.FileProcessing.FILE_PATH_KEY] = filePath
            });

            // Process file and create dataset
            _infrastructure.StructuredLogging.LogStep(context, "File processing started");
            var dataSet = await ExecuteWithTimeoutAsync(
                () => _fileUploadService.ProcessFileAsync(filePath, Path.GetExtension(fileRequest!.FileName)),
                _infrastructure.DefaultTimeout);
            _infrastructure.StructuredLogging.LogStep(context, "File processed", new Dictionary<string, object>
            {
                ["RowCount"] = dataSet.RowCount,
                ["ColumnCount"] = dataSet.ColumnCount
            });

            // Get user settings for retention policy
            _infrastructure.StructuredLogging.LogStep(context, "User settings retrieval started");
            var userSettings = await ExecuteWithTimeoutAsync(
                () => _userSettingsService.GetUserSettingsAsync(createDto!.UserId),
                _infrastructure.QuickTimeout);

            // Set retention expiry date based on user settings
            var retentionDays = userSettings?.RetentionDays ?? 365; // Default to 1 year if no settings
            dataSet.RetentionExpiryDate = DateTime.UtcNow.AddDays(retentionDays);
            _infrastructure.StructuredLogging.LogStep(context, "Retention policy set", new Dictionary<string, object>
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
                ".csv" => FileType.CSV,
                ".json" => FileType.JSON,
                ".xml" => FileType.XML,
                ".xlsx" => FileType.EXCEL,
                _ => FileType.UNKNOWN
            };
            dataSet.StorageProvider = StorageProvider.S3;
            dataSet.UploadedAt = DateTime.UtcNow;
            dataSet.IsProcessed = true;
            dataSet.ProcessedAt = DateTime.UtcNow;

            // Save to database
            _infrastructure.StructuredLogging.LogStep(context, "Database save started");
            var savedDataSet = await ExecuteWithTimeoutAsync(
                () => _dataSetRepository.AddAsync(dataSet),
                _infrastructure.DefaultTimeout);
            _infrastructure.StructuredLogging.LogStep(context, "Database saved", new Dictionary<string, object>
            {
                [SharedConstants.DataStructures.DATASETID] = savedDataSet.Id
            });

            // Log audit action
            await _auditService.LogDataSetActionAsync(savedDataSet.Id, createDto.UserId, "UploadDataSet", new Dictionary<string, object>
            {
                ["FileName"] = fileRequest.FileName,
                ["FileSize"] = fileRequest.FileSize,
                [SharedConstants.DataStructures.CORRELATION_ID] = correlationId
            });

            _infrastructure.StructuredLogging.LogSummary(context, true, "Upload successful");
            return new DataSetUploadResponse
            {
                Success = true,
                Message = "Upload successful",
                DataSetId = savedDataSet.Id
            };
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogException(ex, "Upload failed");
            return new DataSetUploadResponse
            {
                Success = false,
                Message = $"Upload failed: {ex.Message}"
            };
        }
    }

    public async Task<DataSetDto?> GetDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            "GetDataSet",
            userId,
            new Dictionary<string, object> { [SharedConstants.DataStructures.DATASETID] = id },
            () => ValidateGetDataSetInputs(id, userId),
            async (context) =>
            {
                var dataSet = await _dataSetRepository.GetByIdAsync(id);

                if (dataSet == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Dataset not found");
                    return null;
                }

                if (dataSet.UserId != userId)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Access denied - dataset belongs to different user");
                    throw new UnauthorizedAccessException($"{"Access denied to dataset"} {id}");
                }

                // Log audit action
                await _auditService.LogDataSetActionAsync(id, userId, "Viewed", new Dictionary<string, object>
                {
                    [SharedConstants.DataStructures.CORRELATION_ID] = context.CorrelationId
                });

                return dataSet.ToDto();
            });
    }

    public async Task<DataSetDto?> UpdateDataSetAsync(int id, UpdateDataSetDto updateDto, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            "UpdateDataSet",
            userId,
            new Dictionary<string, object> { [SharedConstants.DataStructures.DATASETID] = id },
            () => ValidateUpdateDataSetInputs(id, updateDto, userId),
            async (context) =>
            {
                var dataSet = await _dataSetRepository.GetByIdAsync(id);

                if (dataSet == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Dataset not found");
                    return null;
                }

                if (dataSet.UserId != userId)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Access denied - dataset belongs to different user");
                    throw new UnauthorizedAccessException($"{"Access denied to dataset"} {id}");
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
                await _auditService.LogDataSetActionAsync(id, userId, "UpdateDataSet", new Dictionary<string, object>
                {
                    [SharedConstants.DataStructures.CORRELATION_ID] = context.CorrelationId
                });

                return result.ToDto();
            });
    }

    public async Task<bool> DeleteDataSetAsync(int id, string userId)
    {
        return await ExecuteDataSetOperationAsync(
            "DeleteDataSet",
            userId,
            new Dictionary<string, object> { [SharedConstants.DataStructures.DATASETID] = id },
            () => ValidateDeleteInputs(id, userId),
            async (context) =>
            {
                var dataSet = await _dataSetRepository.GetByIdAsync(id);

                if (dataSet == null)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Dataset not found");
                    return false;
                }

                if (dataSet.UserId != userId)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Access denied - dataset belongs to different user");
                    throw new UnauthorizedAccessException($"{"Access denied to dataset"} {id}");
                }

                if (dataSet.IsDeleted)
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Dataset is already deleted");
                    return true;
                }

                // Soft delete
                dataSet.IsDeleted = true;
                dataSet.DeletedAt = DateTime.UtcNow;

                await _dataSetRepository.UpdateAsync(dataSet);

                // Log audit action
                await _auditService.LogDataSetActionAsync(id, userId, "DeleteDataSet", new Dictionary<string, object>
                {
                    [SharedConstants.DataStructures.CORRELATION_ID] = context.CorrelationId
                });

                _infrastructure.StructuredLogging.LogStep(context, "Dataset soft deleted successfully");
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
            _infrastructure.StructuredLogging.LogWarning("Operation timed out", new Dictionary<string, object>
            {
                ["TimeoutMs"] = timeout.TotalMilliseconds
            });
            throw new TimeoutException($"{"Operation timed out"} after {timeout.TotalMilliseconds}ms");
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateUploadInputs(FileUploadRequest fileRequest, CreateDataSetDto createDto)
    {
        ArgumentNullException.ThrowIfNull(fileRequest);
        ArgumentNullException.ThrowIfNull(createDto);
        if (string.IsNullOrWhiteSpace(createDto.UserId)) throw new ArgumentException("User ID cannot be null or empty");
        if (string.IsNullOrWhiteSpace(createDto.Name)) throw new ArgumentException("Name cannot be null or empty");
        if (string.IsNullOrWhiteSpace(fileRequest.FileName)) throw new ArgumentException("File name cannot be null or empty");
        if (fileRequest.FileSize <= 0) throw new ArgumentException("File size must be positive");

        // Validate file name for security (prevent path traversal attacks)
        if (fileRequest.FileName.Contains("..") || fileRequest.FileName.Contains('/') || fileRequest.FileName.Contains('\\'))
            throw new ArgumentException("Invalid file name");
    }

    private static void ValidateGetDataSetInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateUpdateDataSetInputs(int id, UpdateDataSetDto updateDto, string userId)
    {
        ValidateDataSetIdAndUserId(id, userId);
        ArgumentNullException.ThrowIfNull(updateDto);
        if (string.IsNullOrWhiteSpace(updateDto.Name)) throw new ArgumentException("Name cannot be null or empty");
    }

    private static void ValidateDeleteInputs(int id, string userId) => ValidateDataSetIdAndUserId(id, userId);

    private static void ValidateDataSetIdAndUserId(int id, string userId)
    {
        if (id <= 0) throw new ArgumentException("Dataset ID must be positive");
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or empty");
    }

    #endregion
}