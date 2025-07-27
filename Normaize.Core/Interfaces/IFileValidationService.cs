using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for validating file upload requests and file properties.
/// Extracted from FileUploadService to follow single responsibility principle.
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Validates a file upload request including size, extension, and basic properties.
    /// </summary>
    /// <param name="fileRequest">The file upload request to validate</param>
    /// <returns>True if the file is valid, false otherwise</returns>
    Task<bool> ValidateFileAsync(FileUploadRequest fileRequest);

    /// <summary>
    /// Validates if the file size is within acceptable limits.
    /// </summary>
    /// <param name="fileSize">The size of the file in bytes</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>True if the file size is valid, false otherwise</returns>
    bool IsFileSizeValid(long fileSize, IOperationContext context);

    /// <summary>
    /// Validates if the file extension is allowed and not blocked.
    /// </summary>
    /// <param name="fileExtension">The file extension to validate</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>True if the file extension is valid, false otherwise</returns>
    bool IsFileExtensionValid(string fileExtension, IOperationContext context);

    /// <summary>
    /// Validates a file upload request for required properties.
    /// </summary>
    /// <param name="fileRequest">The file upload request to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    void ValidateFileUploadRequest(FileUploadRequest fileRequest);

    /// <summary>
    /// Validates file processing inputs for required properties.
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <param name="fileType">The file type to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    void ValidateFileProcessingInputs(string filePath, string fileType);

    /// <summary>
    /// Validates a file path for required properties.
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    void ValidateFilePath(string filePath);

    /// <summary>
    /// Validates that a file exists at the specified path.
    /// </summary>
    /// <param name="filePath">The file path to check</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>Task that completes when validation is done</returns>
    /// <exception cref="FileNotFoundException">Thrown when file does not exist</exception>
    Task ValidateFileExistsAsync(string filePath, IOperationContext context);

    /// <summary>
    /// Gets the file extension from a file name.
    /// </summary>
    /// <param name="fileName">The file name to extract extension from</param>
    /// <returns>The file extension in lowercase</returns>
    string GetFileExtension(string fileName);
}