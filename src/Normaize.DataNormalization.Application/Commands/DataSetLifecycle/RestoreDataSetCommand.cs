using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSetLifecycle;

/// <summary>
/// Command to restore a deleted dataset
/// </summary>
public class RestoreDataSetCommand : IRequest<RestoreDataSetResult>
{
    /// <summary>
    /// Gets or sets the ID of the dataset to restore
    /// </summary>
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the user ID performing the restore
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of a restore dataset operation
/// </summary>
public class RestoreDataSetResult
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
    /// Gets or sets the error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}
