using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Service for generating data and statistical summaries from dataset data.
/// </summary>
public class DataSummaryService : IDataSummaryService
{
    private readonly IStatisticalCalculationService _statisticalCalculationService;
    private readonly ILogger<DataSummaryService> _logger;

    public DataSummaryService(
        IStatisticalCalculationService statisticalCalculationService,
        ILogger<DataSummaryService> logger)
    {
        _statisticalCalculationService = statisticalCalculationService;
        _logger = logger;
    }

    public DataSummaryDto GenerateDataSummary(DataSet dataSet, List<Dictionary<string, object?>> data)
    {
        _logger.LogDebug("Generating data summary for dataset {DataSetId}", dataSet.Id);

        // Use existing StatisticalCalculationService to generate summary
        var statistics = _statisticalCalculationService.GenerateDataSummaryAsync(dataSet, data).Result;

        // Map domain Statistics to DTO
        var columnSummaries = new Dictionary<string, ColumnSummaryDto>();
        foreach (var (columnName, columnSummary) in statistics.ColumnSummaries)
        {
            columnSummaries[columnName] = new ColumnSummaryDto
            {
                ColumnName = columnSummary.ColumnName,
                DataType = columnSummary.DataType.TypeName,
                NonNullCount = columnSummary.NonNullCount,
                NullCount = columnSummary.NullCount,
                UniqueCount = columnSummary.UniqueCount,
                SampleValues = columnSummary.SampleValues.ToList(),
                MinValue = columnSummary.MinValue,
                MaxValue = columnSummary.MaxValue,
                Mean = columnSummary.Statistics?.Mean,
                Median = columnSummary.Statistics?.Median,
                StandardDeviation = columnSummary.Statistics?.StandardDeviation
            };
        }

        return new DataSummaryDto
        {
            DataSetId = dataSet.Id,
            TotalRows = statistics.TotalRows,
            TotalColumns = statistics.TotalColumns,
            MissingValues = statistics.MissingValues,
            DuplicateRows = statistics.DuplicateRows,
            ColumnSummaries = columnSummaries
        };
    }

    public StatisticalSummaryDto GenerateStatisticalSummary(DataSet dataSet, List<Dictionary<string, object?>> data)
    {
        _logger.LogDebug("Generating statistical summary for dataset {DataSetId}", dataSet.Id);

        // Use existing StatisticalCalculationService to generate summary
        var statistics = _statisticalCalculationService.GenerateStatisticalSummaryAsync(dataSet, data).Result;

        // Map domain Statistics to DTO
        var columnStatistics = new Dictionary<string, ColumnStatisticsDto>();
        foreach (var (columnName, statisticalMeasure) in statistics.ColumnStatistics)
        {
            columnStatistics[columnName] = new ColumnStatisticsDto
            {
                ColumnName = columnName,
                Mean = statisticalMeasure.Mean,
                Median = statisticalMeasure.Median,
                StandardDeviation = statisticalMeasure.StandardDeviation,
                Min = statisticalMeasure.Min,
                Max = statisticalMeasure.Max,
                Q1 = statisticalMeasure.Q1,
                Q2 = statisticalMeasure.Q2,
                Q3 = statisticalMeasure.Q3,
                Skewness = statisticalMeasure.Skewness,
                Kurtosis = statisticalMeasure.Kurtosis,
                OutlierCount = statisticalMeasure.OutlierCount
            };
        }

        return new StatisticalSummaryDto
        {
            DataSetId = dataSet.Id,
            ColumnStatistics = columnStatistics,
            CorrelationMatrix = new Dictionary<string, double>(), // Not yet implemented
            OutlierColumns = new List<string>(), // Not yet implemented
            OutlierIndices = new List<int>() // Not yet implemented
        };
    }
}
