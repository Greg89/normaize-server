using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Configuration;

public class ServiceConfigurationOptions
{
    public const string SectionName = "ServiceConfiguration";

    [Required(ErrorMessage = "Database configuration is required")]
    public DatabaseConfiguration Database { get; set; } = new();

    [Required(ErrorMessage = "Security configuration is required")]
    public SecurityConfiguration Security { get; set; } = new();

    [Required(ErrorMessage = "Storage configuration is required")]
    public StorageConfiguration Storage { get; set; } = new();

    [Required(ErrorMessage = "Caching configuration is required")]
    public CachingConfiguration Caching { get; set; } = new();

    [Required(ErrorMessage = "Performance configuration is required")]
    public PerformanceConfiguration Performance { get; set; } = new();
}

public class DatabaseConfiguration
{
    [Required(ErrorMessage = "Database provider is required")]
    public string Provider { get; set; } = "InMemory";

    [Range(1, 300, ErrorMessage = "Connection timeout must be between 1 and 300 seconds")]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    [Range(1, 100, ErrorMessage = "Max retry count must be between 1 and 100")]
    public int MaxRetryCount { get; set; } = 3;

    [Range(1, 60, ErrorMessage = "Retry delay must be between 1 and 60 seconds")]
    public int RetryDelaySeconds { get; set; } = 5;

    public bool EnableSensitiveDataLogging { get; set; } = false;

    public bool EnableDetailedErrors { get; set; } = false;
}

public class SecurityConfiguration
{
    [Required(ErrorMessage = "CORS configuration is required")]
    public CorsConfiguration Cors { get; set; } = new();

    [Required(ErrorMessage = "JWT configuration is required")]
    public JwtConfiguration Jwt { get; set; } = new();

    public bool RequireHttps { get; set; } = true;

    public bool EnableSecurityHeaders { get; set; } = true;

    [Range(1, 365, ErrorMessage = "Session timeout must be between 1 and 365 days")]
    public int SessionTimeoutDays { get; set; } = 30;
}

public class CorsConfiguration
{
    [Required(ErrorMessage = "Allowed origins are required")]
    public string[] AllowedOrigins { get; set; } = ["http://localhost:3000", "http://localhost:4200"];

    [Required(ErrorMessage = "Allowed methods are required")]
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];

    [Required(ErrorMessage = "Allowed headers are required")]
    public string[] AllowedHeaders { get; set; } = ["Content-Type", "Authorization", "X-Requested-With"];

    public bool AllowCredentials { get; set; } = true;

    [Range(1, 86400, ErrorMessage = "Max age must be between 1 and 86400 seconds")]
    public int MaxAgeSeconds { get; set; } = 3600;
}

public class JwtConfiguration
{
    [Required(ErrorMessage = "JWT issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT audience is required")]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Token lifetime must be between 1 and 1440 minutes")]
    public int TokenLifetimeMinutes { get; set; } = 60;

    public bool ValidateIssuer { get; set; } = true;

    public bool ValidateAudience { get; set; } = true;

    public bool ValidateLifetime { get; set; } = true;

    public bool ValidateIssuerSigningKey { get; set; } = true;
}

public class StorageConfiguration
{
    [Required(ErrorMessage = "Storage provider is required")]
    public string Provider { get; set; } = "InMemory";

    [Range(1, 100, ErrorMessage = "Max file size must be between 1 and 100 MB")]
    public int MaxFileSizeMB { get; set; } = 10;

    [Range(1, 1000, ErrorMessage = "Max files per upload must be between 1 and 1000")]
    public int MaxFilesPerUpload { get; set; } = 10;

    public string[] AllowedFileExtensions { get; set; } = [".csv", ".json", ".xlsx", ".xml", ".txt"];

    public bool EnableCompression { get; set; } = true;

    public string? S3BucketName { get; set; }

    public string? S3Region { get; set; }
}

public class CachingConfiguration
{
    [Range(1, 1000, ErrorMessage = "Memory cache size must be between 1 and 1000 MB")]
    public int MemoryCacheSizeMB { get; set; } = 100;

    [Range(1, 3600, ErrorMessage = "Default expiration must be between 1 and 3600 seconds")]
    public int DefaultExpirationSeconds { get; set; } = 300;

    public bool EnableDistributedCache { get; set; } = false;

    public string? RedisConnectionString { get; set; }

    [Range(1, 100, ErrorMessage = "Cache hit ratio threshold must be between 1 and 100")]
    public int CacheHitRatioThreshold { get; set; } = 80;
}

public class PerformanceConfiguration
{
    [Range(1, 100, ErrorMessage = "Max concurrent requests must be between 1 and 100")]
    public int MaxConcurrentRequests { get; set; } = 10;

    [Range(1, 60, ErrorMessage = "Request timeout must be between 1 and 60 seconds")]
    public int RequestTimeoutSeconds { get; set; } = 30;

    public bool EnableCompression { get; set; } = true;

    public bool EnableResponseCaching { get; set; } = true;

    [Range(1, 1000, ErrorMessage = "Rate limit requests per minute must be between 1 and 1000")]
    public int RateLimitRequestsPerMinute { get; set; } = 100;

    public bool EnableHealthChecks { get; set; } = true;

    [Range(1, 300, ErrorMessage = "Health check timeout must be between 1 and 300 seconds")]
    public int HealthCheckTimeoutSeconds { get; set; } = 30;
} 