using System.ComponentModel.DataAnnotations;
using Normaize.Core.DTOs;

namespace Normaize.Core.Models;

/// <summary>
/// Represents a data normalization job in the system
/// </summary>
public class DataNormalizationJob
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Associated dataset ID
    /// </summary>
    public int DataSetId { get; set; }

    /// <summary>
    /// User who submitted the job
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Type of normalization operation
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// JSON serialized operation parameters
    /// </summary>
    public string? OperationParameters { get; set; }

    /// <summary>
    /// Current status of the job
    /// </summary>
    public NormalizationJobStatus Status { get; set; }

    /// <summary>
    /// Priority of the job (higher numbers = higher priority)
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// When the job was submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// When the job started processing
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job completed (successfully or with error)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Error message if the job failed
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// JSON serialized results of the normalization operation
    /// </summary>
    public string? Results { get; set; }

    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// When the job should be retried next
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    [MaxLength(255)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When the job was deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the job
    /// </summary>
    [MaxLength(255)]
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who last modified the job
    /// </summary>
    [MaxLength(255)]
    public string? LastModifiedBy { get; set; }

    /// <summary>
    /// Navigation property to the associated dataset
    /// </summary>
    public DataSet DataSet { get; set; } = null!;

    /// <summary>
    /// Navigation property to audit logs
    /// </summary>
    public List<DataNormalizationAuditLog> AuditLogs { get; set; } = [];
}

/// <summary>
/// Audit log for data normalization operations
/// </summary>
public class DataNormalizationAuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Associated normalization job ID
    /// </summary>
    public string NormalizationJobId { get; set; } = string.Empty;

    /// <summary>
    /// User who performed the action
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Action performed
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Created, Started, Progress, Completed, Failed, Cancelled

    /// <summary>
    /// JSON serialized changes or additional data
    /// </summary>
    public string? Changes { get; set; }

    /// <summary>
    /// Timestamp of the action
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address of the user
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Navigation property to the normalization job
    /// </summary>
    public DataNormalizationJob NormalizationJob { get; set; } = null!;
}
