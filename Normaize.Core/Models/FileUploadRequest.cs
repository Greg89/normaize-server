using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Models;

public class FileUploadRequest
{
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public Stream FileStream { get; set; } = Stream.Null;
    
    // Enhanced properties
    public string? Description { get; set; }
    
    public string? Tags { get; set; } // Comma-separated tags
    
    public bool KeepOriginalFile { get; set; } = true;
    
    public bool StoreProcessedData { get; set; } = true;
    
    public int? MaxRowsToProcess { get; set; } // For large files
    
    public string? ProcessingOptions { get; set; } // JSON options
} 