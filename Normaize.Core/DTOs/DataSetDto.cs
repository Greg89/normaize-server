using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.DTOs;

public enum FileType
{
    CSV,
    JSON,
    Excel,
    XML,
    Parquet,
    TXT,
    Custom
}

public enum StorageProvider
{
    Local,
    S3,
    Azure,
    Memory
}

public class DataSetDto
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    public FileType FileType { get; set; }
    
    public long FileSize { get; set; }
    
    public DateTime UploadedAt { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    public int RowCount { get; set; }
    
    public int ColumnCount { get; set; }
    
    public bool IsProcessed { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    public string? PreviewData { get; set; }
    
    // Additional properties from domain model
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;
    
    public StorageProvider StorageProvider { get; set; }
    
    public string? Schema { get; set; }
    
    public string? DataHash { get; set; }
    
    public bool UseSeparateTable { get; set; }
    
    public string? ProcessingErrors { get; set; }
}

public class CreateDataSetDto
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
}

public class DataSetUploadResponse
{
    public int DataSetId { get; set; }
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public bool Success { get; set; }
}

public class FileUploadDto
{
    public IFormFile? File { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
} 