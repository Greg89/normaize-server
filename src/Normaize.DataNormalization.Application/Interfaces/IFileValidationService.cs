using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Interfaces;

/// <summary>
/// Service for validating file upload requests and file properties.
/// Provides configuration-driven validation rules for file uploads.
/// </summary>
public interface IFileValidationService
{
    /// <summary>
    /// Validates file name, size, and extension according to configured rules.
    /// </summary>
    /// <param name="fileName">The name of the file to validate</param>
    /// <param name="fileSize">The size of the file in bytes</param>
    /// <returns>A validation result containing success status and error message if validation fails</returns>
    Task<FileValidationResult> ValidateFileAsync(string fileName, long fileSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the file size is within acceptable limits.
    /// </summary>
    /// <param name="fileSize">The size of the file in bytes</param>
    /// <param name="maxFileSize">The maximum allowed file size in bytes (optional, uses config default if not provided)</param>
    /// <returns>True if the file size is valid, false otherwise</returns>
    bool IsFileSizeValid(long fileSize, long? maxFileSize = null);

    /// <summary>
    /// Validates if the file extension is allowed and not blocked.
    /// </summary>
    /// <param name="fileName">The file name to extract extension from</param>
    /// <returns>True if the file extension is valid, false otherwise</returns>
    bool IsFileExtensionValid(string fileName);

    /// <summary>
    /// Validates that a file name is safe (no path traversal attacks).
    /// </summary>
    /// <param name="fileName">The file name to validate</param>
    /// <returns>True if the file name is safe, false otherwise</returns>
    bool IsFileNameSafe(string fileName);

    /// <summary>
    /// Gets the file extension from a file name.
    /// </summary>
    /// <param name="fileName">The file name to extract extension from</param>
    /// <returns>The file extension in lowercase</returns>
    string GetFileExtension(string fileName);

    /// <summary>
    /// Gets the list of allowed file extensions.
    /// </summary>
    /// <returns>Array of allowed file extensions</returns>
    string[] GetAllowedExtensions();

    /// <summary>
    /// Gets the list of blocked file extensions.
    /// </summary>
    /// <returns>Array of blocked file extensions</returns>
    string[] GetBlockedExtensions();

    /// <summary>
    /// Gets the maximum allowed file size in bytes.
    /// </summary>
    /// <returns>Maximum file size in bytes</returns>
    long GetMaxFileSize();
}
