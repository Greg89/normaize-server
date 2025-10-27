using MediatR;

namespace Normaize.DataNormalization.Application.Queries.DataSetLifecycle;

/// <summary>
/// Query to get retention status for a dataset
/// </summary>
public class GetRetentionStatusQuery : IRequest<GetRetentionStatusResult>
{
    /// <summary>
    /// Gets or sets the ID of the dataset
    /// </summary>
    public Guid DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the user ID requesting the status
    /// </summary>
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Result of a get retention status query
/// </summary>
public class GetRetentionStatusResult
{
    /// <summary>
    /// Gets or sets whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the dataset ID
    /// </summary>
    public Guid? DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the number of retention days (calculated from upload to expiry)
    /// </summary>
    public int? RetentionDays { get; set; }

    /// <summary>
    /// Gets or sets the retention expiry date
    /// </summary>
    public DateTime? RetentionExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets whether the retention period has expired
    /// </summary>
    public bool IsRetentionExpired { get; set; }

    /// <summary>
    /// Gets or sets the number of days until expiry (0 if expired)
    /// </summary>
    public int DaysUntilExpiry { get; set; }

    /// <summary>
    /// Gets or sets the error message if operation failed
    /// </summary>
    public string? Error { get; set; }
}
