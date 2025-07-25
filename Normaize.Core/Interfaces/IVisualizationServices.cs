using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

/// <summary>
/// Grouped interface for visualization-related services.
/// Provides access to statistical calculations, chart generation, and cache management functionality.
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

    /// <summary>
    /// Cache management service for handling cache operations in visualization services.
    /// </summary>
    ICacheManagementService CacheManagement { get; }
}