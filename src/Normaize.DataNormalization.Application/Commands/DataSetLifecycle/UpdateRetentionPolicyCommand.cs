using MediatR;

namespace Normaize.DataNormalization.Application.Commands.DataSetLifecycle;

/// <summary>
/// Command to update retention policy for a dataset
/// </summary>
public class UpdateRetentionPolicyCommand : IRequest<UpdateRetentionPolicyResult>
{
    /// <summary>
    /// Gets or sets the ID of the dataset
    /// </summary>
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the number of days to retain the data (1 to 3650 days)
    /// </summary>
    public int RetentionDays { get; set; }

    /// <summary>
    /// Gets or sets the user ID performing the update
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of an update retention policy operation
/// </summary>
public class UpdateRetentionPolicyResult
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
    /// Gets or sets the retention days applied
    /// </summary>
    public int? RetentionDays { get; set; }

    /// <summary>
    /// Gets or sets the calculated expiry date
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets whether the dataset is expired
    /// </summary>
    public bool? IsExpired { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}
