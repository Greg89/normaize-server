using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Infrastructure services required for data processing operations.
/// Consolidates logging, caching, and chaos engineering dependencies.
/// </summary>
public interface IDataProcessingInfrastructure
{
    /// <summary>
    /// Gets the logger for data processing operations.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the memory cache for caching operations.
    /// </summary>
    IMemoryCache Cache { get; }

    /// <summary>
    /// Gets the structured logging service for detailed operation tracking.
    /// </summary>
    IStructuredLoggingService StructuredLogging { get; }

    /// <summary>
    /// Gets the chaos engineering service for resilience testing.
    /// </summary>
    IChaosEngineeringService ChaosEngineering { get; }

    /// <summary>
    /// Gets the cache expiration time for data processing operations.
    /// </summary>
    TimeSpan CacheExpiration { get; }

    /// <summary>
    /// Gets the default timeout for data processing operations.
    /// </summary>
    TimeSpan DefaultTimeout { get; }

    /// <summary>
    /// Gets the quick timeout for fast operations.
    /// </summary>
    TimeSpan QuickTimeout { get; }
}