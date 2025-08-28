namespace Normaize.Core.Constants;

/// <summary>
/// Analysis operation related constants
/// </summary>
public static class AnalysisConstants
{
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
        public const string ANALYSIS_NAME_REQUIRED = "Analysis name is required";
        public const string ANALYSIS_NAME_TOO_LONG = "Analysis name is too long";
        public const string ANALYSIS_DESCRIPTION_TOO_LONG = "Analysis description is too long";
        public const string DATASET_ID_REQUIRED = "Dataset ID is required";
    }
}
