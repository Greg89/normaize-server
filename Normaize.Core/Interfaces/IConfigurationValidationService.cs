using Normaize.Core.Configuration;

namespace Normaize.Core.Interfaces;

public interface IConfigurationValidationService
{
    ConfigurationValidationResult ValidateConfiguration(CancellationToken cancellationToken = default);
    ConfigurationValidationResult ValidateDatabaseConfiguration(CancellationToken cancellationToken = default);
    ConfigurationValidationResult ValidateSecurityConfiguration(CancellationToken cancellationToken = default);
    ConfigurationValidationResult ValidateStorageConfiguration(CancellationToken cancellationToken = default);
    ConfigurationValidationResult ValidateCachingConfiguration(CancellationToken cancellationToken = default);
    ConfigurationValidationResult ValidatePerformanceConfiguration(CancellationToken cancellationToken = default);
}

public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Details { get; set; } = new();
    public TimeSpan ValidationDuration { get; set; }
} 