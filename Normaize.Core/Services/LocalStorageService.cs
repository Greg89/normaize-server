using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;

namespace Normaize.Core.Services;

public class LocalStorageService : IStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _uploadPath;

    public LocalStorageService(IConfiguration configuration, ILogger<LocalStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _uploadPath = _configuration.GetValue<string>("Storage:Local:UploadPath") ?? 
                     Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        
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
            throw new FileNotFoundException($"File not found: {filePath}");

        var stream = File.OpenRead(filePath);
        return Task.FromResult<Stream>(stream);
    }

    public Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("File deleted: {FilePath}", filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath));
    }

    public Task<long> GetFileSizeAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return Task.FromResult(0L);

        var fileInfo = new FileInfo(filePath);
        return Task.FromResult(fileInfo.Length);
    }

    public Task<string> GetFileUrlAsync(string filePath, TimeSpan? expiry = null)
    {
        // For local storage, return a relative path that can be served by the web server
        var relativePath = Path.GetRelativePath(_uploadPath, filePath);
        var url = $"/uploads/{relativePath.Replace('\\', '/')}";
        return Task.FromResult(url);
    }
} 