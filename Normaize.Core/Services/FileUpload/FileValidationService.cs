using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Diagnostics;

namespace Normaize.Core.Services.FileUpload;

/// <summary>
/// Service for validating file upload requests and file properties.
/// Extracted from FileUploadService to follow single responsibility principle.
/// </summary>
public class FileValidationService : IFileValidationService
{
    private readonly FileUploadConfiguration _fileUploadConfig;
    private readonly IStorageService _storageService;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public FileValidationService(
        IOptions<FileUploadConfiguration> fileUploadConfig,
        IStorageService storageService,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(fileUploadConfig);
        ArgumentNullException.ThrowIfNull(storageService);
        ArgumentNullException.ThrowIfNull(infrastructure);

        _fileUploadConfig = fileUploadConfig.Value;
        _storageService = storageService;
        _infrastructure = infrastructure;
    }

    public async Task<bool> ValidateFileAsync(FileUploadRequest fileRequest)
    {
        return await ExecuteValidationOperationAsync(
            operationName: nameof(ValidateFileAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                [AppConstants.FileProcessing.FILE_NAME_KEY] = fileRequest?.FileName ?? AppConstants.Messages.UNKNOWN,
                ["FileSize"] = fileRequest?.FileSize ?? 0
            },
            validation: () => ValidateFileUploadRequest(fileRequest!),
            operation: (context) =>
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_VALIDATION_STARTED);

                if (!IsFileSizeValid(fileRequest!.FileSize, context))
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_SIZE_VALIDATION_FAILED);
                    return Task.FromResult(false);
                }

                var fileExtension = GetFileExtension(fileRequest.FileName);

                if (!IsFileExtensionValid(fileExtension, context))
                {
                    _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_EXTENSION_VALIDATION_FAILED);
                    return Task.FromResult(false);
                }

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUploadMessages.FILE_VALIDATION_PASSED);

                return Task.FromResult(true);
            });
    }

    public bool IsFileSizeValid(long fileSize, IOperationContext context)
    {
        if (fileSize <= _fileUploadConfig.MaxFileSize) return true;

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_SIZE_EXCEEDS_LIMIT_WARNING, new Dictionary<string, object>
        {
            ["FileSize"] = fileSize,
            ["MaxSize"] = _fileUploadConfig.MaxFileSize
        });
        return false;
    }

    public bool IsFileExtensionValid(string fileExtension, IOperationContext context)
    {
        // Check if extension is blocked
        if (_fileUploadConfig.BlockedExtensions.Contains(fileExtension))
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_EXTENSION_BLOCKED_WARNING, new Dictionary<string, object>
            {
                ["Extension"] = fileExtension
            });
            return false;
        }

        // Check if extension is allowed
        if (!_fileUploadConfig.AllowedExtensions.Contains(fileExtension))
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_EXTENSION_NOT_ALLOWED_WARNING, new Dictionary<string, object>
            {
                ["Extension"] = fileExtension,
                ["AllowedExtensions"] = string.Join(", ", _fileUploadConfig.AllowedExtensions)
            });
            return false;
        }

        return true;
    }

    public void ValidateFileUploadRequest(FileUploadRequest fileRequest)
    {
        ArgumentNullException.ThrowIfNull(fileRequest);

        if (string.IsNullOrWhiteSpace(fileRequest.FileName))
            throw new ArgumentException(AppConstants.FileUpload.FILE_NAME_REQUIRED, nameof(fileRequest));

        if (fileRequest.FileSize <= 0)
            throw new ArgumentException(AppConstants.FileUpload.FILE_SIZE_MUST_BE_POSITIVE, nameof(fileRequest));

        // Validate file name for security (prevent path traversal attacks)
        if (fileRequest.FileName.Contains("..") || fileRequest.FileName.Contains('/') || fileRequest.FileName.Contains('\\'))
            throw new ArgumentException("Invalid file name", nameof(fileRequest));
    }

    public void ValidateFileProcessingInputs(string filePath, string fileType)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(AppConstants.FileUpload.FILE_PATH_REQUIRED, nameof(filePath));

        if (string.IsNullOrWhiteSpace(fileType))
            throw new ArgumentException(AppConstants.FileUpload.FILE_TYPE_REQUIRED, nameof(fileType));
    }

    public void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(AppConstants.FileUpload.FILE_PATH_REQUIRED, nameof(filePath));
    }

    public async Task ValidateFileExistsAsync(string filePath, IOperationContext context)
    {
        var fileExists = await _storageService.FileExistsAsync(filePath);
        if (!fileExists)
        {
            var error = string.Format(AppConstants.FileUpload.FILE_NOT_FOUND_ERROR, filePath);
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FILE_NOT_FOUND_PROCESSING_WARNING, new Dictionary<string, object>
            {
                [AppConstants.FileProcessing.FILE_PATH_KEY] = filePath
            });
            throw new FileNotFoundException(error);
        }
    }

    public string GetFileExtension(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant();

    #region Private Methods

    private async Task<T> ExecuteValidationOperationAsync<T>(
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
            if (ex is FileNotFoundException || ex is FileValidationException)
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
            case nameof(ValidateFileAsync):
                var fileName = metadata.TryGetValue(AppConstants.FileProcessing.FILE_NAME_KEY, out var name) ? name?.ToString() : AppConstants.Messages.UNKNOWN;
                return $"Failed to complete {operationName} for file '{fileName}'";

            default:
                return $"Failed to complete {operationName}";
        }
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion
}

// Custom exception type for file validation errors
public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message) { }
    public FileValidationException(string message, Exception innerException) : base(message, innerException) { }
}