using Normaize.Core.Interfaces;

namespace Normaize.Core.Services.FileUpload;

/// <summary>
/// Composite implementation that groups all file upload sub-services together.
/// This follows the same pattern as VisualizationServices for DataVisualizationService.
/// </summary>
public class FileUploadServices : IFileUploadServices
{
    public FileUploadServices(
        IFileValidationService validation,
        IFileProcessingService processing,
        IFileConfigurationService configuration,
        IFileUtilityService utility,
        IFileStorageService storage)
    {
        ArgumentNullException.ThrowIfNull(validation);
        ArgumentNullException.ThrowIfNull(processing);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(utility);
        ArgumentNullException.ThrowIfNull(storage);

        Validation = validation;
        Processing = processing;
        Configuration = configuration;
        Utility = utility;
        Storage = storage;
    }

    public IFileValidationService Validation { get; }
    public IFileProcessingService Processing { get; }
    public IFileConfigurationService Configuration { get; }
    public IFileUtilityService Utility { get; }
    public IFileStorageService Storage { get; }
}