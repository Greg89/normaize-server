using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

public interface IStorageService
{
    Task<string> SaveFileAsync(FileUploadRequest fileRequest);
    Task<Stream> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
    Task<bool> FileExistsAsync(string filePath);
}