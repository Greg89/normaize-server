using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;

namespace Normaize.Data.Services;

public class StorageConfigurationService : IStorageConfigurationService
{
    private readonly IAppConfigurationService _appConfigService;
    private readonly StorageConfiguration _configuration;

    public StorageConfigurationService(IAppConfigurationService appConfigService, IOptions<StorageConfiguration> configuration)
    {
        _appConfigService = appConfigService;
        _configuration = configuration.Value;
    }

    public StorageConfiguration GetConfiguration()
    {
        return _configuration;
    }

    public StorageDiagnosticsDto GetDiagnostics()
    {
        var environment = _appConfigService.GetEnvironment();
        var storageProvider = GetStorageProvider();

        return new StorageDiagnosticsDto
        {
            StorageProvider = storageProvider,
            S3Configured = IsS3Configured(),
            S3Bucket = !string.IsNullOrEmpty(_configuration.S3Bucket) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET,
            S3AccessKey = !string.IsNullOrEmpty(_configuration.S3AccessKey) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET,
            S3SecretKey = !string.IsNullOrEmpty(_configuration.S3SecretKey) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET,
            S3ServiceUrl = !string.IsNullOrEmpty(_configuration.S3ServiceUrl) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET,
            Environment = !string.IsNullOrEmpty(environment) ? environment : AppConstants.ConfigStatus.NOT_SET
        };
    }

    public bool IsS3Configured()
    {
        return !string.IsNullOrEmpty(_configuration.S3Bucket) &&
               !string.IsNullOrEmpty(_configuration.S3AccessKey) &&
               !string.IsNullOrEmpty(_configuration.S3SecretKey);
    }

    public bool IsAzureConfigured()
    {
        return !string.IsNullOrEmpty(_configuration.AzureConnectionString) &&
               !string.IsNullOrEmpty(_configuration.AzureContainer);
    }

    public StorageProvider GetStorageProvider()
    {
        return _configuration.Provider.ToLowerInvariant() switch
        {
            "s3" => StorageProvider.S3,
            "azure" => StorageProvider.Azure,
            "memory" => StorageProvider.Memory,
            _ => StorageProvider.Local
        };
    }
}