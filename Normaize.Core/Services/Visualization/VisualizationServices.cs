using Normaize.Core.Interfaces;

namespace Normaize.Core.Services.Visualization;

/// <summary>
/// Implementation of IVisualizationServices that provides access to visualization-related services.
/// </summary>
public class VisualizationServices : IVisualizationServices
{
    /// <summary>
    /// Initializes a new instance of the VisualizationServices class.
    /// </summary>
    /// <param name="statisticalCalculationService">The statistical calculation service</param>
    /// <param name="chartGenerationService">The chart generation service</param>
    public VisualizationServices(
        IStatisticalCalculationService statisticalCalculationService,
        IChartGenerationService chartGenerationService)
    {
        StatisticalCalculation = statisticalCalculationService ?? throw new ArgumentNullException(nameof(statisticalCalculationService));
        ChartGeneration = chartGenerationService ?? throw new ArgumentNullException(nameof(chartGenerationService));
    }

    /// <summary>
    /// Gets the statistical calculation service.
    /// </summary>
    public IStatisticalCalculationService StatisticalCalculation { get; }

    /// <summary>
    /// Gets the chart generation service.
    /// </summary>
    public IChartGenerationService ChartGeneration { get; }
}