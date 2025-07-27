using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Normaize.Core.Services.FileUpload;

/// <summary>
/// Service for validating and logging file upload configuration.
/// Extracted from FileUploadService to follow single responsibility principle.
/// </summary>
public class FileConfigurationService : IFileConfigurationService
{
    private readonly FileUploadConfiguration _fileUploadConfig;
    private readonly DataProcessingConfiguration _dataProcessingConfig;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public FileConfigurationService(
        IOptions<FileUploadConfiguration> fileUploadConfig,
        IOptions<DataProcessingConfiguration> dataProcessingConfig,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(fileUploadConfig);
        ArgumentNullException.ThrowIfNull(dataProcessingConfig);
        ArgumentNullException.ThrowIfNull(infrastructure);

        _fileUploadConfig = fileUploadConfig.Value;
        _dataProcessingConfig = dataProcessingConfig.Value;
        _infrastructure = infrastructure;
    }

    public void ValidateConfiguration()
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(_fileUploadConfig);

        if (!Validator.TryValidateObject(_fileUploadConfig, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"{AppConstants.FileUploadMessages.CONFIGURATION_VALIDATION_FAILED}: {errors}");
        }

        validationResults.Clear();
        validationContext = new ValidationContext(_dataProcessingConfig);

        if (!Validator.TryValidateObject(_dataProcessingConfig, validationContext, validationResults, true))
        {
            var errors = string.Join(", ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"{AppConstants.FileUploadMessages.CONFIGURATION_VALIDATION_FAILED}: {errors}");
        }

        // Additional cross-validation
        ValidateExtensionConfiguration();
    }

    public void LogConfiguration()
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(
            "LogConfiguration",
            correlationId,
            AppConstants.Auth.AnonymousUser,
            new Dictionary<string, object>
            {
                ["MaxFileSizeMB"] = _fileUploadConfig.MaxFileSize / AppConstants.FileProcessing.BYTES_PER_MEGABYTE,
                ["MaxRowsPerDataset"] = _dataProcessingConfig.MaxRowsPerDataset,
                ["AllowedExtensions"] = string.Join(", ", _fileUploadConfig.AllowedExtensions),
                ["BlockedExtensions"] = string.Join(", ", _fileUploadConfig.BlockedExtensions)
            });

        _infrastructure.StructuredLogging.LogStep(context, "FileUploadService configuration logged");
        _infrastructure.StructuredLogging.LogSummary(context, true);
    }

    public void ValidateExtensionConfiguration()
    {
        if (_fileUploadConfig.AllowedExtensions.Any(ext => _fileUploadConfig.BlockedExtensions.Contains(ext)))
        {
            throw new InvalidOperationException(AppConstants.FileUploadMessages.ALLOWED_EXTENSIONS_CONFLICT);
        }
    }

    #region Private Methods

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion
}