using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Normaize.Core.Models;
using Normaize.Core.Constants;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Normaize.Core.Services;

public class DataVisualizationService : IDataVisualizationService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IMemoryCache _cache;
    private readonly DataVisualizationOptions _options;
    private readonly IDataProcessingInfrastructure _infrastructure;
    private readonly Random _random;

    public DataVisualizationService(
        IDataSetRepository dataSetRepository,
        IMemoryCache cache,
        IOptions<DataVisualizationOptions> options,
        IDataProcessingInfrastructure infrastructure)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _infrastructure = infrastructure ?? throw new ArgumentNullException(nameof(infrastructure));
        _random = new Random();
        
        _infrastructure.Logger.LogInformation("DataVisualizationService initialized with configuration: CacheExpiration={CacheExpiration}, MaxDataPoints={MaxDataPoints}, ChaosProcessingDelayProbability={ChaosProcessingDelayProbability}",
            _options.CacheExpiration, _options.MaxDataPoints, _options.ChaosProcessingDelayProbability);
    }

    public async Task<ChartDataDto> GenerateChartAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GenerateChartAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                ["DataSetId"] = dataSetId,
                ["ChartType"] = chartType.ToString(),
                ["Configuration"] = configuration?.ToString() ?? "null"
            },
            validation: () => ValidateGenerateChartInputs(dataSetId, chartType, configuration, userId),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate processing delay
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("ProcessingDelay", GetCorrelationId(), context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating processing delay", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "ProcessingDelay"
                    });
                    await Task.Delay(_random.Next(1000, 5000));
                }, new Dictionary<string, object> { ["UserId"] = userId });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.CHART_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    async () => await GenerateChartInternalAsync(dataSetId, chartType, configuration, userId, GetCorrelationId()),
                    _options.ChartGenerationTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_Internal");
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.CHART_GENERATION_COMPLETED);
                
                return result;
            });
    }

    public async Task<ComparisonChartDto> GenerateComparisonChartAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GenerateComparisonChartAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                ["DataSetId1"] = dataSetId1,
                ["DataSetId2"] = dataSetId2,
                ["ChartType"] = chartType.ToString(),
                ["Configuration"] = configuration?.ToString() ?? "null"
            },
            validation: () => ValidateComparisonChartInputs(dataSetId1, dataSetId2, chartType, configuration, userId),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate network latency
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("NetworkLatency", GetCorrelationId(), context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating network latency", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "NetworkLatency"
                    });
                    await Task.Delay(_random.Next(500, 2000));
                }, new Dictionary<string, object> { ["UserId"] = userId });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.COMPARISON_CHART_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    async () => await GenerateComparisonChartInternalAsync(dataSetId1, dataSetId2, chartType, configuration, userId, GetCorrelationId()),
                    _options.ComparisonChartTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_Internal");
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.COMPARISON_CHART_GENERATION_COMPLETED);
                
                return result;
            });
    }

    public async Task<DataSummaryDto> GetDataSummaryAsync(int dataSetId, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GetDataSummaryAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                ["DataSetId"] = dataSetId
            },
            validation: () => ValidateDataSummaryInputs(dataSetId, userId),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate cache failure
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("CacheFailure", GetCorrelationId(), context.OperationName, () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating cache failure", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "CacheFailure"
                    });
                    throw new InvalidOperationException("Simulated cache failure (chaos engineering)");
                }, new Dictionary<string, object> { ["UserId"] = userId });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.DATA_SUMMARY_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    async () => await GetDataSummaryInternalAsync(dataSetId, userId, GetCorrelationId()),
                    _options.SummaryGenerationTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_Internal");
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.DATA_SUMMARY_GENERATION_COMPLETED);
                
                return result;
            });
    }

    public async Task<StatisticalSummaryDto> GetStatisticalSummaryAsync(int dataSetId, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GetStatisticalSummaryAsync),
            additionalMetadata: new Dictionary<string, object>
            {
                ["DataSetId"] = dataSetId
            },
            validation: () => ValidateStatisticalSummaryInputs(dataSetId, userId),
            operation: async (context) =>
            {
                // Chaos engineering: Simulate memory pressure
                await _infrastructure.ChaosEngineering.ExecuteChaosAsync("MemoryPressure", GetCorrelationId(), context.OperationName, async () =>
                {
                    _infrastructure.StructuredLogging.LogStep(context, "Chaos engineering: Simulating memory pressure", new Dictionary<string, object>
                    {
                        ["ChaosType"] = "MemoryPressure"
                    });
                    // Simulate memory pressure by allocating temporary objects
                    var tempObjects = new List<byte[]>();
                    for (int i = 0; i < 30; i++)
                    {
                        tempObjects.Add(new byte[1024 * 1024]); // 1MB each
                    }
                    await Task.Delay(100);
                    tempObjects.Clear();
                }, new Dictionary<string, object> { ["UserId"] = userId });

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.STATISTICAL_SUMMARY_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    async () => await GetStatisticalSummaryInternalAsync(dataSetId, userId, GetCorrelationId()),
                    _options.StatisticalSummaryTimeout,
                    GetCorrelationId(),
                    $"{context.OperationName}_Internal");
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.STATISTICAL_SUMMARY_GENERATION_COMPLETED);
                
                return result;
            });
    }

    public Task<IEnumerable<ChartType>> GetSupportedChartTypesAsync()
    {
        return Task.FromResult<IEnumerable<ChartType>>(Enum.GetValues<ChartType>());
    }

    public bool ValidateChartConfiguration(ChartType chartType, ChartConfigurationDto? configuration)
    {
        return ValidateChartConfigurationInternal(chartType, configuration);
    }

    #region Private Methods

    private async Task<T> ExecuteVisualizationOperationAsync<T>(
        string operationName,
        Dictionary<string, object>? additionalMetadata,
        Action validation,
        Func<IOperationContext, Task<T>> operation)
    {
        var correlationId = GetCorrelationId();
        var context = _infrastructure.StructuredLogging.CreateContext(operationName, correlationId, AppConstants.Auth.AnonymousUser, additionalMetadata);

        try
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_STARTED);
            validation();
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.INPUT_VALIDATION_COMPLETED);

            var result = await operation(context);
            _infrastructure.StructuredLogging.LogSummary(context, true);
            return result;
        }
        catch (Exception ex)
        {
            _infrastructure.StructuredLogging.LogSummary(context, false, ex.Message);
            
            // Create detailed error message based on operation type and metadata
            var errorMessage = CreateDetailedErrorMessage(operationName, additionalMetadata);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private static string CreateDetailedErrorMessage(string operationName, Dictionary<string, object>? metadata)
    {
        if (metadata == null) return $"Failed to complete {operationName}";

        return operationName switch
        {
            nameof(GenerateChartAsync) => CreateChartErrorMessage(metadata),
            nameof(GenerateComparisonChartAsync) => CreateComparisonChartErrorMessage(metadata),
            nameof(GetDataSummaryAsync) => CreateDataSummaryErrorMessage(metadata),
            nameof(GetStatisticalSummaryAsync) => CreateStatisticalSummaryErrorMessage(metadata),
            _ => $"Failed to complete {operationName}"
        };
    }

    private static string CreateChartErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId = GetMetadataValue(metadata, "DataSetId");
        var chartType = GetMetadataValue(metadata, "ChartType");
        return $"Failed to complete GenerateChartAsync for dataset ID {dataSetId} with chart type {chartType}";
    }

    private static string CreateComparisonChartErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId1 = GetMetadataValue(metadata, "DataSetId1");
        var dataSetId2 = GetMetadataValue(metadata, "DataSetId2");
        var chartType = GetMetadataValue(metadata, "ChartType");
        return $"Failed to complete GenerateComparisonChartAsync for dataset IDs {dataSetId1} and {dataSetId2} with chart type {chartType}";
    }

    private static string CreateDataSummaryErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId = GetMetadataValue(metadata, "DataSetId");
        return $"Failed to complete GetDataSummaryAsync for dataset ID {dataSetId}";
    }

    private static string CreateStatisticalSummaryErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId = GetMetadataValue(metadata, "DataSetId");
        return $"Failed to complete GetStatisticalSummaryAsync for dataset ID {dataSetId}";
    }

    private static string GetMetadataValue(Dictionary<string, object> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) ? value?.ToString() ?? AppConstants.Messages.UNKNOWN : AppConstants.Messages.UNKNOWN;
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion

    #region Validation Methods

    private static void ValidateGenerateChartInputs(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string? userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(AppConstants.VisualizationMessages.INVALID_USER_ID, nameof(userId));
        
        ValidateChartConfigurationInternal(chartType, configuration);
    }

    private static void ValidateComparisonChartInputs(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string? userId)
    {
        if (dataSetId1 <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId1));
        
        if (dataSetId2 <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId2));
        
        if (dataSetId1 == dataSetId2)
            throw new ArgumentException("Dataset IDs must be different for comparison", nameof(dataSetId2));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(AppConstants.VisualizationMessages.INVALID_USER_ID, nameof(userId));
        
        ValidateChartConfigurationInternal(chartType, configuration);
    }

    private static void ValidateDataSummaryInputs(int dataSetId, string? userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(AppConstants.VisualizationMessages.INVALID_USER_ID, nameof(userId));
    }

    private static void ValidateStatisticalSummaryInputs(int dataSetId, string? userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(AppConstants.VisualizationMessages.INVALID_USER_ID, nameof(userId));
    }

    private static bool ValidateChartConfigurationInternal(ChartType chartType, ChartConfigurationDto? configuration)
    {
        if (configuration == null) return true;

        // Validate max data points
        if (configuration.MaxDataPoints.HasValue && configuration.MaxDataPoints.Value <= 0)
        {
            throw new ArgumentException("MaxDataPoints must be greater than 0", nameof(configuration));
        }

        // Validate chart-specific configurations
        switch (chartType)
        {
            case ChartType.Pie:
            case ChartType.Donut:
                if (!string.IsNullOrEmpty(configuration.XAxisLabel) || !string.IsNullOrEmpty(configuration.YAxisLabel))
                {
                    // Log warning but don't throw - this is a validation warning, not an error
                }
                break;
                
            case ChartType.Scatter:
            case ChartType.Bubble:
                if (string.IsNullOrEmpty(configuration.XAxisLabel) || string.IsNullOrEmpty(configuration.YAxisLabel))
                {
                    // Log warning but don't throw - this is a validation warning, not an error
                }
                break;
        }

        return true;
    }

    #endregion

    #region Internal Processing Methods

    private async Task<ChartDataDto> GenerateChartInternalAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Chaos engineering: Simulate processing delay
        if (_random.NextDouble() < _options.ChaosProcessingDelayProbability)
        {
            _infrastructure.Logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
            await Task.Delay(_random.Next(1000, 5000)); // 1-5 second delay
        }

        var cacheKey = GenerateCacheKey($"chart_{dataSetId}_{chartType}", configuration);
        
        if (_cache.TryGetValue(cacheKey, out ChartDataDto? cachedChart))
        {
            _infrastructure.Logger.LogDebug("Retrieved chart from cache. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}",
                correlationId, dataSetId, chartType);
            return cachedChart!;
        }

        _infrastructure.Logger.LogDebug("Cache miss, generating new chart. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}",
            correlationId, dataSetId, chartType);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, correlationId);
        var data = ExtractDataSetData(dataSet, correlationId);
        
        var chartData = GenerateChartData(dataSet, data, chartType, configuration, correlationId);
        chartData.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, chartData, _options.CacheExpiration);

        _infrastructure.Logger.LogInformation("Generated chart successfully. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}, ProcessingTime: {ProcessingTime}ms",
            correlationId, dataSetId, chartType, stopwatch.ElapsedMilliseconds);

        return chartData;
    }

    private async Task<ComparisonChartDto> GenerateComparisonChartInternalAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Chaos engineering: Simulate processing delay
        if (_random.NextDouble() < _options.ChaosProcessingDelayProbability)
        {
            _infrastructure.Logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
            await Task.Delay(_random.Next(1000, 5000)); // 1-5 second delay
        }

        var cacheKey = GenerateCacheKey($"comparison_{dataSetId1}_{dataSetId2}_{chartType}", configuration);
        
        if (_cache.TryGetValue(cacheKey, out ComparisonChartDto? cachedChart))
        {
            _infrastructure.Logger.LogDebug("Retrieved comparison chart from cache. CorrelationId: {CorrelationId}, DataSetId1: {DataSetId1}, DataSetId2: {DataSetId2}",
                correlationId, dataSetId1, dataSetId2);
            return cachedChart!;
        }

        _infrastructure.Logger.LogDebug("Cache miss, generating new comparison chart. CorrelationId: {CorrelationId}, DataSetId1: {DataSetId1}, DataSetId2: {DataSetId2}",
            correlationId, dataSetId1, dataSetId2);

        var dataSet1 = await GetAndValidateDataSetAsync(dataSetId1, userId, correlationId);
        var dataSet2 = await GetAndValidateDataSetAsync(dataSetId2, userId, correlationId);
        
        var data1 = ExtractDataSetData(dataSet1, correlationId);
        var data2 = ExtractDataSetData(dataSet2, correlationId);

        var comparisonChart = GenerateComparisonChartData(dataSet1, dataSet2, data1, data2, chartType, configuration, correlationId);
        comparisonChart.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, comparisonChart, _options.CacheExpiration);

        _infrastructure.Logger.LogInformation("Generated comparison chart successfully. CorrelationId: {CorrelationId}, DataSetId1: {DataSetId1}, DataSetId2: {DataSetId2}, ProcessingTime: {ProcessingTime}ms",
            correlationId, dataSetId1, dataSetId2, stopwatch.ElapsedMilliseconds);

        return comparisonChart;
    }

    private async Task<DataSummaryDto> GetDataSummaryInternalAsync(int dataSetId, string userId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Chaos engineering: Simulate processing delay
        if (_random.NextDouble() < _options.ChaosProcessingDelayProbability)
        {
            _infrastructure.Logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
            await Task.Delay(_random.Next(500, 2000)); // 0.5-2 second delay
        }

        var cacheKey = $"summary_{dataSetId}";
        
        if (_cache.TryGetValue(cacheKey, out DataSummaryDto? cachedSummary))
        {
            _infrastructure.Logger.LogDebug("Retrieved data summary from cache. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                correlationId, dataSetId);
            return cachedSummary!;
        }

        _infrastructure.Logger.LogDebug("Cache miss, generating new data summary. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
            correlationId, dataSetId);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, correlationId);
        var data = ExtractDataSetData(dataSet, correlationId);
        
        var summary = GenerateDataSummary(dataSet, data, correlationId);
        summary.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, summary, _options.CacheExpiration);

        _infrastructure.Logger.LogInformation("Generated data summary successfully. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ProcessingTime: {ProcessingTime}ms",
            correlationId, dataSetId, stopwatch.ElapsedMilliseconds);

        return summary;
    }

    private async Task<StatisticalSummaryDto> GetStatisticalSummaryInternalAsync(int dataSetId, string userId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Chaos engineering: Simulate processing delay
        if (_random.NextDouble() < _options.ChaosProcessingDelayProbability)
        {
            _infrastructure.Logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
            await Task.Delay(_random.Next(1000, 3000)); // 1-3 second delay
        }

        var cacheKey = $"stats_{dataSetId}";
        
        if (_cache.TryGetValue(cacheKey, out StatisticalSummaryDto? cachedStats))
        {
            _infrastructure.Logger.LogDebug("Retrieved statistical summary from cache. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                correlationId, dataSetId);
            return cachedStats!;
        }

        _infrastructure.Logger.LogDebug("Cache miss, generating new statistical summary. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
            correlationId, dataSetId);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, correlationId);
        var data = ExtractDataSetData(dataSet, correlationId);
        
        var stats = GenerateStatisticalSummary(dataSet, data, correlationId);
        stats.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, stats, _options.CacheExpiration);

        _infrastructure.Logger.LogInformation("Generated statistical summary successfully. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ProcessingTime: {ProcessingTime}ms",
            correlationId, dataSetId, stopwatch.ElapsedMilliseconds);

        return stats;
    }

    #endregion

    #region Utility Methods

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _infrastructure.Logger.LogError(ex, "Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private async Task<DataSet> GetAndValidateDataSetAsync(int dataSetId, string userId, string correlationId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
        
        if (dataSet == null)
        {
            _infrastructure.Logger.LogWarning("Dataset not found. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, UserId: {UserId}",
                correlationId, dataSetId, userId);
            throw new ArgumentException($"{AppConstants.VisualizationMessages.DATASET_NOT_FOUND} with ID {dataSetId}", nameof(dataSetId));
        }

        if (dataSet.UserId != userId)
        {
            _infrastructure.Logger.LogWarning("Unauthorized access attempt. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, RequestedUserId: {RequestedUserId}, ActualUserId: {ActualUserId}",
                correlationId, dataSetId, userId, dataSet.UserId);
            throw new UnauthorizedAccessException($"{AppConstants.VisualizationMessages.DATASET_ACCESS_DENIED} - User {userId} is not authorized to access dataset {dataSetId}");
        }

        if (dataSet.IsDeleted)
        {
            _infrastructure.Logger.LogWarning("Attempted to access deleted dataset. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, UserId: {UserId}",
                correlationId, dataSetId, userId);
            throw new ArgumentException($"Dataset {dataSetId} has been deleted", nameof(dataSetId));
        }

        return dataSet;
    }

    private List<Dictionary<string, object>> ExtractDataSetData(DataSet dataSet, string correlationId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataSet.ProcessedData))
            {
                _infrastructure.Logger.LogWarning("Dataset has no processed data. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                    correlationId, dataSet.Id);
                return [];
            }

            var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);
            
            if (data == null)
            {
                _infrastructure.Logger.LogWarning("Failed to deserialize dataset JSON data. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                    correlationId, dataSet.Id);
                return [];
            }

            _infrastructure.Logger.LogDebug("Extracted {RowCount} rows from dataset. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                data.Count, correlationId, dataSet.Id);

            return data;
        }
        catch (JsonException ex)
        {
            _infrastructure.Logger.LogError(ex, "Failed to parse dataset JSON data. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                correlationId, dataSet.Id);
            throw new InvalidOperationException($"Failed to parse dataset {dataSet.Id} data: {ex.Message}", ex);
        }
    }

    private static double ExtractDouble(object? value, double fallback = 0)
    {
        if (value == null) return fallback;
        
        return value switch
        {
            double d => d,
            int i => i,
            long l => l,
            float f => f,
            decimal dec => (double)dec,
            string s when double.TryParse(s, out var result) => result,
            _ => fallback
        };
    }

    private static string GenerateCacheKey(string baseKey, ChartConfigurationDto? configuration)
    {
        if (configuration == null) return baseKey;
        
        var configHash = JsonSerializer.Serialize(configuration);
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(configHash));
        return $"{baseKey}_{Convert.ToBase64String(hash)[..8]}";
    }

    #endregion

    #region Chart Generation Methods

    private ChartDataDto GenerateChartData(DataSet dataSet, List<Dictionary<string, object>> data, ChartType chartType, ChartConfigurationDto? configuration, string correlationId)
    {
        if (data.Count == 0)
        {
            _infrastructure.Logger.LogWarning("No data available for chart generation. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}",
                correlationId, dataSet.Id, chartType);
            return new ChartDataDto
            {
                DataSetId = dataSet.Id,
                ChartType = chartType,
                Labels = [],
                Series = [],
                Configuration = configuration
            };
        }

        var maxDataPoints = configuration?.MaxDataPoints ?? _options.MaxDataPoints;
        var limitedData = data.Take(maxDataPoints).ToList();

        var labels = new List<string>();
        var series = new List<ChartSeriesDto>();

        switch (chartType)
        {
            case ChartType.Bar:
            case ChartType.Line:
            case ChartType.Area:
                GenerateBarLineAreaChart(limitedData, labels, series, correlationId);
                break;
                
            case ChartType.Pie:
            case ChartType.Donut:
                GeneratePieDonutChart(limitedData, labels, series, correlationId);
                break;
                
            case ChartType.Scatter:
            case ChartType.Bubble:
                GenerateScatterBubbleChart(limitedData, labels, series, correlationId);
                break;
                
            default:
                _infrastructure.Logger.LogWarning("Unsupported chart type. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}",
                    correlationId, dataSet.Id, chartType);
                break;
        }

        return new ChartDataDto
        {
            DataSetId = dataSet.Id,
            ChartType = chartType,
            Labels = labels,
            Series = series,
            Configuration = configuration
        };
    }

    private void GenerateBarLineAreaChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, string correlationId)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _infrastructure.Logger.LogWarning("No numeric columns found for chart. Using fallback data. CorrelationId: {CorrelationId}", correlationId);
            
            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = "Count",
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first column as labels (if it's not numeric)
        var labelColumn = columns.FirstOrDefault(col => !numericColumns.Contains(col)) ?? columns[0];
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? AppConstants.Messages.UNKNOWN));

        // Create series for each numeric column
        foreach (var column in numericColumns)
        {
            series.Add(new ChartSeriesDto
            {
                Name = column,
                Data = data.Select(row => ExtractDouble(row.GetValueOrDefault(column))).Cast<object>().ToList()
            });
        }
    }

    private void GeneratePieDonutChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, string correlationId)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _infrastructure.Logger.LogWarning("No numeric columns found for pie/donut chart. Using fallback data. CorrelationId: {CorrelationId}", correlationId);
            
            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = "Count",
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first column as labels
        var labelColumn = columns[0];
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? AppConstants.Messages.UNKNOWN));

        // Use first numeric column as data
        var dataColumn = numericColumns[0];
        series.Add(new ChartSeriesDto
        {
            Name = dataColumn,
            Data = data.Select(row => ExtractDouble(row.GetValueOrDefault(dataColumn))).Cast<object>().ToList()
        });
    }

    private void GenerateScatterBubbleChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, string correlationId)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count < 2)
        {
            _infrastructure.Logger.LogWarning("Insufficient numeric columns for scatter/bubble chart. Using fallback data. CorrelationId: {CorrelationId}", correlationId);
            
            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = "Count",
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first column as labels
        var labelColumn = columns[0];
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? AppConstants.Messages.UNKNOWN));

        // Use first two numeric columns as X and Y
        var xColumn = numericColumns[0];
        var yColumn = numericColumns[1];

        series.Add(new ChartSeriesDto
        {
            Name = $"{xColumn} vs {yColumn}",
            Data = data.Select(row => new
            {
                X = ExtractDouble(row.GetValueOrDefault(xColumn)),
                Y = ExtractDouble(row.GetValueOrDefault(yColumn))
            }).Cast<object>().ToList()
        });
    }

    private ComparisonChartDto GenerateComparisonChartData(DataSet dataSet1, DataSet dataSet2, List<Dictionary<string, object>> data1, List<Dictionary<string, object>> data2, ChartType chartType, ChartConfigurationDto? configuration, string correlationId)
    {
        var chart1 = GenerateChartData(dataSet1, data1, chartType, configuration, correlationId);
        var chart2 = GenerateChartData(dataSet2, data2, chartType, configuration, correlationId);

        return new ComparisonChartDto
        {
            DataSetId1 = dataSet1.Id,
            DataSetId2 = dataSet2.Id,
            ChartType = chartType,
            Series = chart1.Series.Concat(chart2.Series).ToList(),
            Labels = chart1.Labels,
            Configuration = configuration
        };
    }

    #endregion

    #region Summary Generation Methods

    private DataSummaryDto GenerateDataSummary(DataSet dataSet, List<Dictionary<string, object>> data, string correlationId)
    {
        if (data.Count == 0)
        {
                    return new DataSummaryDto
        {
            DataSetId = dataSet.Id,
            TotalRows = 0,
            TotalColumns = 0,
            MissingValues = 0,
            DuplicateRows = 0,
            ColumnSummaries = new Dictionary<string, ColumnSummaryDto>(),
            ProcessingTime = TimeSpan.Zero
        };
        }

        var columns = data[0].Keys.ToList();
        var columnSummaries = new Dictionary<string, ColumnSummaryDto>();
        var totalMissingValues = 0;

        foreach (var column in columns)
        {
            var columnData = data.Select(row => row.GetValueOrDefault(column)).ToList();
            var dataType = DetermineDataType(columnData);
            var nullCount = columnData.Count(v => v == null);
            totalMissingValues += nullCount;

            columnSummaries.Add(column, new ColumnSummaryDto
            {
                ColumnName = column,
                DataType = dataType,
                NonNullCount = columnData.Count - nullCount,
                NullCount = nullCount,
                UniqueCount = columnData.Distinct().Count(),
                SampleValues = columnData.Take(5).Select(x => x?.ToString() ?? "null").Cast<object>().ToList()
            });
        }

        var duplicateRows = data.Count - data.Select(row => JsonSerializer.Serialize(row)).Distinct().Count();

        return new DataSummaryDto
        {
            DataSetId = dataSet.Id,
            TotalRows = data.Count,
            TotalColumns = columns.Count,
            MissingValues = totalMissingValues,
            DuplicateRows = duplicateRows,
            ColumnSummaries = columnSummaries,
            ProcessingTime = TimeSpan.Zero
        };
    }

    private StatisticalSummaryDto GenerateStatisticalSummary(DataSet dataSet, List<Dictionary<string, object>> data, string correlationId)
    {
        if (data.Count == 0)
        {
                    return new StatisticalSummaryDto
        {
            DataSetId = dataSet.Id,
            ColumnStatistics = new Dictionary<string, ColumnStatisticsDto>(),
            ProcessingTime = TimeSpan.Zero
        };
        }

        var columns = data[0].Keys.ToList();
        var columnStatistics = new Dictionary<string, ColumnStatisticsDto>();

        foreach (var column in columns)
        {
            var columnData = data.Select(row => row.GetValueOrDefault(column)).ToList();
            
            if (IsNumericColumn(columnData))
            {
                var numericData = columnData.Select(v => ExtractDouble(v)).Where(v => !double.IsNaN(v)).ToList();
                
                if (numericData.Count > 0)
                {
                    columnStatistics[column] = new ColumnStatisticsDto
                    {
                        ColumnName = column,
                        Mean = numericData.Average(),
                        Median = CalculateMedian(numericData),
                        StandardDeviation = CalculateStandardDeviation(numericData),
                        Min = numericData.Min(),
                        Max = numericData.Max(),
                        Q1 = CalculateQuartile(numericData, 0.25),
                        Q2 = CalculateQuartile(numericData, 0.5),
                        Q3 = CalculateQuartile(numericData, 0.75),
                        Skewness = CalculateSkewness(numericData),
                        Kurtosis = CalculateKurtosis(numericData)
                    };
                }
            }
        }

        return new StatisticalSummaryDto
        {
            DataSetId = dataSet.Id,
            ColumnStatistics = columnStatistics,
            ProcessingTime = TimeSpan.Zero
        };
    }

    #endregion

    #region Statistical Calculation Methods

    private static string DetermineDataType(List<object?> data)
    {
        var nonNullData = data.Where(v => v != null).ToList();
        if (nonNullData.Count == 0) return AppConstants.Messages.UNKNOWN;

        if (nonNullData.All(v => IsNumeric(v))) return "Numeric";
        if (nonNullData.All(v => v is DateTime)) return "DateTime";
        if (nonNullData.All(v => v is bool)) return "Boolean";
        return "String";
    }

    private static bool IsNumeric(object? value)
    {
        return value switch
        {
            int or long or float or double or decimal => true,
            string s => double.TryParse(s, out _),
            _ => false
        };
    }

    private static bool IsNumericColumn(List<object?> data)
    {
        var nonNullData = data.Where(v => v != null).ToList();
        return nonNullData.Count > 0 && nonNullData.All(v => IsNumeric(v));
    }

    private static double CalculateMedian(List<double> data)
    {
        if (data.Count == 0) return 0;
        
        var sorted = data.OrderBy(x => x).ToList();
        var mid = sorted.Count / 2;
        
        return sorted.Count % 2 == 0 
            ? (sorted[mid - 1] + sorted[mid]) / 2 
            : sorted[mid];
    }

    private static double CalculateStandardDeviation(List<double> data)
    {
        if (data.Count <= 1) return 0;
        
        var mean = data.Average();
        var variance = data.Select(x => Math.Pow(x - mean, 2)).Average();
        return Math.Sqrt(variance);
    }

    private static double CalculateQuartile(List<double> data, double percentile)
    {
        if (data.Count == 0) return 0;
        
        var sorted = data.OrderBy(x => x).ToList();
        var index = (percentile * (sorted.Count - 1));
        var lower = sorted[(int)Math.Floor(index)];
        var upper = sorted[(int)Math.Ceiling(index)];
        
        return lower + (upper - lower) * (index - Math.Floor(index));
    }

    private static double CalculateSkewness(List<double> data)
    {
        if (data.Count <= 2) return 0;
        
        var mean = data.Average();
        var stdDev = CalculateStandardDeviation(data);
        if (Math.Abs(stdDev) < double.Epsilon) return 0;
        
        var skewness = data.Select(x => Math.Pow((x - mean) / stdDev, 3)).Average();
        return skewness * Math.Sqrt(data.Count * (data.Count - 1)) / (data.Count - 2);
    }

    private static double CalculateKurtosis(List<double> data)
    {
        if (data.Count <= 3) return 0;
        
        var mean = data.Average();
        var stdDev = CalculateStandardDeviation(data);
        if (Math.Abs(stdDev) < double.Epsilon) return 0;
        
        var kurtosis = data.Select(x => Math.Pow((x - mean) / stdDev, 4)).Average();
        return (kurtosis - 3) * Math.Sqrt(data.Count * (data.Count - 1)) / ((data.Count - 2) * (data.Count - 3));
    }

    //private static double CalculateCorrelation(List<double> data1, List<double> data2)
    //{
    //    if (data1.Count != data2.Count || data1.Count == 0) return 0;
        
        //var mean1 = data1.Average();
        //var mean2 = data2.Average();
        
        //var numerator = data1.Zip(data2, (x, y) => (x - mean1) * (y - mean2)).Sum();
        //var denominator1 = data1.Select(x => Math.Pow(x - mean1, 2)).Sum();
        //var denominator2 = data2.Select(y => Math.Pow(y - mean2, 2)).Sum();
        
        //if (Math.Abs(denominator1) < double.Epsilon || Math.Abs(denominator2) < double.Epsilon) return 0;
        
        //return numerator / Math.Sqrt(denominator1 * denominator2);
    //}

    #endregion
}

public class DataVisualizationOptions
{
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public int MaxDataPoints { get; set; } = 1000;
    public TimeSpan ChartGenerationTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan ComparisonChartTimeout { get; set; } = TimeSpan.FromMinutes(3);
    public TimeSpan SummaryGenerationTimeout { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan StatisticalSummaryTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public double ChaosProcessingDelayProbability { get; set; } = 0.001; // 0.1%
} 