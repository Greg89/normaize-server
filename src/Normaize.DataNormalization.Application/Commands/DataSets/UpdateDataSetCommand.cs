using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Command to update dataset metadata
/// </summary>
public record UpdateDataSetCommand(
    Guid DataSetId,
    string UserId,
    string Name,
    string? Description,
    DateTime? RetentionExpiryDate = null,
    string? ModifiedBy = null) : IRequest<UpdateDataSetResult>;

public record UpdateDataSetResult(
    bool Success,
    string Message,
    string? Error = null);
