using System.ComponentModel.DataAnnotations;
using Normaize.Core.DTOs;

namespace Normaize.Core.Models;

public class DataSet
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public FileType FileType { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // User association
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? Schema { get; set; }

    public int RowCount { get; set; }

    public int ColumnCount { get; set; }

    public string? PreviewData { get; set; }

    public bool IsProcessed { get; set; }

    public DateTime? ProcessedAt { get; set; }

    // Enhanced file storage properties
    public string FilePath { get; set; } = string.Empty;

    public StorageProvider StorageProvider { get; set; } = StorageProvider.Local;

    // For small datasets, store processed data directly
    public string? ProcessedData { get; set; } // JSON serialized data

    // For large datasets, use separate table
    public bool UseSeparateTable { get; set; } = false;

    // Data processing metadata
    public string? ProcessingErrors { get; set; }

    public string? DataHash { get; set; } // For change detection

    // Soft delete properties
    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    // Audit trail properties
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    public string? LastModifiedBy { get; set; }

    // Data retention policy
    public DateTime? RetentionExpiryDate { get; set; }

    public bool IsRetentionExpired => RetentionExpiryDate.HasValue && RetentionExpiryDate.Value <= DateTime.UtcNow;

    public List<Analysis> Analyses { get; set; } = [];

    public List<DataSetRow> Rows { get; set; } = [];

    public List<DataSetAuditLog> AuditLogs { get; set; } = [];
}

public class DataSetRow
{
    public int Id { get; set; }

    public int DataSetId { get; set; }

    public int RowIndex { get; set; }

    public string Data { get; set; } = string.Empty; // JSON serialized row data

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DataSet DataSet { get; set; } = null!;
}

// New audit trail model
public class DataSetAuditLog
{
    public int Id { get; set; }

    public int DataSetId { get; set; }

    [Required]
    [MaxLength(255)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, Processed

    public string? Changes { get; set; } // JSON serialized changes

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DataSet DataSet { get; set; } = null!;
}