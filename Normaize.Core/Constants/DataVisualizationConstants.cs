namespace Normaize.Core.Constants;

/// <summary>
/// Data visualization, processing and analysis related constants
/// </summary>
public static class DataVisualizationConstants
{
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
        public const string ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER = SharedConstants.DataSetMessages.ACCESS_DENIED_DATASET_BELONGS_TO_DIFFERENT_USER;
        public const string ACCESS_DENIED_TO_DATASET = SharedConstants.DataSetMessages.ACCESS_DENIED_TO_DATASET;
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
        public const string DATASET_ID_MUST_BE_POSITIVE = SharedConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE;

        // File extension mappings (alias to FileProcessing canonical values)
        public const string CSV_EXTENSION = FileProcessingConstants.FileProcessing.CSV_EXTENSION;
        public const string JSON_EXTENSION = FileProcessingConstants.FileProcessing.JSON_EXTENSION;
        public const string XML_EXTENSION = FileProcessingConstants.FileProcessing.XML_EXTENSION;
        public const string XLSX_EXTENSION = FileProcessingConstants.FileProcessing.XLSX_EXTENSION;
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
        public const string DATASET_NOT_FOUND_LOG = SharedConstants.DataSetMessages.DATASET_NOT_FOUND;
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
        public const string DATASET_NOT_FOUND = SharedConstants.DataSetMessages.DATASET_NOT_FOUND;
        public const string DATASET_ACCESS_DENIED = "Dataset access denied";
        public const string INVALID_CHART_TYPE = "Invalid chart type";
        public const string INVALID_DATASET_ID = "Invalid dataset ID";
        public const string INVALID_USER_ID = "Invalid user ID";
        public const string MAX_DATA_POINTS_EXCEEDED = "Max data points exceeded";
        public const string CHART_CONFIGURATION_INVALID = "Chart configuration invalid";
    }
}
