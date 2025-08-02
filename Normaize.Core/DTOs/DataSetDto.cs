using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

/// <summary>
/// Supported file types for dataset uploads
/// </summary>
/// <remarks>
/// Defines the file formats that can be uploaded and processed by the Normaize platform.
/// Each type supports different parsing and processing capabilities.
/// </remarks>
public enum FileType
{
    /// <summary>Comma-separated values file format</summary>
    CSV,
    /// <summary>JavaScript Object Notation file format</summary>
    JSON,
    /// <summary>Microsoft Excel file format (xlsx, xls)</summary>
    Excel,
    /// <summary>Extensible Markup Language file format</summary>
    XML,
    /// <summary>Apache Parquet columnar storage format</summary>
    Parquet,
    /// <summary>Plain text file format</summary>
    TXT,
    /// <summary>Custom or unsupported file format</summary>
    Custom
}

/// <summary>
/// Supported storage providers for dataset files
/// </summary>
/// <remarks>
/// Defines the storage backends where dataset files can be stored.
/// Each provider offers different performance, cost, and availability characteristics.
/// </remarks>
public enum StorageProvider
{
    /// <summary>Local file system storage</summary>
    Local,
    /// <summary>Amazon S3 cloud storage</summary>
    S3,
    /// <summary>Microsoft Azure Blob storage</summary>
    Azure,
    /// <summary>In-memory storage for testing</summary>
    Memory
}

/// <summary>
/// Data Transfer Object for comprehensive dataset information
/// </summary>
/// <remarks>
/// This DTO provides complete information about a dataset including metadata, file details,
/// processing status, and storage information. It serves as the primary data transfer object
/// for dataset management operations and is used extensively by the DataSetsController
/// and DataProcessingService for CRUD operations and data retrieval.
/// 
/// The DTO includes both basic metadata (name, description) and technical details
/// (file size, processing status, storage location) to support comprehensive dataset management.
/// </remarks>
public class DataSetDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the dataset
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the dataset
    /// </summary>
    /// <remarks>
    /// Required field that provides a human-readable identifier for the dataset.
    /// Used for display purposes and dataset identification.
    /// </remarks>
    [Required]
    [StringLength(255)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the dataset
    /// </summary>
    /// <remarks>
    /// Provides additional context about the dataset's purpose, contents, or usage.
    /// Optional field that can help users understand the dataset better.
    /// </remarks>
    [StringLength(1000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the original filename of the uploaded file
    /// </summary>
    /// <remarks>
    /// The name of the file as it was uploaded by the user.
    /// Used for reference and file identification purposes.
    /// </remarks>
    [Required]
    [StringLength(255)]
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of file format
    /// </summary>
    /// <remarks>
    /// Determines how the file should be parsed and processed.
    /// Affects the available analysis and visualization options.
    /// </remarks>
    [JsonPropertyName("fileType")]
    public FileType FileType { get; set; }

    /// <summary>
    /// Gets or sets the size of the file in bytes
    /// </summary>
    /// <remarks>
    /// Used for storage management and processing time estimation.
    /// Helps users understand the scale of their dataset.
    /// </remarks>
    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the file was uploaded
    /// </summary>
    /// <remarks>
    /// Used for audit trails and dataset lifecycle management.
    /// Helps track when datasets were added to the system.
    /// </remarks>
    [JsonPropertyName("uploadedAt")]
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the Auth0 user identifier who owns this dataset
    /// </summary>
    /// <remarks>
    /// Required field that links the dataset to its owner.
    /// Used for access control and user-specific dataset management.
    /// </remarks>
    [Required]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of rows in the dataset
    /// </summary>
    /// <remarks>
    /// Updated after successful processing to reflect the actual data size.
    /// Used for dataset statistics and processing validation.
    /// </remarks>
    [JsonPropertyName("rowCount")]
    public int RowCount { get; set; }

    /// <summary>
    /// Gets or sets the number of columns in the dataset
    /// </summary>
    /// <remarks>
    /// Updated after successful processing to reflect the actual data structure.
    /// Used for dataset statistics and schema information.
    /// </remarks>
    [JsonPropertyName("columnCount")]
    public int ColumnCount { get; set; }

    /// <summary>
    /// Gets or sets whether the dataset has been successfully processed
    /// </summary>
    /// <remarks>
    /// Indicates whether the file has been parsed and is ready for analysis.
    /// Affects which operations are available on the dataset.
    /// </remarks>
    [JsonPropertyName("isProcessed")]
    public bool IsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when processing was completed
    /// </summary>
    /// <remarks>
    /// Null until processing is successfully completed.
    /// Used for audit trails and processing status tracking.
    /// </remarks>
    [JsonPropertyName("processedAt")]
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Gets or sets a preview of the dataset content
    /// </summary>
    /// <remarks>
    /// Contains a sample of the data for quick preview purposes.
    /// Typically includes the first few rows to help users understand the data structure.
    /// </remarks>
    [JsonPropertyName("previewData")]
    public string? PreviewData { get; set; }

    /// <summary>
    /// Gets or sets the file path where the dataset is stored
    /// </summary>
    /// <remarks>
    /// The internal path used by the storage system to locate the file.
    /// May be a local path, S3 key, or other storage-specific identifier.
    /// </remarks>
    [StringLength(500)]
    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage provider used for this dataset
    /// </summary>
    /// <remarks>
    /// Determines which storage backend is used for this dataset.
    /// Affects performance, cost, and availability characteristics.
    /// </remarks>
    [JsonPropertyName("storageProvider")]
    public StorageProvider StorageProvider { get; set; }

    /// <summary>
    /// Gets or sets the JSON schema of the dataset
    /// </summary>
    /// <remarks>
    /// Contains the structure and data types of the dataset columns.
    /// Used for data validation and visualization configuration.
    /// </remarks>
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }

    /// <summary>
    /// Gets or sets the hash of the dataset content
    /// </summary>
    /// <remarks>
    /// Used for data integrity verification and duplicate detection.
    /// Helps ensure data hasn't been corrupted during storage or transfer.
    /// </remarks>
    [JsonPropertyName("dataHash")]
    public string? DataHash { get; set; }

    /// <summary>
    /// Gets or sets whether the dataset uses a separate table for storage
    /// </summary>
    /// <remarks>
    /// Indicates whether the dataset is stored in its own database table.
    /// Affects query performance and storage efficiency for large datasets.
    /// </remarks>
    [JsonPropertyName("useSeparateTable")]
    public bool UseSeparateTable { get; set; }

    /// <summary>
    /// Gets or sets any errors that occurred during processing
    /// </summary>
    /// <remarks>
    /// Contains error messages if processing failed.
    /// Helps users understand why a dataset couldn't be processed.
    /// </remarks>
    [JsonPropertyName("processingErrors")]
    public string? ProcessingErrors { get; set; }
}

/// <summary>
/// Data Transfer Object for creating new datasets
/// </summary>
/// <remarks>
/// This DTO contains the minimal information required to create a new dataset.
/// Used by the DataSetsController for dataset creation operations and by the
/// DataProcessingService for initializing new dataset records.
/// 
/// Unlike DataSetDto which contains complete dataset information, this DTO
/// only includes the essential fields needed for dataset creation.
/// </remarks>
public class CreateDataSetDto
{
    /// <summary>
    /// Gets or sets the name of the dataset
    /// </summary>
    /// <remarks>
    /// Required field that provides a human-readable identifier for the dataset.
    /// Should be descriptive and unique within the user's dataset collection.
    /// </remarks>
    [Required]
    [StringLength(255)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the dataset
    /// </summary>
    /// <remarks>
    /// Provides additional context about the dataset's purpose, contents, or usage.
    /// Helps users organize and understand their dataset collection.
    /// </remarks>
    [StringLength(1000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the Auth0 user identifier who will own this dataset
    /// </summary>
    /// <remarks>
    /// Required field that links the dataset to its owner.
    /// Used for access control and user-specific dataset management.
    /// </remarks>
    [Required]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO for dataset upload operations
/// </summary>
/// <remarks>
/// This DTO provides feedback about the success or failure of dataset upload operations.
/// Used by the DataSetsController to communicate upload results to clients.
/// 
/// Includes both success status and detailed message information to help users
/// understand the outcome of their upload operation.
/// </remarks>
public class DataSetUploadResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the created dataset
    /// </summary>
    /// <remarks>
    /// The ID of the newly created dataset if the upload was successful.
    /// Used by clients to reference the dataset in subsequent operations.
    /// </remarks>
    [JsonPropertyName("dataSetId")]
    public int DataSetId { get; set; }

    /// <summary>
    /// Gets or sets the response message
    /// </summary>
    /// <remarks>
    /// Provides detailed information about the upload result.
    /// May contain success confirmation or error details.
    /// </remarks>
    [Required]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the upload operation was successful
    /// </summary>
    /// <remarks>
    /// Boolean flag indicating the overall success of the upload operation.
    /// Used by clients to determine how to handle the response.
    /// </remarks>
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

/// <summary>
/// Data Transfer Object for file upload operations
/// </summary>
/// <remarks>
/// This DTO combines file upload data with dataset metadata for comprehensive
/// upload operations. Used by the DataSetsController for handling multipart
/// form data uploads that include both file content and metadata.
/// 
/// Supports the standard ASP.NET Core IFormFile interface for seamless
/// integration with web form uploads.
/// </remarks>
public class FileUploadDto
{
    /// <summary>
    /// Gets or sets the uploaded file
    /// </summary>
    /// <remarks>
    /// The actual file content being uploaded.
    /// Supports all file types defined in the FileType enum.
    /// </remarks>
    [JsonPropertyName("file")]
    public IFormFile? File { get; set; }

    /// <summary>
    /// Gets or sets the name for the dataset
    /// </summary>
    /// <remarks>
    /// Required field that provides a human-readable identifier for the dataset.
    /// Should be descriptive and unique within the user's dataset collection.
    /// </remarks>
    [Required]
    [StringLength(255)]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description for the dataset
    /// </summary>
    /// <remarks>
    /// Provides additional context about the dataset's purpose, contents, or usage.
    /// Helps users organize and understand their dataset collection.
    /// </remarks>
    [StringLength(1000)]
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}