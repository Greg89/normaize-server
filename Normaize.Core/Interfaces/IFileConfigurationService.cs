using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for validating and logging file upload configuration.
/// Extracted from FileUploadService to follow single responsibility principle.
/// </summary>
public interface IFileConfigurationService
{
    /// <summary>
    /// Validates the file upload configuration for required properties and constraints.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when configuration validation fails</exception>
    void ValidateConfiguration();

    /// <summary>
    /// Logs the current configuration settings for debugging and monitoring purposes.
    /// </summary>
    void LogConfiguration();

    /// <summary>
    /// Validates that there are no conflicts between allowed and blocked file extensions.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when there are extension conflicts</exception>
    void ValidateExtensionConfiguration();
}