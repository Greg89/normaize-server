using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Normaize.DataNormalization.Infrastructure.HealthChecks;

/// <summary>
/// Health check for storage provider configuration and accessibility.
/// Validates storage provider configuration and tests basic connectivity.
/// </summary>
public class StorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public StorageHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = _configuration["Storage:Provider"];
            var basePath = _configuration["Storage:BasePath"];
            var warnings = new List<string>();
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(provider))
            {
                errors.Add("Storage provider not configured");
            }

            var data = new Dictionary<string, object>
            {
                ["provider"] = provider ?? "Unknown",
                ["timestamp"] = DateTime.UtcNow
            };

            // Validate based on provider type
            if (provider?.Equals("Local", StringComparison.OrdinalIgnoreCase) == true)
            {
                data["basePath"] = basePath ?? "Not configured";
                
                if (string.IsNullOrWhiteSpace(basePath))
                {
                    errors.Add("Local storage provider requires BasePath configuration");
                }
                else
                {
                    // Check if directory exists and is writable
                    try
                    {
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                            data["directoryCreated"] = true;
                        }

                        data["directoryExists"] = Directory.Exists(basePath);
                        
                        // Test write access
                        var testFile = Path.Combine(basePath, $".health_check_{Guid.NewGuid()}.tmp");
                        File.WriteAllText(testFile, "health_check");
                        File.Delete(testFile);
                        
                        data["writeAccess"] = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        errors.Add($"No write access to storage path: {basePath}");
                        data["writeAccess"] = false;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Storage path validation failed: {ex.Message}");
                        data["writeAccess"] = false;
                    }
                }
            }
            else if (provider?.Equals("S3", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Use AWS standard naming to match S3StorageService
                var bucketName = _configuration["AWS_S3_BUCKET"];
                var region = _configuration["AWS_REGION"];
                
                data["bucketName"] = bucketName ?? "Not configured";
                data["region"] = region ?? "Not configured";

                if (string.IsNullOrWhiteSpace(bucketName))
                {
                    errors.Add("AWS_S3_BUCKET configuration is required for S3 storage");
                }

                if (string.IsNullOrWhiteSpace(region))
                {
                    errors.Add("AWS_REGION configuration is required for S3 storage");
                }

                // Check for AWS credentials in environment
                var awsAccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                var awsSecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

                if (string.IsNullOrWhiteSpace(awsAccessKey))
                {
                    warnings.Add("AWS_ACCESS_KEY_ID environment variable not found");
                }

                if (string.IsNullOrWhiteSpace(awsSecretKey))
                {
                    warnings.Add("AWS_SECRET_ACCESS_KEY environment variable not found");
                }

                data["hasAwsAccessKey"] = !string.IsNullOrWhiteSpace(awsAccessKey);
                data["hasAwsSecretKey"] = !string.IsNullOrWhiteSpace(awsSecretKey);
            }
            else if (!string.IsNullOrWhiteSpace(provider))
            {
                warnings.Add($"Unknown storage provider: {provider}");
            }

            // Determine health status
            if (errors.Any())
            {
                data["errors"] = errors;
                
                // Enhanced logging for debugging
                Console.WriteLine($"❌ Storage Health Check Failed - Errors: {string.Join(", ", errors)}");
                Console.WriteLine($"   Provider: {provider ?? "Not set"}");
                Console.WriteLine($"   AWS_S3_BUCKET: {_configuration["AWS_S3_BUCKET"] ?? "Missing"}");
                Console.WriteLine($"   AWS_REGION: {_configuration["AWS_REGION"] ?? "Missing"}");
                Console.WriteLine($"   AWS_ACCESS_KEY_ID env: {(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ? "Present" : "Missing")}");
                
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Storage configuration has {errors.Count} error(s)",
                    data: data));
            }

            if (warnings.Any())
            {
                data["warnings"] = warnings;
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Storage is configured but has {warnings.Count} warning(s)",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Storage provider '{provider}' is properly configured",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Storage health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                }));
        }
    }
}
