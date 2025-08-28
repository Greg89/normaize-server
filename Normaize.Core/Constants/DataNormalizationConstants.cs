namespace Normaize.Core.Constants;

/// <summary>
/// Data normalization operation related constants
/// </summary>
public static class DataNormalizationConstants
{
    /// <summary>
    /// Data normalization constants
    /// </summary>
    public static class DataNormalization
    {
        // Operation types
        public const string REMOVE_DUPLICATE_ROWS = "RemoveDuplicateRows";
        public const string STANDARDIZE_FORMATS = "StandardizeFormats";
        public const string VALIDATE_DATA_TYPES = "ValidateDataTypes";
        public const string HANDLE_MISSING_VALUES = "HandleMissingValues";
        public const string NORMALIZE_DATA = "NormalizeData";
        public const string CLEAN_DATA = "CleanData";

        // Job status messages
        public const string JOB_SUBMITTED_SUCCESSFULLY = "Normalization job submitted successfully";
        public const string JOB_STARTED_PROCESSING = "Normalization job started processing";
        public const string JOB_COMPLETED_SUCCESSFULLY = "Normalization job completed successfully";
        public const string JOB_FAILED = "Normalization job failed";
        public const string JOB_CANCELLED = "Normalization job cancelled";
        public const string JOB_QUEUED = "Normalization job queued for processing";

        // Progress messages
        public const string ANALYZING_DATASET = "Analyzing dataset structure";
        public const string PROCESSING_ROWS = "Processing dataset rows";
        public const string REMOVING_DUPLICATES = "Removing duplicate rows";
        public const string UPDATING_DATASET = "Updating dataset with normalized data";
        public const string VALIDATING_RESULTS = "Validating normalization results";

        // Event types
        public const string JOB_CREATED = "JobCreated";
        public const string JOB_STARTED = "JobStarted";
        public const string JOB_COMPLETED = "JobCompleted";
        public const string JOB_PROGRESS_UPDATED = "JobProgressUpdated";

        // Error messages
        public const string INVALID_COLUMN_NAMES = "Invalid column names specified";
        public const string DATASET_NOT_FOUND = "Dataset not found";
        public const string ACCESS_DENIED = "Access denied to dataset";
        public const string PROCESSING_TIMEOUT = "Processing timeout exceeded";
        public const string INSUFFICIENT_MEMORY = "Insufficient memory for processing";
        public const string COLUMN_NOT_FOUND = "One or more specified columns not found in dataset";

        // Validation messages
        public const string AT_LEAST_ONE_COLUMN_REQUIRED = "At least one column must be specified for duplicate detection";
        public const string COLUMNS_MUST_EXIST_IN_DATASET = "All specified columns must exist in the dataset";
        public const string DATASET_MUST_BE_PROCESSED = "Dataset must be processed before normalization";

        // Processing limits
        public const int MAX_COLUMNS_FOR_DUPLICATE_DETECTION = 10;
        public const int MAX_ROWS_FOR_SYNC_PROCESSING = 10000;
        public const int DEFAULT_BATCH_SIZE = 1000;
        public const int MAX_BATCH_SIZE = 10000;

        // Timeout values (in milliseconds)
        public const int DEFAULT_PROCESSING_TIMEOUT_MS = 300000; // 5 minutes
        public const int MAX_PROCESSING_TIMEOUT_MS = 1800000; // 30 minutes
        public const int PROGRESS_UPDATE_INTERVAL_MS = 1000; // 1 second

        // Memory limits (in MB)
        public const double MAX_MEMORY_USAGE_MB = 2048; // 2GB
        public const double WARNING_MEMORY_USAGE_MB = 1024; // 1GB
    }
}
