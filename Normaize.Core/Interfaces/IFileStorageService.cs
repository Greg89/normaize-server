using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for file storage operations specific to file upload functionality.
/// Extends IStorageService with file upload specific operations.
/// </summary>
public interface IFileStorageService : IStorageService
{
    /// <summary>
    /// Saves a file from a file upload request.
    /// </summary>
    /// <param name="fileRequest">The file upload request containing the file data</param>
    /// <returns>The path where the file was saved</returns>
    Task<string> SaveFileAsync(FileUploadRequest fileRequest);

    /// <summary>
    /// Deletes a file at the specified path with chaos engineering and logging.
    /// </summary>
    /// <param name="filePath">The path to the file to delete</param>
    /// <returns>Task that completes when deletion is done</returns>
    Task DeleteFileAsync(string filePath);

    /// <summary>
    /// Determines the storage provider from a file path.
    /// </summary>
    /// <param name="filePath">The file path to analyze</param>
    /// <returns>The corresponding StorageProvider enum value</returns>
    StorageProvider GetStorageProviderFromPath(string filePath);
}