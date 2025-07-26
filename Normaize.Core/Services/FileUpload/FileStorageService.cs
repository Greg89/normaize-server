using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Diagnostics;

namespace Normaize.Core.Services.FileUpload;

public class FileStorageService : IFileStorageService
{
    private readonly IStorageService _storageService;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public FileStorageService(
        IStorageService storageService,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(storageService);
        ArgumentNullException.ThrowIfNull(infrastructure);

        _storageService = storageService;
        _infrastructure = infrastructure;
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        return await ExecuteStorageOperationAsync(
            operationName: nameof(SaveFileAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.FileProcessing.FILE_NAME_KEY] = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN,
                ["FileSize"] = fileRequest?.FileSize ?? 0
            },
            validation: () => { }, // Validation handled by validation service
            operation: async (context) =>
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_UPLOAD_STARTED);

                // Apply chaos engineering for file upload
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync(
                    AppConstants.FileProcessing.STORAGE_FAILURE_SCENARIO,
                    GetCorrelationId(),
                    context.OperationName,
                    async () => await Task.Delay(AppConstants.FileUpload.FILE_UPLOAD_CHAOS_DELAY_MS),
                    new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_NAME_KEY] = fileRequest!.FileName });

                try
                {
                    var filePath = await _storageService.SaveFileAsync(fileRequest!);

                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_UPLOAD_SUCCESS);

                    return filePath;
                }
                catch (Exception ex)
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_UPLOAD_FAILED);
                    throw new FileUploadException(string.Format(AppConstants.FileUpload.FAILED_SAVE_FILE_ERROR, fileRequest!.FileName), ex);
                }
            });
    }

    public async Task DeleteFileAsync(string filePath)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(
            nameof(DeleteFileAsync),
            correlationId,
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath });

        try
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_DELETION_STARTED);

            // Apply chaos engineering for file deletion
            await _infrastructure.ChaosEngineering.ExecuteChaosAsync(
                AppConstants.FileProcessing.STORAGE_FAILURE_SCENARIO,
                correlationId,
                context.OperationName,
                async () => await Task.Delay(AppConstants.FileUpload.FILE_DELETION_CHAOS_DELAY_MS),
                new Dictionary<string, object> { [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath });

            try
            {
                await _storageService.DeleteFileAsync(filePath);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_DELETED_SUCCESS);
            }
            catch (Exception)
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_DELETION_FAILED);
                // Don't re-throw - log and continue (as per original behavior)
            }

            _infrastructure.StructuredLogging.LogSummary(context, true);
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogSummary(context, false, ex.Message);
            throw;
        }
    }

    public StorageProvider GetStorageProviderFromPath(string filePath)
    {
        return filePath switch
        {
            var path when path.StartsWith("s3://") => StorageProvider.S3,
            var path when path.StartsWith("azure://") => StorageProvider.Azure,
            var path when path.StartsWith("memory://") => StorageProvider.Memory,
            _ => StorageProvider.Local
        };
    }

    #region IStorageService Implementation (Delegation)

    public Task<Stream> GetFileAsync(string filePath) => _storageService.GetFileAsync(filePath);

    public Task<bool> FileExistsAsync(string filePath) => _storageService.FileExistsAsync(filePath);

    #endregion

    #region Private Methods

    private async Task<T> ExecuteStorageOperationAsync<T>(
        string operationName,
        Dictionary<string, object>? additionalMetadata,
        Action validation,
        Func<IOperationContext, Task<T>> operation)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(operationName, correlationId, AppConstants.Auth.AnonymousUser, additionalMetadata);

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

            // Preserve specific exception types for better error handling
            if (ex is FileUploadException)
            {
                throw; // Re-throw specific exceptions as-is
            }

            // Create detailed error message based on operation type and metadata
            var errorMessage = CreateDetailedErrorMessage(operationName, additionalMetadata);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private static string CreateDetailedErrorMessage(string operationName, Dictionary<string, object>? metadata)
    {
        if (metadata == null) return $"Failed to complete {operationName}";

        // Handle specific operation types with detailed error messages
        switch (operationName)
        {
            case nameof(SaveFileAsync):
                var fileName = metadata.TryGetValue(AppConstants.FileProcessing.FILE_NAME_KEY, out var name) ? name?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for file '{fileName}'";

            case nameof(DeleteFileAsync):
                var filePath = metadata.TryGetValue(AppConstants.FileProcessing.FILE_PATH_KEY, out var path) ? path?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for file '{filePath}'";

            default:
                return $"Failed to complete {operationName}";
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion
}

// Custom exception type for file upload errors
public class FileUploadException : Exception
{
    public FileUploadException(string message) : base(message) { }
    public FileUploadException(string message, Exception innerException) : base(message, innerException) { }
}