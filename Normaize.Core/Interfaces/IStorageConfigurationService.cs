using Normaize.Core.Configuration;
using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

public interface IStorageConfigurationService
{
    StorageConfiguration GetConfiguration();
    StorageDiagnosticsDto GetDiagnostics();
    bool IsS3Configured();
    bool IsAzureConfigured();
    StorageProvider GetStorageProvider();
} 