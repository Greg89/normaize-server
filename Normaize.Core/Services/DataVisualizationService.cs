using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Normaize.Core.Models;
using System.Text.Json;
using System.Diagnostics;

namespace Normaize.Core.Services;

public class DataVisualizationService : IDataVisualizationService
{
    private readonly ILogger<DataVisualizationService> _logger;
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    public DataVisualizationService(
        ILogger<DataVisualizationService> logger,
        IDataSetRepository dataSetRepository,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<ChartDataDto> GenerateChartAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            ValidateInputs(dataSetId, userId);
            ValidateChartConfiguration(chartType, configuration);

            var cacheKey = $"chart_{dataSetId}_{chartType}_{JsonSerializer.Serialize(configuration)}";
            
            if (_cache.TryGetValue(cacheKey, out ChartDataDto? cachedChart))
            {
                _logger.LogDebug("Retrieved chart from cache for dataset {DataSetId}, type {ChartType}", dataSetId, chartType);
                return cachedChart!;
            }

            _logger.LogInformation("Generating chart for dataset {DataSetId}, type {ChartType}, user {UserId}", dataSetId, chartType, userId);

            var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId);
            var data = ExtractDataSetData(dataSet);
            
            var chartData = GenerateChartData(dataSet, data, chartType, configuration);
            chartData.ProcessingTime = stopwatch.Elapsed;

            _cache.Set(cacheKey, chartData, _cacheExpiration);

            _logger.LogInformation("Generated chart for dataset {DataSetId}, type {ChartType} in {ProcessingTime}ms", 
                dataSetId, chartType, stopwatch.ElapsedMilliseconds);

            return chartData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chart for dataset {DataSetId}, type {ChartType}, user {UserId}", 
                dataSetId, chartType, userId);
            throw;
        }
    }

    public async Task<ComparisonChartDto> GenerateComparisonChartAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            ValidateComparisonInputs(dataSetId1, dataSetId2, userId);
            ValidateChartConfiguration(chartType, configuration);

            var cacheKey = $"comparison_{dataSetId1}_{dataSetId2}_{chartType}_{JsonSerializer.Serialize(configuration)}";
            
            if (_cache.TryGetValue(cacheKey, out ComparisonChartDto? cachedChart))
            {
                _logger.LogDebug("Retrieved comparison chart from cache for datasets {DataSetId1}, {DataSetId2}", dataSetId1, dataSetId2);
                return cachedChart!;
            }

            _logger.LogInformation("Generating comparison chart for datasets {DataSetId1}, {DataSetId2}, type {ChartType}, user {UserId}", 
                dataSetId1, dataSetId2, chartType, userId);

            var dataSet1 = await GetAndValidateDataSetAsync(dataSetId1, userId);
            var dataSet2 = await GetAndValidateDataSetAsync(dataSetId2, userId);
            
            var data1 = ExtractDataSetData(dataSet1);
            var data2 = ExtractDataSetData(dataSet2);

            var comparisonChart = GenerateComparisonChartData(dataSet1, dataSet2, data1, data2, chartType, configuration);
            comparisonChart.ProcessingTime = stopwatch.Elapsed;

            _cache.Set(cacheKey, comparisonChart, _cacheExpiration);

            _logger.LogInformation("Generated comparison chart for datasets {DataSetId1}, {DataSetId2} in {ProcessingTime}ms", 
                dataSetId1, dataSetId2, stopwatch.ElapsedMilliseconds);

            return comparisonChart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comparison chart for datasets {DataSetId1}, {DataSetId2}, user {UserId}", 
                dataSetId1, dataSetId2, userId);
            throw;
        }
    }

    public async Task<DataSummaryDto> GetDataSummaryAsync(int dataSetId, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            ValidateInputs(dataSetId, userId);

            var cacheKey = $"summary_{dataSetId}";
            
            if (_cache.TryGetValue(cacheKey, out DataSummaryDto? cachedSummary))
            {
                _logger.LogDebug("Retrieved data summary from cache for dataset {DataSetId}", dataSetId);
                return cachedSummary!;
            }

            _logger.LogInformation("Generating data summary for dataset {DataSetId}, user {UserId}", dataSetId, userId);

            var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId);
            var data = ExtractDataSetData(dataSet);
            
            var summary = GenerateDataSummary(dataSet, data);
            summary.ProcessingTime = stopwatch.Elapsed;

            _cache.Set(cacheKey, summary, _cacheExpiration);

            _logger.LogInformation("Generated data summary for dataset {DataSetId} in {ProcessingTime}ms", 
                dataSetId, stopwatch.ElapsedMilliseconds);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data summary for dataset {DataSetId}, user {UserId}", dataSetId, userId);
            throw;
        }
    }

    public async Task<StatisticalSummaryDto> GetStatisticalSummaryAsync(int dataSetId, string userId)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            ValidateInputs(dataSetId, userId);

            var cacheKey = $"stats_{dataSetId}";
            
            if (_cache.TryGetValue(cacheKey, out StatisticalSummaryDto? cachedStats))
            {
                _logger.LogDebug("Retrieved statistical summary from cache for dataset {DataSetId}", dataSetId);
                return cachedStats!;
            }

            _logger.LogInformation("Generating statistical summary for dataset {DataSetId}, user {UserId}", dataSetId, userId);

            var dataSet = await GetAndValidateDataSetAsync(dataSetId, userId);
            var data = ExtractDataSetData(dataSet);
            
            var stats = GenerateStatisticalSummary(dataSet, data);
            stats.ProcessingTime = stopwatch.Elapsed;

            _cache.Set(cacheKey, stats, _cacheExpiration);

            _logger.LogInformation("Generated statistical summary for dataset {DataSetId} in {ProcessingTime}ms", 
                dataSetId, stopwatch.ElapsedMilliseconds);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating statistical summary for dataset {DataSetId}, user {UserId}", dataSetId, userId);
            throw;
        }
    }

    public Task<IEnumerable<ChartType>> GetSupportedChartTypesAsync()
    {
        return Task.FromResult<IEnumerable<ChartType>>(Enum.GetValues<ChartType>());
    }

    public bool ValidateChartConfiguration(ChartType chartType, ChartConfigurationDto? configuration)
    {
        try
        {
            if (configuration == null) return true;

            // Validate max data points
            if (configuration.MaxDataPoints.HasValue && configuration.MaxDataPoints.Value <= 0)
            {
                throw new ArgumentException("MaxDataPoints must be greater than 0");
            }

            // Validate chart-specific configurations
            switch (chartType)
            {
                case ChartType.Pie:
                case ChartType.Donut:
                    if (!string.IsNullOrEmpty(configuration.XAxisLabel) || !string.IsNullOrEmpty(configuration.YAxisLabel))
                    {
                        _logger.LogWarning("X/Y axis labels are not applicable for {ChartType} charts", chartType);
                    }
                    break;
                    
                case ChartType.Scatter:
                case ChartType.Bubble:
                    if (string.IsNullOrEmpty(configuration.XAxisLabel) || string.IsNullOrEmpty(configuration.YAxisLabel))
                    {
                        _logger.LogWarning("X and Y axis labels are recommended for {ChartType} charts", chartType);
                    }
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating chart configuration for type {ChartType}", chartType);
            throw;
        }
    }

    #region Private Methods

    private void ValidateInputs(int dataSetId, string userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException("Dataset ID must be greater than 0", nameof(dataSetId));
        
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID is required", nameof(userId));
    }

    private void ValidateComparisonInputs(int dataSetId1, int dataSetId2, string userId)
    {
        ValidateInputs(dataSetId1, userId);
        
        if (dataSetId2 <= 0)
            throw new ArgumentException("Second dataset ID must be greater than 0", nameof(dataSetId2));
        
        if (dataSetId1 == dataSetId2)
            throw new ArgumentException("Cannot compare dataset with itself");
    }

    private async Task<DataSet> GetAndValidateDataSetAsync(int dataSetId, string userId)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(dataSetId);
        
        if (dataSet == null)
            throw new ArgumentException($"Dataset with ID {dataSetId} not found");
        
        if (dataSet.UserId != userId)
            throw new UnauthorizedAccessException($"Access denied to dataset {dataSetId}");
        
        if (dataSet.IsDeleted)
            throw new ArgumentException($"Dataset {dataSetId} has been deleted");
        
        return dataSet;
    }

    private List<Dictionary<string, object>> ExtractDataSetData(DataSet dataSet)
    {
        try
        {
            if (!dataSet.UseSeparateTable && !string.IsNullOrEmpty(dataSet.ProcessedData))
            {
                var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);
                return data ?? new List<Dictionary<string, object>>();
            }
            else
            {
                // For large datasets, we would fetch from separate table
                // This is a placeholder for the actual implementation
                _logger.LogWarning("Large dataset processing not yet implemented for dataset {DataSetId}", dataSet.Id);
                return new List<Dictionary<string, object>>();
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing data for dataset {DataSetId}", dataSet.Id);
            throw new InvalidOperationException($"Invalid data format for dataset {dataSet.Id}");
        }
    }

    private double ExtractDouble(object? value, double fallback = 0)
    {
        if (value is System.Text.Json.JsonElement je)
        {
            if (je.ValueKind == System.Text.Json.JsonValueKind.Number && je.TryGetDouble(out var d))
                return d;
            if (je.ValueKind == System.Text.Json.JsonValueKind.String && double.TryParse(je.GetString(), out var s))
                return s;
        }
        if (value is IConvertible conv)
            return Convert.ToDouble(conv);
        return fallback;
    }

    private ChartDataDto GenerateChartData(DataSet dataSet, List<Dictionary<string, object>> data, ChartType chartType, ChartConfigurationDto? configuration)
    {
        var chartData = new ChartDataDto
        {
            DataSetId = dataSet.Id,
            ChartType = chartType,
            Configuration = configuration
        };

        if (!data.Any())
        {
            _logger.LogWarning("No data available for chart generation in dataset {DataSetId}", dataSet.Id);
            return chartData;
        }

        // Extract column names
        var columns = data.First().Keys.ToList();
        
        // Generate sample data based on chart type
        switch (chartType)
        {
            case ChartType.Bar:
            case ChartType.Column:
                chartData.Series.Add(new ChartSeriesDto
                {
                    Name = "Values",
                    Data = data.Take(configuration?.MaxDataPoints ?? 10).Select((row, index) => (object)ExtractDouble(row.Values.FirstOrDefault(), index)).ToList(),
                    Color = "#3498db"
                });
                chartData.Labels = data.Take(configuration?.MaxDataPoints ?? 10).Select((row, index) => $"Item {index + 1}").ToList();
                break;

            case ChartType.Line:
            case ChartType.Area:
                chartData.Series.Add(new ChartSeriesDto
                {
                    Name = "Trend",
                    Data = data.Take(configuration?.MaxDataPoints ?? 20).Select((row, index) => (object)ExtractDouble(row.Values.FirstOrDefault(), index)).ToList(),
                    Color = "#2ecc71"
                });
                chartData.Labels = data.Take(configuration?.MaxDataPoints ?? 20).Select((row, index) => $"Point {index + 1}").ToList();
                break;

            case ChartType.Pie:
            case ChartType.Donut:
                var pieData = data.Take(configuration?.MaxDataPoints ?? 5).ToList();
                chartData.Series.Add(new ChartSeriesDto
                {
                    Name = "Distribution",
                    Data = pieData.Select(row => (object)ExtractDouble(row.Values.FirstOrDefault(), 1)).ToList(),
                    Color = "#e74c3c"
                });
                chartData.Labels = pieData.Select((row, index) => $"Category {index + 1}").ToList();
                break;

            default:
                // Default to bar chart for unsupported types
                chartData.ChartType = ChartType.Bar;
                chartData.Series.Add(new ChartSeriesDto
                {
                    Name = "Values",
                    Data = data.Take(10).Select((row, index) => (object)ExtractDouble(row.Values.FirstOrDefault(), index)).ToList(),
                    Color = "#9b59b6"
                });
                chartData.Labels = data.Take(10).Select((row, index) => $"Item {index + 1}").ToList();
                break;
        }

        return chartData;
    }

    private ComparisonChartDto GenerateComparisonChartData(DataSet dataSet1, DataSet dataSet2, List<Dictionary<string, object>> data1, List<Dictionary<string, object>> data2, ChartType chartType, ChartConfigurationDto? configuration)
    {
        var comparisonChart = new ComparisonChartDto
        {
            DataSetId1 = dataSet1.Id,
            DataSetId2 = dataSet2.Id,
            ChartType = chartType,
            Configuration = configuration
        };

        if (!data1.Any() || !data2.Any())
        {
            _logger.LogWarning("Insufficient data for comparison chart between datasets {DataSetId1}, {DataSetId2}", dataSet1.Id, dataSet2.Id);
            return comparisonChart;
        }

        // Calculate similarity score
        var columns1 = data1.First().Keys.ToList();
        var columns2 = data2.First().Keys.ToList();
        
        comparisonChart.CommonColumns = columns1.Intersect(columns2).ToList();
        comparisonChart.Differences = columns1.Union(columns2).Except(comparisonChart.CommonColumns).ToList();
        
        comparisonChart.SimilarityScore = comparisonChart.CommonColumns.Count / (double)Math.Max(columns1.Count, columns2.Count);

        // Generate comparison series
        var maxPoints = configuration?.MaxDataPoints ?? 10;
        
        comparisonChart.Series.Add(new ChartSeriesDto
        {
            Name = $"Dataset {dataSet1.Id}",
            Data = data1.Take(maxPoints).Select((row, index) => (object)ExtractDouble(row.Values.FirstOrDefault(), index)).ToList(),
            Color = "#3498db"
        });

        comparisonChart.Series.Add(new ChartSeriesDto
        {
            Name = $"Dataset {dataSet2.Id}",
            Data = data2.Take(maxPoints).Select((row, index) => (object)ExtractDouble(row.Values.FirstOrDefault(), index)).ToList(),
            Color = "#e74c3c"
        });

        comparisonChart.Labels = Enumerable.Range(1, maxPoints).Select(i => $"Point {i}").ToList();

        return comparisonChart;
    }

    private DataSummaryDto GenerateDataSummary(DataSet dataSet, List<Dictionary<string, object>> data)
    {
        var summary = new DataSummaryDto
        {
            DataSetId = dataSet.Id,
            TotalRows = data.Count,
            TotalColumns = data.Any() ? data.First().Count : 0
        };

        if (!data.Any()) return summary;

        // Calculate missing values and duplicates
        summary.MissingValues = data.Sum(row => row.Values.Count(v => v == null));
        
        var uniqueRows = data.Select(row => JsonSerializer.Serialize(row)).Distinct().Count();
        summary.DuplicateRows = data.Count - uniqueRows;

        // Generate column summaries
        var columns = data.First().Keys.ToList();
        foreach (var column in columns)
        {
            var columnData = data.Select(row => row.ContainsKey(column) ? row[column] : null).ToList();
            var nonNullData = columnData.Where(v => v != null).ToList();
            
            var columnSummary = new ColumnSummaryDto
            {
                ColumnName = column,
                DataType = DetermineDataType(columnData),
                NonNullCount = nonNullData.Count,
                NullCount = columnData.Count - nonNullData.Count,
                UniqueCount = nonNullData.Distinct().Count(),
                SampleValues = nonNullData.Take(5).Cast<object>().ToList()
            };

            // Calculate numeric statistics if applicable
            if (IsNumericColumn(columnData))
            {
                var numericData = nonNullData.Select(v => ExtractDouble(v)).ToList();
                columnSummary.MinValue = numericData.Min();
                columnSummary.MaxValue = numericData.Max();
                columnSummary.Mean = numericData.Average();
                columnSummary.Median = CalculateMedian(numericData);
                columnSummary.StandardDeviation = CalculateStandardDeviation(numericData);
            }

            summary.ColumnSummaries[column] = columnSummary;
        }

        return summary;
    }

    private StatisticalSummaryDto GenerateStatisticalSummary(DataSet dataSet, List<Dictionary<string, object>> data)
    {
        var stats = new StatisticalSummaryDto
        {
            DataSetId = dataSet.Id
        };

        if (!data.Any()) return stats;

        var columns = data.First().Keys.ToList();
        
        foreach (var column in columns)
        {
            var columnData = data.Select(row => row.ContainsKey(column) ? row[column] : null)
                                .Where(v => v != null && IsNumeric(v))
                                .Select(v => ExtractDouble(v))
                                .ToList();

            if (columnData.Any())
            {
                var columnStats = new ColumnStatisticsDto
                {
                    ColumnName = column,
                    Mean = columnData.Average(),
                    Median = CalculateMedian(columnData),
                    StandardDeviation = CalculateStandardDeviation(columnData),
                    Min = columnData.Min(),
                    Max = columnData.Max(),
                    Q1 = CalculateQuartile(columnData, 0.25),
                    Q2 = CalculateQuartile(columnData, 0.5),
                    Q3 = CalculateQuartile(columnData, 0.75),
                    Skewness = CalculateSkewness(columnData),
                    Kurtosis = CalculateKurtosis(columnData)
                };

                // Detect outliers using IQR method
                var iqr = columnStats.Q3 - columnStats.Q1;
                var lowerBound = columnStats.Q1 - 1.5 * iqr;
                var upperBound = columnStats.Q3 + 1.5 * iqr;
                
                columnStats.OutlierCount = columnData.Count(v => v < lowerBound || v > upperBound);
                
                if (columnStats.OutlierCount > 0)
                {
                    stats.OutlierColumns.Add(column);
                }

                stats.ColumnStatistics[column] = columnStats;
            }
        }

        // Calculate correlation matrix for numeric columns
        var numericColumns = stats.ColumnStatistics.Keys.ToList();
        for (int i = 0; i < numericColumns.Count; i++)
        {
            for (int j = i + 1; j < numericColumns.Count; j++)
            {
                var col1 = numericColumns[i];
                var col2 = numericColumns[j];
                
                var data1 = data.Select(row => ExtractDouble(row[col1])).ToList();
                var data2 = data.Select(row => ExtractDouble(row[col2])).ToList();
                
                var correlation = CalculateCorrelation(data1, data2);
                stats.CorrelationMatrix[$"{col1}_{col2}"] = correlation;
            }
        }

        return stats;
    }

    #endregion

    #region Statistical Helper Methods

    private string DetermineDataType(List<object?> data)
    {
        var nonNullData = data.Where(v => v != null).ToList();
        if (!nonNullData.Any()) return "Unknown";

        var firstValue = nonNullData.First();
        
        if (IsNumeric(firstValue)) return "Numeric";
        if (firstValue is DateTime) return "DateTime";
        if (firstValue is bool) return "Boolean";
        return "String";
    }

    private bool IsNumeric(object? value)
    {
        return value is int || value is long || value is float || value is double || value is decimal;
    }

    private bool IsNumericColumn(List<object?> data)
    {
        return data.Where(v => v != null).All(IsNumeric);
    }

    private double CalculateMedian(List<double> data)
    {
        if (!data.Any()) return 0;
        
        var sorted = data.OrderBy(x => x).ToList();
        var count = sorted.Count;
        
        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
        else
            return sorted[count / 2];
    }

    private double CalculateStandardDeviation(List<double> data)
    {
        if (!data.Any()) return 0;
        
        var mean = data.Average();
        var variance = data.Select(x => Math.Pow(x - mean, 2)).Average();
        return Math.Sqrt(variance);
    }

    private double CalculateQuartile(List<double> data, double percentile)
    {
        if (!data.Any()) return 0;
        
        var sorted = data.OrderBy(x => x).ToList();
        var index = (int)Math.Ceiling(percentile * (sorted.Count - 1));
        return sorted[index];
    }

    private double CalculateSkewness(List<double> data)
    {
        if (data.Count < 3) return 0;
        
        var mean = data.Average();
        var stdDev = CalculateStandardDeviation(data);
        if (stdDev == 0) return 0;
        
        var skewness = data.Select(x => Math.Pow((x - mean) / stdDev, 3)).Average();
        return skewness * Math.Sqrt(data.Count * (data.Count - 1)) / (data.Count - 2);
    }

    private double CalculateKurtosis(List<double> data)
    {
        if (data.Count < 4) return 0;
        
        var mean = data.Average();
        var stdDev = CalculateStandardDeviation(data);
        if (stdDev == 0) return 0;
        
        var kurtosis = data.Select(x => Math.Pow((x - mean) / stdDev, 4)).Average();
        return kurtosis - 3; // Excess kurtosis
    }

    private double CalculateCorrelation(List<double> data1, List<double> data2)
    {
        if (data1.Count != data2.Count || data1.Count < 2) return 0;
        
        var mean1 = data1.Average();
        var mean2 = data2.Average();
        
        var numerator = data1.Zip(data2, (x, y) => (x - mean1) * (y - mean2)).Sum();
        var denominator1 = data1.Select(x => Math.Pow(x - mean1, 2)).Sum();
        var denominator2 = data2.Select(x => Math.Pow(x - mean2, 2)).Sum();
        
        if (denominator1 == 0 || denominator2 == 0) return 0;
        
        return numerator / Math.Sqrt(denominator1 * denominator2);
    }

    #endregion
} 