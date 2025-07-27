using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for managing cache operations in visualization services.
/// Extracted from DataVisualizationService to follow single responsibility principle.
/// </summary>
public interface ICacheManagementService
{
    /// <summary>
    /// Attempts to retrieve a value from cache.
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="cacheKey">The cache key to look up</param>
    /// <param name="value">The cached value if found</param>
    /// <returns>True if the value was found in cache</returns>
    bool TryGetValue<T>(string cacheKey, out T? value);

    /// <summary>
    /// Stores a value in cache with the specified expiration.
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="cacheKey">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="expiration">Cache expiration time</param>
    void Set<T>(string cacheKey, T value, TimeSpan expiration);

    /// <summary>
    /// Generates a cache key for chart data with configuration hash.
    /// </summary>
    /// <param name="baseKey">The base cache key</param>
    /// <param name="configuration">Optional chart configuration</param>
    /// <returns>A unique cache key</returns>
    string GenerateCacheKey(string baseKey, ChartConfigurationDto? configuration);

    /// <summary>
    /// Generates a cache key for chart data.
    /// </summary>
    /// <param name="dataSetId">The dataset ID</param>
    /// <param name="chartType">The chart type</param>
    /// <param name="configuration">Optional chart configuration</param>
    /// <returns>A unique cache key for chart data</returns>
    string GenerateChartCacheKey(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration);

    /// <summary>
    /// Generates a cache key for comparison chart data.
    /// </summary>
    /// <param name="dataSetId1">The first dataset ID</param>
    /// <param name="dataSetId2">The second dataset ID</param>
    /// <param name="chartType">The chart type</param>
    /// <param name="configuration">Optional chart configuration</param>
    /// <returns>A unique cache key for comparison chart data</returns>
    string GenerateComparisonChartCacheKey(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration);

    /// <summary>
    /// Generates a cache key for data summary.
    /// </summary>
    /// <param name="dataSetId">The dataset ID</param>
    /// <returns>A unique cache key for data summary</returns>
    string GenerateDataSummaryCacheKey(int dataSetId);

    /// <summary>
    /// Generates a cache key for statistical summary.
    /// </summary>
    /// <param name="dataSetId">The dataset ID</param>
    /// <returns>A unique cache key for statistical summary</returns>
    string GenerateStatisticalSummaryCacheKey(int dataSetId);

    /// <summary>
    /// Removes a value from cache.
    /// </summary>
    /// <param name="cacheKey">The cache key to remove</param>
    void Remove(string cacheKey);

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    void Clear();
}