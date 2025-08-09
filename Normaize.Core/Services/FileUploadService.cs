using Normaize.Core.Interfaces;
using Normaize.Core.Models;

namespace Normaize.Core.Services;

/// <summary>
/// Main file upload service that orchestrates file upload operations by delegating to specialized sub-services.
/// This service has been refactored to follow the Single Responsibility Principle by extracting functionality
/// into focused sub-services while maintaining the same public API.
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly IFileUploadServices _fileUploadServices;

    public FileUploadService(
        IFileUploadServices fileUploadServices,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(fileUploadServices);
        ArgumentNullException.ThrowIfNull(infrastructure);

        _fileUploadServices = fileUploadServices;

        // Validate and log configuration on startup
        _fileUploadServices.Configuration.ValidateConfiguration();
        _fileUploadServices.Configuration.LogConfiguration();
    }

    /// <summary>
    /// Saves a file from a file upload request after validation.
    /// </summary>
    /// <param name="fileRequest">The file upload request containing the file data</param>
    /// <returns>The path where the file was saved</returns>
    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        // Validate the file first
        var isValid = await _fileUploadServices.Validation.ValidateFileAsync(fileRequest);
        if (!isValid)
        {
            throw new FileValidationException("File validation failed");
        }

        try
        {
            // Save the file using the storage service
            return await _fileUploadServices.Storage.SaveFileAsync(fileRequest);
        }
        catch (Exception ex) when (ex is not FileUploadException)
        {
            throw new FileUploadException("Failed to save file", ex);
        }
    }

    /// <summary>
    /// Validates a file upload request.
    /// </summary>
    /// <param name="fileRequest">The file upload request to validate</param>
    /// <returns>True if the file is valid, false otherwise</returns>
    public async Task<bool> ValidateFileAsync(FileUploadRequest fileRequest)
    {
        return await _fileUploadServices.Validation.ValidateFileAsync(fileRequest);
    }

    /// <summary>
    /// Processes a file and extracts data into a DataSet.
    /// </summary>
    /// <param name="filePath">The path to the file to process</param>
    /// <param name="fileType">The type of the file</param>
    /// <returns>The processed DataSet</returns>
    public async Task<DataSet> ProcessFileAsync(string filePath, string fileType)
    {
        return await _fileUploadServices.Processing.ProcessFileAsync(filePath, fileType);
    }

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    /// <param name="filePath">The path to the file to delete</param>
    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            await _fileUploadServices.Storage.DeleteFileAsync(filePath);
        }
        catch (Exception)
        {
            // Log the error but don't re-throw (as per original behavior)
            // The storage service already handles logging internally
        }
    }

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    /// <param name="filePath">The path to the file to check</param>
    /// <returns>True if the file exists, false otherwise</returns>
    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            return await _fileUploadServices.Storage.FileExistsAsync(filePath);
        }
        catch (Exception)
        {
            // Log the error but don't re-throw
            // Return false if we can't determine file existence
            return false;
        }
    }
}

// Custom exception types for better error handling
public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message) { }
    public FileValidationException(string message, Exception innerException) : base(message, innerException) { }
}

public class FileUploadException : Exception
{
    public FileUploadException(string message) : base(message) { }
    public FileUploadException(string message, Exception innerException) : base(message, innerException) { }
}

public class FileProcessingException : Exception
{
    public FileProcessingException(string message) : base(message) { }
    public FileProcessingException(string message, Exception innerException) : base(message, innerException) { }
}

public class UnsupportedFileTypeException : Exception
{
    public UnsupportedFileTypeException(string message) : base(message) { }
    public UnsupportedFileTypeException(string message, Exception innerException) : base(message, innerException) { }
}