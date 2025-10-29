using MediatR;

namespace Normaize.DataNormalization.Application.Queries.FileUpload;

/// <summary>
/// Query to check if a file exists
/// </summary>
public record CheckFileExistsQuery : IRequest<CheckFileExistsResult>
{
    public required string FilePath { get; init; }
}

/// <summary>
/// Result of file existence check
/// </summary>
public record CheckFileExistsResult(
    bool Exists,
    string? Error = null
);
