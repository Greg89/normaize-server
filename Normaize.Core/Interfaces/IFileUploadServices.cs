using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Composite interface that groups all file upload sub-services together.
/// This follows the same pattern as IVisualizationServices for DataVisualizationService.
/// </summary>
public interface IFileUploadServices
{
    /// <summary>
    /// File validation service for validating file upload requests and file properties.
    /// </summary>
    IFileValidationService Validation { get; }

    /// <summary>
    /// File processing service for processing different file types and extracting data.
    /// </summary>
    IFileProcessingService Processing { get; }

    /// <summary>
    /// File configuration service for validating and logging file upload configuration.
    /// </summary>
    IFileConfigurationService Configuration { get; }

    /// <summary>
    /// File utility service for file utility operations like type detection and hash generation.
    /// </summary>
    IFileUtilityService Utility { get; }

    /// <summary>
    /// File storage service for file storage operations specific to file upload functionality.
    /// </summary>
    IFileStorageService Storage { get; }
}