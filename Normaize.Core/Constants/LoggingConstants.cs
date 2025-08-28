namespace Normaize.Core.Constants;

/// <summary>
/// Logging and audit related constants
/// </summary>
public static class LoggingConstants
{
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
    /// Startup service constants
    /// </summary>
    public static class StartupService
    {
        // Logging property names for structured logging
        public const string CORRELATION_ID_LOG_PROPERTY = "CorrelationId";
        public const string OPERATION_LOG_PROPERTY = "Operation";
        public const string ATTEMPT_LOG_PROPERTY = "Attempt";
        public const string MAX_ATTEMPTS_LOG_PROPERTY = "MaxAttempts";
        public const string DELAY_LOG_PROPERTY = "Delay";
        public const string TIMEOUT_LOG_PROPERTY = "Timeout";
        public const string ENVIRONMENT_LOG_PROPERTY = "Environment";

        // Logging limits
        public const int MAX_LOG_MESSAGE_LENGTH = 1000;
    }
}
