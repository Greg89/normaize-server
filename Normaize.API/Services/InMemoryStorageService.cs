using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;

namespace Normaize.API.Services;

public class InMemoryStorageService : IStorageService
{
    private readonly Dictionary<string, byte[]> _fileStorage = new();
    private readonly ILogger<InMemoryStorageService> _logger;

    public InMemoryStorageService(ILogger<InMemoryStorageService> logger)
    {
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        var fileName = $"{Guid.NewGuid()}_{fileRequest.FileName}";
        var filePath = $"memory://{fileName}";

        using var memoryStream = new MemoryStream();
        await fileRequest.FileStream.CopyToAsync(memoryStream);
        
        _fileStorage[filePath] = memoryStream.ToArray();
        
        _logger.LogInformation("File saved in memory: {FilePath}", filePath);
        return filePath;
    }

    public Task<Stream> GetFileAsync(string filePath)
    {
        if (!_fileStorage.ContainsKey(filePath))
        {
            throw new FileNotFoundException($"File not found in memory: {filePath}");
        }

        var fileData = _fileStorage[filePath];
        return Task.FromResult<Stream>(new MemoryStream(fileData));
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (_fileStorage.ContainsKey(filePath))
        {
            _fileStorage.Remove(filePath);
            _logger.LogInformation("File deleted from memory: {FilePath}", filePath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(_fileStorage.ContainsKey(filePath));
    }
} 