using Normaize.Core.Configuration;

namespace Normaize.Core.Interfaces;

public interface IConfigurationValidationService
{
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
    Task<ConfigurationValidationResult> ValidateDatabaseConfigurationAsync(CancellationToken cancellationToken = default);
    Task<ConfigurationValidationResult> ValidateSecurityConfigurationAsync(CancellationToken cancellationToken = default);
    Task<ConfigurationValidationResult> ValidateStorageConfigurationAsync(CancellationToken cancellationToken = default);
    Task<ConfigurationValidationResult> ValidateCachingConfigurationAsync(CancellationToken cancellationToken = default);
    Task<ConfigurationValidationResult> ValidatePerformanceConfigurationAsync(CancellationToken cancellationToken = default);
}

public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Details { get; set; } = new();
    public TimeSpan ValidationDuration { get; set; }
} 