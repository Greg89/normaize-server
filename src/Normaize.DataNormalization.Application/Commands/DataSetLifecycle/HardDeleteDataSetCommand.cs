using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSetLifecycle;

/// <summary>
/// Command to permanently delete a dataset (file and database record)
/// </summary>
public class HardDeleteDataSetCommand : IRequest<HardDeleteDataSetResult>
{
    /// <summary>
    /// Gets or sets the ID of the dataset to delete
    /// </summary>
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the user ID performing the delete
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of a hard delete dataset operation
/// </summary>
public class HardDeleteDataSetResult
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
    /// Gets or sets the dataset ID that was deleted
    /// </summary>
    public Guid? DataSetId { get; set; }

    /// <summary>
    /// Gets or sets whether the file was deleted successfully
    /// </summary>
    public bool? FileDeleted { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}
