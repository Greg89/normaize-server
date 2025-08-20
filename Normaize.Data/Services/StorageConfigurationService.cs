using Microsoft.Extensions.Options;
using Normaize.Core.Configuration;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;

namespace Normaize.Data.Services;

/// <summary>
/// Service for managing storage configuration and providing storage-related diagnostics
/// </summary>
/// <remarks>
/// This service is responsible for:
/// - Retrieving storage configuration settings
/// - Determining the active storage provider
/// - Validating storage provider configurations (S3, Azure, Memory, Local)
/// - Providing comprehensive storage diagnostics for monitoring and troubleshooting
/// 
/// The service follows Clean Architecture principles by:
/// - Depending only on interfaces defined in the Core layer
/// - Not exposing implementation details to higher layers
/// - Providing a clean abstraction for storage configuration management
/// </remarks>
public class StorageConfigurationService : IStorageConfigurationService
{
    private readonly IAppConfigurationService _appConfigService;
    private readonly StorageConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the StorageConfigurationService
    /// </summary>
    /// <param name="appConfigService">Service for retrieving application configuration</param>
    /// <param name="configuration">Storage configuration options</param>
    /// <exception cref="ArgumentNullException">Thrown when appConfigService or configuration is null</exception>
    public StorageConfigurationService(IAppConfigurationService appConfigService, IOptions<StorageConfiguration> configuration)
    {
        _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Retrieves the current storage configuration
    /// </summary>
    /// <returns>The current storage configuration settings</returns>
    /// <remarks>
    /// This method provides access to all storage-related configuration options
    /// including provider settings, file size limits, and cloud storage credentials.
    /// </remarks>
    public StorageConfiguration GetConfiguration()
    {
        return _configuration;
    }

    /// <summary>
    /// Generates comprehensive storage diagnostics information
    /// </summary>
    /// <returns>A StorageDiagnosticsDto containing detailed storage configuration status</returns>
    /// <remarks>
    /// This method analyzes the current storage configuration and provides:
    /// - Active storage provider identification
    /// - S3 configuration status (bucket, credentials, service URL)
    /// - Azure configuration status (connection string, container)
    /// - Environment information
    /// - Configuration completeness indicators
    /// 
    /// The diagnostics are useful for:
    /// - Monitoring storage service health
    /// - Troubleshooting configuration issues
    /// - Validating deployment configurations
    /// </remarks>
    public StorageDiagnosticsDto GetDiagnostics()
    {
        var environment = _appConfigService.GetEnvironment();
        var storageProvider = GetStorageProvider();

        return new StorageDiagnosticsDto
        {
            StorageProvider = storageProvider,
            S3Configured = IsS3Configured(),
            S3Bucket = GetConfigurationStatus(_configuration.S3Bucket),
            S3AccessKey = GetConfigurationStatus(_configuration.S3AccessKey),
            S3SecretKey = GetConfigurationStatus(_configuration.S3SecretKey),
            S3ServiceUrl = GetConfigurationStatus(_configuration.S3ServiceUrl),
            Environment = GetConfigurationStatus(environment)
        };
    }

    /// <summary>
    /// Determines if S3 storage is properly configured
    /// </summary>
    /// <returns>True if all required S3 configuration parameters are set, false otherwise</returns>
    /// <remarks>
    /// S3 storage requires the following configuration parameters:
    /// - S3Bucket: The S3 bucket name for file storage
    /// - S3AccessKey: AWS access key for authentication
    /// - S3SecretKey: AWS secret key for authentication
    /// 
    /// Note: S3ServiceUrl is optional and only required for custom S3-compatible endpoints.
    /// </remarks>
    public bool IsS3Configured()
    {
        return !string.IsNullOrEmpty(_configuration.S3Bucket) &&
               !string.IsNullOrEmpty(_configuration.S3AccessKey) &&
               !string.IsNullOrEmpty(_configuration.S3SecretKey);
    }

    /// <summary>
    /// Determines if Azure Blob storage is properly configured
    /// </summary>
    /// <returns>True if all required Azure configuration parameters are set, false otherwise</returns>
    /// <remarks>
    /// Azure Blob storage requires the following configuration parameters:
    /// - AzureConnectionString: Connection string to the Azure storage account
    /// - AzureContainer: Container name within the storage account
    /// 
    /// The connection string should include account name, account key, and endpoint information.
    /// </remarks>
    public bool IsAzureConfigured()
    {
        return !string.IsNullOrEmpty(_configuration.AzureConnectionString) &&
               !string.IsNullOrEmpty(_configuration.AzureContainer);
    }

    /// <summary>
    /// Determines the active storage provider based on configuration
    /// </summary>
    /// <returns>The StorageProvider enum value representing the active storage backend</returns>
    /// <remarks>
    /// This method maps the configuration provider string to the corresponding StorageProvider enum.
    /// Supported providers include:
    /// - "s3" or "S3" → StorageProvider.S3
    /// - "azure" or "Azure" → StorageProvider.Azure
    /// - "memory" or "Memory" → StorageProvider.Memory
    /// - "local" or "Local" → StorageProvider.Local
    /// - Any other value → StorageProvider.Local (default fallback)
    /// 
    /// The comparison is case-insensitive for better user experience.
    /// </remarks>
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

    #region Private Methods

    /// <summary>
    /// Determines the configuration status for a given configuration value
    /// </summary>
    /// <param name="value">The configuration value to check</param>
    /// <returns>"SET" if the value is configured, "NOT SET" if it's missing or empty</returns>
    /// <remarks>
    /// This helper method provides consistent status reporting across all configuration parameters.
    /// It handles null, empty string, and whitespace-only values as "NOT SET".
    /// </remarks>
    private static string GetConfigurationStatus(string? value)
    {
        return !string.IsNullOrEmpty(value) ? AppConstants.ConfigStatus.SET : AppConstants.ConfigStatus.NOT_SET;
    }

    #endregion
}