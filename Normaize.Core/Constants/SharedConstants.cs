namespace Normaize.Core.Constants;

/// <summary>
/// Shared constants used across multiple services and domains
/// </summary>
public static class SharedConstants
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
    /// Storage provider constants
    /// </summary>
    public static class StorageProvider
    {
        public const string MEMORY = "memory";
        public const string S3 = "s3";
        public const string LOCAL = "local";
    }

    /// <summary>
    /// Common validation messages
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
    /// Common data structure keys for structured logging
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
        public const string RESET_TYPE_KEY = "ResetType";
        public const string RETENTION_DAYS = "RetentionDays";
        public const string OPERATION = "Operation";
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
}
