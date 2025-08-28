namespace Normaize.Core.Constants;

/// <summary>
/// Dataset operation and lifecycle related constants
/// </summary>
public static class DataSetOperationConstants
{
    /// <summary>
    /// Dataset lifecycle operation constants
    /// </summary>
    public static class DataSetLifecycle
    {
        // Operation names
        public const string RESET_DATA_SET = "ResetDataSet";
        public const string UPDATE_RETENTION_POLICY = "UpdateRetentionPolicy";
        public const string GET_RETENTION_STATUS = "GetRetentionStatus";
        public const string RESTORE_DATA_SET = "RestoreDataSet";
        public const string HARD_DELETE_DATA_SET = "HardDeleteDataSet";

        // Audit action names
        public const string RESET_DATA_SET_FILE_BASED = "ResetDataSetFileBased";
        public const string RESET_DATA_SET_DATABASE_ONLY = "RestoreDeletedDataset";
        public const string AUDIT_ACTION_RESTORE_DATA_SET = "RestoreDataSet";
        public const string AUDIT_ACTION_HARD_DELETE_DATA_SET = "HardDeleteDataSet";
        public const string AUDIT_ACTION_UPDATE_RETENTION_POLICY = "UpdateRetentionPolicy";

        // Error messages
        public const string DATASET_NOT_FOUND = SharedConstants.DataSetMessages.DATASET_NOT_FOUND;
        public const string ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER = SharedConstants.DataSetMessages.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER;
        public const string ACCESS_DENIED_TO_DATASET = SharedConstants.DataSetMessages.ACCESS_DENIED_TO_DATASET;
        public const string DATASET_ID_MUST_BE_POSITIVE = SharedConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE;
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
        public const string DATASET_ID_MUST_BE_POSITIVE = SharedConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE;
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
        public const string DATASET_NOT_FOUND = SharedConstants.DataSetMessages.DATASET_NOT_FOUND;
        public const string ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER = "Access denied - dataset belongs to different user";
        public const string ACCESS_DENIED_TO_DATASET = "Access denied to dataset";

        // Limits
        public const int MAX_PREVIEW_ROWS = 1000;
    }
}
