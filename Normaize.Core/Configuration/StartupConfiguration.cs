using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Configuration;

public class StartupConfigurationOptions
{
    public const string SectionName = "Startup";

    [Required(ErrorMessage = "Database startup configuration is required")]
    public DatabaseStartupConfiguration Database { get; set; } = new();

    [Required(ErrorMessage = "Health check startup configuration is required")]
    public HealthCheckStartupConfiguration HealthCheck { get; set; } = new();

    [Required(ErrorMessage = "Environment configuration is required")]
    public EnvironmentStartupConfiguration Environment { get; set; } = new();

    [Required(ErrorMessage = "Retry configuration is required")]
    public RetryConfiguration Retry { get; set; } = new();
}

public class DatabaseStartupConfiguration
{
    [Range(1, 300, ErrorMessage = "Migration timeout must be between 1 and 300 seconds")]
    public int MigrationTimeoutSeconds { get; set; } = 60;

    [Range(1, 10, ErrorMessage = "Max migration retries must be between 1 and 10")]
    public int MaxMigrationRetries { get; set; } = 3;

    public bool EnableMigrationLogging { get; set; } = true;

    public bool FailOnMigrationError { get; set; } = true;

    [Range(1, 60, ErrorMessage = "Migration retry delay must be between 1 and 60 seconds")]
    public int MigrationRetryDelaySeconds { get; set; } = 5;
}

public class HealthCheckStartupConfiguration
{
    [Range(1, 300, ErrorMessage = "Health check timeout must be between 1 and 300 seconds")]
    public int HealthCheckTimeoutSeconds { get; set; } = 30;

    [Range(1, 10, ErrorMessage = "Max health check retries must be between 1 and 10")]
    public int MaxHealthCheckRetries { get; set; } = 3;

    public bool EnableHealthCheckLogging { get; set; } = true;

    public bool FailOnHealthCheckError { get; set; } = true;

    [Range(1, 60, ErrorMessage = "Health check retry delay must be between 1 and 60 seconds")]
    public int HealthCheckRetryDelaySeconds { get; set; } = 5;

    public bool RunHealthChecksInParallel { get; set; } = false;
}

public class EnvironmentStartupConfiguration
{
    [Required(ErrorMessage = "Production environments are required")]
    public string[] ProductionEnvironments { get; set; } = ["Production", "Staging", "Beta"];

    [Required(ErrorMessage = "Development environments are required")]
    public string[] DevelopmentEnvironments { get; set; } = ["Development", "Local"];

    public bool EnableStartupChecksInDevelopment { get; set; } = false;

    public bool EnableStartupChecksInContainer { get; set; } = true;

    public bool EnableStartupChecksWithDatabase { get; set; } = true;
}

public class RetryConfiguration
{
    [Range(1, 10, ErrorMessage = "Max retries must be between 1 and 10")]
    public int MaxRetries { get; set; } = 3;

    [Range(1, 60, ErrorMessage = "Base delay must be between 1 and 60 seconds")]
    public int BaseDelaySeconds { get; set; } = 2;

    [Range(1, 10, ErrorMessage = "Max delay must be between 1 and 10 times base delay")]
    public int MaxDelayMultiplier { get; set; } = 5;

    public bool EnableExponentialBackoff { get; set; } = true;

    public bool EnableJitter { get; set; } = true;

    [Range(0, 1, ErrorMessage = "Jitter factor must be between 0 and 1")]
    public double JitterFactor { get; set; } = 0.1;
} 