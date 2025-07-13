using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;

namespace Normaize.Data.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _uploadPath;
    private readonly ILogger<LocalStorageService> _logger;

    public LocalStorageService(IConfiguration configuration, ILogger<LocalStorageService> logger)
    {
        _uploadPath = configuration["FileUpload:UploadPath"] ?? "uploads";
        _logger = logger;
        
        // Ensure upload directory exists
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        var fileName = $"{Guid.NewGuid()}_{fileRequest.FileName}";
        var filePath = Path.Combine(_uploadPath, fileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await fileRequest.FileStream.CopyToAsync(fileStream);
        }

        _logger.LogInformation("File saved locally: {FilePath}", filePath);
        return filePath;
    }

    public Task<Stream> GetFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("File deleted locally: {FilePath}", filePath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }
} 