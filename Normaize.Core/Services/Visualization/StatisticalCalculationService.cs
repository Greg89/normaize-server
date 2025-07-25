using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using System.Text.Json;

namespace Normaize.Core.Services.Visualization;

/// <summary>
/// Service for performing statistical calculations on datasets.
/// Extracted from DataVisualizationService to follow single responsibility principle.
/// </summary>
public class StatisticalCalculationService : IStatisticalCalculationService
{
    public DataSummaryDto GenerateDataSummary(DataSet dataSet, List<Dictionary<string, object>> data)
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
                UniqueCount = columnData.Where(v => v != null).Distinct().Count(),
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

    public StatisticalSummaryDto GenerateStatisticalSummary(DataSet dataSet, List<Dictionary<string, object>> data)
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

    public double CalculateMedian(List<double> data)
    {
        if (data.Count == 0) return 0;

        var sorted = data.OrderBy(x => x).ToList();
        var mid = sorted.Count / 2;

        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    public double CalculateStandardDeviation(List<double> data)
    {
        if (data.Count <= 1) return 0;

        var mean = data.Average();
        var variance = data.Select(x => Math.Pow(x - mean, 2)).Average();
        return Math.Sqrt(variance);
    }

    public double CalculateQuartile(List<double> data, double percentile)
    {
        if (data.Count == 0) return 0;

        var sorted = data.OrderBy(x => x).ToList();
        var index = percentile * (sorted.Count - 1);
        var lower = sorted[(int)Math.Floor(index)];
        var upper = sorted[(int)Math.Ceiling(index)];

        return lower + (upper - lower) * (index - Math.Floor(index));
    }

    public double CalculateSkewness(List<double> data)
    {
        if (data.Count <= 2) return 0;

        var mean = data.Average();
        var stdDev = CalculateStandardDeviation(data);
        if (Math.Abs(stdDev) < double.Epsilon) return 0;

        var skewness = data.Select(x => Math.Pow((x - mean) / stdDev, 3)).Average();
        return skewness * Math.Sqrt(data.Count * (data.Count - 1)) / (data.Count - 2);
    }

    public double CalculateKurtosis(List<double> data)
    {
        if (data.Count <= 3) return 0;

        var mean = data.Average();
        var stdDev = CalculateStandardDeviation(data);
        if (Math.Abs(stdDev) < double.Epsilon) return 0;

        var kurtosis = data.Select(x => Math.Pow((x - mean) / stdDev, 4)).Average();
        return (kurtosis - AppConstants.DataProcessing.KURTOSIS_ADJUSTMENT) * Math.Sqrt(data.Count * (data.Count - 1)) / ((data.Count - 2) * (data.Count - 3));
    }

    public string DetermineDataType(List<object?> data)
    {
        var nonNullData = data.Where(v => v != null).ToList();
        if (nonNullData.Count == 0) return AppConstants.Messages.UNKNOWN;

        if (nonNullData.All(IsNumeric)) return AppConstants.DataProcessing.DATA_TYPE_NUMERIC;
        if (nonNullData.All(IsDateTime)) return AppConstants.DataProcessing.DATA_TYPE_DATETIME;
        if (nonNullData.All(IsBoolean)) return AppConstants.DataProcessing.DATA_TYPE_BOOLEAN;
        return AppConstants.DataProcessing.DATA_TYPE_STRING;
    }

    public bool IsNumeric(object? value)
    {
        return value switch
        {
            int or long or float or double or decimal => true,
            string s => double.TryParse(s, out _),
            _ => false
        };
    }

    public bool IsDateTime(object? value)
    {
        return value switch
        {
            DateTime => true,
            string s => DateTime.TryParse(s, out _),
            _ => false
        };
    }

    public bool IsBoolean(object? value)
    {
        return value switch
        {
            bool => true,
            string s => bool.TryParse(s, out _),
            _ => false
        };
    }

    public bool IsNumericColumn(List<object?> data)
    {
        var nonNullData = data.Where(v => v != null).ToList();
        return nonNullData.Count > 0 && nonNullData.All(IsNumeric);
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
            string s => double.TryParse(s, out var result) ? result : fallback,
            _ => fallback
        };
    }
}