using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Normaize.Core.Models;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Normaize.Core.Services;

public class DataVisualizationService : IDataVisualizationService
{
    private readonly ILogger<DataVisualizationService> _logger;
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IMemoryCache _cache;
    private readonly DataVisualizationOptions _options;
    private readonly Random _chaosRandom;

    public DataVisualizationService(
        ILogger<DataVisualizationService> logger,
        IDataSetRepository dataSetRepository,
        IMemoryCache cache,
        IOptions<DataVisualizationOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _chaosRandom = new Random();
        
        _logger.LogInformation("DataVisualizationService initialized with configuration: CacheExpiration={CacheExpiration}, MaxDataPoints={MaxDataPoints}, ChaosProcessingDelayProbability={ChaosProcessingDelayProbability}",
            _options.CacheExpiration, _options.MaxDataPoints, _options.ChaosProcessingDelayProbability);
    }

    public async Task<ChartDataDto> GenerateChartAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var operationName = "GenerateChartAsync";
        
        _logger.LogInformation("Starting chart generation. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}, UserId: {UserId}",
            correlationId, dataSetId, chartType, userId);

        try
        {
            // Validate inputs first (before try-catch so exceptions are thrown)
            ValidateGenerateChartInputs(dataSetId, chartType, configuration, userId);

            return await ExecuteWithTimeoutAsync(
                async () => await GenerateChartInternalAsync(dataSetId, chartType, configuration, userId, correlationId),
                _options.ChartGenerationTimeout,
                correlationId,
                operationName);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Failed to generate chart. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}, UserId: {UserId}",
                correlationId, dataSetId, chartType, userId);
            throw;
        }
    }

    public async Task<ComparisonChartDto> GenerateComparisonChartAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var operationName = "GenerateComparisonChartAsync";
        
        _logger.LogInformation("Starting comparison chart generation. CorrelationId: {CorrelationId}, DataSetId1: {DataSetId1}, DataSetId2: {DataSetId2}, ChartType: {ChartType}, UserId: {UserId}",
            correlationId, dataSetId1, dataSetId2, chartType, userId);

        try
        {
            // Validate inputs first
            ValidateComparisonChartInputs(dataSetId1, dataSetId2, chartType, configuration, userId);

            return await ExecuteWithTimeoutAsync(
                async () => await GenerateComparisonChartInternalAsync(dataSetId1, dataSetId2, chartType, configuration, userId, correlationId),
                _options.ComparisonChartTimeout,
                correlationId,
                operationName);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Failed to generate comparison chart. CorrelationId: {CorrelationId}, DataSetId1: {DataSetId1}, DataSetId2: {DataSetId2}, UserId: {UserId}",
                correlationId, dataSetId1, dataSetId2, userId);
            throw;
        }
    }

    public async Task<DataSummaryDto> GetDataSummaryAsync(int dataSetId, string userId)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var operationName = "GetDataSummaryAsync";
        
        _logger.LogInformation("Starting data summary generation. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, UserId: {UserId}",
            correlationId, dataSetId, userId);

        try
        {
            // Validate inputs first
            ValidateDataSummaryInputs(dataSetId, userId);

            return await ExecuteWithTimeoutAsync(
                async () => await GetDataSummaryInternalAsync(dataSetId, userId, correlationId),
                _options.SummaryGenerationTimeout,
                correlationId,
                operationName);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Failed to generate data summary. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, UserId: {UserId}",
                correlationId, dataSetId, userId);
            throw;
        }
    }

    public async Task<StatisticalSummaryDto> GetStatisticalSummaryAsync(int dataSetId, string userId)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        var operationName = "GetStatisticalSummaryAsync";
        
        _logger.LogInformation("Starting statistical summary generation. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, UserId: {UserId}",
            correlationId, dataSetId, userId);

        try
        {
            // Validate inputs first
            ValidateStatisticalSummaryInputs(dataSetId, userId);

            return await ExecuteWithTimeoutAsync(
                async () => await GetStatisticalSummaryInternalAsync(dataSetId, userId, correlationId),
                _options.StatisticalSummaryTimeout,
                correlationId,
                operationName);
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not ArgumentNullException)
        {
            _logger.LogError(ex, "Failed to generate statistical summary. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, UserId: {UserId}",
                correlationId, dataSetId, userId);
            throw;
        }
    }

    public Task<IEnumerable<ChartType>> GetSupportedChartTypesAsync()
    {
        return Task.FromResult<IEnumerable<ChartType>>(Enum.GetValues<ChartType>());
    }

    public bool ValidateChartConfiguration(ChartType chartType, ChartConfigurationDto? configuration)
    {
        return ValidateChartConfigurationInternal(chartType, configuration);
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

    #region Private Methods

    private async Task<ChartDataDto> GenerateChartInternalAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Chaos engineering: Simulate processing delay
        if (_chaosRandom.NextDouble() < _options.ChaosProcessingDelayProbability)
        {
            _logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
            await Task.Delay(_chaosRandom.Next(1000, 5000)); // 1-5 second delay
        }

        var cacheKey = GenerateCacheKey($"chart_{dataSetId}_{chartType}", configuration);
        
        if (_cache.TryGetValue(cacheKey, out ChartDataDto? cachedChart))
        {
            _logger.LogDebug("Retrieved chart from cache. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}",
                correlationId, dataSetId, chartType);
            return cachedChart!;
        }

        _logger.LogDebug("Cache miss, generating new chart. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}",
            correlationId, dataSetId, chartType);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, correlationId);
        var data = ExtractDataSetData(dataSet, correlationId);
        
        var chartData = GenerateChartData(dataSet, data, chartType, configuration, correlationId);
        chartData.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, chartData, _options.CacheExpiration);

        _logger.LogInformation("Generated chart successfully. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}, ProcessingTime: {ProcessingTime}ms",
            correlationId, dataSetId, chartType, stopwatch.ElapsedMilliseconds);

        return chartData;
    }

    private async Task<ComparisonChartDto> GenerateComparisonChartInternalAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Chaos engineering: Simulate processing delay
        if (_chaosRandom.NextDouble() < _options.ChaosProcessingDelayProbability)
        {
            _logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
            await Task.Delay(_chaosRandom.Next(1000, 5000)); // 1-5 second delay
        }

        var cacheKey = GenerateCacheKey($"comparison_{dataSetId1}_{dataSetId2}_{chartType}", configuration);
        
        if (_cache.TryGetValue(cacheKey, out ComparisonChartDto? cachedChart))
        {
            _logger.LogDebug("Retrieved comparison chart from cache. CorrelationId: {CorrelationId}, DataSetId1: {DataSetId1}, DataSetId2: {DataSetId2}",
                correlationId, dataSetId1, dataSetId2);
            return cachedChart!;
        }

        _logger.LogDebug("Cache miss, generating new comparison chart. CorrelationId: {CorrelationId}, DataSetId1: {DataSetId1}, DataSetId2: {DataSetId2}",
            correlationId, dataSetId1, dataSetId2);

        var dataSet1 = await GetAndValidateDataSetAsync(dataSetId1, userId, correlationId);
        var dataSet2 = await GetAndValidateDataSetAsync(dataSetId2, userId, correlationId);
        
        var data1 = ExtractDataSetData(dataSet1, correlationId);
        var data2 = ExtractDataSetData(dataSet2, correlationId);

        var comparisonChart = GenerateComparisonChartData(dataSet1, dataSet2, data1, data2, chartType, configuration, correlationId);
        comparisonChart.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, comparisonChart, _options.CacheExpiration);

        _logger.LogInformation("Generated comparison chart successfully. CorrelationId: {CorrelationId}, DataSetId1: {DataSetId1}, DataSetId2: {DataSetId2}, ProcessingTime: {ProcessingTime}ms",
            correlationId, dataSetId1, dataSetId2, stopwatch.ElapsedMilliseconds);

        return comparisonChart;
    }

    private async Task<DataSummaryDto> GetDataSummaryInternalAsync(int dataSetId, string userId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Chaos engineering: Simulate processing delay
        if (_chaosRandom.NextDouble() < _options.ChaosProcessingDelayProbability)
        {
            _logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
            await Task.Delay(_chaosRandom.Next(500, 2000)); // 0.5-2 second delay
        }

        var cacheKey = $"summary_{dataSetId}";
        
        if (_cache.TryGetValue(cacheKey, out DataSummaryDto? cachedSummary))
        {
            _logger.LogDebug("Retrieved data summary from cache. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                correlationId, dataSetId);
            return cachedSummary!;
        }

        _logger.LogDebug("Cache miss, generating new data summary. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
            correlationId, dataSetId);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, correlationId);
        var data = ExtractDataSetData(dataSet, correlationId);
        
        var summary = GenerateDataSummary(dataSet, data, correlationId);
        summary.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, summary, _options.CacheExpiration);

        _logger.LogInformation("Generated data summary successfully. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ProcessingTime: {ProcessingTime}ms",
            correlationId, dataSetId, stopwatch.ElapsedMilliseconds);

        return summary;
    }

    private async Task<StatisticalSummaryDto> GetStatisticalSummaryInternalAsync(int dataSetId, string userId, string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Chaos engineering: Simulate processing delay
        if (_chaosRandom.NextDouble() < _options.ChaosProcessingDelayProbability)
        {
            _logger.LogWarning("Chaos engineering: Simulating processing delay. CorrelationId: {CorrelationId}", correlationId);
            await Task.Delay(_chaosRandom.Next(1000, 3000)); // 1-3 second delay
        }

        var cacheKey = $"stats_{dataSetId}";
        
        if (_cache.TryGetValue(cacheKey, out StatisticalSummaryDto? cachedStats))
        {
            _logger.LogDebug("Retrieved statistical summary from cache. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                correlationId, dataSetId);
            return cachedStats!;
        }

        _logger.LogDebug("Cache miss, generating new statistical summary. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
            correlationId, dataSetId);

        var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId, correlationId);
        var data = ExtractDataSetData(dataSet, correlationId);
        
        var stats = GenerateStatisticalSummary(dataSet, data, correlationId);
        stats.ProcessingTime = stopwatch.Elapsed;

        _cache.Set(cacheKey, stats, _options.CacheExpiration);

        _logger.LogInformation("Generated statistical summary successfully. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ProcessingTime: {ProcessingTime}ms",
            correlationId, dataSetId, stopwatch.ElapsedMilliseconds);

        return stats;
    }

    private async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, TimeSpan timeout, string correlationId, string operationName)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            return await operation().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            _logger.LogError("Operation {OperationName} timed out after {Timeout}. CorrelationId: {CorrelationId}", 
                operationName, timeout, correlationId);
            throw new TimeoutException($"Operation {operationName} timed out after {timeout}");
        }
    }

    private static void ValidateGenerateChartInputs(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string? userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException("Dataset ID must be greater than 0", nameof(dataSetId));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        
        ValidateChartConfigurationInternal(chartType, configuration);
    }

    private static void ValidateComparisonChartInputs(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string? userId)
    {
        if (dataSetId1 <= 0)
            throw new ArgumentException("First dataset ID must be greater than 0", nameof(dataSetId1));
        
        if (dataSetId2 <= 0)
            throw new ArgumentException("Second dataset ID must be greater than 0", nameof(dataSetId2));
        
        if (dataSetId1 == dataSetId2)
            throw new ArgumentException("Dataset IDs must be different for comparison", nameof(dataSetId2));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
        
        ValidateChartConfigurationInternal(chartType, configuration);
    }

    private static void ValidateDataSummaryInputs(int dataSetId, string? userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException("Dataset ID must be greater than 0", nameof(dataSetId));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
    }

    private static void ValidateStatisticalSummaryInputs(int dataSetId, string? userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException("Dataset ID must be greater than 0", nameof(dataSetId));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
    }

    private async Task<DataSet> GetAndValidateDataSetAsync(int dataSetId, string userId, string correlationId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
        
        if (dataSet == null)
        {
            _logger.LogWarning("Dataset not found. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, UserId: {UserId}",
                correlationId, dataSetId, userId);
            throw new ArgumentException($"Dataset with ID {dataSetId} not found", nameof(dataSetId));
        }

        if (dataSet.UserId != userId)
        {
            _logger.LogWarning("Unauthorized access attempt. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, RequestedUserId: {RequestedUserId}, ActualUserId: {ActualUserId}",
                correlationId, dataSetId, userId, dataSet.UserId);
            throw new UnauthorizedAccessException($"User {userId} is not authorized to access dataset {dataSetId}");
        }

        if (dataSet.IsDeleted)
        {
            _logger.LogWarning("Attempted to access deleted dataset. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, UserId: {UserId}",
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
                _logger.LogWarning("Dataset has no processed data. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                    correlationId, dataSet.Id);
                return new List<Dictionary<string, object>>();
            }

            var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);
            
            if (data == null)
            {
                _logger.LogWarning("Failed to deserialize dataset JSON data. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                    correlationId, dataSet.Id);
                return new List<Dictionary<string, object>>();
            }

            _logger.LogDebug("Extracted {RowCount} rows from dataset. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                data.Count, correlationId, dataSet.Id);

            return data;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse dataset JSON data. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
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

    private ChartDataDto GenerateChartData(DataSet dataSet, List<Dictionary<string, object>> data, ChartType chartType, ChartConfigurationDto? configuration, string correlationId)
    {
        if (data.Count == 0)
        {
            _logger.LogWarning("No data available for chart generation. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}",
                correlationId, dataSet.Id, chartType);
            return new ChartDataDto
            {
                DataSetId = dataSet.Id,
                ChartType = chartType,
                Labels = new List<string>(),
                Series = new List<ChartSeriesDto>(),
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
                _logger.LogWarning("Unsupported chart type. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}, ChartType: {ChartType}",
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
            _logger.LogWarning("No numeric columns found for chart. Using fallback data. CorrelationId: {CorrelationId}", correlationId);
            
            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = "Count",
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first numeric column for values
        var valueColumn = numericColumns[0];
        var labelColumn = columns.FirstOrDefault(col => col != valueColumn) ?? valueColumn;

        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? "Unknown"));
        
        series.Add(new ChartSeriesDto
        {
            Name = valueColumn,
            Data = data.Select(row => ExtractDouble(row.GetValueOrDefault(valueColumn))).Cast<object>().ToList()
        });
    }

    private void GeneratePieDonutChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, string correlationId)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _logger.LogWarning("No numeric columns found for pie chart. Using fallback data. CorrelationId: {CorrelationId}", correlationId);
            
            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = "Count",
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        var valueColumn = numericColumns[0];
        var labelColumn = columns.FirstOrDefault(col => col != valueColumn) ?? valueColumn;

        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? "Unknown"));
        
        series.Add(new ChartSeriesDto
        {
            Name = valueColumn,
            Data = data.Select(row => ExtractDouble(row.GetValueOrDefault(valueColumn))).Cast<object>().ToList()
        });
    }

    private void GenerateScatterBubbleChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, string correlationId)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count < 2)
        {
            _logger.LogWarning("Insufficient numeric columns for scatter chart. Using fallback data. CorrelationId: {CorrelationId}", correlationId);
            
            // Fallback: use row indices as X and Y coordinates
            labels.AddRange(data.Select((_, index) => $"Point {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = "Data Points",
                Data = data.Select((_, index) => new { X = index, Y = index + 1 }).Cast<object>().ToList()
            });
            return;
        }

        var xColumn = numericColumns[0];
        var yColumn = numericColumns[1];

        labels.AddRange(data.Select(row => row.GetValueOrDefault(xColumn)?.ToString() ?? "Unknown"));
        
        series.Add(new ChartSeriesDto
        {
            Name = $"{xColumn} vs {yColumn}",
            Data = data.Select(row => new { X = ExtractDouble(row.GetValueOrDefault(xColumn)), Y = ExtractDouble(row.GetValueOrDefault(yColumn)) }).Cast<object>().ToList()
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

    private DataSummaryDto GenerateDataSummary(DataSet dataSet, List<Dictionary<string, object>> data, string correlationId)
    {
        if (data.Count == 0)
        {
            _logger.LogWarning("No data available for summary generation. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                correlationId, dataSet.Id);
            return new DataSummaryDto
            {
                DataSetId = dataSet.Id,
                TotalRows = 0,
                TotalColumns = 0,
                ColumnSummaries = new Dictionary<string, ColumnSummaryDto>()
            };
        }

        var columns = data[0].Keys.ToList();
        var columnSummaries = new Dictionary<string, ColumnSummaryDto>();

        foreach (var column in columns)
        {
            var columnData = data.Select(row => row.GetValueOrDefault(column)).ToList();
            var dataType = DetermineDataType(columnData);
            var uniqueValues = columnData.Distinct().Count();
            var nullCount = columnData.Count(x => x == null);

            columnSummaries.Add(column, new ColumnSummaryDto
            {
                ColumnName = column,
                DataType = dataType,
                NonNullCount = columnData.Count - nullCount,
                NullCount = nullCount,
                UniqueCount = uniqueValues,
                SampleValues = columnData.Take(5).Select(x => x?.ToString() ?? "null").Cast<object>().ToList()
            });
        }

        return new DataSummaryDto
        {
            DataSetId = dataSet.Id,
            TotalRows = data.Count,
            TotalColumns = columns.Count,
            ColumnSummaries = columnSummaries
        };
    }

    private StatisticalSummaryDto GenerateStatisticalSummary(DataSet dataSet, List<Dictionary<string, object>> data, string correlationId)
    {
        if (data.Count == 0)
        {
            _logger.LogWarning("No data available for statistical summary. CorrelationId: {CorrelationId}, DataSetId: {DataSetId}",
                correlationId, dataSet.Id);
            return new StatisticalSummaryDto
            {
                DataSetId = dataSet.Id,
                ColumnStatistics = new Dictionary<string, ColumnStatisticsDto>()
            };
        }

        var columns = data[0].Keys.ToList();
        var columnStatistics = new Dictionary<string, ColumnStatisticsDto>();

        foreach (var column in columns)
        {
            var columnData = data.Select(row => row.GetValueOrDefault(column)).ToList();
            
            if (IsNumericColumn(columnData))
            {
                var numericData = columnData.Select(x => ExtractDouble(x)).Where(x => !double.IsNaN(x)).ToList();
                
                if (numericData.Count > 0)
                {
                    columnStatistics.Add(column, new ColumnStatisticsDto
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
                    });
                }
            }
        }

        return new StatisticalSummaryDto
        {
            DataSetId = dataSet.Id,
            ColumnStatistics = columnStatistics
        };
    }

    private static string DetermineDataType(List<object?> data)
    {
        var nonNullData = data.Where(x => x != null).ToList();
        
        if (nonNullData.Count == 0) return "Unknown";
        
        var allNumeric = nonNullData.All(x => IsNumeric(x));
        if (allNumeric) return "Numeric";
        
        var allDates = nonNullData.All(x => x is DateTime || (x is string s && DateTime.TryParse(s, out _)));
        if (allDates) return "DateTime";
        
        return "String";
    }

    private static bool IsNumeric(object? value)
    {
        return value switch
        {
            double or int or long or float or decimal => true,
            string s => double.TryParse(s, out _),
            _ => false
        };
    }

    private static bool IsNumericColumn(List<object?> data)
    {
        var nonNullData = data.Where(x => x != null).ToList();
        return nonNullData.Count > 0 && nonNullData.All(x => IsNumeric(x));
    }

    private static double CalculateMedian(List<double> data)
    {
        if (data.Count == 0) return 0;
        
        var sorted = data.OrderBy(x => x).ToList();
        var count = sorted.Count;
        
        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        else
            return sorted[count / 2];
    }

    private static double CalculateStandardDeviation(List<double> data)
    {
        if (data.Count <= 1) return 0;
        
        var mean = data.Average();
        var sumSquaredDifferences = data.Sum(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(sumSquaredDifferences / (data.Count - 1));
    }

    private static double CalculateQuartile(List<double> data, double percentile)
    {
        if (data.Count == 0) return 0;
        
        var sorted = data.OrderBy(x => x).ToList();
        var index = (percentile * (sorted.Count - 1));
        var lowerIndex = (int)Math.Floor(index);
        var upperIndex = (int)Math.Ceiling(index);
        
        if (lowerIndex == upperIndex) return sorted[lowerIndex];
        
        var weight = index - lowerIndex;
        return sorted[lowerIndex] * (1 - weight) + sorted[upperIndex] * weight;
    }

    private static double CalculateSkewness(List<double> data)
    {
        if (data.Count < 3) return 0;
        
        var mean = data.Average();
        var stdDev = CalculateStandardDeviation(data);
        
        if (stdDev == 0) return 0;
        
        var sumCubedDifferences = data.Sum(x => Math.Pow((x - mean) / stdDev, 3));
        return (sumCubedDifferences * data.Count) / ((data.Count - 1) * (data.Count - 2));
    }

    private static double CalculateKurtosis(List<double> data)
    {
        if (data.Count < 4) return 0;
        
        var mean = data.Average();
        var stdDev = CalculateStandardDeviation(data);
        
        if (stdDev == 0) return 0;
        
        var sumFourthDifferences = data.Sum(x => Math.Pow((x - mean) / stdDev, 4));
        return (sumFourthDifferences * data.Count * (data.Count + 1)) / ((data.Count - 1) * (data.Count - 2) * (data.Count - 3)) - (3 * Math.Pow(data.Count - 1, 2)) / ((data.Count - 2) * (data.Count - 3));
    }

    private static double CalculateCorrelation(List<double> data1, List<double> data2)
    {
        if (data1.Count != data2.Count || data1.Count < 2) return 0;
        
        var mean1 = data1.Average();
        var mean2 = data2.Average();
        
        var sumProduct = data1.Zip(data2, (x, y) => (x - mean1) * (y - mean2)).Sum();
        var sumSquared1 = data1.Sum(x => Math.Pow(x - mean1, 2));
        var sumSquared2 = data2.Sum(x => Math.Pow(x - mean2, 2));
        
        if (sumSquared1 == 0 || sumSquared2 == 0) return 0;
        
        return sumProduct / Math.Sqrt(sumSquared1 * sumSquared2);
    }

    private static string GenerateCacheKey(string baseKey, ChartConfigurationDto? configuration)
    {
        var configJson = configuration != null ? JsonSerializer.Serialize(configuration) : "null";
        return $"{baseKey}_{configJson}";
    }

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