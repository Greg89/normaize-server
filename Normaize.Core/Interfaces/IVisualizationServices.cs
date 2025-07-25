using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Grouped interface for visualization-related services.
/// Provides access to statistical calculations and chart generation functionality.
/// </summary>
public interface IVisualizationServices
{
    /// <summary>
    /// Statistical calculation service for data analysis and computations.
    /// </summary>
    IStatisticalCalculationService StatisticalCalculation { get; }

    /// <summary>
    /// Chart generation service for creating various chart types and data transformation.
    /// </summary>
    IChartGenerationService ChartGeneration { get; }
}