using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.DTOs;

/// <summary>
/// Request DTO for removing duplicate rows from a dataset
/// </summary>
public class RemoveDuplicateRowsRequest
{
    /// <summary>
    /// Array of column names to use for determining duplicates
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one column must be specified")]
    public string[] ColumnNames { get; set; } = [];

    /// <summary>
    /// When true, keeps the first occurrence; when false, keeps the last occurrence
    /// </summary>
    public bool KeepFirstOccurrence { get; set; } = true;

    /// <summary>
    /// When true, considers letter casing for duplicate determination; when false, ignores case
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}

/// <summary>
/// Response DTO for normalization job submission
/// </summary>
public class NormalizationJobResponse
{
    /// <summary>
    /// Unique identifier for the normalization job
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Status of the job
    /// </summary>
    public NormalizationJobStatus Status { get; set; }

    /// <summary>
    /// Message describing the current state
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the job was submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Estimated completion time (if available)
    /// </summary>
    public DateTime? EstimatedCompletionAt { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }
}

/// <summary>
/// Response DTO for normalization job status
/// </summary>
public class NormalizationJobStatusResponse
{
    /// <summary>
    /// Unique identifier for the normalization job
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the job
    /// </summary>
    public NormalizationJobStatus Status { get; set; }

    /// <summary>
    /// Detailed message about the current state
    /// </summary>
    public string Message { get; set; } = string.Empty;

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
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Results of the normalization operation
    /// </summary>
    public NormalizationResults? Results { get; set; }
}

/// <summary>
/// Results of a normalization operation
/// </summary>
public class NormalizationResults
{
    /// <summary>
    /// Number of rows processed
    /// </summary>
    public int RowsProcessed { get; set; }

    /// <summary>
    /// Number of duplicate rows removed
    /// </summary>
    public int DuplicateRowsRemoved { get; set; }

    /// <summary>
    /// Number of rows remaining after normalization
    /// </summary>
    public int RowsRemaining { get; set; }

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Memory usage in MB
    /// </summary>
    public double MemoryUsageMB { get; set; }
}

/// <summary>
/// Status of a normalization job
/// </summary>
public enum NormalizationJobStatus
{
    /// <summary>
    /// Job has been submitted and is waiting to be processed
    /// </summary>
    Queued = 0,

    /// <summary>
    /// Job is currently being processed
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Job has completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Job has failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Job has been cancelled
    /// </summary>
    Cancelled = 4
}
