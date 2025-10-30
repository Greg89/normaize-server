using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Normaize.DataNormalization.Infrastructure.HealthChecks;

/// <summary>
/// Health check for required configuration values.
/// Validates that critical configuration values are present and properly formatted.
/// </summary>
public class ConfigurationHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly string[] _requiredSettings;

    public ConfigurationHealthCheck(
        IConfiguration configuration,
        string[]? requiredSettings = null)
    {
        _configuration = configuration;
        _requiredSettings = requiredSettings ?? GetDefaultRequiredSettings();
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var missingSettings = new List<string>();
            var warnings = new List<string>();
            var validSettings = new List<string>();

            foreach (var setting in _requiredSettings)
            {
                var value = _configuration[setting];
                
                // Special handling for connection string - allow DATABASE_URL as fallback
                if (setting == "ConnectionStrings:DefaultConnection" && string.IsNullOrWhiteSpace(value))
                {
                    value = _configuration["DATABASE_URL"];
                }
                
                if (string.IsNullOrWhiteSpace(value))
                {
                    missingSettings.Add(setting);
                }
                else
                {
                    validSettings.Add(setting);
                }
            }

            // Check environment-specific warnings
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            
            // Check for insecure development settings in production
            if (environment?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true)
            {
                var enableSensitiveLogging = _configuration["Database:EnableSensitiveDataLogging"];
                if (bool.TryParse(enableSensitiveLogging, out var sensitiveLogging) && sensitiveLogging)
                {
                    warnings.Add("Sensitive data logging is enabled in production");
                }

                var corsAllowAll = _configuration["Cors:AllowAllOrigins"];
                if (bool.TryParse(corsAllowAll, out var allowAll) && allowAll)
                {
                    warnings.Add("CORS allows all origins in production");
                }
            }

            var data = new Dictionary<string, object>
            {
                ["environment"] = environment ?? "Unknown",
                ["checkedSettings"] = _requiredSettings.Length,
                ["validSettings"] = validSettings.Count,
                ["missingSettings"] = missingSettings.Count,
                ["warnings"] = warnings.Count,
                ["timestamp"] = DateTime.UtcNow
            };

            if (missingSettings.Any())
            {
                data["missingSettingsList"] = missingSettings;
                
                // Enhanced logging for debugging
                var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfigurationHealthCheck>();
                Console.WriteLine($"❌ Configuration Health Check Failed - Missing settings: {string.Join(", ", missingSettings)}");
                Console.WriteLine($"   Checked DATABASE_URL: {(!string.IsNullOrWhiteSpace(_configuration["DATABASE_URL"]) ? "Present" : "Missing")}");
                Console.WriteLine($"   Checked Storage:Provider: {(!string.IsNullOrWhiteSpace(_configuration["Storage:Provider"]) ? _configuration["Storage:Provider"] : "Missing")}");
                
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Missing {missingSettings.Count} required configuration setting(s)",
                    data: data));
            }

            if (warnings.Any())
            {
                data["warningsList"] = warnings;
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Configuration is valid but has {warnings.Count} warning(s)",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"All {validSettings.Count} required configuration settings are present",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Configuration health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                }));
        }
    }

    private static string[] GetDefaultRequiredSettings()
    {
        return new[]
        {
            "ConnectionStrings:DefaultConnection",
            "Storage:Provider"
        };
    }
}
