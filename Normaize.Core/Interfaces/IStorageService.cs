using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

public interface IStorageService
{
    Task<string> SaveFileAsync(FileUploadRequest fileRequest);
    Task<Stream> GetFileAsync(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
    Task<long> GetFileSizeAsync(string filePath);
    Task<string> GetFileUrlAsync(string filePath, TimeSpan? expiry = null);
} 