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
        public const string OPERATION_FAILED = "Operation {Operation} failed. CorrelationId: {CorrelationId}";
        public const string OPERATION_FAILED_WITH_USER = "Failed to complete {Operation} for ID: {DataSetId}, user: {UserId}. CorrelationId: {CorrelationId}";
    }
} 