using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Normaize.Data.Services;

public class ConfigurationValidationService(
    ILogger<ConfigurationValidationService> logger,
    IOptions<ServiceConfigurationOptions> config) : IConfigurationValidationService
{
    private readonly ILogger<ConfigurationValidationService> _logger = logger;
    private readonly ServiceConfigurationOptions _config = config.Value;

    public ConfigurationValidationResult ValidateConfiguration(CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting configuration validation. CorrelationId: {CorrelationId}", correlationId);

        try
        {
            var results = new List<ConfigurationValidationResult>
            {
                // Validate all configuration sections
                ValidateDatabaseConfiguration(cancellationToken),
                ValidateSecurityConfiguration(cancellationToken),
                ValidateStorageConfiguration(cancellationToken),
                ValidateCachingConfiguration(cancellationToken),
                ValidatePerformanceConfiguration(cancellationToken)
            };

            stopwatch.Stop();

            var overallResult = new ConfigurationValidationResult
            {
                IsValid = results.All(r => r.IsValid),
                ValidationDuration = stopwatch.Elapsed
            };

            // Aggregate all errors and warnings
            foreach (var result in results)
            {
                overallResult.Errors.AddRange(result.Errors);
                overallResult.Warnings.AddRange(result.Warnings);
            }

            // Add summary details
            overallResult.Details["totalSections"] = results.Count;
            overallResult.Details["validSections"] = results.Count(r => r.IsValid);
            overallResult.Details["invalidSections"] = results.Count(r => !r.IsValid);
            overallResult.Details["totalErrors"] = overallResult.Errors.Count;
            overallResult.Details["totalWarnings"] = overallResult.Warnings.Count;

            if (overallResult.IsValid)
            {
                _logger.LogInformation("Configuration validation completed successfully. Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                    overallResult.ValidationDuration.TotalMilliseconds, correlationId);
            }
            else
            {
                _logger.LogWarning("Configuration validation failed with {ErrorCount} errors and {WarningCount} warnings. Duration: {Duration}ms, CorrelationId: {CorrelationId}",
                    overallResult.Errors.Count, overallResult.Warnings.Count, overallResult.ValidationDuration.TotalMilliseconds, correlationId);
            }

            return overallResult;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during configuration validation. CorrelationId: {CorrelationId}", correlationId);

            return new ConfigurationValidationResult
            {
                IsValid = false,
                Errors = { $"Configuration validation error: {ex.Message}" },
                ValidationDuration = stopwatch.Elapsed
            };
        }
    }

    public ConfigurationValidationResult ValidateDatabaseConfiguration(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConfigurationValidationResult();

        try
        {
            var dbConfig = _config.Database;
            var validationContext = new ValidationContext(dbConfig);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(dbConfig, validationContext, validationResults, true))
            {
                result.Errors.AddRange(validationResults.Select(v => $"Database: {v.ErrorMessage}"));
            }

            // Additional validation logic
            if (dbConfig.Provider.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
                if (string.IsNullOrEmpty(connectionString))
                {
                    result.Warnings.Add("Database: MySQL provider selected but no connection string found in environment variables");
                }
            }

            if (dbConfig.EnableSensitiveDataLogging && !IsDevelopmentEnvironment())
            {
                result.Warnings.Add("Database: Sensitive data logging is enabled in non-development environment");
            }

            stopwatch.Stop();
            result.IsValid = result.Errors.Count == 0;
            result.ValidationDuration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error validating database configuration");
            result.Errors.Add($"Database configuration validation error: {ex.Message}");
            result.ValidationDuration = stopwatch.Elapsed;
            return result;
        }
    }

    public ConfigurationValidationResult ValidateSecurityConfiguration(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConfigurationValidationResult();

        try
        {
            var securityConfig = _config.Security;
            var validationContext = new ValidationContext(securityConfig);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(securityConfig, validationContext, validationResults, true))
            {
                result.Errors.AddRange(validationResults.Select(v => $"Security: {v.ErrorMessage}"));
            }

            // Validate CORS configuration
            var corsValidation = ValidateCorsConfiguration(securityConfig.Cors);
            result.Errors.AddRange(corsValidation.Errors);
            result.Warnings.AddRange(corsValidation.Warnings);

            // Validate JWT configuration
            var jwtValidation = ValidateJwtConfiguration(securityConfig.Jwt);
            result.Errors.AddRange(jwtValidation.Errors);
            result.Warnings.AddRange(jwtValidation.Warnings);

            // Additional security validations
            if (securityConfig.Cors.AllowedOrigins.Contains("*") && !IsDevelopmentEnvironment())
            {
                result.Warnings.Add("Security: Wildcard CORS origin is configured in non-development environment");
            }

            if (!securityConfig.RequireHttps && IsProductionEnvironment())
            {
                result.Warnings.Add("Security: HTTPS is not required in production environment");
            }

            stopwatch.Stop();
            result.IsValid = result.Errors.Count == 0;
            result.ValidationDuration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error validating security configuration");
            result.Errors.Add($"Security configuration validation error: {ex.Message}");
            result.ValidationDuration = stopwatch.Elapsed;
            return result;
        }
    }

    public ConfigurationValidationResult ValidateStorageConfiguration(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConfigurationValidationResult();

        try
        {
            var storageConfig = _config.Storage;
            var validationContext = new ValidationContext(storageConfig);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(storageConfig, validationContext, validationResults, true))
            {
                result.Errors.AddRange(validationResults.Select(v => $"Storage: {v.ErrorMessage}"));
            }

            // Validate S3 configuration if provider is S3
            if (storageConfig.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            {
                var s3Validation = ValidateS3Configuration(storageConfig);
                result.Errors.AddRange(s3Validation.Errors);
                result.Warnings.AddRange(s3Validation.Warnings);
            }

            // Validate file extensions
            if (storageConfig.AllowedFileExtensions.Length == 0)
            {
                result.Warnings.Add("Storage: No allowed file extensions configured");
            }

            stopwatch.Stop();
            result.IsValid = result.Errors.Count == 0;
            result.ValidationDuration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error validating storage configuration");
            result.Errors.Add($"Storage configuration validation error: {ex.Message}");
            result.ValidationDuration = stopwatch.Elapsed;
            return result;
        }
    }

    public ConfigurationValidationResult ValidateCachingConfiguration(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConfigurationValidationResult();

        try
        {
            var cachingConfig = _config.Caching;
            var validationContext = new ValidationContext(cachingConfig);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(cachingConfig, validationContext, validationResults, true))
            {
                result.Errors.AddRange(validationResults.Select(v => $"Caching: {v.ErrorMessage}"));
            }

            // Validate Redis configuration if distributed cache is enabled
            if (cachingConfig.EnableDistributedCache && string.IsNullOrEmpty(cachingConfig.RedisConnectionString))
            {
                result.Warnings.Add("Caching: Distributed cache is enabled but no Redis connection string provided");
            }

            stopwatch.Stop();
            result.IsValid = result.Errors.Count == 0;
            result.ValidationDuration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error validating caching configuration");
            result.Errors.Add($"Caching configuration validation error: {ex.Message}");
            result.ValidationDuration = stopwatch.Elapsed;
            return result;
        }
    }

    public ConfigurationValidationResult ValidatePerformanceConfiguration(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ConfigurationValidationResult();

        try
        {
            var perfConfig = _config.Performance;
            var validationContext = new ValidationContext(perfConfig);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(perfConfig, validationContext, validationResults, true))
            {
                result.Errors.AddRange(validationResults.Select(v => $"Performance: {v.ErrorMessage}"));
            }

            // Additional performance validations
            if (perfConfig.MaxConcurrentRequests > 50 && !IsProductionEnvironment())
            {
                result.Warnings.Add("Performance: High concurrent request limit configured for non-production environment");
            }

            if (perfConfig.RateLimitRequestsPerMinute > 500)
            {
                result.Warnings.Add("Performance: Very high rate limit configured, may impact legitimate users");
            }

            stopwatch.Stop();
            result.IsValid = result.Errors.Count == 0;
            result.ValidationDuration = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error validating performance configuration");
            result.Errors.Add($"Performance configuration validation error: {ex.Message}");
            result.ValidationDuration = stopwatch.Elapsed;
            return result;
        }
    }

    private static ConfigurationValidationResult ValidateCorsConfiguration(CorsConfiguration corsConfig)
    {
        var result = new ConfigurationValidationResult();
        var validationContext = new ValidationContext(corsConfig);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(corsConfig, validationContext, validationResults, true))
        {
            result.Errors.AddRange(validationResults.Select(v => $"CORS: {v.ErrorMessage}"));
        }

        return result;
    }

    private static ConfigurationValidationResult ValidateJwtConfiguration(JwtConfiguration jwtConfig)
    {
        var result = new ConfigurationValidationResult();
        var validationContext = new ValidationContext(jwtConfig);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(jwtConfig, validationContext, validationResults, true))
        {
            result.Errors.AddRange(validationResults.Select(v => $"JWT: {v.ErrorMessage}"));
        }

        // Check for required environment variables
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AUTH0_ISSUER")))
        {
            result.Warnings.Add("JWT: AUTH0_ISSUER environment variable not found");
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AUTH0_AUDIENCE")))
        {
            result.Warnings.Add("JWT: AUTH0_AUDIENCE environment variable not found");
        }

        return result;
    }

    private static ConfigurationValidationResult ValidateS3Configuration(StorageConfiguration storageConfig)
    {
        var result = new ConfigurationValidationResult();

        if (string.IsNullOrEmpty(storageConfig.S3BucketName))
        {
            result.Warnings.Add("S3: Bucket name not configured");
        }

        if (string.IsNullOrEmpty(storageConfig.S3Region))
        {
            result.Warnings.Add("S3: Region not configured");
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")))
        {
            result.Warnings.Add("S3: AWS_ACCESS_KEY_ID environment variable not found");
        }

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")))
        {
            result.Warnings.Add("S3: AWS_SECRET_ACCESS_KEY environment variable not found");
        }

        return result;
    }

    private static bool IsDevelopmentEnvironment() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsProductionEnvironment() =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;
}