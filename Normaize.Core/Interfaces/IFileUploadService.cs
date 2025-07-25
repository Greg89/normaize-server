using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

public interface IFileUploadService
{
    Task<string> SaveFileAsync(FileUploadRequest fileRequest);
    Task<bool> ValidateFileAsync(FileUploadRequest fileRequest);
    Task<DataSet> ProcessFileAsync(string filePath, string fileType);
    Task DeleteFileAsync(string filePath);
}