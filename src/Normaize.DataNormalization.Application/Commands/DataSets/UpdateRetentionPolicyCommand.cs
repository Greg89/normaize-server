using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Command to update dataset retention policy
/// </summary>
public record UpdateRetentionPolicyCommand(
    Guid DataSetId,
    string UserId,
    int RetentionDays) : IRequest<UpdateRetentionPolicyResult>;

public record UpdateRetentionPolicyResult(
    bool Success,
    string Message,
    DateTime? NewExpiryDate = null,
    string? Error = null);
