using MediatR;

namespace Normaize.DataNormalization.Application.Commands.FileUpload;

/// <summary>
/// Command to delete an uploaded file
/// </summary>
public record DeleteFileCommand : IRequest<DeleteFileResult>
{
    public required string FilePath { get; init; }
    public required string UserId { get; init; }
}

/// <summary>
/// Result of file deletion operation
/// </summary>
public record DeleteFileResult(
    bool Success,
    string? Error = null
);
