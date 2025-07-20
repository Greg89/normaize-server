namespace Normaize.Core.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Configuration status constants
    /// </summary>
    public static class ConfigStatus
    {
        public const string SET = "SET";
        public const string NOT_SET = "NOT SET";
        public const string REDACTED = "[REDACTED]";
    }

    /// <summary>
    /// Environment constants
    /// </summary>
    public static class Environment
    {
        public const string DEVELOPMENT = "Development";
        public const string STAGING = "Staging";
        public const string PRODUCTION = "Production";
        public const string BETA = "Beta";
        public const string TEST = "Test";
        public const string ASPNETCORE_ENVIRONMENT = "ASPNETCORE_ENVIRONMENT";
    }

    /// <summary>
    /// Storage provider constants
    /// </summary>
    public static class StorageProvider
    {
        public const string MEMORY = "memory";
        public const string S3 = "s3";
        public const string LOCAL = "local";
    }

    /// <summary>
    /// HTTP status messages
    /// </summary>
    public static class Messages
    {
        public const string SUCCESS = "Success";
        public const string ERROR = "Error";
        public const string NOT_FOUND = "Not Found";
        public const string UNAUTHORIZED = "Unauthorized";
        public const string UNKNOWN = "Unknown";
    }

    /// <summary>
    /// Authentication and authorization constants
    /// </summary>
    public static class Auth
    {
        public const string BEARER = "Bearer";
        public const string AUTHORIZATION_HEADER = "Authorization";
        public const string JWT_SCHEME = "Bearer";
        public const string AnonymousUser = "anonymous";
    }

    /// <summary>
    /// Logging message templates
    /// </summary>
    public static class LogMessages
    {
        public const string STARTING_OPERATION = "Starting {Operation} for ID: {AnalysisId}. CorrelationId: {CorrelationId}";
        public const string STARTING_OPERATION_WITH_USER = "Starting {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}";
        public const string STARTING_OPERATION_WITH_ROWS = "Starting {Operation} for ID: {DataSetId}, rows: {Rows}, user: {UserId}. CorrelationId: {CorrelationId}";
        public const string STARTING_OPERATION_WITH_FILE = "Starting {Operation} for file {FileName} by user {UserId}. CorrelationId: {CorrelationId}";
        public const string STARTING_OPERATION_WITH_PAGINATION = "Starting {Operation} for user: {UserId}, page: {Page}, pageSize: {PageSize}. CorrelationId: {CorrelationId}";
        public const string STARTING_OPERATION_WITH_SEARCH = "Starting {Operation} for user: {UserId}, term: '{SearchTerm}', page: {Page}, pageSize: {PageSize}. CorrelationId: {CorrelationId}";
        public const string STARTING_OPERATION_WITH_FILETYPE = "Starting {Operation} for file type {FileType}, user: {UserId}, page: {Page}, pageSize: {PageSize}. CorrelationId: {CorrelationId}";
        public const string STARTING_OPERATION_WITH_DATERANGE = "Starting {Operation} for date range {StartDate} to {EndDate}, user: {UserId}, page: {Page}, pageSize: {PageSize}. CorrelationId: {CorrelationId}";
        public const string STARTING_OPERATION_FOR_STATISTICS = "Starting {Operation} for user: {UserId}. CorrelationId: {CorrelationId}";
        public const string OPERATION_COMPLETED = "Operation {Operation} completed successfully. CorrelationId: {CorrelationId}";
        public const string OPERATION_COMPLETED_WITH_ANALYSIS = "Successfully completed {Operation} for ID: {AnalysisId}. CorrelationId: {CorrelationId}";
        public const string OPERATION_FAILED = "Operation {Operation} failed. CorrelationId: {CorrelationId}";
        public const string OPERATION_FAILED_WITH_ANALYSIS = "Failed to complete {Operation} for ID: {AnalysisId}. CorrelationId: {CorrelationId}";
        public const string OPERATION_FAILED_WITH_USER = "Failed to complete {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}";
        public const string INPUT_VALIDATION_STARTED = "Input validation started";
        public const string INPUT_VALIDATION_COMPLETED = "Input validation completed";
        public const string DATABASE_RETRIEVAL_STARTED = "Database retrieval started";
        public const string DATABASE_RETRIEVAL_COMPLETED = "Database retrieval completed";
        public const string DTO_MAPPING_STARTED = "DTO mapping started";
        public const string DTO_MAPPING_COMPLETED = "DTO mapping completed";
        public const string AUDIT_LOGGING_STARTED = "Audit logging started";
        public const string AUDIT_LOGGING_COMPLETED = "Audit logging completed";
        public const string PAGINATION_STARTED = "Pagination processing started";
        public const string PAGINATION_COMPLETED = "Pagination processing completed";
    }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public static class ValidationMessages
    {
        public const string DATASET_ID_MUST_BE_POSITIVE = "Dataset ID must be positive";
        public const string ANALYSIS_ID_MUST_BE_POSITIVE = "Analysis ID must be positive";
        public const string INVALID_ANALYSIS_STATUS = "Invalid analysis status: {0}";
        public const string INVALID_ANALYSIS_TYPE = "Invalid analysis type: {0}";
    }

    /// <summary>
    /// Analysis-specific messages
    /// </summary>
    public static class AnalysisMessages
    {
        public const string ANALYSIS_NOT_FOUND = "Analysis not found";
        public const string ANALYSIS_NOT_FOUND_OR_DELETED = "Analysis not found or already deleted";
        public const string ANALYSIS_ALREADY_COMPLETED = "Analysis already completed";
        public const string ANALYSIS_STATE_VALIDATION_STARTED = "Analysis state validation started";
        public const string ANALYSIS_STATE_VALIDATION_COMPLETED = "Analysis state validation completed";
        public const string ANALYSIS_EXECUTION_STARTED = "Analysis execution started";
        public const string ANALYSIS_EXECUTION_COMPLETED = "Analysis execution completed";
        public const string ANALYSIS_COMPLETED_SUCCESSFULLY = "Analysis completed successfully";
        public const string ANALYSIS_FAILED = "Analysis failed";
        public const string SETTING_ANALYSIS_STATUS_TO_PROCESSING = "Setting analysis status to processing";
        public const string RESULTS_DESERIALIZATION_STARTED = "Results deserialization started";
        public const string RESULTS_DESERIALIZATION_COMPLETED = "Results deserialization completed";
        public const string DATABASE_DELETION_STARTED = "Database deletion started";
        public const string DATABASE_DELETION_COMPLETED = "Database deletion completed";
    }

    /// <summary>
    /// Data visualization messages
    /// </summary>
    public static class VisualizationMessages
    {
        public const string CHART_GENERATION_STARTED = "Chart generation started";
        public const string CHART_GENERATION_COMPLETED = "Chart generation completed";
        public const string COMPARISON_CHART_GENERATION_STARTED = "Comparison chart generation started";
        public const string COMPARISON_CHART_GENERATION_COMPLETED = "Comparison chart generation completed";
        public const string DATA_SUMMARY_GENERATION_STARTED = "Data summary generation started";
        public const string DATA_SUMMARY_GENERATION_COMPLETED = "Data summary generation completed";
        public const string STATISTICAL_SUMMARY_GENERATION_STARTED = "Statistical summary generation started";
        public const string STATISTICAL_SUMMARY_GENERATION_COMPLETED = "Statistical summary generation completed";
        public const string CACHE_RETRIEVAL_STARTED = "Cache retrieval started";
        public const string CACHE_RETRIEVAL_COMPLETED = "Cache retrieval completed";
        public const string CACHE_STORAGE_STARTED = "Cache storage started";
        public const string CACHE_STORAGE_COMPLETED = "Cache storage completed";
        public const string DATASET_RETRIEVAL_STARTED = "Dataset retrieval started";
        public const string DATASET_RETRIEVAL_COMPLETED = "Dataset retrieval completed";
        public const string DATA_EXTRACTION_STARTED = "Data extraction started";
        public const string DATA_EXTRACTION_COMPLETED = "Data extraction completed";
        public const string CHART_DATA_GENERATION_STARTED = "Chart data generation started";
        public const string CHART_DATA_GENERATION_COMPLETED = "Chart data generation completed";
        public const string CONFIGURATION_VALIDATION_STARTED = "Configuration validation started";
        public const string CONFIGURATION_VALIDATION_COMPLETED = "Configuration validation completed";
        public const string DATASET_NOT_FOUND = "Dataset not found";
        public const string DATASET_ACCESS_DENIED = "Dataset access denied";
        public const string INVALID_CHART_TYPE = "Invalid chart type";
        public const string INVALID_DATASET_ID = "Invalid dataset ID";
        public const string INVALID_USER_ID = "Invalid user ID";
        public const string MAX_DATA_POINTS_EXCEEDED = "Max data points exceeded";
        public const string CHART_CONFIGURATION_INVALID = "Chart configuration invalid";
    }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public static class DataStructures
    {
        public const string CUSTOMER_ID = "customer_id";
        public const string ORDER_AMOUNT = "order_amount";
        public const string DATASET_ID = "dataset_id";
        public const string ACTUAL_USER_ID = "ActualUserId";
        public const string EXPECTED_USER_ID = "ExpectedUserId";
        public const string DATASET_FOUND = "DataSetFound";
    }

    /// <summary>
    /// File upload and processing constants
    /// </summary>
    public static class FileProcessing
    {
        // File processing defaults
        public const int DEFAULT_COLUMN_INDEX = 1;
        public const int HEADER_ROW_INDEX = 1;
        public const int DATA_START_ROW_INDEX = 2;
        public const string DEFAULT_COLUMN_PREFIX = "Column";
        public const string DEFAULT_DELIMITER = ",";
        
        // Collection capacity defaults
        public const int DEFAULT_RECORDS_CAPACITY = 1000;
        
        // Text file processing
        public const string LINE_NUMBER_COLUMN = "LineNumber";
        public const string CONTENT_COLUMN = "Content";
        
        // File type identifiers
        public const string CSV_FILE_TYPE = "CSV";
        public const string EXCEL_FILE_TYPE = "Excel";
        public const string XML_FILE_TYPE = "XML";
        public const string TEXT_FILE_TYPE = "text";
        
        // Chaos engineering scenario names
        public const string STORAGE_FAILURE_SCENARIO = "StorageFailure";
        public const string PROCESSING_DELAY_SCENARIO = "ProcessingDelay";
        
        // Context keys for structured logging
        public const string FILE_NAME_KEY = "FileName";
        public const string FILE_TYPE_KEY = "FileType";
        public const string FILE_PATH_KEY = "FilePath";
        
        // Size conversion constants
        public const int BYTES_PER_MEGABYTE = 1024 * 1024;
        public const int BYTES_PER_KILOBYTE = 1024;
    }

    /// <summary>
    /// File upload error messages
    /// </summary>
    public static class FileUploadMessages
    {
        public const string FILE_UPLOAD_STARTED = "File upload started";
        public const string FILE_UPLOAD_SUCCESS = "File uploaded successfully";
        public const string FILE_UPLOAD_FAILED = "File upload failed";
        public const string FILE_VALIDATION_STARTED = "File validation started";
        public const string FILE_VALIDATION_PASSED = "File validation passed";
        public const string FILE_VALIDATION_FAILED = "File validation failed";
        public const string FILE_VALIDATION_ERROR = "File validation error";
        public const string FILE_SIZE_VALIDATION_FAILED = "File size validation failed";
        public const string FILE_EXTENSION_VALIDATION_FAILED = "File extension validation failed";
        public const string FILE_PROCESSING_STARTED = "File processing started";
        public const string FILE_PROCESSED_SUCCESS = "File processed successfully";
        public const string FILE_PROCESSING_FAILED = "File processing failed";
        public const string FILE_DELETION_STARTED = "File deletion started";
        public const string FILE_DELETED_SUCCESS = "File deleted successfully";
        public const string FILE_DELETION_FAILED = "File deletion failed";
        public const string FILE_NOT_FOUND = "File not found";
        public const string UNSUPPORTED_FILE_TYPE = "Unsupported file type";
        public const string CONFIGURATION_VALIDATION_FAILED = "Configuration validation failed";
        public const string ALLOWED_EXTENSIONS_CONFLICT = "AllowedExtensions cannot contain blocked extensions";
    }
} 