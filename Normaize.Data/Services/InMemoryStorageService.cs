using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Normaize.Data.Services;

public class InMemoryStorageService : IStorageService, IDisposable
{
    private readonly ConcurrentDictionary<string, FileMetadata> _fileStorage = new();
    private readonly ILogger<InMemoryStorageService> _logger;
    private readonly InMemoryStorageOptions _options;
    private readonly Timer? _cleanupTimer;
    private readonly SemaphoreSlim _storageSemaphore;
    private readonly Random _chaosRandom;
    private bool _disposed;

    public InMemoryStorageService(
        ILogger<InMemoryStorageService> logger,
        IOptions<InMemoryStorageOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _storageSemaphore = new SemaphoreSlim(_options.MaxConcurrentOperations, _options.MaxConcurrentOperations);
        _chaosRandom = new Random();
        
        // Start cleanup timer for memory management
        _cleanupTimer = new Timer(CleanupExpiredFiles, null, _options.CleanupInterval, _options.CleanupInterval);
        
        _logger.LogInformation("InMemoryStorageService initialized with max file size: {MaxFileSizeMB}MB, Max files: {MaxFiles}, Cleanup interval: {CleanupInterval}",
            _options.MaxFileSizeBytes / (1024 * 1024), _options.MaxFiles, _options.CleanupInterval);
    }

    public async Task<string> SaveFileAsync(FileUploadRequest fileRequest)
    {
        ThrowIfDisposed();
        
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var operationName = "SaveFileAsync";
        
        _logger.LogInformation("Starting file save operation. CorrelationId: {CorrelationId}, FileName: {FileName}, FileSize: {FileSize}",
            correlationId, fileRequest?.FileName, fileRequest?.FileSize);

        try
        {
            // Validate inputs first (before try-catch so exceptions are thrown)
            ValidateSaveFileInputs(fileRequest);

            return await ExecuteWithTimeoutAsync(
                async () => await SaveFileInternalAsync(fileRequest!, correlationId),
                _options.OperationTimeout,
                correlationId,
                operationName);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Failed to save file. CorrelationId: {CorrelationId}, FileName: {FileName}",
                correlationId, fileRequest?.FileName);
            throw new InvalidOperationException($"Failed to complete {operationName} for file '{fileRequest?.FileName}'", ex);
        }
    }

    public async Task<Stream> GetFileAsync(string filePath)
    {
        ThrowIfDisposed();
        
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var operationName = "GetFileAsync";
        
        _logger.LogInformation("Starting file retrieval operation. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
            correlationId, filePath);

        try
        {
            // Validate inputs first
            ValidateFilePath(filePath);

            return await ExecuteWithTimeoutAsync(
                async () => await GetFileInternalAsync(filePath, correlationId),
                _options.OperationTimeout,
                correlationId,
                operationName);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Failed to retrieve file. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
                correlationId, filePath);
            throw new InvalidOperationException($"Failed to complete {operationName} for file path '{filePath}'", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        ThrowIfDisposed();
        
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var operationName = "DeleteFileAsync";
        
        _logger.LogInformation("Starting file deletion operation. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
            correlationId, filePath);

        try
        {
            // Validate inputs first
            ValidateFilePath(filePath);

            await ExecuteWithTimeoutAsync(
                async () => await DeleteFileInternalAsync(filePath, correlationId),
                _options.OperationTimeout,
                correlationId,
                operationName);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Failed to delete file. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
                correlationId, filePath);
            throw new InvalidOperationException($"Failed to complete {operationName} for file path '{filePath}'", ex);
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        ThrowIfDisposed();
        
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var operationName = "FileExistsAsync";
        
        _logger.LogDebug("Checking file existence. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
            correlationId, filePath);

        try
        {
            // Validate inputs first
            ValidateFilePath(filePath);

            return await ExecuteWithTimeoutAsync(
                async () => await FileExistsInternalAsync(filePath, correlationId),
                _options.QuickTimeout,
                correlationId,
                operationName);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Failed to check file existence. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
                correlationId, filePath);
            throw new InvalidOperationException($"Failed to complete {operationName} for file path '{filePath}'", ex);
        }
    }

    private async Task<string> SaveFileInternalAsync(FileUploadRequest fileRequest, string correlationId)
    {
        await _storageSemaphore.WaitAsync();
        
        try
        {
            // Chaos engineering: Simulate storage full condition
            if (_chaosRandom.NextDouble() < _options.ChaosStorageFullProbability)
            {
                _logger.LogWarning("Chaos engineering: Simulating storage full condition. CorrelationId: {CorrelationId}", correlationId);
                throw new InvalidOperationException("Storage is full (chaos engineering simulation)");
            }

            // Check storage limits
            if (_fileStorage.Count >= _options.MaxFiles)
            {
                _logger.LogWarning("Storage limit reached. Max files: {MaxFiles}, Current files: {CurrentFiles}. CorrelationId: {CorrelationId}",
                    _options.MaxFiles, _fileStorage.Count, correlationId);
                throw new InvalidOperationException($"Storage limit reached. Maximum {_options.MaxFiles} files allowed.");
            }

            var fileName = SanitizeFileName(fileRequest.FileName);
            var fileId = GenerateFileId();
            var filePath = $"memory://{fileId}_{fileName}";

            using var memoryStream = new MemoryStream();
            await fileRequest.FileStream.CopyToAsync(memoryStream);
            
            var fileData = memoryStream.ToArray();
            
            // Validate file size
            if (fileData.Length > _options.MaxFileSizeBytes)
            {
                _logger.LogWarning("File size exceeds limit. FileSize: {FileSize}, MaxSize: {MaxSize}. CorrelationId: {CorrelationId}",
                    fileData.Length, _options.MaxFileSizeBytes, correlationId);
                throw new InvalidOperationException($"File size {fileData.Length} bytes exceeds maximum allowed size of {_options.MaxFileSizeBytes} bytes");
            }

            // Calculate file hash for integrity
            var fileHash = CalculateFileHash(fileData);
            
            var metadata = new FileMetadata
            {
                Data = fileData,
                FileName = fileName,
                FileSize = fileData.Length,
                FileHash = fileHash,
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow
            };

            _fileStorage[filePath] = metadata;
            
            _logger.LogInformation("File saved successfully. CorrelationId: {CorrelationId}, FilePath: {FilePath}, Size: {FileSize} bytes, Hash: {FileHash}",
                correlationId, filePath, fileData.Length, fileHash);
            
            return filePath;
        }
        finally
        {
            _storageSemaphore.Release();
        }
    }

    private async Task<Stream> GetFileInternalAsync(string filePath, string correlationId)
    {
        await _storageSemaphore.WaitAsync();
        
        try
        {
            // Chaos engineering: Simulate file corruption
            if (_chaosRandom.NextDouble() < _options.ChaosFileCorruptionProbability)
            {
                _logger.LogWarning("Chaos engineering: Simulating file corruption. CorrelationId: {CorrelationId}", correlationId);
                throw new InvalidOperationException("File is corrupted (chaos engineering simulation)");
            }

            if (!_fileStorage.TryGetValue(filePath, out var metadata))
            {
                _logger.LogWarning("File not found in memory. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
                    correlationId, filePath);
                throw new FileNotFoundException($"File not found in memory: {filePath}");
            }

            // Update last accessed time
            metadata.LastAccessed = DateTime.UtcNow;

            // Verify file integrity
            var currentHash = CalculateFileHash(metadata.Data);
            if (currentHash != metadata.FileHash)
            {
                _logger.LogError("File integrity check failed. CorrelationId: {CorrelationId}, FilePath: {FilePath}, ExpectedHash: {ExpectedHash}, ActualHash: {ActualHash}",
                    correlationId, filePath, metadata.FileHash, currentHash);
                throw new InvalidOperationException("File integrity check failed - file may be corrupted");
            }

            // Return a new MemoryStream with the file data to avoid disposal issues
            var stream = new MemoryStream(metadata.Data, false);
            
            _logger.LogDebug("File retrieved successfully. CorrelationId: {CorrelationId}, FilePath: {FilePath}, Size: {FileSize} bytes",
                correlationId, filePath, metadata.Data.Length);
            
            return stream;
        }
        finally
        {
            _storageSemaphore.Release();
        }
    }

    private async Task DeleteFileInternalAsync(string filePath, string correlationId)
    {
        await _storageSemaphore.WaitAsync();
        
        try
        {
            if (_fileStorage.TryRemove(filePath, out var removedMetadata))
            {
                _logger.LogInformation("File deleted successfully. CorrelationId: {CorrelationId}, FilePath: {FilePath}, Size: {FileSize} bytes",
                    correlationId, filePath, removedMetadata.Data.Length);
            }
            else
            {
                _logger.LogDebug("Attempted to delete non-existent file. CorrelationId: {CorrelationId}, FilePath: {FilePath}",
                    correlationId, filePath);
            }
        }
        finally
        {
            _storageSemaphore.Release();
        }
    }

    private async Task<bool> FileExistsInternalAsync(string filePath, string correlationId)
    {
        await _storageSemaphore.WaitAsync();
        
        try
        {
            var exists = _fileStorage.ContainsKey(filePath);
            
            if (exists)
            {
                // Update last accessed time for existing files
                if (_fileStorage.TryGetValue(filePath, out var metadata))
                {
                    metadata.LastAccessed = DateTime.UtcNow;
                }
            }
            
            return exists;
        }
        finally
        {
            _storageSemaphore.Release();
        }
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private async Task ExecuteWithTimeoutAsync(Func<Task> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private static void ValidateSaveFileInputs(FileUploadRequest? fileRequest)
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

    private static void ValidateFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("FilePath cannot be null or empty", nameof(filePath));
        
        if (!filePath.StartsWith("memory://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("FilePath must start with 'memory://'", nameof(filePath));
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove path traversal attempts and invalid characters
        var sanitized = Path.GetFileName(fileName);
        if (string.IsNullOrEmpty(sanitized))
        {
            throw new ArgumentException("Invalid file name after sanitization", nameof(fileName));
        }
        return sanitized;
    }

    private static string GenerateFileId()
    {
        return Guid.NewGuid().ToString("N");
    }

    private static string CalculateFileHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToBase64String(hashBytes);
    }

    private void CleanupExpiredFiles(object? state)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        _logger.LogDebug("Starting memory cleanup. CorrelationId: {CorrelationId}, Current files: {FileCount}",
            correlationId, _fileStorage.Count);

        try
        {
            var currentTime = DateTime.UtcNow;
            var filesToRemove = new List<string>();
            
            // Remove files that haven't been accessed recently
            var cutoffTime = currentTime.AddMinutes(-_options.FileRetentionMinutes);
            
            foreach (var kvp in _fileStorage)
            {
                if (kvp.Value.LastAccessed < cutoffTime)
                {
                    filesToRemove.Add(kvp.Key);
                }
            }
            
            // If still too many files, remove oldest ones
            if (_fileStorage.Count > _options.MaxFiles)
            {
                var oldestFiles = _fileStorage
                    .OrderBy(kvp => kvp.Value.CreatedAt)
                    .Take(_fileStorage.Count - _options.MaxFiles + filesToRemove.Count)
                    .Select(kvp => kvp.Key)
                    .Where(key => !filesToRemove.Contains(key))
                    .ToList();
                
                filesToRemove.AddRange(oldestFiles);
            }
            
            var removedCount = 0;
            foreach (var filePath in filesToRemove)
            {
                if (_fileStorage.TryRemove(filePath, out var removedMetadata))
                {
                    removedCount++;
                    _logger.LogDebug("Cleaned up file from memory. CorrelationId: {CorrelationId}, FilePath: {FilePath}, Size: {FileSize} bytes",
                        correlationId, filePath, removedMetadata.Data.Length);
                }
            }
            
            if (removedCount > 0)
            {
                _logger.LogInformation("Memory cleanup completed. CorrelationId: {CorrelationId}, Removed: {RemovedCount}, Remaining: {RemainingCount}",
                    correlationId, removedCount, _fileStorage.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory cleanup. CorrelationId: {CorrelationId}", correlationId);
            // Don't rethrow - cleanup failures shouldn't break the service
        }
    }

    public (int FileCount, long TotalSizeBytes) GetStorageStatistics()
    {
        ThrowIfDisposed();
        
        var totalSize = _fileStorage.Values.Sum(metadata => metadata.Data.Length);
        return (_fileStorage.Count, totalSize);
    }

    public void ClearAllFiles()
    {
        ThrowIfDisposed();
        
        var fileCount = _fileStorage.Count;
        var totalSize = _fileStorage.Values.Sum(metadata => metadata.Data.Length);
        
        _fileStorage.Clear();
        
        _logger.LogInformation("Cleared all files from memory. FileCount: {FileCount}, TotalSize: {TotalSize} bytes",
            fileCount, totalSize);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            // Dispose managed resources
            _cleanupTimer?.Dispose();
            _storageSemaphore?.Dispose();
        }
        
        // Dispose unmanaged resources (none in this case)
        
        _disposed = true;
    }

    ~InMemoryStorageService()
    {
        Dispose(false);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(InMemoryStorageService));
        }
    }

    private class FileMetadata
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}

public class InMemoryStorageOptions
{
    public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    public int MaxFiles { get; set; } = 1000;
    public int MaxConcurrentOperations { get; set; } = 10;
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan QuickTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(30);
    public int FileRetentionMinutes { get; set; } = 60; // 1 hour
    public double ChaosStorageFullProbability { get; set; } = 0.001; // 0.1%
    public double ChaosFileCorruptionProbability { get; set; } = 0.0005; // 0.05%
} 