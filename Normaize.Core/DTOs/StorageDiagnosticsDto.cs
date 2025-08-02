using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

/// <summary>
/// Data Transfer Object for storage configuration diagnostics
/// </summary>
/// <remarks>
/// This DTO provides comprehensive information about the current storage configuration,
/// including the active storage provider, S3 configuration status, and environment details.
/// Used by the DiagnosticsController to provide storage configuration insights.
/// </remarks>
public class StorageDiagnosticsDto
{
    /// <summary>
    /// Gets or sets the current storage provider being used by the application
    /// </summary>
    /// <remarks>
    /// Indicates which storage provider is currently active (Local, S3, Azure, Memory).
    /// This is determined by the application configuration and environment settings.
    /// </remarks>
    [JsonPropertyName("storageProvider")]
    public StorageProvider StorageProvider { get; set; }

    /// <summary>
    /// Gets or sets whether S3 storage is properly configured
    /// </summary>
    /// <remarks>
    /// Indicates if all required S3 configuration parameters (bucket, access key, secret key) are set.
    /// This is used to determine if S3 storage operations are available.
    /// </remarks>
    [JsonPropertyName("s3Configured")]
    public bool S3Configured { get; set; }

    /// <summary>
    /// Gets or sets the S3 bucket configuration status
    /// </summary>
    /// <remarks>
    /// Shows "SET" if the S3 bucket is configured, "NOT SET" if it's missing.
    /// This provides visibility into S3 configuration without exposing sensitive values.
    /// </remarks>
    [JsonPropertyName("s3Bucket")]
    public string S3Bucket { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the S3 access key configuration status
    /// </summary>
    /// <remarks>
    /// Shows "SET" if the S3 access key is configured, "NOT SET" if it's missing.
    /// This provides visibility into S3 configuration without exposing sensitive values.
    /// </remarks>
    [JsonPropertyName("s3AccessKey")]
    public string S3AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the S3 secret key configuration status
    /// </summary>
    /// <remarks>
    /// Shows "SET" if the S3 secret key is configured, "NOT SET" if it's missing.
    /// This provides visibility into S3 configuration without exposing sensitive values.
    /// </remarks>
    [JsonPropertyName("s3SecretKey")]
    public string S3SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the S3 service URL configuration status
    /// </summary>
    /// <remarks>
    /// Shows "SET" if the S3 service URL is configured, "NOT SET" if it's missing.
    /// This is used for custom S3-compatible storage endpoints.
    /// </remarks>
    [JsonPropertyName("s3ServiceUrl")]
    public string S3ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current application environment
    /// </summary>
    /// <remarks>
    /// Indicates the current application environment (Development, Staging, Production, etc.).
    /// This helps with environment-specific diagnostics and troubleshooting.
    /// </remarks>
    [JsonPropertyName("environment")]
    public string Environment { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for storage connectivity and functionality test results
/// </summary>
/// <remarks>
/// This DTO provides detailed results from storage service tests, including file operations
/// (save, exists, get, delete) and validation of storage connectivity and functionality.
/// Used by the DiagnosticsController to verify storage service health.
/// </remarks>
public class StorageTestResultDto
{
    /// <summary>
    /// Gets or sets the type of storage service being tested
    /// </summary>
    /// <remarks>
    /// Indicates the storage service type (e.g., "S3StorageService", "InMemoryStorageService").
    /// This helps identify which storage implementation was tested.
    /// </remarks>
    [Required]
    [StringLength(100)]
    [JsonPropertyName("storageType")]
    public string StorageType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overall test result status
    /// </summary>
    /// <remarks>
    /// Indicates whether the storage test passed ("SUCCESS") or failed ("FAILED").
    /// This provides a quick summary of the test outcome.
    /// </remarks>
    [Required]
    [StringLength(20)]
    [JsonPropertyName("testResult")]
    public string TestResult { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path used during the test
    /// </summary>
    /// <remarks>
    /// The path where the test file was created during the storage test.
    /// This helps with debugging and verification of test operations.
    /// </remarks>
    [StringLength(500)]
    [JsonPropertyName("filePath")]
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets or sets whether the test file exists after creation
    /// </summary>
    /// <remarks>
    /// Indicates if the file existence check passed during the test.
    /// This validates the storage service's file existence functionality.
    /// </remarks>
    [JsonPropertyName("exists")]
    public bool? Exists { get; set; }

    /// <summary>
    /// Gets or sets whether the retrieved file content matches the original test content
    /// </summary>
    /// <remarks>
    /// Indicates if the file content retrieval and comparison test passed.
    /// This validates the storage service's file read functionality and data integrity.
    /// </remarks>
    [JsonPropertyName("contentMatch")]
    public bool? ContentMatch { get; set; }

    /// <summary>
    /// Gets or sets a descriptive message about the test result
    /// </summary>
    /// <remarks>
    /// Provides a human-readable description of the test outcome.
    /// This helps with understanding the test results and any issues encountered.
    /// </remarks>
    [Required]
    [StringLength(500)]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any error message from the test
    /// </summary>
    /// <remarks>
    /// Contains error details if the storage test failed.
    /// This provides specific information about what went wrong during testing.
    /// </remarks>
    [StringLength(1000)]
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}