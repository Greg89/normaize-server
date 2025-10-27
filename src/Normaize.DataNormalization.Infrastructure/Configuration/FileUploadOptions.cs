using System.ComponentModel.DataAnnotations;

namespace Normaize.DataNormalization.Infrastructure.Configuration;

/// <summary>
/// Configuration options for file upload validation
/// </summary>
public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    /// <summary>
    /// Maximum allowed file size in bytes
    /// </summary>
    [Range(1024, 104857600, ErrorMessage = "MaxFileSizeBytes must be between 1KB and 100MB")]
    public long MaxFileSizeBytes { get; set; } = 10485760; // 10MB default

    /// <summary>
    /// List of allowed file extensions (e.g., ".csv", ".json")
    /// </summary>
    [Required(ErrorMessage = "AllowedExtensions is required")]
    [MinLength(1, ErrorMessage = "At least one file extension must be allowed")]
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".csv", ".json", ".xlsx", ".xls", ".xml", ".txt"
    };

    /// <summary>
    /// List of blocked file extensions that should never be allowed (e.g., ".exe", ".bat")
    /// </summary>
    public List<string> BlockedExtensions { get; set; } = new()
    {
        ".exe", ".bat", ".cmd", ".ps1", ".sh", ".dll", ".so", ".dylib"
    };

    /// <summary>
    /// Maximum number of rows to preview from uploaded files
    /// </summary>
    [Range(10, 1000, ErrorMessage = "MaxPreviewRows must be between 10 and 1,000")]
    public int MaxPreviewRows { get; set; } = 100;

    /// <summary>
    /// Maximum number of concurrent file uploads allowed
    /// </summary>
    [Range(1, 100, ErrorMessage = "MaxConcurrentUploads must be between 1 and 100")]
    public int MaxConcurrentUploads { get; set; } = 5;

    /// <summary>
    /// Whether to enable compression for stored files
    /// </summary>
    public bool EnableCompression { get; set; } = true;
}
