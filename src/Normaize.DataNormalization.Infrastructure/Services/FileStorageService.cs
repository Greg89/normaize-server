using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Implementation of file storage service for saving and retrieving files
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _basePath;

    public FileStorageService(
        ILogger<FileStorageService> _logger,
        string basePath = "uploads")
    {
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
        _basePath = basePath;

        // Ensure the base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving file: {FileName} for user: {UserId}", fileName, userId);

        // Create user-specific directory
        var userDirectory = Path.Combine(_basePath, userId);
        if (!Directory.Exists(userDirectory))
        {
            Directory.CreateDirectory(userDirectory);
        }

        // Generate unique file name to avoid collisions
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(userDirectory, uniqueFileName);

        // Save the file
        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(fileStreamOutput, cancellationToken);
        }

        _logger.LogInformation("Successfully saved file: {FilePath}", filePath);
        return filePath;
    }

    public async Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting file: {FilePath}", filePath);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var memoryStream = new MemoryStream();
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting file: {FilePath}", filePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("Successfully deleted file: {FilePath}", filePath);
        }
        else
        {
            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(filePath));
    }
}
