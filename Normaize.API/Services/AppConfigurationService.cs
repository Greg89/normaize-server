using Normaize.Core.Interfaces;

namespace Normaize.API.Services;

public class AppConfigurationService : IAppConfigurationService
{
    public string? Get(string key)
    {
        return Environment.GetEnvironmentVariable(key);
    }
} 