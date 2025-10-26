namespace Normaize.DataNormalization.Application.Interfaces;

/// <summary>
/// Service for storing and retrieving files
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string userId, CancellationToken cancellationToken = default);
    Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for processing and validating files
/// </summary>
public interface IFileProcessingService
{
    Task<FileValidationResult> ValidateFileAsync(Stream fileStream, string fileName, long fileSize, CancellationToken cancellationToken = default);
    Task<FileProcessingResult> ProcessFileAsync(string filePath, Domain.ValueObjects.FileType fileType, CancellationToken cancellationToken = default);
}

public record FileValidationResult(
    bool IsValid,
    string? Error = null);

public record FileProcessingResult(
    bool IsSuccess,
    string? Schema = null,
    int RowCount = 0,
    int ColumnCount = 0,
    string? PreviewData = null,
    string? Error = null);

/// <summary>
/// Service for audit logging
/// </summary>
public interface IAuditService
{
    Task LogDataSetActionAsync(
        Guid dataSetId,
        string userId,
        string action,
        Dictionary<string, object> metadata,
        CancellationToken cancellationToken = default);
}
