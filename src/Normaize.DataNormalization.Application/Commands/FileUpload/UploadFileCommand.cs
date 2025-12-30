using MediatR;

namespace Normaize.DataNormalization.Application.Commands.FileUpload;

/// <summary>
/// Command to upload and process a file
/// </summary>
public record UploadFileCommand : IRequest<UploadFileResult>
{
    public required string FileName { get; init; }
    public required Stream FileStream { get; init; }
    public required long FileSize { get; init; }
    public required string UserId { get; init; }
    public string? Description { get; init; }
    public string? Tags { get; init; }
    public bool ProcessImmediately { get; init; } = true;
}

/// <summary>
/// Result of file upload operation
/// </summary>
public record UploadFileResult(
    bool Success,
    string? FilePath = null,
    string? FileId = null,
    FileProcessingResult? ProcessingResult = null,
    string? Error = null
);

/// <summary>
/// Result of file processing operation
/// </summary>
public record FileProcessingResult(
    bool IsSuccess,
    string? Schema = null,
    int RowCount = 0,
    int ColumnCount = 0,
    string? PreviewData = null,
    string? Error = null
);
