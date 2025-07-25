using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Service for validating inputs and configurations for data visualization operations.
/// Extracted from DataVisualizationService to follow single responsibility principle.
/// </summary>
public interface IVisualizationValidationService
{
    /// <summary>
    /// Validates inputs for chart generation operations.
    /// </summary>
    /// <param name="dataSetId">The dataset ID to validate.</param>
    /// <param name="chartType">The chart type to validate.</param>
    /// <param name="configuration">The chart configuration to validate.</param>
    /// <param name="userId">The user ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    void ValidateGenerateChartInputs(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string? userId);

    /// <summary>
    /// Validates inputs for comparison chart generation operations.
    /// </summary>
    /// <param name="dataSetId1">The first dataset ID to validate.</param>
    /// <param name="dataSetId2">The second dataset ID to validate.</param>
    /// <param name="chartType">The chart type to validate.</param>
    /// <param name="configuration">The chart configuration to validate.</param>
    /// <param name="userId">The user ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    void ValidateComparisonChartInputs(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string? userId);

    /// <summary>
    /// Validates inputs for data summary generation operations.
    /// </summary>
    /// <param name="dataSetId">The dataset ID to validate.</param>
    /// <param name="userId">The user ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    void ValidateDataSummaryInputs(int dataSetId, string? userId);

    /// <summary>
    /// Validates inputs for statistical summary generation operations.
    /// </summary>
    /// <param name="dataSetId">The dataset ID to validate.</param>
    /// <param name="userId">The user ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    void ValidateStatisticalSummaryInputs(int dataSetId, string? userId);

    /// <summary>
    /// Validates chart configuration for a specific chart type.
    /// </summary>
    /// <param name="chartType">The chart type to validate configuration for.</param>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>True if the configuration is valid, false otherwise.</returns>
    bool ValidateChartConfiguration(ChartType chartType, ChartConfigurationDto? configuration);
}