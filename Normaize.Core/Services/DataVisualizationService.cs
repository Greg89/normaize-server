using Microsoft.Extensions.Options;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Normaize.Core.Models;
using Normaize.Core.Constants;
using System.Text.Json;
using System.Diagnostics;

namespace Normaize.Core.Services;

public class DataVisualizationService : IDataVisualizationService
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly DataVisualizationOptions _options;
    private readonly IDataProcessingInfrastructure _infrastructure;
    private readonly IVisualizationServices _visualizationServices;
    private readonly Random _random;

    public DataVisualizationService(
        IDataSetRepository dataSetRepository,
        IOptions<DataVisualizationOptions> options,
        IDataProcessingInfrastructure infrastructure,
        IVisualizationServices visualizationServices)
    {
        ArgumentNullException.ThrowIfNull(dataSetRepository);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(infrastructure);
        ArgumentNullException.ThrowIfNull(visualizationServices);

        _dataSetRepository = dataSetRepository;
        _options = options.Value;
        _infrastructure = infrastructure;
        _visualizationServices = visualizationServices;
        _random = new Random();
    }

    public async Task<ChartDataDto> GenerateChartAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GenerateChartAsync),
            additionalMetadata: CreateChartMetadata(dataSetId, chartType, configuration),
            validation: () => _visualizationServices.Validation.ValidateGenerateChartInputs(dataSetId, chartType, configuration, userId),
            operation: async (context) =>
            {
                await ExecuteChaosEngineeringAsync(context, AppConstants.ChaosEngineering.PROCESSING_DELAY, userId);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.CHART_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => GenerateChartInternalAsync(dataSetId, chartType, configuration, userId, context),
                    _options.ChartGenerationTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.CHART_GENERATION_COMPLETED);

                return result;
            });
    }

    public async Task<ComparisonChartDto> GenerateComparisonChartAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GenerateComparisonChartAsync),
            additionalMetadata: CreateComparisonChartMetadata(dataSetId1, dataSetId2, chartType, configuration),
            validation: () => _visualizationServices.Validation.ValidateComparisonChartInputs(dataSetId1, dataSetId2, chartType, configuration, userId),
            operation: async (context) =>
            {
                await ExecuteChaosEngineeringAsync(context, AppConstants.ChaosEngineering.NETWORK_LATENCY, userId);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.COMPARISON_CHART_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => GenerateComparisonChartInternalAsync(dataSetId1, dataSetId2, chartType, configuration, userId, context),
                    _options.ComparisonChartTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.COMPARISON_CHART_GENERATION_COMPLETED);

                return result;
            });
    }

    public async Task<DataSummaryDto> GetDataSummaryAsync(int dataSetId, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GetDataSummaryAsync),
            additionalMetadata: CreateDataSummaryMetadata(dataSetId),
            validation: () => _visualizationServices.Validation.ValidateDataSummaryInputs(dataSetId, userId),
            operation: async (context) =>
            {
                await ExecuteChaosEngineeringAsync(context, AppConstants.ChaosEngineering.CACHE_FAILURE, userId);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.DATA_SUMMARY_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => GetDataSummaryInternalAsync(dataSetId, userId, context),
                    _options.SummaryGenerationTimeout,
                    context);
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.DATA_SUMMARY_GENERATION_COMPLETED);

                return result;
            });
    }

    public async Task<StatisticalSummaryDto> GetStatisticalSummaryAsync(int dataSetId, string userId)
    {
        return await ExecuteVisualizationOperationAsync(
            operationName: nameof(GetStatisticalSummaryAsync),
            additionalMetadata: CreateDataSummaryMetadata(dataSetId),
            validation: () => _visualizationServices.Validation.ValidateStatisticalSummaryInputs(dataSetId, userId),
            operation: async (context) =>
            {
                await ExecuteChaosEngineeringAsync(context, AppConstants.ChaosEngineering.MEMORY_PRESSURE, userId);

                _infrastructure.StructuredLogging.LogStep(context, AppConstants.VisualizationMessages.STATISTICAL_SUMMARY_GENERATION_STARTED);
                var result = await ExecuteWithTimeoutAsync(
                    () => GetStatisticalSummaryInternalAsync(dataSetId, userId, context),
                    _options.StatisticalSummaryTimeout,
                    context);
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
        return _visualizationServices.Validation.ValidateChartConfiguration(chartType, configuration);
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
                _infrastructure.StructuredLogging.LogStep(context, string.Format(AppConstants.DataVisualization.CHAOS_ENGINEERING_SIMULATING, chaosType), new Dictionary<string, object>
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
                throw new InvalidOperationException(AppConstants.DataVisualization.SIMULATED_CACHE_FAILURE_MESSAGE);

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
            [AppConstants.DataVisualization.DATASET_ID_1] = dataSetId1,
            [AppConstants.DataVisualization.DATASET_ID_2] = dataSetId2,
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
        if (metadata == null) return string.Format(AppConstants.DataVisualization.FAILED_TO_COMPLETE_OPERATION, operationName);

        return operationName switch
        {
            nameof(GenerateChartAsync) => CreateChartErrorMessage(metadata),
            nameof(GenerateComparisonChartAsync) => CreateComparisonChartErrorMessage(metadata),
            nameof(GetDataSummaryAsync) => CreateDataSummaryErrorMessage(metadata),
            nameof(GetStatisticalSummaryAsync) => CreateStatisticalSummaryErrorMessage(metadata),
            _ => string.Format(AppConstants.DataVisualization.FAILED_TO_COMPLETE_OPERATION, operationName)
        };
    }

    private static string CreateChartErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId = GetMetadataValue(metadata, AppConstants.DataStructures.DATASETID);
        var chartType = GetMetadataValue(metadata, AppConstants.DataStructures.CHART_TYPE);
        return string.Format(AppConstants.DataVisualization.FAILED_TO_COMPLETE_GENERATE_CHART, dataSetId, chartType);
    }

    private static string CreateComparisonChartErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId1 = GetMetadataValue(metadata, AppConstants.DataVisualization.DATASET_ID_1);
        var dataSetId2 = GetMetadataValue(metadata, AppConstants.DataVisualization.DATASET_ID_2);
        var chartType = GetMetadataValue(metadata, AppConstants.DataStructures.CHART_TYPE);
        return string.Format(AppConstants.DataVisualization.FAILED_TO_COMPLETE_GENERATE_COMPARISON_CHART, dataSetId1, dataSetId2, chartType);
    }

    private static string CreateDataSummaryErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId = GetMetadataValue(metadata, AppConstants.DataStructures.DATASETID);
        return string.Format(AppConstants.DataVisualization.FAILED_TO_COMPLETE_GET_DATA_SUMMARY, dataSetId);
    }

    private static string CreateStatisticalSummaryErrorMessage(Dictionary<string, object> metadata)
    {
        var dataSetId = GetMetadataValue(metadata, AppConstants.DataStructures.DATASETID);
        return string.Format(AppConstants.DataVisualization.FAILED_TO_COMPLETE_GET_STATISTICAL_SUMMARY, dataSetId);
    }

    private static string GetMetadataValue(Dictionary<string, object> metadata, string key)
    {
        return metadata.TryGetValue(key, out var value) ? value?.ToString() ?? AppConstants.Messages.UNKNOWN : AppConstants.Messages.UNKNOWN;
    }

    private static string GetCorrelationId() => Activity.Current?.Id ?? Guid.NewGuid().ToString();

    #endregion



    #region Internal Processing Methods

    private async Task<ChartDataDto> GenerateChartInternalAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId, IOperationContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var cacheKey = _visualizationServices.CacheManagement.GenerateChartCacheKey(dataSetId, chartType, configuration);

        if (_visualizationServices.CacheManagement.TryGetValue(cacheKey, out ChartDataDto? cachedChart))
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.RETRIEVED_CHART_FROM_CACHE);
            return cachedChart!;
        }

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.CACHE_MISS_GENERATING_NEW_CHART);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, context);
        var data = ExtractDataSetData(dataSet, context);

        var chartData = _visualizationServices.ChartGeneration.GenerateChartData(dataSet, data, chartType, configuration, context);
        chartData.ProcessingTime = stopwatch.Elapsed;

        _visualizationServices.CacheManagement.Set(cacheKey, chartData, _options.CacheExpiration);

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.GENERATED_CHART_SUCCESSFULLY, new Dictionary<string, object>
        {
            [AppConstants.DataVisualization.PROCESSING_TIME_MS] = stopwatch.ElapsedMilliseconds
        });

        return chartData;
    }

    private async Task<ComparisonChartDto> GenerateComparisonChartInternalAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId, IOperationContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var cacheKey = _visualizationServices.CacheManagement.GenerateComparisonChartCacheKey(dataSetId1, dataSetId2, chartType, configuration);

        if (_visualizationServices.CacheManagement.TryGetValue(cacheKey, out ComparisonChartDto? cachedChart))
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.RETRIEVED_COMPARISON_CHART_FROM_CACHE);
            return cachedChart!;
        }

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.CACHE_MISS_GENERATING_NEW_COMPARISON_CHART);

        var dataSet1 = await GetAndValidateDataSetAsync(dataSetId1, userId, context);
        var dataSet2 = await GetAndValidateDataSetAsync(dataSetId2, userId, context);

        var data1 = ExtractDataSetData(dataSet1, context);
        var data2 = ExtractDataSetData(dataSet2, context);

        var comparisonChart = _visualizationServices.ChartGeneration.GenerateComparisonChartData(dataSet1, dataSet2, data1, data2, chartType, configuration, context);
        comparisonChart.ProcessingTime = stopwatch.Elapsed;

        _visualizationServices.CacheManagement.Set(cacheKey, comparisonChart, _options.CacheExpiration);

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.GENERATED_COMPARISON_CHART_SUCCESSFULLY, new Dictionary<string, object>
        {
            [AppConstants.DataVisualization.PROCESSING_TIME_MS] = stopwatch.ElapsedMilliseconds
        });

        return comparisonChart;
    }

    private async Task<DataSummaryDto> GetDataSummaryInternalAsync(int dataSetId, string userId, IOperationContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var cacheKey = _visualizationServices.CacheManagement.GenerateDataSummaryCacheKey(dataSetId);

        if (_visualizationServices.CacheManagement.TryGetValue(cacheKey, out DataSummaryDto? cachedSummary))
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.RETRIEVED_DATA_SUMMARY_FROM_CACHE);
            return cachedSummary!;
        }

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.CACHE_MISS_GENERATING_NEW_DATA_SUMMARY);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, context);
        var data = ExtractDataSetData(dataSet, context);

        var summary = _visualizationServices.StatisticalCalculation.GenerateDataSummary(dataSet, data);
        summary.ProcessingTime = stopwatch.Elapsed;

        _visualizationServices.CacheManagement.Set(cacheKey, summary, _options.CacheExpiration);

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.GENERATED_DATA_SUMMARY_SUCCESSFULLY, new Dictionary<string, object>
        {
            [AppConstants.DataVisualization.PROCESSING_TIME_MS] = stopwatch.ElapsedMilliseconds
        });

        return summary;
    }

    private async Task<StatisticalSummaryDto> GetStatisticalSummaryInternalAsync(int dataSetId, string userId, IOperationContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        var cacheKey = _visualizationServices.CacheManagement.GenerateStatisticalSummaryCacheKey(dataSetId);

        if (_visualizationServices.CacheManagement.TryGetValue(cacheKey, out StatisticalSummaryDto? cachedStats))
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.RETRIEVED_STATISTICAL_SUMMARY_FROM_CACHE);
            return cachedStats!;
        }

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.CACHE_MISS_GENERATING_NEW_STATISTICAL_SUMMARY);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, context);
        var data = ExtractDataSetData(dataSet, context);

        var stats = _visualizationServices.StatisticalCalculation.GenerateStatisticalSummary(dataSet, data);
        stats.ProcessingTime = stopwatch.Elapsed;

        _visualizationServices.CacheManagement.Set(cacheKey, stats, _options.CacheExpiration);

        _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.GENERATED_STATISTICAL_SUMMARY_SUCCESSFULLY, new Dictionary<string, object>
        {
            [AppConstants.DataVisualization.PROCESSING_TIME_MS] = stopwatch.ElapsedMilliseconds
        });

        return stats;
    }

    #endregion

    #region Utility Methods

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, IOperationContext context)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.LogMessages.OPERATION_TIMED_OUT, new Dictionary<string, object>
            {
                [AppConstants.DataVisualization.TIMEOUT] = timeout.ToString(),
                [AppConstants.DataVisualization.OPERATION_NAME] = context.OperationName,
                [AppConstants.DataVisualization.ERROR_MESSAGE] = ex.Message
            });
            throw new TimeoutException(string.Format(AppConstants.DataVisualization.OPERATION_TIMED_OUT_AFTER, context.OperationName, timeout));
        }
    }

    private async Task<DataSet> GetAndValidateDataSetAsync(int dataSetId, string userId, IOperationContext context)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);

        if (dataSet == null)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.DATASET_NOT_FOUND_LOG, new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSetId,
                [AppConstants.DataStructures.USER_ID] = userId
            });
            throw new ArgumentException(string.Format(AppConstants.DataVisualization.DATASET_NOT_FOUND_WITH_ID, dataSetId), nameof(dataSetId));
        }

        if (dataSet.UserId != userId)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.UNAUTHORIZED_ACCESS_ATTEMPT, new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSetId,
                [AppConstants.DataStructures.USER_ID] = userId,
                [AppConstants.DataStructures.ACTUAL_USER_ID] = dataSet.UserId
            });
            throw new UnauthorizedAccessException(string.Format(AppConstants.DataVisualization.DATASET_ACCESS_DENIED_USER_NOT_AUTHORIZED, userId, dataSetId));
        }

        if (dataSet.IsDeleted)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.ATTEMPTED_TO_ACCESS_DELETED_DATASET, new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSetId,
                [AppConstants.DataStructures.USER_ID] = userId
            });
            throw new ArgumentException(string.Format(AppConstants.DataVisualization.DATASET_HAS_BEEN_DELETED, dataSetId), nameof(dataSetId));
        }

        return dataSet;
    }

    private List<Dictionary<string, object?>> ExtractDataSetData(DataSet dataSet, IOperationContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataSet.ProcessedData))
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.DATASET_HAS_NO_PROCESSED_DATA, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.DATASETID] = dataSet.Id
                });
                return [];
            }

            var data = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(dataSet.ProcessedData);

            if (data == null)
            {
                _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.FAILED_TO_DESERIALIZE_DATASET_JSON_DATA, new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.DATASETID] = dataSet.Id
                });
                return [];
            }

            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.EXTRACTED_ROWS_FROM_DATASET, new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSet.Id,
                [AppConstants.DataVisualization.ROW_COUNT] = data.Count
            });

            return data;
        }
        catch (JsonException ex)
        {
            _infrastructure.StructuredLogging.LogStep(context, AppConstants.DataVisualization.FAILED_TO_PARSE_DATASET_JSON_DATA, new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSet.Id,
                [AppConstants.DataVisualization.ERROR_MESSAGE] = ex.Message
            });
            throw new InvalidOperationException(string.Format(AppConstants.DataVisualization.FAILED_TO_PARSE_DATASET_DATA, dataSet.Id, ex.Message), ex);
        }
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