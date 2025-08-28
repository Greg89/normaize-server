namespace Normaize.Core.Constants;

/// <summary>
/// Chaos engineering and testing related constants
/// </summary>
public static class ChaosEngineeringConstants
{
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
    /// Chaos engineering messages
    /// </summary>
    public static class ChaosMessages
    {
        public const string SIMULATED_ANALYSIS_CREATION_FAILURE = "Simulated analysis creation failure (chaos engineering)";
        public const string SIMULATED_CACHE_FAILURE = "Simulated cache failure (chaos engineering)";
        public const string SIMULATED_STORAGE_FAILURE = "Simulated storage failure (chaos engineering)";
    }
}
