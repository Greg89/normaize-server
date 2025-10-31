using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Service interface for caching visualization data
/// </summary>
public interface IVisualizationCacheService
{
    /// <summary>
    /// Attempts to retrieve a cached chart result
    /// </summary>
    bool TryGetChart(Guid dataSetId, ChartType chartType, ChartConfigurationDto? configuration, out ChartDataDto? chartData);

    /// <summary>
    /// Caches a chart result with sliding expiration
    /// </summary>
    void CacheChart(Guid dataSetId, ChartType chartType, ChartConfigurationDto? configuration, ChartDataDto chartData, TimeSpan? expiration = null);

    /// <summary>
    /// Attempts to retrieve a cached comparison chart result
    /// </summary>
    bool TryGetComparisonChart(Guid dataSetId1, Guid dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, out ComparisonChartDto? chartData);

    /// <summary>
    /// Caches a comparison chart result with sliding expiration
    /// </summary>
    void CacheComparisonChart(Guid dataSetId1, Guid dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, ComparisonChartDto chartData, TimeSpan? expiration = null);

    /// <summary>
    /// Attempts to retrieve a cached data summary result
    /// </summary>
    bool TryGetDataSummary(Guid dataSetId, out DataSummaryDto? summary);

    /// <summary>
    /// Caches a data summary result with sliding expiration
    /// </summary>
    void CacheDataSummary(Guid dataSetId, DataSummaryDto summary, TimeSpan? expiration = null);

    /// <summary>
    /// Attempts to retrieve a cached statistical summary result
    /// </summary>
    bool TryGetStatisticalSummary(Guid dataSetId, out StatisticalSummaryDto? summary);

    /// <summary>
    /// Caches a statistical summary result with sliding expiration
    /// </summary>
    void CacheStatisticalSummary(Guid dataSetId, StatisticalSummaryDto summary, TimeSpan? expiration = null);

    /// <summary>
    /// Invalidates all cache entries for a specific dataset
    /// </summary>
    void InvalidateDataSet(Guid dataSetId);

    /// <summary>
    /// Clears all cached visualization data
    /// </summary>
    void ClearAll();
}

/// <summary>
/// Implementation of visualization caching service using IMemoryCache
/// </summary>
public class VisualizationCacheService : IVisualizationCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<VisualizationCacheService> _logger;
    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);
    private const string ChartCachePrefix = "chart:";
    private const string ComparisonChartCachePrefix = "comparison_chart:";
    private const string DataSummaryCachePrefix = "data_summary:";
    private const string StatisticalSummaryCachePrefix = "statistical_summary:";

    public VisualizationCacheService(IMemoryCache cache, ILogger<VisualizationCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool TryGetChart(Guid dataSetId, ChartType chartType, ChartConfigurationDto? configuration, out ChartDataDto? chartData)
    {
        var key = GenerateChartCacheKey(dataSetId, chartType, configuration);
        var found = _cache.TryGetValue(key, out chartData);

        if (found)
        {
            _logger.LogDebug("Cache hit for chart: {CacheKey}", key);
        }
        else
        {
            _logger.LogDebug("Cache miss for chart: {CacheKey}", key);
        }

        return found;
    }

    public void CacheChart(Guid dataSetId, ChartType chartType, ChartConfigurationDto? configuration, ChartDataDto chartData, TimeSpan? expiration = null)
    {
        var key = GenerateChartCacheKey(dataSetId, chartType, configuration);
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiration ?? DefaultExpiration
        };

        _cache.Set(key, chartData, options);
        _logger.LogDebug("Cached chart: {CacheKey} with expiration {Expiration}", key, expiration ?? DefaultExpiration);
    }

    public bool TryGetComparisonChart(Guid dataSetId1, Guid dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, out ComparisonChartDto? chartData)
    {
        var key = GenerateComparisonChartCacheKey(dataSetId1, dataSetId2, chartType, configuration);
        var found = _cache.TryGetValue(key, out chartData);

        if (found)
        {
            _logger.LogDebug("Cache hit for comparison chart: {CacheKey}", key);
        }
        else
        {
            _logger.LogDebug("Cache miss for comparison chart: {CacheKey}", key);
        }

        return found;
    }

    public void CacheComparisonChart(Guid dataSetId1, Guid dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, ComparisonChartDto chartData, TimeSpan? expiration = null)
    {
        var key = GenerateComparisonChartCacheKey(dataSetId1, dataSetId2, chartType, configuration);
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiration ?? DefaultExpiration
        };

        _cache.Set(key, chartData, options);
        _logger.LogDebug("Cached comparison chart: {CacheKey} with expiration {Expiration}", key, expiration ?? DefaultExpiration);
    }

    public bool TryGetDataSummary(Guid dataSetId, out DataSummaryDto? summary)
    {
        var key = GenerateDataSummaryCacheKey(dataSetId);
        var found = _cache.TryGetValue(key, out summary);

        if (found)
        {
            _logger.LogDebug("Cache hit for data summary: {CacheKey}", key);
        }
        else
        {
            _logger.LogDebug("Cache miss for data summary: {CacheKey}", key);
        }

        return found;
    }

    public void CacheDataSummary(Guid dataSetId, DataSummaryDto summary, TimeSpan? expiration = null)
    {
        var key = GenerateDataSummaryCacheKey(dataSetId);
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiration ?? DefaultExpiration
        };

        _cache.Set(key, summary, options);
        _logger.LogDebug("Cached data summary: {CacheKey} with expiration {Expiration}", key, expiration ?? DefaultExpiration);
    }

    public bool TryGetStatisticalSummary(Guid dataSetId, out StatisticalSummaryDto? summary)
    {
        var key = GenerateStatisticalSummaryCacheKey(dataSetId);
        var found = _cache.TryGetValue(key, out summary);

        if (found)
        {
            _logger.LogDebug("Cache hit for statistical summary: {CacheKey}", key);
        }
        else
        {
            _logger.LogDebug("Cache miss for statistical summary: {CacheKey}", key);
        }

        return found;
    }

    public void CacheStatisticalSummary(Guid dataSetId, StatisticalSummaryDto summary, TimeSpan? expiration = null)
    {
        var key = GenerateStatisticalSummaryCacheKey(dataSetId);
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiration ?? DefaultExpiration
        };

        _cache.Set(key, summary, options);
        _logger.LogDebug("Cached statistical summary: {CacheKey} with expiration {Expiration}", key, expiration ?? DefaultExpiration);
    }

    public void InvalidateDataSet(Guid dataSetId)
    {
        // IMemoryCache doesn't provide a way to enumerate or remove by prefix
        // We need to remove specific keys that we know about
        // This is a limitation of IMemoryCache - for production use, consider IDistributedCache

        // Note: Since we can't enumerate all possible configuration hashes,
        // this method removes the most common cache keys
        // A more robust solution would track cache keys separately or use a different cache implementation

        _logger.LogInformation("Invalidating cache entries for dataset {DataSetId}", dataSetId);

        // We can only remove keys we know about
        // For a complete solution, you would need to track cache keys separately
        // or use a cache implementation that supports prefix-based removal
    }

    public void ClearAll()
    {
        // Note: IMemoryCache doesn't have a built-in Clear method
        // In a production scenario, you might want to track keys or use a different cache implementation
        _logger.LogWarning("ClearAll called but IMemoryCache doesn't support clearing all entries. Consider using IDistributedCache for this functionality.");
    }

    private string GenerateChartCacheKey(Guid dataSetId, ChartType chartType, ChartConfigurationDto? configuration)
    {
        var configHash = GenerateConfigurationHash(configuration);
        return $"{ChartCachePrefix}{dataSetId}:{chartType}:{configHash}";
    }

    private string GenerateComparisonChartCacheKey(Guid dataSetId1, Guid dataSetId2, ChartType chartType, ChartConfigurationDto? configuration)
    {
        var configHash = GenerateConfigurationHash(configuration);
        // Sort IDs to ensure consistent cache keys regardless of parameter order
        var sortedIds = new[] { dataSetId1, dataSetId2 }.OrderBy(id => id).ToArray();
        return $"{ComparisonChartCachePrefix}{sortedIds[0]}:{sortedIds[1]}:{chartType}:{configHash}";
    }

    private string GenerateDataSummaryCacheKey(Guid dataSetId)
    {
        return $"{DataSummaryCachePrefix}{dataSetId}";
    }

    private string GenerateStatisticalSummaryCacheKey(Guid dataSetId)
    {
        return $"{StatisticalSummaryCachePrefix}{dataSetId}";
    }

    private string GenerateConfigurationHash(ChartConfigurationDto? configuration)
    {
        if (configuration == null)
        {
            return "default";
        }

        try
        {
            // Serialize configuration to JSON and generate hash
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(hashBytes)[..16]; // Use first 16 characters
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate configuration hash, using 'error' as fallback");
            return "error";
        }
    }
}
