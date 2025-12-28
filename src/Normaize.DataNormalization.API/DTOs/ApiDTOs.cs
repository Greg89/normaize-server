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
/// Request DTO for duplicate removal job (body-based route format)
/// Used by /api/normalization/remove-duplicates
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
    public int? RetentionDays { get; set; }
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
    public DateTime? RetentionExpiryDate { get; set; }
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
    public DateTime? RetentionExpiryDate { get; set; }
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

/// <summary>
/// Response DTO for retention status
/// </summary>
public class RetentionStatusResponse
{
    public Guid DataSetId { get; set; }
    public int RetentionDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsExpired { get; set; }
    public bool CanExtend { get; set; }
    public bool FileExists { get; set; }
}

/// <summary>
/// Request DTO for updating retention policy
/// </summary>
public class UpdateRetentionPolicyRequest
{
    public int RetentionDays { get; set; }
}

/// <summary>
/// Request DTO for dataset reset operation
/// Matches client DataSetResetDto expectations
/// </summary>
public class ResetDataSetRequest
{
    /// <summary>
    /// Type of reset operation: RESTORE or REPROCESS
    /// </summary>
    public string ResetType { get; set; } = "REPROCESS"; // Default to REPROCESS

    /// <summary>
    /// Optional reason for the reset operation
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for duplicate removal from client (legacy format)
/// Matches client RemoveDuplicateRowsRequest expectations
/// Used for backward compatibility with path-parameter route
/// </summary>
public class RemoveDuplicateRowsRequest
{
    /// <summary>
    /// Column names to use for duplicate comparison
    /// </summary>
    public List<string> ColumnNames { get; set; } = new();

    /// <summary>
    /// Whether to keep first occurrence (true) or last occurrence (false)
    /// </summary>
    public bool KeepFirstOccurrence { get; set; } = true;

    /// <summary>
    /// Whether comparison should be case sensitive
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}

/// <summary>
/// Response DTO for detailed dataset statistics
/// </summary>
public class DatasetDetailedStatisticsResponse
{
    public Guid DataSetId { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public long FileSizeBytes { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public List<ColumnSummaryResponse> ColumnSummaries { get; set; } = new();
}

/// <summary>
/// Response DTO for column summary statistics
/// </summary>
public class ColumnSummaryResponse
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int UniqueValues { get; set; }
    public int NullCount { get; set; }
    public int NonNullCount { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public double? AverageValue { get; set; }
    public double? StandardDeviation { get; set; }
    public Dictionary<string, int> TopValues { get; set; } = new();
}

/// <summary>
/// Response DTO for user statistics overview
/// </summary>
public class UserStatisticsOverviewResponse
{
    public string UserId { get; set; } = string.Empty;
    public int TotalDataSets { get; set; }
    public long TotalRows { get; set; }
    public long TotalFileSize { get; set; }
    public long AverageFileSize { get; set; }
    public Dictionary<string, int> DataSetsByFileType { get; set; } = new();
    public Dictionary<string, int> DataSetsByStorageProvider { get; set; } = new();
    public int ProcessedDataSets { get; set; }
    public int UnprocessedDataSets { get; set; }
    public int DeletedDataSets { get; set; }
    public int DataSetsCreatedThisMonth { get; set; }
    public DateTime? MostRecentUpload { get; set; }
    public DateTime? OldestDataSet { get; set; }
}

/// <summary>
/// Response DTO for storage statistics
/// </summary>
public class StorageStatisticsResponse
{
    public string UserId { get; set; } = string.Empty;
    public long TotalStorageUsed { get; set; }
    public Dictionary<string, long> StorageByProvider { get; set; } = new();
    public Dictionary<string, long> StorageByFileType { get; set; } = new();
    public List<DataSetSizeInfo> LargestDataSets { get; set; } = new();
    public long StorageGrowthLastMonth { get; set; }
    public long AverageDataSetSize { get; set; }
}

/// <summary>
/// Dataset size information
/// </summary>
public class DataSetSizeInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response DTO for dataset analytics
/// </summary>
public class DataSetAnalyticsResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<AnalyticsDataPoint> DataPoints { get; set; } = new();
    public AnalyticsSummary Summary { get; set; } = new();
}

/// <summary>
/// Analytics data point for time series
/// </summary>
public class AnalyticsDataPoint
{
    public DateTime Date { get; set; }
    public int DataSetsCreated { get; set; }
    public long TotalRows { get; set; }
    public long TotalSizeBytes { get; set; }
}

/// <summary>
/// Analytics summary information
/// </summary>
public class AnalyticsSummary
{
    public int TotalDataSets { get; set; }
    public long TotalRows { get; set; }
    public long TotalSizeBytes { get; set; }
    public double AverageDataSetsPerPeriod { get; set; }
    public DateTime? PeakCreationDate { get; set; }
    public int PeakCreationCount { get; set; }
}