using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Collections.Concurrent;

namespace Normaize.Data.Services;

public class InMemoryStorageService : IStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> _fileStorage = new();
    private readonly ILogger<InMemoryStorageService> _logger;
    private readonly long _maxFileSizeBytes;
    private readonly Timer? _cleanupTimer;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30); // Cleanup every 30 minutes

    public InMemoryStorageService(ILogger<InMemoryStorageService> logger)
    {
        _logger = logger;
        _maxFileSizeBytes = 100 * 1024 * 1024; // 100MB default limit
        
        // Start cleanup timer for memory management
        _cleanupTimer = new Timer(CleanupExpiredFiles, null, _cleanupInterval, _cleanupInterval);
        
        _logger.LogInformation("InMemoryStorageService initialized with max file size: {MaxFileSizeMB}MB", _maxFileSizeBytes / (1024 * 1024));
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        ValidateFileRequest(fileRequest);
        
        var fileName = $"{Guid.NewGuid()}_{fileRequest.FileName}";
        var filePath = $"memory://{fileName}";

        using var memoryStream = new MemoryStream();
        await fileRequest.FileStream.CopyToAsync(memoryStream);
        
        var fileData = memoryStream.ToArray();
        
        // Validate file size
        if (fileData.Length > _maxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size {fileData.Length} bytes exceeds maximum allowed size of {_maxFileSizeBytes} bytes");
        }
        
        _fileStorage[filePath] = fileData;
        
        _logger.LogInformation("File saved in memory: {FilePath}, Size: {FileSize} bytes", filePath, fileData.Length);
        return filePath;
    }

    public Task<Stream> GetFileAsync(string filePath)
    {
        ValidateFilePath(filePath);
        
        if (!_fileStorage.TryGetValue(filePath, out var fileData))
        {
            throw new FileNotFoundException($"File not found in memory: {filePath}");
        }

        // Return a new MemoryStream with the file data to avoid disposal issues
        var stream = new MemoryStream(fileData, false);
        return Task.FromResult<Stream>(stream);
    }

    public Task DeleteFileAsync(string filePath)
    {
        ValidateFilePath(filePath);
        
        if (_fileStorage.TryRemove(filePath, out var removedData))
        {
            _logger.LogInformation("File deleted from memory: {FilePath}, Size: {FileSize} bytes", filePath, removedData.Length);
        }
        else
        {
            _logger.LogWarning("Attempted to delete non-existent file: {FilePath}", filePath);
        }
        
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath)
    {
        ValidateFilePath(filePath);
        return Task.FromResult(_fileStorage.ContainsKey(filePath));
    }

    private void ValidateFileRequest(FileUploadRequest fileRequest)
    {
        if (fileRequest == null)
            throw new ArgumentNullException(nameof(fileRequest));
        
        if (string.IsNullOrWhiteSpace(fileRequest.FileName))
            throw new ArgumentException("FileName cannot be null or empty", nameof(fileRequest));
        
        if (fileRequest.FileStream == null)
            throw new ArgumentException("FileStream cannot be null", nameof(fileRequest));
        
        if (!fileRequest.FileStream.CanRead)
            throw new ArgumentException("FileStream must be readable", nameof(fileRequest));
    }

    private void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("FilePath cannot be null or empty", nameof(filePath));
        
        if (!filePath.StartsWith("memory://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("FilePath must start with 'memory://'", nameof(filePath));
    }

    private void CleanupExpiredFiles(object? state)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var filesToRemove = new List<string>();
            
            if (_fileStorage.Count > 1000) 
            {
                var oldestFiles = _fileStorage.Take(_fileStorage.Count - 500).Select(kvp => kvp.Key).ToList();
                filesToRemove.AddRange(oldestFiles);
            }
            
            foreach (var filePath in filesToRemove)
            {
                if (_fileStorage.TryRemove(filePath, out var removedData))
                {
                    _logger.LogDebug("Cleaned up file from memory: {FilePath}, Size: {FileSize} bytes", filePath, removedData.Length);
                }
            }
            
            if (filesToRemove.Count > 0)
            {
                _logger.LogInformation("Memory cleanup completed: removed {FileCount} files", filesToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory cleanup");
        }
    }

    public (int FileCount, long TotalSizeBytes) GetStorageStatistics()
    {
        var totalSize = _fileStorage.Values.Sum(data => data.Length);
        return (_fileStorage.Count, totalSize);
    }

    public void ClearAllFiles()
    {
        var fileCount = _fileStorage.Count;
        _fileStorage.Clear();
        _logger.LogInformation("Cleared all files from memory: {FileCount} files removed", fileCount);
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
    }
} 