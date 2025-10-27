using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Service for generating charts from dataset data.
/// </summary>
public class ChartGenerationService : IChartGenerationService
{
    private readonly IStatisticalCalculationService _statisticalCalculationService;
    private readonly ILogger<ChartGenerationService> _logger;
    private const int DEFAULT_MAX_DATA_POINTS = 1000;

    public ChartGenerationService(
        IStatisticalCalculationService statisticalCalculationService,
        ILogger<ChartGenerationService> logger)
    {
        _statisticalCalculationService = statisticalCalculationService;
        _logger = logger;
    }

    public ChartDataDto GenerateChartData(
        DataSet dataSet,
        List<Dictionary<string, object?>> data,
        ChartType chartType,
        ChartConfiguration? configuration)
    {
        _logger.LogDebug("Generating {ChartType} chart for dataset {DataSetId}", chartType, dataSet.Id);

        if (data.Count == 0)
        {
            _logger.LogWarning("No data available for chart generation for dataset {DataSetId}", dataSet.Id);
            return new ChartDataDto
            {
                DataSetId = dataSet.Id,
                ChartType = chartType,
                Labels = new List<string>(),
                Series = new List<ChartSeriesDto>()
            };
        }

        var maxDataPoints = configuration?.MaxDataPoints ?? DEFAULT_MAX_DATA_POINTS;
        var limitedData = data.Take(maxDataPoints).ToList();

        var labels = new List<string>();
        var series = new List<ChartSeriesDto>();

        switch (chartType)
        {
            case ChartType.Bar:
            case ChartType.Line:
            case ChartType.Area:
            case ChartType.Column:
                GenerateBarLineAreaChart(limitedData, labels, series);
                break;

            case ChartType.Pie:
            case ChartType.Donut:
                GeneratePieDonutChart(limitedData, labels, series);
                break;

            case ChartType.Scatter:
            case ChartType.Bubble:
                GenerateScatterBubbleChart(limitedData, labels, series);
                break;

            case ChartType.Histogram:
                GenerateHistogramChart(limitedData, labels, series);
                break;

            case ChartType.BoxPlot:
                GenerateBoxPlotChart(limitedData, labels, series);
                break;

            case ChartType.Heatmap:
                GenerateHeatmapChart(limitedData, labels, series);
                break;

            case ChartType.Radar:
                GenerateRadarChart(limitedData, labels, series);
                break;

            default:
                _logger.LogWarning("Unsupported chart type {ChartType} for dataset {DataSetId}", chartType, dataSet.Id);
                break;
        }

        return new ChartDataDto
        {
            DataSetId = dataSet.Id,
            ChartType = chartType,
            Labels = labels,
            Series = series
        };
    }

    public ComparisonChartDto GenerateComparisonChartData(
        DataSet dataSet1,
        DataSet dataSet2,
        List<Dictionary<string, object?>> data1,
        List<Dictionary<string, object?>> data2,
        ChartType chartType,
        ChartConfiguration? configuration)
    {
        _logger.LogDebug("Generating comparison {ChartType} chart for datasets {DataSetId1} and {DataSetId2}",
            chartType, dataSet1.Id, dataSet2.Id);

        var chart1 = GenerateChartData(dataSet1, data1, chartType, configuration);
        var chart2 = GenerateChartData(dataSet2, data2, chartType, configuration);

        // Rename series to differentiate between datasets
        foreach (var series in chart1.Series)
        {
            series.Name = $"{dataSet1.Name} - {series.Name}";
        }

        foreach (var series in chart2.Series)
        {
            series.Name = $"{dataSet2.Name} - {series.Name}";
        }

        // Calculate similarity score (simplified)
        var commonColumns = GetCommonColumns(data1, data2);
        var similarities = new List<double>();
        var differences = new List<string>();

        foreach (var column in commonColumns)
        {
            var values1 = data1.Select(r => r.GetValueOrDefault(column)).Where(v => v != null).ToList();
            var values2 = data2.Select(r => r.GetValueOrDefault(column)).Where(v => v != null).ToList();

            if (values1.Any() && values2.Any())
            {
                var similarity = CalculateColumnSimilarity(values1, values2);
                similarities.Add(similarity);

                if (similarity < 0.8)
                {
                    differences.Add($"Column '{column}' shows significant difference (similarity: {similarity:P0})");
                }
            }
        }

        var similarityScore = similarities.Any() ? similarities.Average() : 0.0;

        return new ComparisonChartDto
        {
            DataSetId1 = dataSet1.Id,
            DataSetId2 = dataSet2.Id,
            ChartType = chartType,
            Series = chart1.Series.Concat(chart2.Series).ToList(),
            Labels = chart1.Labels,
            SimilarityScore = similarityScore,
            CommonColumns = commonColumns,
            Differences = differences
        };
    }

    public bool ValidateChartConfiguration(ChartType chartType, ChartConfiguration? configuration)
    {
        if (configuration == null) return true;

        // Validate max data points
        if (configuration.MaxDataPoints.HasValue && configuration.MaxDataPoints.Value <= 0)
        {
            throw new ArgumentException("MaxDataPoints must be greater than 0");
        }

        return true;
    }

    #region Private Chart Generation Methods

    private void GenerateBarLineAreaChart(List<Dictionary<string, object?>> data, List<string> labels, List<ChartSeriesDto> series)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _logger.LogDebug("No numeric columns found for chart. Using fallback data");
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
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? "Unknown"));

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

    private void GeneratePieDonutChart(List<Dictionary<string, object?>> data, List<string> labels, List<ChartSeriesDto> series)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _logger.LogDebug("No numeric columns found for pie/donut chart. Using fallback data");
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = "Value",
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first column as labels
        var labelColumn = columns[0];
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? "Unknown"));

        // Use first numeric column as data
        var dataColumn = numericColumns[0];
        series.Add(new ChartSeriesDto
        {
            Name = dataColumn,
            Data = data.Select(row => ExtractDouble(row.GetValueOrDefault(dataColumn))).Cast<object>().ToList()
        });
    }

    private void GenerateScatterBubbleChart(List<Dictionary<string, object?>> data, List<string> labels, List<ChartSeriesDto> series)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count < 2)
        {
            _logger.LogDebug("Insufficient numeric columns for scatter/bubble chart. Using fallback data");
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = "Data",
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first column as labels
        var labelColumn = columns[0];
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? "Unknown"));

        // Use first two numeric columns as X and Y
        var xColumn = numericColumns[0];
        var yColumn = numericColumns[1];

        series.Add(new ChartSeriesDto
        {
            Name = $"{xColumn} vs {yColumn}",
            Data = data.Select(row => new Dictionary<string, object>
            {
                ["x"] = ExtractDouble(row.GetValueOrDefault(xColumn)),
                ["y"] = ExtractDouble(row.GetValueOrDefault(yColumn))
            }).Cast<object>().ToList()
        });
    }

    private void GenerateHistogramChart(List<Dictionary<string, object?>> data, List<string> labels, List<ChartSeriesDto> series)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _logger.LogDebug("No numeric columns found for histogram. Using fallback data");
            return;
        }

        // Create histogram bins for first numeric column
        var column = numericColumns[0];
        var values = data.Select(row => ExtractDouble(row.GetValueOrDefault(column))).Where(v => !double.IsNaN(v)).ToList();

        if (values.Count == 0) return;

        var binCount = Math.Min(20, (int)Math.Sqrt(values.Count));
        var min = values.Min();
        var max = values.Max();
        var binWidth = (max - min) / binCount;

        var bins = new Dictionary<string, int>();
        for (int i = 0; i < binCount; i++)
        {
            var binStart = min + (i * binWidth);
            var binEnd = binStart + binWidth;
            var binLabel = $"{binStart:F1}-{binEnd:F1}";
            bins[binLabel] = values.Count(v => v >= binStart && v < binEnd);
        }

        labels.AddRange(bins.Keys);
        series.Add(new ChartSeriesDto
        {
            Name = column,
            Data = bins.Values.Cast<object>().ToList()
        });
    }

    private void GenerateBoxPlotChart(List<Dictionary<string, object?>> data, List<string> labels, List<ChartSeriesDto> series)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        foreach (var column in numericColumns)
        {
            var values = data.Select(row => ExtractDouble(row.GetValueOrDefault(column)))
                .Where(v => !double.IsNaN(v))
                .OrderBy(v => v)
                .ToList();

            if (values.Count == 0) continue;

            var q1 = CalculateQuartile(values, 0.25);
            var q2 = CalculateQuartile(values, 0.50);
            var q3 = CalculateQuartile(values, 0.75);

            labels.Add(column);
            series.Add(new ChartSeriesDto
            {
                Name = column,
                Data = new List<object> { values.Min(), q1, q2, q3, values.Max() }
            });
        }
    }

    private void GenerateHeatmapChart(List<Dictionary<string, object?>> data, List<string> labels, List<ChartSeriesDto> series)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        labels.AddRange(numericColumns);

        // Create correlation matrix-like heatmap
        foreach (var col1 in numericColumns)
        {
            var values = new List<object>();
            foreach (var col2 in numericColumns)
            {
                var data1 = data.Select(row => ExtractDouble(row.GetValueOrDefault(col1))).ToList();
                var data2 = data.Select(row => ExtractDouble(row.GetValueOrDefault(col2))).ToList();
                var correlation = CalculateCorrelation(data1, data2);
                values.Add(correlation);
            }
            series.Add(new ChartSeriesDto { Name = col1, Data = values });
        }
    }

    private void GenerateRadarChart(List<Dictionary<string, object?>> data, List<string> labels, List<ChartSeriesDto> series)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        labels.AddRange(numericColumns);

        // Take first few rows as series
        var rowCount = Math.Min(5, data.Count);
        for (int i = 0; i < rowCount; i++)
        {
            series.Add(new ChartSeriesDto
            {
                Name = $"Row {i + 1}",
                Data = numericColumns.Select(col => ExtractDouble(data[i].GetValueOrDefault(col))).Cast<object>().ToList()
            });
        }
    }

    #endregion

    #region Private Utility Methods

    private bool IsNumericColumn(List<object?> values)
    {
        var nonNullValues = values.Where(v => v != null).ToList();
        if (nonNullValues.Count == 0) return false;

        var numericCount = nonNullValues.Count(v => v is int || v is long || v is float || v is double || v is decimal ||
                                                     (v is string s && double.TryParse(s, out _)));

        return (double)numericCount / nonNullValues.Count >= 0.8;
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

    private static List<string> GetCommonColumns(List<Dictionary<string, object?>> data1, List<Dictionary<string, object?>> data2)
    {
        if (data1.Count == 0 || data2.Count == 0) return new List<string>();

        var columns1 = data1[0].Keys.ToHashSet();
        var columns2 = data2[0].Keys.ToHashSet();

        return columns1.Intersect(columns2).ToList();
    }

    private static double CalculateColumnSimilarity(List<object?> values1, List<object?> values2)
    {
        // Simplified similarity calculation
        var set1 = values1.Select(v => v?.ToString() ?? "").ToHashSet();
        var set2 = values2.Select(v => v?.ToString() ?? "").ToHashSet();

        var intersection = set1.Intersect(set2).Count();
        var union = set1.Union(set2).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    private static double CalculateQuartile(List<double> sortedData, double percentile)
    {
        if (sortedData.Count == 0) return 0;

        var index = percentile * (sortedData.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper) return sortedData[lower];

        var lowerValue = sortedData[lower];
        var upperValue = sortedData[upper];
        var fraction = index - lower;

        return lowerValue + (fraction * (upperValue - lowerValue));
    }

    private static double CalculateCorrelation(List<double> data1, List<double> data2)
    {
        if (data1.Count == 0 || data2.Count == 0 || data1.Count != data2.Count) return 0;

        var mean1 = data1.Average();
        var mean2 = data2.Average();

        var sum1 = data1.Sum(x => (x - mean1) * (x - mean1));
        var sum2 = data2.Sum(x => (x - mean2) * (x - mean2));
        var sumProduct = data1.Zip(data2, (x, y) => (x - mean1) * (y - mean2)).Sum();

        if (sum1 == 0 || sum2 == 0) return 0;

        return sumProduct / Math.Sqrt(sum1 * sum2);
    }

    #endregion
}
