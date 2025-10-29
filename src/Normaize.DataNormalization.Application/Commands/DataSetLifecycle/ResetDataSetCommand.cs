using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSetLifecycle;

/// <summary>
/// Command to reset a dataset to its original state
/// </summary>
public class ResetDataSetCommand : IRequest<ResetDataSetResult>
{
    /// <summary>
    /// Gets or sets the ID of the dataset to reset
    /// </summary>
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the type of reset operation (Reprocess or Restore)
    /// </summary>
    public ResetType ResetType { get; set; }

    /// <summary>
    /// Gets or sets the optional reason for the reset operation
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the user ID performing the reset
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Enumeration for dataset reset types
/// </summary>
public enum ResetType
{
    /// <summary>Restore deleted dataset (keep current data)</summary>
    Restore,
    /// <summary>Reprocess from original file (fresh data)</summary>
    Reprocess
}

/// <summary>
/// Result of a reset dataset operation
/// </summary>
public class ResetDataSetResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dataset ID
    /// </summary>
    public Guid? DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the reset type used
    /// </summary>
    public string? ResetType { get; set; }

    /// <summary>
    /// Gets or sets whether the file was available for reprocessing
    /// </summary>
    public bool? FileAvailable { get; set; }

    /// <summary>
    /// Gets or sets whether reprocessing was performed
    /// </summary>
    public bool? Reprocessed { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the error code if file was unavailable
    /// </summary>
    public string? ErrorCode { get; set; }
}
