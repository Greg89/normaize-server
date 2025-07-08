using System.ComponentModel.DataAnnotations;

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
    public string FileType { get; set; } = string.Empty;
    
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
    
    public string StorageProvider { get; set; } = "Local"; // Local, S3, Azure, etc.
    
    // For small datasets, store processed data directly
    public string? ProcessedData { get; set; } // JSON serialized data
    
    // For large datasets, use separate table
    public bool UseSeparateTable { get; set; } = false;
    
    // Data processing metadata
    public string? ProcessingErrors { get; set; }
    
    public string? DataHash { get; set; } // For change detection
    
    public List<Analysis> Analyses { get; set; } = new();
    
    public List<DataSetRow> Rows { get; set; } = new();
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