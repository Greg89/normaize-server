namespace Normaize.Tests
{
    /// <summary>
    /// Centralized test configuration for controlling test execution
    /// </summary>
    public static class TestConfiguration
    {
        /// <summary>
        /// Whether to run slow tests (tests that take > 5 seconds)
        /// </summary>
        public static bool RunSlowTests => GetEnvironmentVariable("RUN_SLOW_TESTS", "false") == "true";

        /// <summary>
        /// Whether to run integration tests
        /// </summary>
        public static bool RunIntegrationTests => GetEnvironmentVariable("RUN_INTEGRATION_TESTS", "true") == "true";

        /// <summary>
        /// Whether to run external dependency tests
        /// </summary>
        public static bool RunExternalTests => GetEnvironmentVariable("RUN_EXTERNAL_TESTS", "false") == "true";

        /// <summary>
        /// Maximum parallel test threads
        /// </summary>
        public static int MaxParallelThreads => int.Parse(GetEnvironmentVariable("MAX_PARALLEL_THREADS", "4"));

        /// <summary>
        /// Test timeout in seconds
        /// </summary>
        public static int TestTimeoutSeconds => int.Parse(GetEnvironmentVariable("TEST_TIMEOUT_SECONDS", "30"));

        /// <summary>
        /// Whether to enable test parallelization
        /// </summary>
        public static bool EnableParallelization => GetEnvironmentVariable("ENABLE_PARALLELIZATION", "true") == "true";

        private static string GetEnvironmentVariable(string name, string defaultValue)
        {
            return Environment.GetEnvironmentVariable(name) ?? defaultValue;
        }
    }
}