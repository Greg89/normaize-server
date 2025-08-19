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
        // Alias to canonical environment variable name
        public const string ASPNETCORE_ENVIRONMENT = EnvironmentVariables.ASPNETCORE_ENVIRONMENT;
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
    /// Chaos engineering constants
    /// </summary>
    public static class ChaosEngineering
    {
        public const string CHAOS_TYPE = "ChaosType";
        public const string PROCESSING_DELAY = "ProcessingDelay";
        public const string NETWORK_LATENCY = "NetworkLatency";
        public const string CACHE_FAILURE = "CacheFailure";
        public const string MEMORY_PRESSURE = "MemoryPressure";
        public const string ANALYSIS_CREATION_FAILURE = "AnalysisCreationFailure";
        public const string DATABASE_TIMEOUT = "DatabaseTimeout";
        public const string STORAGE_FAILURE = "StorageFailure";
        public const string RESTORE_OPERATION_DELAY = "RestoreOperationDelay";
        public const string FILE_PROCESSING_FAILURE = "FileProcessingFailure";
        public const string DELAY_MS_KEY = "DelayMs";
        public const string SIMULATED_PROCESSING_DELAY_MESSAGE = "Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}";

        // Chaos engineering delay constants
        public const int MIN_PROCESSING_DELAY_MS = 1000;
        public const int MAX_PROCESSING_DELAY_MS = 5000;
        public const int MIN_NETWORK_LATENCY_MS = 500;
        public const int MAX_NETWORK_LATENCY_MS = 2000;
        public const int MIN_SUMMARY_DELAY_MS = 500;
        public const int MAX_SUMMARY_DELAY_MS = 2000;
        public const int MIN_STATS_DELAY_MS = 1000;
        public const int MAX_STATS_DELAY_MS = 3000;
        public const int DEFAULT_CHAOS_DELAY_MS = 100;
        public const int MAX_CHAOS_DELAY_MS = 500;
        public const int MIN_CHAOS_DELAY_MS = 100;

        // Memory pressure simulation constants
        public const int MEMORY_PRESSURE_OBJECT_COUNT = 30;
        public const int MEMORY_PRESSURE_OBJECT_SIZE_BYTES = 1024 * 1024; // 1MB
        public const int MEMORY_PRESSURE_DELAY_MS = 100;

        // Dataset lifecycle chaos engineering constants
        public const int RESTORE_OPERATION_DELAY_MIN_MS = 2000;
        public const int RESTORE_OPERATION_DELAY_MAX_MS = 8000;
        public const int RETENTION_POLICY_TIMEOUT_MIN_MS = 10000;
        public const int RETENTION_POLICY_TIMEOUT_MAX_MS = 20000;
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
        public const string HEALTHY = "healthy";
        public const string SIMULATED_ANALYSIS_CREATION_FAILURE = "Simulated analysis creation failure (chaos engineering)";
        public const string SIMULATED_CACHE_FAILURE = "Simulated cache failure (chaos engineering)";
        public const string SIMULATED_STORAGE_FAILURE = "Simulated storage failure (chaos engineering)";
        public const string ANALYSIS_NOT_FOUND = "Analysis not found";
        public const string ANALYSIS_NAME_REQUIRED = "Analysis name is required";
        public const string ANALYSIS_NAME_TOO_LONG = "Analysis name is too long";
        public const string ANALYSIS_DESCRIPTION_TOO_LONG = "Analysis description is too long";
        public const string DATASET_ID_REQUIRED = "Dataset ID is required";
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
        public const string OPERATION_TIMED_OUT = "Operation timed out";
        public const string AUDIT_LOGGING_STARTED = "Audit logging started";
        public const string AUDIT_LOGGING_COMPLETED = "Audit logging completed";
        public const string PAGINATION_STARTED = "Pagination processing started";
        public const string PAGINATION_COMPLETED = "Pagination processing completed";
        public const string DATASET_UPDATED_SUCCESSFULLY = "Dataset updated successfully";
    }

    /// <summary>
    /// Validation error messages
    /// </summary>
    public static class ValidationMessages
    {
        public const string DATASET_ID_MUST_BE_POSITIVE = "Dataset ID must be positive"; // canonical
        public const string ANALYSIS_ID_MUST_BE_POSITIVE = "Analysis ID must be positive";
        public const string INVALID_ANALYSIS_STATUS = "Invalid analysis status: {0}";
        public const string INVALID_ANALYSIS_TYPE = "Invalid analysis type: {0}";
        public const string DATASET_NOT_FOUND_OR_ACCESS_DENIED = "Dataset not found or access denied";
        public const string USER_ID_CANNOT_BE_NULL_OR_EMPTY = "User ID cannot be null or empty";
    }

    /// <summary>
    /// Dataset-related common messages (canonical)
    /// </summary>
    public static class DataSetMessages
    {
        public const string DATASET_NOT_FOUND = "Dataset not found";
        public const string ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER = "Access denied - dataset belongs to different user";
        public const string ACCESS_DENIED_TO_DATASET = "Access denied to dataset";
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
        public const string DATASET_NOT_FOUND = DataSetMessages.DATASET_NOT_FOUND;
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
        public const string DATASETID = "DataSetId";
        public const string ACTUAL_USER_ID = "ActualUserId";
        public const string EXPECTED_USER_ID = "ExpectedUserId";
        public const string DATASET_FOUND = "DataSetFound";
        public const string PAGE = "Page";
        public const string PAGE_SIZE = "PageSize";
        public const string SEARCH_TERM = "SearchTerm";
        public const string FILE_TYPE = "FileType";
        public const string START_DATE = "StartDate";
        public const string END_DATE = "EndDate";
        public const string USER_ID = "UserId";
        public const string ANALYSIS_ID = "AnalysisId";
        public const string STATUS = "Status";
        public const string TOTAL_DATASETS = "TotalDataSets";
        public const string CHART_TYPE = "ChartType";
        public const string CORRELATION_ID = "CorrelationId";
        public const string RESTORE_TYPE_KEY = "RestoreType";
        public const string RESET_TYPE_KEY = "ResetType";
        public const string RETENTION_DAYS = "RetentionDays";
        public const string OPERATION = "Operation";

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

        // File extensions (canonical definitions)
        public const string CSV_EXTENSION = ".csv";
        public const string JSON_EXTENSION = ".json";
        public const string XLSX_EXTENSION = ".xlsx";
        public const string XLS_EXTENSION = ".xls";
        public const string XML_EXTENSION = ".xml";
        public const string TXT_EXTENSION = ".txt";
        public const string PARQUET_EXTENSION = ".parquet";

        // Storage provider prefixes
        public const string S3_PREFIX = "s3://";
        public const string AZURE_PREFIX = "azure://";
        public const string MEMORY_PREFIX = "memory://";

        // Default values
        public const int DEFAULT_FILE_SIZE = 0;
        public const int DEFAULT_DICTIONARY_CAPACITY = 2;
        public const int DEFAULT_ROW_COUNT = 0;
        public const int DEFAULT_CHILDREN_COUNT = 0;
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

    /// <summary>
    /// Data processing and visualization constants
    /// </summary>
    public static class DataProcessing
    {
        // Data processing constants
        public const int SAMPLE_VALUES_COUNT = 5;
        public const int CACHE_KEY_HASH_LENGTH = 8;

        // Statistical calculation constants
        public const double Q1_PERCENTILE = 0.25;
        public const double Q2_PERCENTILE = 0.5;
        public const double Q3_PERCENTILE = 0.75;
        public const double KURTOSIS_ADJUSTMENT = 3.0;

        // Default timeout values (in minutes)
        public const int DEFAULT_CACHE_EXPIRATION_MINUTES = 30;
        public const int DEFAULT_MAX_DATA_POINTS = 1000;
        public const int DEFAULT_CHART_GENERATION_TIMEOUT_MINUTES = 2;
        public const int DEFAULT_COMPARISON_CHART_TIMEOUT_MINUTES = 3;
        public const int DEFAULT_SUMMARY_GENERATION_TIMEOUT_MINUTES = 1;
        public const int DEFAULT_STATISTICAL_SUMMARY_TIMEOUT_MINUTES = 2;
        public const double DEFAULT_CHAOS_PROCESSING_DELAY_PROBABILITY = 0.001; // 0.1%

        // Data type constants
        public const string DATA_TYPE_NUMERIC = "Numeric";
        public const string DATA_TYPE_DATETIME = "DateTime";
        public const string DATA_TYPE_BOOLEAN = "Boolean";
        public const string DATA_TYPE_STRING = "String";
        public const string DATA_TYPE_NULL = "null";

        // Chart and data constants
        public const string FALLBACK_SERIES_NAME = "Count";
        public const string CONFIGURATION_KEY = "Configuration";

        // Data processing operation constants
        public const string DELETION_FAILURE = "DeletionFailure";
        public const string CACHE_CORRUPTION = "CacheCorruption";
        public const string UNNAMED_DATASET = "Unnamed Dataset";
        public const string STATS_CACHE_KEY_PREFIX = "stats_";
        public const string DATASET_UPLOADED_SUCCESSFULLY = "Dataset uploaded successfully";
        public const string ERROR_UPLOADING_DATASET = "Error uploading dataset: ";
        public const string INVALID_FILE_FORMAT_OR_SIZE = "Invalid file format or size";
        public const string FILE_DELETION_FAILED_CONTINUING = "File deletion failed, continuing with database deletion";
        public const string NO_FILE_PATH_TO_DELETE = "No file path to delete";
        public const string ACCESS_DENIED_OR_NO_PREVIEW_DATA = "Access denied or no preview data";
        public const string ACCESS_DENIED_OR_NO_SCHEMA = "Access denied or no schema";
        public const string FILTERING_DELETED_DATASETS_COMPLETED = "Filtering deleted datasets completed";
        public const string SEARCH_OPERATION_COMPLETED = "Search operation completed";
        public const string DATABASE_RETRIEVAL_BY_FILE_TYPE_COMPLETED = "Database retrieval by file type completed";
        public const string DATABASE_RETRIEVAL_BY_DATE_RANGE_COMPLETED = "Database retrieval by date range completed";
        public const string CACHE_LOOKUP_STARTED = "Cache lookup started";
        public const string STATISTICS_RETRIEVED_FROM_CACHE = "Statistics retrieved from cache";
        public const string CACHE_MISS_CALCULATING_STATISTICS = "Cache miss - calculating statistics";
        public const string CACHE_STORAGE_STARTED = "Cache storage started";
        public const string CACHE_STORAGE_COMPLETED = "Cache storage completed";
        public const string CACHE_CLEARING_STARTED = "Cache clearing started";
        public const string CACHE_CLEARING_COMPLETED = "Cache clearing completed";
        public const string DATASET_RETRIEVAL_STARTED = "Dataset retrieval started";
        public const string DATASET_RETRIEVAL_COMPLETED = "Dataset retrieval completed";
        public const string FILE_DELETION_STARTED = "File deletion started";
        public const string FILE_DELETION_COMPLETED_SUCCESSFULLY = "File deletion completed successfully";
        public const string FILE_SAVE_STARTED = "File save started";
        public const string FILE_SAVED = "File saved";
        public const string FILE_PROCESSING_STARTED = "File processing started";
        public const string FILE_PROCESSING_COMPLETED = "File processing completed";
        public const string DATABASE_SAVE_STARTED = "Database save started";
        public const string DATABASE_SAVE_COMPLETED = "Database save completed";
        public const string CHAOS_ENGINEERING_DELAY = "Chaos engineering delay";
        public const string CHAOS_ENGINEERING_SIMULATING_DELETION_FAILURE = "Chaos engineering: Simulating deletion failure";
        public const string CHAOS_ENGINEERING_SIMULATING_CACHE_CORRUPTION = "Chaos engineering: Simulating cache corruption";
        public const string ACCESS_DENIED_USER_MISMATCH = "Access denied - user mismatch";
        public const string SIMULATED_DELETION_FAILURE_MESSAGE = "Simulated deletion failure (chaos engineering)";

        // Metadata keys for structured logging
        public const string METADATA_FILE_PATH = "FilePath";
        public const string METADATA_FILE_TYPE = "FileType";
        public const string METADATA_ERROR_MESSAGE = "ErrorMessage";
        public const string METADATA_ROW_COUNT = "RowCount";
        public const string METADATA_COLUMN_COUNT = "ColumnCount";
        public const string METADATA_MAX_COLUMNS = "MaxColumns";
        public const string METADATA_VALUE_KIND = "ValueKind";

        // File processing status constants
        public const string FILE_PROCESSED = "File processed successfully";
        public const string DATABASE_SAVED = "Database saved successfully";
        public const string UPLOAD_SUCCESSFUL = "Upload successful";
        public const string UPLOAD_FAILED = "Upload failed";

        // Operation names
        public const string GET_DATA_SET = "GetDataSet";
        public const string UPDATE_DATA_SET = "UpdateDataSet";
        public const string DELETE_DATA_SET = "DeleteDataSet";
        public const string UPLOAD_DATA_SET = "UploadDataSet";

        // Audit action names
        public const string AUDIT_ACTION_VIEWED = "Viewed";
        public const string AUDIT_ACTION_UPLOAD_DATA_SET = "UploadDataSet";
        public const string AUDIT_ACTION_UPDATE_DATA_SET = "UpdateDataSet";
        public const string AUDIT_ACTION_DELETE_DATA_SET = "DeleteDataSet";

        // Logging messages
        public const string DATASET_NOT_FOUND = "Dataset not found";
        public const string ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER = DataSetMessages.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER;
        public const string ACCESS_DENIED_TO_DATASET = DataSetMessages.ACCESS_DENIED_TO_DATASET;
        public const string DATASET_IS_ALREADY_DELETED = "Dataset is already deleted";
        public const string DATASET_SOFT_DELETED_SUCCESSFULLY = "Dataset soft deleted successfully";
        public const string OPERATION_TIMED_OUT = "Operation timed out";
        public const string USER_SETTINGS_RETRIEVAL_STARTED = "User settings retrieval started";
        public const string RETENTION_POLICY_SET = "Retention policy set based on user settings";

        // Validation messages
        public const string USER_ID_CANNOT_BE_NULL_OR_EMPTY = "User ID cannot be null or empty";
        public const string NAME_CANNOT_BE_NULL_OR_EMPTY = "Name cannot be null or empty";
        public const string FILE_NAME_CANNOT_BE_NULL_OR_EMPTY = "File name cannot be null or empty";
        public const string FILE_SIZE_MUST_BE_POSITIVE = "File size must be positive";
        public const string INVALID_FILE_NAME = "Invalid file name";
        public const string DATASET_ID_MUST_BE_POSITIVE = ValidationMessages.DATASET_ID_MUST_BE_POSITIVE;

        // File extension mappings (alias to FileProcessing canonical values)
        public const string CSV_EXTENSION = FileProcessing.CSV_EXTENSION;
        public const string JSON_EXTENSION = FileProcessing.JSON_EXTENSION;
        public const string XML_EXTENSION = FileProcessing.XML_EXTENSION;
        public const string XLSX_EXTENSION = FileProcessing.XLSX_EXTENSION;
    }

    /// <summary>
    /// File upload and processing constants
    /// </summary>
    public static class FileUpload
    {
        // File upload operation constants
        public const string FILE_NAME_REQUIRED = "File name is required";
        public const string FILE_SIZE_MUST_BE_POSITIVE = "File size must be positive";
        public const string FILE_PATH_REQUIRED = "File path is required";
        public const string FILE_TYPE_REQUIRED = "File type is required";
        public const string FILE_NOT_FOUND_ERROR = "File not found: {0}";
        public const string UNSUPPORTED_FILE_TYPE_ERROR = "File type {0} is not supported";
        public const string CSV_PARSING_ERROR = "CSV parsing error: {0}";
        public const string JSON_PARSING_ERROR = "JSON parsing error: {0}";
        public const string JSON_SERIALIZATION_ERROR = "JSON serialization error during {0} processing: {1}";
        public const string EXCEL_PROCESSING_ERROR = "Excel processing error: {0}";
        public const string XML_PARSING_ERROR = "XML parsing error: {0}";
        public const string UNSUPPORTED_JSON_STRUCTURE = "Unsupported JSON structure: {0}";
        public const string NO_WORKSHEET_FOUND = "No worksheet found in Excel file";
        public const string CSV_NO_HEADERS_WARNING = "CSV file has no headers";
        public const string FILE_TOO_MANY_COLUMNS_WARNING = "File has too many columns";
        public const string FILE_SIZE_EXCEEDS_LIMIT_WARNING = "File size exceeds limit";
        public const string FILE_EXTENSION_BLOCKED_WARNING = "File extension is blocked";
        public const string FILE_EXTENSION_NOT_ALLOWED_WARNING = "File extension not allowed";
        public const string FILE_NOT_FOUND_PROCESSING_WARNING = "File not found during processing";
        public const string FILE_PROCESSING_COMPLETED_DEBUG = "File processing completed";
        public const string CSV_PARSING_FAILED_ERROR = "CSV parsing failed";
        public const string JSON_SERIALIZATION_FAILED_ERROR = "JSON serialization failed during {0} processing";
        public const string JSON_PARSING_FAILED_ERROR = "JSON parsing failed";
        public const string EXCEL_PROCESSING_FAILED_ERROR = "Excel processing failed";
        public const string XML_PARSING_FAILED_ERROR = "XML parsing failed";
        public const string UNSUPPORTED_JSON_STRUCTURE_WARNING = "Unsupported JSON structure";
        public const string FAILED_GENERATE_DATA_HASH_WARNING = "Failed to generate data hash for file {0}";
        public const string ERROR_PROCESSING_FILE = "Error processing file {0}: {1}";
        public const string UNEXPECTED_ERROR_FILE_PROCESSING = "Unexpected error during file processing";
        public const string FILE_VALIDATION_FAILED_ERROR = "File validation failed for {0}";
        public const string FAILED_SAVE_FILE_ERROR = "Failed to save file {0}";

        // Chaos engineering constants for file operations
        public const int FILE_UPLOAD_CHAOS_DELAY_MS = 100;
        public const int FILE_PROCESSING_CHAOS_DELAY_MS = 100;
        public const int FILE_DELETION_CHAOS_DELAY_MS = 100;
    }

    /// <summary>
    /// Database configuration constants
    /// </summary>
    public static class Database
    {
        // Default database configuration values
        public const string DEFAULT_PORT = "3306";
        public const string DEFAULT_HOST = "localhost";
        public const string DEFAULT_DATABASE = "testdb";
        public const string DEFAULT_USER = "testuser";
        public const string DEFAULT_PASSWORD = "testpass";
        public const string DEFAULT_CHARSET = "utf8mb4";

        // Connection string parts
        public const string SERVER_PREFIX = "Server=";
        public const string DATABASE_PREFIX = "Database=";
        public const string USER_PREFIX = "User=";
        public const string PASSWORD_PREFIX = "Password=";
        public const string PORT_PREFIX = "Port=";
        public const string CHARSET_PREFIX = "CharSet=";

        // MySQL specific constants
        public const string MYSQL_VERSION = "8.0.0";
        public const string ALLOW_LOAD_LOCAL_INFILE = "AllowLoadLocalInfile=true";
        public const string CONVERT_ZERO_DATETIME = "Convert Zero Datetime=True";
        public const string ALLOW_ZERO_DATETIME = "Allow Zero Datetime=True";
        public const string TEST_DATABASE_NAME = "TestDatabase";
    }

    /// <summary>
    /// CORS configuration constants
    /// </summary>
    public static class Cors
    {
        // Development origins
        public const string LOCALHOST_3000 = "http://localhost:3000";
        public const string LOCALHOST_4200 = "http://localhost:4200";
        public const string LOCALHOST_8080 = "http://localhost:8080";
        public const string LOCALHOST_5173 = "http://localhost:5173";  // Vite/React default
        public const string LOCALHOST_127_3000 = "http://127.0.0.1:3000";
        public const string LOCALHOST_127_4200 = "http://127.0.0.1:4200";
        public const string LOCALHOST_127_8080 = "http://127.0.0.1:8080";
        public const string LOCALHOST_127_5173 = "http://127.0.0.1:5173";  // Vite/React default

        // Production origins
        public const string NORMAIZE_COM = "https://normaize.com";
        public const string WWW_NORMAIZE_COM = "https://www.normaize.com";
        public const string APP_NORMAIZE_COM = "https://app.normaize.com";
        public const string BETA_NORMAIZE_COM = "https://beta.normaize.com";

        // HTTP methods
        public const string GET = "GET";
        public const string POST = "POST";
        public const string PUT = "PUT";
        public const string DELETE = "DELETE";
        public const string OPTIONS = "OPTIONS";

        // Headers
        public const string CONTENT_TYPE = "Content-Type";
        public const string AUTHORIZATION = "Authorization";
        public const string X_REQUESTED_WITH = "X-Requested-With";
        public const string ACCEPT = "Accept";

        // Policy names
        public const string DEVELOPMENT_POLICY = "Development";
        public const string BETA_POLICY = "Beta";
        public const string PRODUCTION_POLICY = "Production";
    }

    /// <summary>
    /// Environment variable names
    /// </summary>
    public static class EnvironmentVariables
    {
        public const string ASPNETCORE_ENVIRONMENT = "ASPNETCORE_ENVIRONMENT";
        public const string MYSQLHOST = "MYSQLHOST";
        public const string MYSQLDATABASE = "MYSQLDATABASE";
        public const string MYSQLUSER = "MYSQLUSER";
        public const string MYSQLPASSWORD = "MYSQLPASSWORD";
        public const string MYSQLPORT = "MYSQLPORT";
        public const string STORAGE_PROVIDER = "STORAGE_PROVIDER";
        public const string AWS_ACCESS_KEY_ID = "AWS_ACCESS_KEY_ID";
        public const string AWS_SECRET_ACCESS_KEY = "AWS_SECRET_ACCESS_KEY";
        public const string AUTH0_ISSUER = "AUTH0_ISSUER";
        public const string AUTH0_AUDIENCE = "AUTH0_AUDIENCE";
        public const string REDIS_CONNECTION_STRING = "REDIS_CONNECTION_STRING";
    }

    /// <summary>
    /// Configuration section names
    /// </summary>
    public static class ConfigurationSections
    {
        public const string STORAGE = "Storage";
    }

    /// <summary>
    /// Data visualization and processing constants
    /// </summary>
    public static class DataVisualization
    {
        // Metadata keys
        public const string DATASET_ID_1 = "DataSetId1";
        public const string DATASET_ID_2 = "DataSetId2";
        public const string PROCESSING_TIME_MS = "ProcessingTimeMs";
        public const string ROW_COUNT = "RowCount";
        public const string ERROR_MESSAGE = "ErrorMessage";
        public const string TIMEOUT = "Timeout";
        public const string OPERATION_NAME = "OperationName";

        // Logging messages
        public const string RETRIEVED_CHART_FROM_CACHE = "Retrieved chart from cache";
        public const string CACHE_MISS_GENERATING_NEW_CHART = "Cache miss, generating new chart";
        public const string GENERATED_CHART_SUCCESSFULLY = "Generated chart successfully";
        public const string RETRIEVED_COMPARISON_CHART_FROM_CACHE = "Retrieved comparison chart from cache";
        public const string CACHE_MISS_GENERATING_NEW_COMPARISON_CHART = "Cache miss, generating new comparison chart";
        public const string GENERATED_COMPARISON_CHART_SUCCESSFULLY = "Generated comparison chart successfully";
        public const string RETRIEVED_DATA_SUMMARY_FROM_CACHE = "Retrieved data summary from cache";
        public const string CACHE_MISS_GENERATING_NEW_DATA_SUMMARY = "Cache miss, generating new data summary";
        public const string GENERATED_DATA_SUMMARY_SUCCESSFULLY = "Generated data summary successfully";
        public const string RETRIEVED_STATISTICAL_SUMMARY_FROM_CACHE = "Retrieved statistical summary from cache";
        public const string CACHE_MISS_GENERATING_NEW_STATISTICAL_SUMMARY = "Cache miss, generating new statistical summary";
        public const string GENERATED_STATISTICAL_SUMMARY_SUCCESSFULLY = "Generated statistical summary successfully";

        // Error and validation messages
        public const string DATASET_NOT_FOUND_LOG = DataSetMessages.DATASET_NOT_FOUND;
        public const string UNAUTHORIZED_ACCESS_ATTEMPT = "Unauthorized access attempt";
        public const string ATTEMPTED_TO_ACCESS_DELETED_DATASET = "Attempted to access deleted dataset";
        public const string DATASET_HAS_NO_PROCESSED_DATA = "Dataset has no processed data";
        public const string FAILED_TO_DESERIALIZE_DATASET_JSON_DATA = "Failed to deserialize dataset JSON data";
        public const string EXTRACTED_ROWS_FROM_DATASET = "Extracted rows from dataset";
        public const string FAILED_TO_PARSE_DATASET_JSON_DATA = "Failed to parse dataset JSON data";
        public const string SIMULATED_CACHE_FAILURE_MESSAGE = "Simulated cache failure (chaos engineering)";
        public const string CHAOS_ENGINEERING_SIMULATING = "Chaos engineering: Simulating {0}";

        // Error message templates
        public const string FAILED_TO_COMPLETE_OPERATION = "Failed to complete {0}";
        public const string FAILED_TO_COMPLETE_GENERATE_CHART = "Failed to complete GenerateChartAsync for dataset ID {0} with chart type {1}";
        public const string FAILED_TO_COMPLETE_GENERATE_COMPARISON_CHART = "Failed to complete GenerateComparisonChartAsync for dataset IDs {0} and {1} with chart type {2}";
        public const string FAILED_TO_COMPLETE_GET_DATA_SUMMARY = "Failed to complete GetDataSummaryAsync for dataset ID {0}";
        public const string FAILED_TO_COMPLETE_GET_STATISTICAL_SUMMARY = "Failed to complete GetStatisticalSummaryAsync for dataset ID {0}";
        public const string DATASET_NOT_FOUND_WITH_ID = "Dataset not found with ID {0}";
        public const string DATASET_ACCESS_DENIED_USER_NOT_AUTHORIZED = "Dataset access denied - User {0} is not authorized to access dataset {1}";
        public const string DATASET_HAS_BEEN_DELETED = "Dataset {0} has been deleted";
        public const string OPERATION_TIMED_OUT_AFTER = "Operation {0} timed out after {1}";
        public const string FAILED_TO_PARSE_DATASET_DATA = "Failed to parse dataset {0} data: {1}";
    }

    /// <summary>
    /// File processing and serialization constants
    /// </summary>
    public static class FileProcessingConstants
    {
        // JSON serialization options
        public const string WRITE_INDENTED = "WriteIndented";
        public const string PROPERTY_NAMING_POLICY = "PropertyNamingPolicy";
        public const string CAMEL_CASE = "CamelCase";

        // Default values
        public const string EMPTY_STRING = "";
        public const string UNKNOWN_FILE_PATH = "Unknown file path";
        public const string UNKNOWN_FILE_TYPE = "Unknown file type";

        // Error message templates
        public const string FAILED_TO_COMPLETE_OPERATION = "Failed to complete {0}";
        public const string FAILED_TO_COMPLETE_FILE_PROCESSING = "Failed to complete {0} for file '{1}' of type '{2}'";

        // Excel processing constants
        public const string EXCEL_LICENSE_CONTEXT = "NonCommercial";
        public const string EXCEL_WORKSHEET_NOT_FOUND = "No worksheet found in Excel file";

        // File processing indices and counts
        public const int EXCEL_HEADER_ROW = 1;
        public const int EXCEL_DATA_START_ROW = 2;
        public const int EXCEL_DEFAULT_COLUMN = 1;
        public const int EXCEL_DEFAULT_CHILDREN_COUNT = 0;

        // Text processing
        public const char NEWLINE_CHAR = '\n';
        public const string NEWLINE_SPLIT_OPTIONS = "RemoveEmptyEntries";

        // JSON processing
        public const string JSON_VALUE_KIND_ARRAY = "Array";
        public const string JSON_VALUE_KIND_OBJECT = "Object";
        public const string JSON_PROPERTY_NAME = "Name";
        public const string JSON_PROPERTY_VALUE = "Value";

        // XML processing
        public const string XML_ELEMENT_NAME = "LocalName";
        public const string XML_ATTRIBUTE_NAME = "LocalName";
        public const string XML_ELEMENT_VALUE = "Value";
        public const string XML_ATTRIBUTE_VALUE = "Value";

        // CSV processing
        public const string CSV_HEADER_RECORD = "HeaderRecord";
        public const string CSV_FIELD_VALUE = "Field";
        public const string CSV_HEADER_VALIDATED = "HeaderValidated";
        public const string CSV_MISSING_FIELD_FOUND = "MissingFieldFound";
        public const string CSV_HAS_HEADER_RECORD = "HasHeaderRecord";

        // File processing metadata
        public const string METADATA_FILE_NAME = "FileName";
        public const string METADATA_FILE_PATH = "FilePath";
        public const string METADATA_FILE_TYPE = "FileType";
        public const string METADATA_FILE_SIZE = "FileSize";
        public const string METADATA_UPLOADED_AT = "UploadedAt";
        public const string METADATA_STORAGE_PROVIDER = "StorageProvider";
        public const string METADATA_DATA_HASH = "DataHash";
        public const string METADATA_USE_SEPARATE_TABLE = "UseSeparateTable";
        public const string METADATA_IS_PROCESSED = "IsProcessed";
        public const string METADATA_PROCESSED_AT = "ProcessedAt";
        public const string METADATA_PROCESSING_ERRORS = "ProcessingErrors";
        public const string METADATA_COLUMN_COUNT = "ColumnCount";
        public const string METADATA_ROW_COUNT = "RowCount";
        public const string METADATA_SCHEMA = "Schema";
        public const string METADATA_PREVIEW_DATA = "PreviewData";
        public const string METADATA_PROCESSED_DATA = "ProcessedData";

        // Processing status
        public const string PROCESSING_STATUS_PROCESSED = "Processed";
        public const string PROCESSING_STATUS_ERROR = "Error";
        public const string PROCESSING_STATUS_PENDING = "Pending";

        // Storage provider detection
        public const string STORAGE_PROVIDER_S3 = "S3";
        public const string STORAGE_PROVIDER_AZURE = "Azure";
        public const string STORAGE_PROVIDER_MEMORY = "Memory";
        public const string STORAGE_PROVIDER_LOCAL = "Local";

        // File type detection
        public const string FILE_TYPE_CSV = "CSV";
        public const string FILE_TYPE_JSON = "JSON";
        public const string FILE_TYPE_EXCEL = "Excel";
        public const string FILE_TYPE_XML = "XML";
        public const string FILE_TYPE_TXT = "TXT";
        public const string FILE_TYPE_PARQUET = "Parquet";
        public const string FILE_TYPE_CUSTOM = "Custom";
    }

    /// <summary>
    /// JSON serialization configuration constants
    /// </summary>
    public static class JsonSerialization
    {
        public const string CAMEL_CASE_POLICY = "CamelCase";
        public const string PASCAL_CASE_POLICY = "PascalCase";
        public const string SNAKE_CASE_POLICY = "SnakeCase";

        // Default configuration values
        public const bool DEFAULT_WRITE_INDENTED = false;
        public const bool DEFAULT_IGNORE_NULL_VALUES = true;
        public const string DEFAULT_ENCODER = "UnsafeRelaxedJsonEscaping";

        // Error messages
        public const string SERIALIZATION_ERROR = "JSON serialization failed";
        public const string DESERIALIZATION_ERROR = "JSON deserialization failed";
    }

    /// <summary>
    /// Validation patterns and formats
    /// </summary>
    public static class Validation
    {
        // Regex patterns
        public const string NUMERIC_ONLY_PATTERN = @"^\d+$";
    }

    /// <summary>
    /// Dataset lifecycle operation constants
    /// </summary>
    public static class DataSetLifecycle
    {
        // Operation names
        public const string RESTORE_DATA_SET_ENHANCED = "RestoreDataSetEnhanced";
        public const string RESET_DATA_SET = "ResetDataSet";
        public const string UPDATE_RETENTION_POLICY = "UpdateRetentionPolicy";
        public const string GET_RETENTION_STATUS = "GetRetentionStatus";
        public const string RESTORE_DATA_SET = "RestoreDataSet";
        public const string HARD_DELETE_DATA_SET = "HardDeleteDataSet";

        // Audit action names
        public const string RESTORE_DATA_SET_SIMPLE = "RestoreDataSetSimple";
        public const string RESTORE_DATA_SET_FULL = "RestoreDataSetFull";
        public const string RESET_DATA_SET_FILE_BASED = "ResetDataSetFileBased";
        public const string RESET_DATA_SET_DATABASE_ONLY = "RestoreDeletedDataset";
        public const string AUDIT_ACTION_RESTORE_DATA_SET = "RestoreDataSet";
        public const string AUDIT_ACTION_HARD_DELETE_DATA_SET = "HardDeleteDataSet";
        public const string AUDIT_ACTION_UPDATE_RETENTION_POLICY = "UpdateRetentionPolicy";

        // Error messages
        public const string DATASET_NOT_FOUND = DataSetMessages.DATASET_NOT_FOUND;
        public const string ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER = DataSetMessages.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER;
        public const string ACCESS_DENIED_TO_DATASET = DataSetMessages.ACCESS_DENIED_TO_DATASET;
        public const string DATASET_ID_MUST_BE_POSITIVE = ValidationMessages.DATASET_ID_MUST_BE_POSITIVE;
        public const string USER_ID_CANNOT_BE_NULL_OR_EMPTY = "User ID cannot be null or empty";
        public const string RETENTION_DAYS_MUST_BE_POSITIVE = "Retention days must be positive";
        public const string DATASET_NOT_FOUND_WITH_ID = "Dataset not found with ID {0}";

        // File availability messages
        public const string NO_FILE_PATH_ASSOCIATED_WITH_DATASET = "No file path associated with dataset";
        public const string ORIGINAL_FILE_NO_LONGER_EXISTS_IN_STORAGE = "Original file no longer exists in storage";
        public const string FILE_IS_AVAILABLE_FOR_PROCESSING = "File is available for processing";
        public const string ERROR_CHECKING_FILE_AVAILABILITY = "Error checking file availability";

        // Operation result messages
        public const string DATASET_IS_NOT_DELETED_NO_RESTORE_ACTION_NEEDED = "Dataset is not deleted, no restore action needed";
        public const string DATASET_RESTORED_SUCCESSFULLY_SIMPLE_RESTORE = "Dataset restored successfully (simple restore)";
        public const string DATASET_RESTORED_SUCCESSFULLY_FULL_RESTORE = "Dataset restored successfully (full restore - processing status reset)";
        public const string CANNOT_RESET_DATASET = "Cannot reset dataset";
        public const string DATASET_RESET_SUCCESSFULLY_USING_ORIGINAL_FILE = "Dataset reset successfully using original file";
        public const string FAILED_TO_RESET_DATASET = "Failed to reset dataset";
        public const string DATASET_RESET_SUCCESSFULLY_DATABASE_ONLY = "Dataset restored successfully (deletion status only)";
        public const string RETENTION_POLICY_UPDATED_SUCCESSFULLY = "Retention policy updated successfully. Data will be retained for {0} days.";

        // File operation messages
        public const string FILE_DELETED_FROM_STORAGE = "File deleted from storage";
        public const string FAILED_TO_DELETE_FILE_FROM_STORAGE = "Failed to delete file from storage";
        public const string DATASET_RESTORED_SUCCESSFULLY = "Dataset restored successfully";
        public const string DATASET_PERMANENTLY_DELETED = "Dataset permanently deleted";

        // Error codes
        public const string NO_FILE_PATH = "NO_FILE_PATH";
        public const string FILE_NOT_FOUND = "FILE_NOT_FOUND";
        public const string CHECK_ERROR = "CHECK_ERROR";

        // Reset types
        public const string RESET_TYPE_FILE_BASED = "FileBased";
        public const string RESET_TYPE_DATABASE_ONLY = "RestoreOnly";

        // Restore types
        public const string RESTORE_TYPE_SIMPLE = "Simple";
        public const string RESTORE_TYPE_FULL = "Full";

        // Limits
        public const int RECENT_UPLOADS_COUNT = 5;
    }

    /// <summary>
    /// Dataset query operation constants
    /// </summary>
    public static class DataSetQuery
    {
        // Operation names
        public const string GET_DATA_SETS_BY_USER = "GetDataSetsByUser";
        public const string GET_DELETED_DATA_SETS = "GetDeletedDataSets";
        public const string SEARCH_DATA_SETS = "SearchDataSets";
        public const string GET_DATA_SETS_BY_FILE_TYPE = "GetDataSetsByFileType";
        public const string GET_DATA_SETS_BY_DATE_RANGE = "GetDataSetsByDateRange";
        public const string GET_DATA_SET_STATISTICS = "GetDataSetStatistics";

        // Validation messages
        public const string SEARCH_TERM_CANNOT_BE_NULL_OR_EMPTY = "Search term cannot be null or empty";
        public const string START_DATE_CANNOT_BE_AFTER_END_DATE = "Start date cannot be after end date";
        public const string PAGE_MUST_BE_POSITIVE = "Page must be positive";
        public const string PAGE_SIZE_MUST_BE_POSITIVE = "Page size must be positive";
        public const string PAGE_SIZE_CANNOT_EXCEED_100 = "Page size cannot exceed 100";
        public const string USER_ID_CANNOT_BE_NULL_OR_EMPTY = "User ID cannot be null or empty";

        // Logging messages
        public const string PAGINATION_APPLIED = "Pagination applied";

        // Limits
        public const int MAX_PAGE_SIZE = 100;
        public const int RECENT_UPLOADS_COUNT = 5;

        // Processing status labels
        public const string PROCESSED = "Processed";
        public const string UNPROCESSED = "Unprocessed";
    }

    /// <summary>
    /// Dataset preview operation constants
    /// </summary>
    public static class DataSetPreview
    {
        // Operation names
        public const string GET_DATA_SET_PREVIEW = "GetDataSetPreview";
        public const string GET_DATA_SET_SCHEMA = "GetDataSetSchema";

        // Validation messages
        public const string DATASET_ID_MUST_BE_POSITIVE = ValidationMessages.DATASET_ID_MUST_BE_POSITIVE;
        public const string USER_ID_CANNOT_BE_NULL_OR_EMPTY = "User ID cannot be null or empty";
        public const string ROWS_MUST_BE_POSITIVE = "Rows must be positive";
        public const string ROWS_CANNOT_EXCEED_1000 = "Rows cannot exceed 1000";

        // Logging messages
        public const string NO_PREVIEW_DATA_AVAILABLE = "No preview data available";
        public const string PREVIEW_DATA_RETRIEVED_SUCCESSFULLY = "Preview data retrieved successfully";
        public const string FAILED_TO_DESERIALIZE_PREVIEW_DATA = "Failed to deserialize preview data";
        public const string NO_SCHEMA_DATA_AVAILABLE = "No schema data available";
        public const string SCHEMA_DESERIALIZED_SUCCESSFULLY = "Schema deserialized successfully";
        public const string FAILED_TO_DESERIALIZE_SCHEMA = "Failed to deserialize schema";
        public const string DATASET_NOT_FOUND = DataSetMessages.DATASET_NOT_FOUND;
        public const string ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER = "Access denied - dataset belongs to different user";
        public const string ACCESS_DENIED_TO_DATASET = "Access denied to dataset";

        // Limits
        public const int MAX_PREVIEW_ROWS = 1000;
    }
}