using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.API.DTOs;

/// <summary>
/// Request DTO for submitting a new normalization job
/// </summary>
public class SubmitJobRequest
{
    public Guid DataSetId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Request DTO for duplicate removal job
/// </summary>
public class RemoveDuplicatesRequest
{
    public Guid DataSetId { get; set; }
    public string Strategy { get; set; } = "KeepFirst"; // KeepFirst, KeepLast, KeepMostComplete
    public List<string> ComparisonColumns { get; set; } = new();
    public bool CaseSensitive { get; set; } = false;
    public bool TrimWhitespace { get; set; } = true;
}

/// <summary>
/// Response DTO for job submission
/// </summary>
public class JobSubmissionResponse
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? EstimatedCompletionTime { get; set; }
}

/// <summary>
/// Response DTO for job status
/// </summary>
public class JobStatusResponse
{
    public Guid JobId { get; set; }
    public Guid DataSetId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public int ProgressPercentage { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public JobResultsResponse? Results { get; set; }
}

/// <summary>
/// Response DTO for job results
/// </summary>
public class JobResultsResponse
{
    public int ProcessedRows { get; set; }
    public int RowsRemoved { get; set; }
    public int RowsModified { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public Dictionary<string, object> Statistics { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Request DTO for dataset creation
/// </summary>
public class CreateDataSetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
    public string? StorageProvider { get; set; }
}

/// <summary>
/// Response DTO for dataset information
/// </summary>
public class DataSetResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsDeleted { get; set; }
    public FileMetadataResponse? FileMetadata { get; set; }
    public DatasetStatisticsResponse Statistics { get; set; } = new();
}

/// <summary>
/// Response DTO for file metadata
/// </summary>
public class FileMetadataResponse
{
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for dataset statistics
/// </summary>
public class DatasetStatisticsResponse
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime LastProcessedAt { get; set; }
}

/// <summary>
/// Request DTO for updating dataset
/// </summary>
public class UpdateDataSetRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for dataset preview
/// </summary>
public class DataSetPreviewResponse
{
    public Guid DataSetId { get; set; }
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int PreviewRows { get; set; }
}

/// <summary>
/// Column information for preview
/// </summary>
public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int Index { get; set; }
    public bool AllowNull { get; set; }
}

/// <summary>
/// Request DTO for canceling a job
/// </summary>
public class CancelJobRequest
{
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for job list
/// </summary>
public class JobListResponse
{
    public List<JobStatusResponse> Jobs { get; set; } = new();
    public int TotalJobs { get; set; }
}

/// <summary>
/// Request DTO for job filtering
/// </summary>
public class JobFilterRequest
{
    public Guid? DataSetId { get; set; }
    public string? Status { get; set; }
    public string? JobType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}