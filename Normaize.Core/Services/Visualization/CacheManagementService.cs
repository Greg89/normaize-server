using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using System.Security.Cryptography;
using System.Text.Json;

namespace Normaize.Core.Services.Visualization;

/// <summary>
/// Service for managing cache operations in visualization services.
/// Extracted from DataVisualizationService to follow single responsibility principle.
/// </summary>
public class CacheManagementService(IMemoryCache cache, IOptions<DataVisualizationOptions> options) : ICacheManagementService
{
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly DataVisualizationOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    public bool TryGetValue<T>(string cacheKey, out T? value)
    {
        return _cache.TryGetValue(cacheKey, out value);
    }

    public void Set<T>(string cacheKey, T value, TimeSpan expiration)
    {
        _cache.Set(cacheKey, value, expiration);
    }

    /// <summary>
    /// Stores a value in cache with default expiration from configuration.
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="cacheKey">The cache key</param>
    /// <param name="value">The value to cache</param>
    public void Set<T>(string cacheKey, T value)
    {
        _cache.Set(cacheKey, value, _options.CacheExpiration);
    }

    public string GenerateCacheKey(string baseKey, ChartConfigurationDto? configuration)
    {
        if (configuration == null) return baseKey;

        var configHash = JsonSerializer.Serialize(configuration);
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(configHash));
        return $"{baseKey}_{Convert.ToBase64String(hash)[..AppConstants.DataProcessing.CACHE_KEY_HASH_LENGTH]}";
    }

    public string GenerateChartCacheKey(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration)
    {
        var baseKey = $"chart_{dataSetId}_{chartType}";
        return GenerateCacheKey(baseKey, configuration);
    }

    public string GenerateComparisonChartCacheKey(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration)
    {
        var baseKey = $"comparison_{dataSetId1}_{dataSetId2}_{chartType}";
        return GenerateCacheKey(baseKey, configuration);
    }

    public string GenerateDataSummaryCacheKey(int dataSetId)
    {
        return $"summary_{dataSetId}";
    }

    public string GenerateStatisticalSummaryCacheKey(int dataSetId)
    {
        return $"stats_{dataSetId}";
    }

    public void Remove(string cacheKey)
    {
        _cache.Remove(cacheKey);
    }

    public void Clear()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
    }
}