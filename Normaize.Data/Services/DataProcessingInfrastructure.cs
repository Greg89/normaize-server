using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;

namespace Normaize.Data.Services;

/// <summary>
/// Implementation of data processing infrastructure that consolidates
/// logging, caching, and chaos engineering dependencies.
/// </summary>
public class DataProcessingInfrastructure : IDataProcessingInfrastructure
{
    public ILogger Logger { get; }
    public IMemoryCache Cache { get; }
    public IStructuredLoggingService StructuredLogging { get; }
    public IChaosEngineeringService ChaosEngineering { get; }
    public TimeSpan CacheExpiration { get; } = TimeSpan.FromMinutes(5);
    public TimeSpan DefaultTimeout { get; } = TimeSpan.FromMinutes(10);
    public TimeSpan QuickTimeout { get; } = TimeSpan.FromSeconds(30);

    public DataProcessingInfrastructure(
        ILoggerFactory loggerFactory,
        IMemoryCache cache,
        IStructuredLoggingService structuredLogging,
        IChaosEngineeringService chaosEngineering)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(structuredLogging);
        ArgumentNullException.ThrowIfNull(chaosEngineering);
        Logger = loggerFactory.CreateLogger<DataProcessingInfrastructure>();
        Cache = cache;
        StructuredLogging = structuredLogging;
        ChaosEngineering = chaosEngineering;
    }
}