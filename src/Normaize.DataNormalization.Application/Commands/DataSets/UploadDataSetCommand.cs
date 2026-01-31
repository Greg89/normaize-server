using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Command to upload a new dataset file
/// </summary>
public record UploadDataSetCommand(
    string Name,
    string? Description,
    string UserId,
    string FileName,
    string FilePath,
    long FileSize,
    Stream FileStream,
    int? RetentionDays = null) : IRequest<UploadDataSetResult>;

public record UploadDataSetResult(
    bool Success,
    string Message,
    Guid? DataSetId = null,
    Guid? ProcessingJobId = null,
    string? Error = null);
