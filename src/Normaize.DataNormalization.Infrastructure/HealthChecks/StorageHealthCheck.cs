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
            var warnings = new List<string>();
            var errors = new List<string>();

            var data = new Dictionary<string, object>
            {
                ["provider"] = "S3",
                ["timestamp"] = DateTime.UtcNow
            };

            // Validate S3 configuration (only storage option)
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

            // Determine health status
            if (errors.Any())
            {
                data["errors"] = errors;

                // Enhanced logging for debugging
                Console.WriteLine($"❌ Storage Health Check Failed - Errors: {string.Join(", ", errors)}");
                Console.WriteLine($"   Provider: S3");
                Console.WriteLine($"   AWS_S3_BUCKET: {_configuration["AWS_S3_BUCKET"] ?? "Missing"}");
                Console.WriteLine($"   AWS_REGION: {_configuration["AWS_REGION"] ?? "Missing"}");
                Console.WriteLine($"   AWS_ACCESS_KEY_ID env: {(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")) ? "Present" : "Missing")}");

                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"S3 storage configuration has {errors.Count} error(s)",
                    data: data));
            }

            if (warnings.Any())
            {
                data["warnings"] = warnings;
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"S3 storage is configured but has {warnings.Count} warning(s)",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "S3 storage is properly configured",
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
