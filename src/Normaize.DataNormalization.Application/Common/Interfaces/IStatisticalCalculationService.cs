using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Domain.Aggregates;

namespace Normaize.DataNormalization.Application.Common.Interfaces;

/// <summary>
/// Service interface for statistical calculations
/// </summary>
public interface IStatisticalCalculationService
{
    /// <summary>
    /// Generates basic data summary statistics
    /// </summary>
    Task<Domain.Aggregates.Statistics> GenerateDataSummaryAsync(
        Domain.Entities.DataSet dataSet,
        List<Dictionary<string, object?>> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates comprehensive statistical summary
    /// </summary>
    Task<Domain.Aggregates.Statistics> GenerateStatisticalSummaryAsync(
        Domain.Entities.DataSet dataSet,
        List<Dictionary<string, object?>> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates correlation matrix for numeric columns
    /// </summary>
    Task<Dictionary<string, double>> CalculateCorrelationMatrixAsync(
        Dictionary<string, List<double>> numericColumns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects outliers in numeric data
    /// </summary>
    Task<(List<string> outlierColumns, List<int> outlierIndices)> DetectOutliersAsync(
        Dictionary<string, List<double>> numericColumns,
        CancellationToken cancellationToken = default);
}