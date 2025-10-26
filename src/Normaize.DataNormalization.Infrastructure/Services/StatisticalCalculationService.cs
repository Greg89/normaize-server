using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;
using System.Globalization;
using System.Text.Json;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Infrastructure service for performing statistical calculations on datasets
/// </summary>
public class StatisticalCalculationService : IStatisticalCalculationService
{
    private readonly ILogger<StatisticalCalculationService> _logger;
    private const int SAMPLE_VALUES_COUNT = 10;

    public StatisticalCalculationService(ILogger<StatisticalCalculationService> logger)
    {
        _logger = logger;
    }

    public async Task<Statistics> GenerateDataSummaryAsync(
        DataSet dataSet,
        List<Dictionary<string, object?>> data,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating data summary for DataSet {DataSetId}", dataSet.Id);

        var startTime = DateTime.UtcNow;

        try
        {
            if (data.Count == 0)
            {
                return Statistics.CreateDataSummary(
                    dataSet.Id,
                    dataSet.Name,
                    0, 0, 0, 0,
                    new Dictionary<string, ColumnSummary>(),
                    TimeSpan.Zero);
            }

            var columns = data[0].Keys.ToList();
            var columnSummaries = new Dictionary<string, ColumnSummary>();
            var totalMissingValues = 0;

            foreach (var column in columns)
            {
                var columnData = data.Select(row => row.GetValueOrDefault(column)).ToList();
                var dataType = DataTypeClassification.DetermineFromValues(columnData);
                
                var nullCount = columnData.Count(v => v == null);
                totalMissingValues += nullCount;
                var nonNullData = columnData.Where(v => v != null).ToList();
                
                var sampleValues = columnData
                    .Take(SAMPLE_VALUES_COUNT)
                    .Select(x => x ?? "NULL")
                    .ToList();

                object? minValue = null;
                object? maxValue = null;
                StatisticalMeasure? statistics = null;

                // Calculate basic statistics for numeric columns
                if (dataType.IsNumeric && nonNullData.Any())
                {
                    var numericData = nonNullData.Select((value, index) => ExtractDouble(value, 0)).Where(v => !double.IsNaN(v)).ToList();
                    if (numericData.Any())
                    {
                        minValue = numericData.Min();
                        maxValue = numericData.Max();
                        
                        statistics = new StatisticalMeasure(
                            mean: numericData.Average(),
                            median: CalculateMedian(numericData),
                            standardDeviation: CalculateStandardDeviation(numericData),
                            min: numericData.Min(),
                            max: numericData.Max(),
                            q1: CalculateQuartile(numericData, 0.25),
                            q2: CalculateQuartile(numericData, 0.50),
                            q3: CalculateQuartile(numericData, 0.75),
                            skewness: CalculateSkewness(numericData),
                            kurtosis: CalculateKurtosis(numericData));
                    }
                }

                var columnSummary = new ColumnSummary(
                    columnName: column,
                    dataType: dataType,
                    nonNullCount: nonNullData.Count,
                    nullCount: nullCount,
                    uniqueCount: nonNullData.Distinct().Count(),
                    sampleValues: sampleValues,
                    minValue: minValue,
                    maxValue: maxValue,
                    statistics: statistics);

                columnSummaries[column] = columnSummary;
            }

            var duplicateRows = data.Count - data.Select(row => JsonSerializer.Serialize(row)).Distinct().Count();
            var processingTime = DateTime.UtcNow - startTime;

            var result = Statistics.CreateDataSummary(
                dataSet.Id,
                dataSet.Name,
                data.Count,
                columns.Count,
                totalMissingValues,
                duplicateRows,
                columnSummaries,
                processingTime);

            _logger.LogInformation("Successfully generated data summary for DataSet {DataSetId} in {ProcessingTimeMs}ms", 
                dataSet.Id, processingTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data summary for DataSet {DataSetId}", dataSet.Id);
            throw;
        }
    }

    public async Task<Statistics> GenerateStatisticalSummaryAsync(
        DataSet dataSet,
        List<Dictionary<string, object?>> data,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating statistical summary for DataSet {DataSetId}", dataSet.Id);

        var startTime = DateTime.UtcNow;

        try
        {
            // First generate basic data summary
            var basicSummary = await GenerateDataSummaryAsync(dataSet, data, cancellationToken);

            if (data.Count == 0)
            {
                return Statistics.CreateStatisticalSummary(
                    dataSet.Id,
                    dataSet.Name,
                    0, 0, 0, 0,
                    new Dictionary<string, ColumnSummary>(),
                    new Dictionary<string, StatisticalMeasure>(),
                    TimeSpan.Zero);
            }

            var columnStatistics = new Dictionary<string, StatisticalMeasure>();

            // Generate comprehensive statistics for numeric columns
            foreach (var (columnName, columnSummary) in basicSummary.ColumnSummaries)
            {
                if (columnSummary.DataType.IsNumeric && columnSummary.Statistics != null)
                {
                    columnStatistics[columnName] = columnSummary.Statistics;
                }
            }

            var processingTime = DateTime.UtcNow - startTime;

            var result = Statistics.CreateStatisticalSummary(
                dataSet.Id,
                dataSet.Name,
                data.Count,
                basicSummary.TotalColumns,
                basicSummary.MissingValues,
                basicSummary.DuplicateRows,
                basicSummary.ColumnSummaries.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                columnStatistics,
                processingTime);

            _logger.LogInformation("Successfully generated statistical summary for DataSet {DataSetId} in {ProcessingTimeMs}ms", 
                dataSet.Id, processingTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating statistical summary for DataSet {DataSetId}", dataSet.Id);
            throw;
        }
    }

    public async Task<Dictionary<string, double>> CalculateCorrelationMatrixAsync(
        Dictionary<string, List<double>> numericColumns,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating correlation matrix for {ColumnCount} columns", numericColumns.Count);

        return await Task.Run(() =>
        {
            var correlations = new Dictionary<string, double>();
            var columnNames = numericColumns.Keys.ToList();

            for (int i = 0; i < columnNames.Count; i++)
            {
                for (int j = i + 1; j < columnNames.Count; j++)
                {
                    var col1 = columnNames[i];
                    var col2 = columnNames[j];
                    var correlation = CalculateCorrelation(numericColumns[col1], numericColumns[col2]);
                    correlations[$"{col1}-{col2}"] = correlation;
                }
            }

            return correlations;
        }, cancellationToken);
    }

    public async Task<(List<string> outlierColumns, List<int> outlierIndices)> DetectOutliersAsync(
        Dictionary<string, List<double>> numericColumns,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting outliers for {ColumnCount} columns", numericColumns.Count);

        return await Task.Run(() =>
        {
            var outlierColumns = new List<string>();
            var outlierIndices = new HashSet<int>();

            foreach (var (columnName, values) in numericColumns)
            {
                var q1 = CalculateQuartile(values, 0.25);
                var q3 = CalculateQuartile(values, 0.75);
                var iqr = q3 - q1;
                var lowerBound = q1 - 1.5 * iqr;
                var upperBound = q3 + 1.5 * iqr;

                var hasOutliers = false;
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] < lowerBound || values[i] > upperBound)
                    {
                        outlierIndices.Add(i);
                        hasOutliers = true;
                    }
                }

                if (hasOutliers)
                {
                    outlierColumns.Add(columnName);
                }
            }

            return (outlierColumns, outlierIndices.OrderBy(x => x).ToList());
        }, cancellationToken);
    }

    #region Private Statistical Calculation Methods

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
        return (kurtosis - 3) * Math.Sqrt(data.Count * (data.Count - 1)) / ((data.Count - 2) * (data.Count - 3));
    }

    private static double CalculateCorrelation(List<double> x, List<double> y)
    {
        if (x.Count != y.Count || x.Count <= 1) return 0;

        var meanX = x.Average();
        var meanY = y.Average();

        var numerator = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
        var denominatorX = Math.Sqrt(x.Select(xi => Math.Pow(xi - meanX, 2)).Sum());
        var denominatorY = Math.Sqrt(y.Select(yi => Math.Pow(yi - meanY, 2)).Sum());

        var denominator = denominatorX * denominatorY;
        return Math.Abs(denominator) < double.Epsilon ? 0 : numerator / denominator;
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
            string s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : fallback,
            _ => fallback
        };
    }

    #endregion
}