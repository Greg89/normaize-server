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
} 