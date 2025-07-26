using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Normaize.Core.Services.FileUpload;

/// <summary>
/// Service for file utility operations like type detection, hash generation, and storage strategy.
/// Extracted from FileUploadService to follow single responsibility principle.
/// </summary>
public class FileUtilityService : IFileUtilityService
{
    private readonly FileUploadConfiguration _fileUploadConfig;
    private readonly DataProcessingConfiguration _dataProcessingConfig;
    private readonly IStorageService _storageService;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public FileUtilityService(
        IOptions<FileUploadConfiguration> fileUploadConfig,
        IOptions<DataProcessingConfiguration> dataProcessingConfig,
        IStorageService storageService,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(fileUploadConfig);
        ArgumentNullException.ThrowIfNull(dataProcessingConfig);
        ArgumentNullException.ThrowIfNull(storageService);
        ArgumentNullException.ThrowIfNull(infrastructure);

        _fileUploadConfig = fileUploadConfig.Value;
        _dataProcessingConfig = dataProcessingConfig.Value;
        _storageService = storageService;
        _infrastructure = infrastructure;
    }

    public async Task<string> GenerateDataHashAsync(string filePath)
    {
        try
        {
            using var stream = await _storageService.GetFileAsync(filePath);
            var hash = await SHA256.HashDataAsync(stream);
            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            var correlationId = GetCorrelationId();
            var context = _infrastructure.StructuredLogging.CreateContext(
                "GenerateDataHashAsync",
                correlationId,
                AppConstants.Auth.AnonymousUser,
                new Dictionary<string, object> { ["FilePath"] = filePath });

            _infrastructure.StructuredLogging.LogStep(context, AppConstants.FileUpload.FAILED_GENERATE_DATA_HASH_WARNING, new Dictionary<string, object>
            {
                ["FilePath"] = filePath,
                ["ErrorMessage"] = ex.Message
            });
            return string.Empty; // Return empty string instead of throwing
        }
    }

    public FileType GetFileTypeFromExtension(string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            ".csv" => FileType.CSV,
            ".json" => FileType.JSON,
            ".xlsx" or ".xls" => FileType.Excel,
            ".xml" => FileType.XML,
            ".txt" => FileType.TXT,
            ".parquet" => FileType.Parquet,
            _ => FileType.Custom
        };
    }

    public bool ShouldUseSeparateTable(DataSet dataSet) =>
        dataSet.RowCount >= _dataProcessingConfig.MaxRowsPerDataset ||
        dataSet.FileSize > _fileUploadConfig.MaxFileSize;

    public string GetFileExtension(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant();

    public StorageProvider GetStorageProviderFromPath(string filePath)
    {
        return filePath switch
        {
            var path when path.StartsWith("s3://") => StorageProvider.S3,
            var path when path.StartsWith("azure://") => StorageProvider.Azure,
            var path when path.StartsWith("memory://") => StorageProvider.Memory,
            _ => StorageProvider.Local
        };
    }

    #region Private Methods

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion
}