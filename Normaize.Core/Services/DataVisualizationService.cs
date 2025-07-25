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
        ArgumentNullException.ThrowIfNull(dataSetRepository);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(infrastructure);

        _dataSetRepository = dataSetRepository;
        _cache = cache;
        _options = options.Value;
        _infrastructure = infrastructure;
        _random = new Random();
    }

    public async Task<ChartDataDto> GenerateChartAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GenerateChartAsync),
            additionalMetadata: CreateChartMetadata(dataSetId, chartType, configuration),
            validation: () => ValidateGenerateChartInputs(dataSetId, chartType, configuration, userId),
            operation: async (context) =>
            {
                await ExecuteChaosEngineeringAsync(context, AppConstants.ChaosEngineering.PROCESSING_DELAY, userId);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.CHART_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => GenerateChartInternalAsync(dataSetId, chartType, configuration, userId, context),
                    _options.ChartGenerationTimeout,
                    context.CorrelationId,
                    $"{context.OperationName}_Internal");
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.CHART_GENERATION_COMPLETED);

                return result;
            });
    }

    public async Task<ComparisonChartDto> GenerateComparisonChartAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GenerateComparisonChartAsync),
            additionalMetadata: CreateComparisonChartMetadata(dataSetId1, dataSetId2, chartType, configuration),
            validation: () => ValidateComparisonChartInputs(dataSetId1, dataSetId2, chartType, configuration, userId),
            operation: async (context) =>
            {
                await ExecuteChaosEngineeringAsync(context, AppConstants.ChaosEngineering.NETWORK_LATENCY, userId);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.COMPARISON_CHART_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => GenerateComparisonChartInternalAsync(dataSetId1, dataSetId2, chartType, configuration, userId, context),
                    _options.ComparisonChartTimeout,
                    context.CorrelationId,
                    $"{context.OperationName}_Internal");
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.COMPARISON_CHART_GENERATION_COMPLETED);

                return result;
            });
    }

    public async Task<DataSummaryDto> GetDataSummaryAsync(int dataSetId, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GetDataSummaryAsync),
            additionalMetadata: CreateDataSummaryMetadata(dataSetId),
            validation: () => ValidateDataSummaryInputs(dataSetId, userId),
            operation: async (context) =>
            {
                await ExecuteChaosEngineeringAsync(context, AppConstants.ChaosEngineering.CACHE_FAILURE, userId);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.DATA_SUMMARY_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => GetDataSummaryInternalAsync(dataSetId, userId, context),
                    _options.SummaryGenerationTimeout,
                    context.CorrelationId,
                    $"{context.OperationName}_Internal");
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.DATA_SUMMARY_GENERATION_COMPLETED);

                return result;
            });
    }

    public async Task<StatisticalSummaryDto> GetStatisticalSummaryAsync(int dataSetId, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GetStatisticalSummaryAsync),
            additionalMetadata: CreateDataSummaryMetadata(dataSetId),
            validation: () => ValidateStatisticalSummaryInputs(dataSetId, userId),
            operation: async (context) =>
            {
                await ExecuteChaosEngineeringAsync(context, AppConstants.ChaosEngineering.MEMORY_PRESSURE, userId);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.STATISTICAL_SUMMARY_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => GetStatisticalSummaryInternalAsync(dataSetId, userId, context),
                    _options.StatisticalSummaryTimeout,
                    context.CorrelationId,
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
            var errorMessage = CreateDetailedErrorMessage(operationName, additionalMetadata);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    private async Task ExecuteChaosEngineeringAsync(IOperationContext context, string chaosType, string userId)
    {
        await _infrastructure.ChaosEngineering.ExecuteChaosAsync(
            chaosType,
            context.CorrelationId,
            context.OperationName,
            async () =>
            {
                _infrastructure.StructuredLogging.LogStep(context, $"Chaos engineering: Simulating {chaosType}", new Dictionary<string, object>
                {
                    [AppConstants.ChaosEngineering.CHAOS_TYPE] = chaosType
                });

                await SimulateChaosEffectAsync(chaosType);
            },
            new Dictionary<string, object> { [AppConstants.DataStructures.USER_ID] = userId });
    }

    private async Task SimulateChaosEffectAsync(string chaosType)
    {
        switch (chaosType)
        {
            case AppConstants.ChaosEngineering.PROCESSING_DELAY:
                await Task.Delay(_random.Next(AppConstants.ChaosEngineering.MIN_PROCESSING_DELAY_MS, AppConstants.ChaosEngineering.MAX_PROCESSING_DELAY_MS));
                break;

            case AppConstants.ChaosEngineering.NETWORK_LATENCY:
                await Task.Delay(_random.Next(AppConstants.ChaosEngineering.MIN_NETWORK_LATENCY_MS, AppConstants.ChaosEngineering.MAX_NETWORK_LATENCY_MS));
                break;

            case AppConstants.ChaosEngineering.CACHE_FAILURE:
                throw new InvalidOperationException("Simulated cache failure (chaos engineering)");

            case AppConstants.ChaosEngineering.MEMORY_PRESSURE:
                await SimulateMemoryPressureAsync();
                break;

            default:
                await Task.Delay(_random.Next(AppConstants.ChaosEngineering.DEFAULT_CHAOS_DELAY_MS, AppConstants.ChaosEngineering.MAX_CHAOS_DELAY_MS));
                break;
        }
    }

    private static async Task SimulateMemoryPressureAsync()
    {
        var tempObjects = new List<byte[]>();
        for (int i = 0; i < AppConstants.ChaosEngineering.MEMORY_PRESSURE_OBJECT_COUNT; i++)
        {
            tempObjects.Add(new byte[AppConstants.ChaosEngineering.MEMORY_PRESSURE_OBJECT_SIZE_BYTES]);
        }
        await Task.Delay(AppConstants.ChaosEngineering.MEMORY_PRESSURE_DELAY_MS);
        tempObjects.Clear();
    }

    private static Dictionary<string, object> CreateChartMetadata(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration)
    {
        return new Dictionary<string, object>
        {
            [AppConstants.DataStructures.DATASETID] = dataSetId,
            [AppConstants.DataStructures.CHART_TYPE] = chartType.ToString(),
            [AppConstants.DataProcessing.CONFIGURATION_KEY] = configuration?.ToString() ?? AppConstants.DataProcessing.DATA_TYPE_NULL
        };
    }

    private static Dictionary<string, object> CreateComparisonChartMetadata(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration)
    {
        return new Dictionary<string, object>
        {
            ["DataSetId1"] = dataSetId1,
            ["DataSetId2"] = dataSetId2,
            [AppConstants.DataStructures.CHART_TYPE] = chartType.ToString(),
            [AppConstants.DataProcessing.CONFIGURATION_KEY] = configuration?.ToString() ?? AppConstants.DataProcessing.DATA_TYPE_NULL
        };
    }

    private static Dictionary<string, object> CreateDataSummaryMetadata(int dataSetId)
    {
        return new Dictionary<string, object>
        {
            [AppConstants.DataStructures.DATASETID] = dataSetId
        };
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
        var dataSetId = GetMetadataValue(metadata, AppConstants.DataStructures.DATASETID);
        var chartType = GetMetadataValue(metadata, AppConstants.DataStructures.CHART_TYPE);
        return $"Failed to complete GenerateChartAsync for dataset ID {dataSetId} with chart type {chartType}";
    }

    private static string CreateComparisonChartErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId1 = GetMetadataValue(metadata, "DataSetId1");
        var dataSetId2 = GetMetadataValue(metadata, "DataSetId2");
        var chartType = GetMetadataValue(metadata, AppConstants.DataStructures.CHART_TYPE);
        return $"Failed to complete GenerateComparisonChartAsync for dataset IDs {dataSetId1} and {dataSetId2} with chart type {chartType}";
    }

    private static string CreateDataSummaryErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId = GetMetadataValue(metadata, AppConstants.DataStructures.DATASETID);
        return $"Failed to complete GetDataSummaryAsync for dataset ID {dataSetId}";
    }

    private static string CreateStatisticalSummaryErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId = GetMetadataValue(metadata, AppConstants.DataStructures.DATASETID);
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

    private static void ValidateStatisticalSummaryInputs(int dataSetId, string? userId) => ValidateDataSummaryInputs(dataSetId, userId);

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

    private async Task<ChartDataDto> GenerateChartInternalAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId, IOperationContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var cacheKey = GenerateCacheKey($"chart_{dataSetId}_{chartType}", configuration);

        if (_cache.TryGetValue(cacheKey, out ChartDataDto? cachedChart))
        {
            _infrastructure.StructuredLogging.LogStep(context, "Retrieved chart from cache");
            return cachedChart!;
        }

        _infrastructure.StructuredLogging.LogStep(context, "Cache miss, generating new chart");

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, context);
        var data = ExtractDataSetData(dataSet, context);

        var chartData = GenerateChartData(dataSet, data, chartType, configuration, context);
        chartData.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, chartData, _options.CacheExpiration);

        _infrastructure.StructuredLogging.LogStep(context, "Generated chart successfully", new Dictionary<string, object>
        {
            ["ProcessingTimeMs"] = stopwatch.ElapsedMilliseconds
        });

        return chartData;
    }

    private async Task<ComparisonChartDto> GenerateComparisonChartInternalAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId, IOperationContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var cacheKey = GenerateCacheKey($"comparison_{dataSetId1}_{dataSetId2}_{chartType}", configuration);

        if (_cache.TryGetValue(cacheKey, out ComparisonChartDto? cachedChart))
        {
            _infrastructure.StructuredLogging.LogStep(context, "Retrieved comparison chart from cache");
            return cachedChart!;
        }

        _infrastructure.StructuredLogging.LogStep(context, "Cache miss, generating new comparison chart");

        var dataSet1 = await GetAndValidateDataSetAsync(dataSetId1, userId, context);
        var dataSet2 = await GetAndValidateDataSetAsync(dataSetId2, userId, context);

        var data1 = ExtractDataSetData(dataSet1, context);
        var data2 = ExtractDataSetData(dataSet2, context);

        var comparisonChart = GenerateComparisonChartData(dataSet1, dataSet2, data1, data2, chartType, configuration, context);
        comparisonChart.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, comparisonChart, _options.CacheExpiration);

        _infrastructure.StructuredLogging.LogStep(context, "Generated comparison chart successfully", new Dictionary<string, object>
        {
            ["ProcessingTimeMs"] = stopwatch.ElapsedMilliseconds
        });

        return comparisonChart;
    }

    private async Task<DataSummaryDto> GetDataSummaryInternalAsync(int dataSetId, string userId, IOperationContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var cacheKey = $"summary_{dataSetId}";

        if (_cache.TryGetValue(cacheKey, out DataSummaryDto? cachedSummary))
        {
            _infrastructure.StructuredLogging.LogStep(context, "Retrieved data summary from cache");
            return cachedSummary!;
        }

        _infrastructure.StructuredLogging.LogStep(context, "Cache miss, generating new data summary");

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, context);
        var data = ExtractDataSetData(dataSet, context);

        var summary = GenerateDataSummary(dataSet, data);
        summary.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, summary, _options.CacheExpiration);

        _infrastructure.StructuredLogging.LogStep(context, "Generated data summary successfully", new Dictionary<string, object>
        {
            ["ProcessingTimeMs"] = stopwatch.ElapsedMilliseconds
        });

        return summary;
    }

    private async Task<StatisticalSummaryDto> GetStatisticalSummaryInternalAsync(int dataSetId, string userId, IOperationContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var cacheKey = $"stats_{dataSetId}";

        if (_cache.TryGetValue(cacheKey, out StatisticalSummaryDto? cachedStats))
        {
            _infrastructure.StructuredLogging.LogStep(context, "Retrieved statistical summary from cache");
            return cachedStats!;
        }

        _infrastructure.StructuredLogging.LogStep(context, "Cache miss, generating new statistical summary");

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, context);
        var data = ExtractDataSetData(dataSet, context);

        var stats = GenerateStatisticalSummary(dataSet, data);
        stats.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, stats, _options.CacheExpiration);

        _infrastructure.StructuredLogging.LogStep(context, "Generated statistical summary successfully", new Dictionary<string, object>
        {
            ["ProcessingTimeMs"] = stopwatch.ElapsedMilliseconds
        });

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

    private async Task<DataSet> GetAndValidateDataSetAsync(int dataSetId, string userId, IOperationContext context)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);

        if (dataSet == null)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Dataset not found", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSetId,
                [AppConstants.DataStructures.USER_ID] = userId
            });
            throw new ArgumentException($"{AppConstants.VisualizationMessages.DATASET_NOT_FOUND} with ID {dataSetId}", nameof(dataSetId));
        }

        if (dataSet.UserId != userId)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Unauthorized access attempt", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSetId,
                [AppConstants.DataStructures.USER_ID] = userId,
                [AppConstants.DataStructures.ACTUAL_USER_ID] = dataSet.UserId
            });
            throw new UnauthorizedAccessException($"{AppConstants.VisualizationMessages.DATASET_ACCESS_DENIED} - User {userId} is not authorized to access dataset {dataSetId}");
        }

        if (dataSet.IsDeleted)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Attempted to access deleted dataset", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSetId,
                [AppConstants.DataStructures.USER_ID] = userId
            });
            throw new ArgumentException($"Dataset {dataSetId} has been deleted", nameof(dataSetId));
        }

        return dataSet;
    }

    private List<Dictionary<string, object>> ExtractDataSetData(DataSet dataSet, IOperationContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataSet.ProcessedData))
            {
                _infrastructure.StructuredLogging.LogStep(context, "Dataset has no processed data", new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.DATASETID] = dataSet.Id
                });
                return [];
            }

            var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);

            if (data == null)
            {
                _infrastructure.StructuredLogging.LogStep(context, "Failed to deserialize dataset JSON data", new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.DATASETID] = dataSet.Id
                });
                return [];
            }

            _infrastructure.StructuredLogging.LogStep(context, "Extracted rows from dataset", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSet.Id,
                ["RowCount"] = data.Count
            });

            return data;
        }
        catch (JsonException ex)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Failed to parse dataset JSON data", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSet.Id,
                ["ErrorMessage"] = ex.Message
            });
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
        return $"{baseKey}_{Convert.ToBase64String(hash)[..AppConstants.DataProcessing.CACHE_KEY_HASH_LENGTH]}";
    }

    #endregion

    #region Chart Generation Methods

    private ChartDataDto GenerateChartData(DataSet dataSet, List<Dictionary<string, object>> data, ChartType chartType, ChartConfigurationDto? configuration, IOperationContext context)
    {
        if (data.Count == 0)
        {
            _infrastructure.StructuredLogging.LogStep(context, "No data available for chart generation", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSet.Id,
                [AppConstants.DataStructures.CHART_TYPE] = chartType.ToString()
            });
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
                GenerateBarLineAreaChart(limitedData, labels, series, context);
                break;

            case ChartType.Pie:
            case ChartType.Donut:
                GeneratePieDonutChart(limitedData, labels, series, context);
                break;

            case ChartType.Scatter:
            case ChartType.Bubble:
                GenerateScatterBubbleChart(limitedData, labels, series, context);
                break;

            default:
                _infrastructure.StructuredLogging.LogStep(context, "Unsupported chart type", new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.DATASETID] = dataSet.Id,
                    [AppConstants.DataStructures.CHART_TYPE] = chartType.ToString()
                });
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

    private void GenerateBarLineAreaChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, IOperationContext context)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _infrastructure.StructuredLogging.LogStep(context, "No numeric columns found for chart. Using fallback data");

            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = AppConstants.DataProcessing.FALLBACK_SERIES_NAME,
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

    private void GeneratePieDonutChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, IOperationContext context)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _infrastructure.StructuredLogging.LogStep(context, "No numeric columns found for pie/donut chart. Using fallback data");

            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = AppConstants.DataProcessing.FALLBACK_SERIES_NAME,
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

    private void GenerateScatterBubbleChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, IOperationContext context)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count < 2)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Insufficient numeric columns for scatter/bubble chart. Using fallback data");

            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = AppConstants.DataProcessing.FALLBACK_SERIES_NAME,
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

    private ComparisonChartDto GenerateComparisonChartData(DataSet dataSet1, DataSet dataSet2, List<Dictionary<string, object>> data1, List<Dictionary<string, object>> data2, ChartType chartType, ChartConfigurationDto? configuration, IOperationContext context)
    {
        var chart1 = GenerateChartData(dataSet1, data1, chartType, configuration, context);
        var chart2 = GenerateChartData(dataSet2, data2, chartType, configuration, context);

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

    private static DataSummaryDto GenerateDataSummary(DataSet dataSet, List<Dictionary<string, object>> data)
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
                ColumnSummaries = [],
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
                SampleValues = columnData.Take(AppConstants.DataProcessing.SAMPLE_VALUES_COUNT).Select(x => x?.ToString() ?? AppConstants.DataProcessing.DATA_TYPE_NULL).Cast<object>().ToList()
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

    private static StatisticalSummaryDto GenerateStatisticalSummary(DataSet dataSet, List<Dictionary<string, object>> data)
    {
        if (data.Count == 0)
        {
            return new StatisticalSummaryDto
            {
                DataSetId = dataSet.Id,
                ColumnStatistics = [],
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
                        Q1 = CalculateQuartile(numericData, AppConstants.DataProcessing.Q1_PERCENTILE),
                        Q2 = CalculateQuartile(numericData, AppConstants.DataProcessing.Q2_PERCENTILE),
                        Q3 = CalculateQuartile(numericData, AppConstants.DataProcessing.Q3_PERCENTILE),
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

        if (nonNullData.All(IsNumeric)) return AppConstants.DataProcessing.DATA_TYPE_NUMERIC;
        if (nonNullData.All(v => v is DateTime)) return AppConstants.DataProcessing.DATA_TYPE_DATETIME;
        if (nonNullData.All(v => v is bool)) return AppConstants.DataProcessing.DATA_TYPE_BOOLEAN;
        return AppConstants.DataProcessing.DATA_TYPE_STRING;
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
        return nonNullData.Count > 0 && nonNullData.All(IsNumeric);
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
        var index = percentile * (sorted.Count - 1);
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
        return (kurtosis - AppConstants.DataProcessing.KURTOSIS_ADJUSTMENT) * Math.Sqrt(data.Count * (data.Count - 1)) / ((data.Count - 2) * (data.Count - 3));
    }

    #endregion
}

public class DataVisualizationOptions
{
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(AppConstants.DataProcessing.DEFAULT_CACHE_EXPIRATION_MINUTES);
    public int MaxDataPoints { get; set; } = AppConstants.DataProcessing.DEFAULT_MAX_DATA_POINTS;
    public TimeSpan ChartGenerationTimeout { get; set; } = TimeSpan.FromMinutes(AppConstants.DataProcessing.DEFAULT_CHART_GENERATION_TIMEOUT_MINUTES);
    public TimeSpan ComparisonChartTimeout { get; set; } = TimeSpan.FromMinutes(AppConstants.DataProcessing.DEFAULT_COMPARISON_CHART_TIMEOUT_MINUTES);
    public TimeSpan SummaryGenerationTimeout { get; set; } = TimeSpan.FromMinutes(AppConstants.DataProcessing.DEFAULT_SUMMARY_GENERATION_TIMEOUT_MINUTES);
    public TimeSpan StatisticalSummaryTimeout { get; set; } = TimeSpan.FromMinutes(AppConstants.DataProcessing.DEFAULT_STATISTICAL_SUMMARY_TIMEOUT_MINUTES);
    public double ChaosProcessingDelayProbability { get; set; } = AppConstants.DataProcessing.DEFAULT_CHAOS_PROCESSING_DELAY_PROBABILITY;
}