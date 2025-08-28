namespace Normaize.Core.Constants;

/// <summary>
/// File processing, upload, and storage related constants
/// </summary>
public static class FileProcessingConstants
{
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
        public const string FILE_VALIDATION_FAILED = "Invalid file format or size";
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
    /// File processing and serialization constants
    /// </summary>
    public static class FileProcessingInternal
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
        public const string METADATA_ERROR_MESSAGE = "ErrorMessage";
        public const string METADATA_VALUE_KIND = "ValueKind";
        public const string METADATA_MAX_COLUMNS = "MaxColumns";

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
}
