using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Command to delete a dataset (soft delete)
/// </summary>
public record DeleteDataSetCommand(
    Guid DataSetId,
    string UserId,
    string DeletedBy) : IRequest<DeleteDataSetResult>;

public record DeleteDataSetResult(
    bool Success,
    string Message,
    string? Error = null);
