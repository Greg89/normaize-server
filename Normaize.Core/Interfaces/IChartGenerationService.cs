using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for generating charts from dataset data.
/// Handles the creation of various chart types and their data transformation.
/// </summary>
public interface IChartGenerationService
{
    /// <summary>
    /// Generates chart data for a single dataset.
    /// </summary>
    /// <param name="dataSet">The dataset to generate chart from</param>
    /// <param name="data">The processed data from the dataset</param>
    /// <param name="chartType">The type of chart to generate</param>
    /// <param name="configuration">Optional chart configuration</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>Chart data DTO</returns>
    ChartDataDto GenerateChartData(DataSet dataSet, List<Dictionary<string, object>> data, ChartType chartType, ChartConfigurationDto? configuration, IOperationContext context);

    /// <summary>
    /// Generates comparison chart data for two datasets.
    /// </summary>
    /// <param name="dataSet1">First dataset</param>
    /// <param name="dataSet2">Second dataset</param>
    /// <param name="data1">Processed data from first dataset</param>
    /// <param name="data2">Processed data from second dataset</param>
    /// <param name="chartType">The type of chart to generate</param>
    /// <param name="configuration">Optional chart configuration</param>
    /// <param name="context">Operation context for logging</param>
    /// <returns>Comparison chart data DTO</returns>
    ComparisonChartDto GenerateComparisonChartData(DataSet dataSet1, DataSet dataSet2, List<Dictionary<string, object>> data1, List<Dictionary<string, object>> data2, ChartType chartType, ChartConfigurationDto? configuration, IOperationContext context);

    /// <summary>
    /// Validates chart configuration for a specific chart type.
    /// </summary>
    /// <param name="chartType">The chart type to validate</param>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>True if configuration is valid</returns>
    bool ValidateChartConfiguration(ChartType chartType, ChartConfigurationDto? configuration);
}