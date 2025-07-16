namespace Normaize.Core.DTOs;

public class StorageDiagnosticsDto
{
    public StorageProvider StorageProvider { get; set; }
    public bool S3Configured { get; set; }
    public string S3Bucket { get; set; } = string.Empty;
    public string S3AccessKey { get; set; } = string.Empty;
    public string S3SecretKey { get; set; } = string.Empty;
    public string S3ServiceUrl { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}

public class StorageTestResultDto
{
    public string StorageType { get; set; } = string.Empty;
    public string TestResult { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public bool? Exists { get; set; }
    public bool? ContentMatch { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
} 