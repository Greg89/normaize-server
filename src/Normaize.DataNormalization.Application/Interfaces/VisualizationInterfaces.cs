using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Interfaces;

/// <summary>
/// Service interface for generating charts from dataset data.
/// </summary>
public interface IChartGenerationService
{
    /// <summary>
    /// Generates chart data for a single dataset.
    /// </summary>
    ChartDataDto GenerateChartData(
        DataSet dataSet,
        List<Dictionary<string, object?>> data,
        ChartType chartType,
        ChartConfiguration? configuration);

    /// <summary>
    /// Generates comparison chart data for two datasets.
    /// </summary>
    ComparisonChartDto GenerateComparisonChartData(
        DataSet dataSet1,
        DataSet dataSet2,
        List<Dictionary<string, object?>> data1,
        List<Dictionary<string, object?>> data2,
        ChartType chartType,
        ChartConfiguration? configuration);

    /// <summary>
    /// Validates chart configuration for a specific chart type.
    /// </summary>
    bool ValidateChartConfiguration(ChartType chartType, ChartConfiguration? configuration);
}

/// <summary>
/// Service interface for generating statistical summaries from dataset data.
/// </summary>
public interface IDataSummaryService
{
    /// <summary>
    /// Generates a data summary for a dataset.
    /// </summary>
    DataSummaryDto GenerateDataSummary(DataSet dataSet, List<Dictionary<string, object?>> data);

    /// <summary>
    /// Generates a statistical summary for a dataset.
    /// </summary>
    StatisticalSummaryDto GenerateStatisticalSummary(DataSet dataSet, List<Dictionary<string, object?>> data);
}
