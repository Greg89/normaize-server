using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for file utility operations like type detection, hash generation, and storage strategy.
/// Extracted from FileUploadService to follow single responsibility principle.
/// </summary>
public interface IFileUtilityService
{
    /// <summary>
    /// Generates a SHA256 hash for the file content to detect changes.
    /// </summary>
    /// <param name="filePath">The path to the file to hash</param>
    /// <returns>The base64 encoded hash string</returns>
    Task<string> GenerateDataHashAsync(string filePath);

    /// <summary>
    /// Determines the file type from a file extension.
    /// </summary>
    /// <param name="fileType">The file extension to analyze</param>
    /// <returns>The corresponding FileType enum value</returns>
    FileType GetFileTypeFromExtension(string fileType);

    /// <summary>
    /// Determines if a DataSet should use a separate table based on size and configuration.
    /// </summary>
    /// <param name="dataSet">The DataSet to evaluate</param>
    /// <returns>True if the DataSet should use a separate table</returns>
    bool ShouldUseSeparateTable(DataSet dataSet);

    /// <summary>
    /// Gets the file extension from a file name.
    /// </summary>
    /// <param name="fileName">The file name to extract extension from</param>
    /// <returns>The file extension in lowercase</returns>
    string GetFileExtension(string fileName);

    /// <summary>
    /// Determines the storage provider from a file path.
    /// </summary>
    /// <param name="filePath">The file path to analyze</param>
    /// <returns>The corresponding StorageProvider enum value</returns>
    StorageProvider GetStorageProviderFromPath(string filePath);
}