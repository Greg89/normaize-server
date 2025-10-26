using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSets;

/// <summary>
/// Command to reset a dataset to its original state
/// </summary>
public record ResetDataSetCommand(
    Guid DataSetId,
    string UserId,
    ResetType ResetType,
    string? Reason = null) : IRequest<ResetDataSetResult>;

public enum ResetType
{
    Soft,           // Reset processing status only
    Hard,           // Reprocess from original file
    Full            // Delete and re-upload
}

public record ResetDataSetResult(
    bool Success,
    string Message,
    string? Error = null);
