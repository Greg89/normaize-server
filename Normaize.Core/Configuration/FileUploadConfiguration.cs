using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Configuration;

public class FileUploadConfiguration
{
    public const string SectionName = "FileUpload";
    
    [Range(1024, 104857600, ErrorMessage = "MaxFileSize must be between 1KB and 100MB")]
    public int MaxFileSize { get; set; } = 10485760; // 10MB default
    
    [Required(ErrorMessage = "AllowedExtensions is required")]
    [MinLength(1, ErrorMessage = "At least one file extension must be allowed")]
    public string[] AllowedExtensions { get; set; } = [".csv", ".json", ".xlsx", ".xls", ".xml", ".parquet", ".txt"];
    
    [Range(100, 1000000, ErrorMessage = "MaxPreviewRows must be between 100 and 1,000,000")]
    public int MaxPreviewRows { get; set; } = 100;
    
    [Range(1, 100, ErrorMessage = "MaxConcurrentUploads must be between 1 and 100")]
    public int MaxConcurrentUploads { get; set; } = 5;
    
    public bool EnableCompression { get; set; } = true;
    
    public string[] BlockedExtensions { get; set; } = [".exe", ".bat", ".cmd", ".ps1", ".sh", ".dll", ".so", ".dylib"];
} 