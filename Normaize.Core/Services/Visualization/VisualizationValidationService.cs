using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;

namespace Normaize.Core.Services.Visualization;

/// <summary>
/// Service for validating inputs and configurations for data visualization operations.
/// Extracted from DataVisualizationService to follow single responsibility principle.
/// </summary>
public class VisualizationValidationService : IVisualizationValidationService
{
    private readonly IChartGenerationService _chartGenerationService;

    public VisualizationValidationService(IChartGenerationService chartGenerationService)
    {
        _chartGenerationService = chartGenerationService ?? throw new ArgumentNullException(nameof(chartGenerationService));
    }

    public void ValidateGenerateChartInputs(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string? userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(AppConstants.VisualizationMessages.INVALID_USER_ID, nameof(userId));

        _chartGenerationService.ValidateChartConfiguration(chartType, configuration);
    }

    public void ValidateComparisonChartInputs(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string? userId)
    {
        if (dataSetId1 <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId1));

        if (dataSetId2 <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId2));

        if (dataSetId1 == dataSetId2)
            throw new ArgumentException("Dataset IDs must be different for comparison", nameof(dataSetId2));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(AppConstants.VisualizationMessages.INVALID_USER_ID, nameof(userId));

        _chartGenerationService.ValidateChartConfiguration(chartType, configuration);
    }

    public void ValidateDataSummaryInputs(int dataSetId, string? userId)
    {
        if (dataSetId <= 0)
            throw new ArgumentException(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE, nameof(dataSetId));

        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(AppConstants.VisualizationMessages.INVALID_USER_ID, nameof(userId));
    }

    public void ValidateStatisticalSummaryInputs(int dataSetId, string? userId)
    {
        ValidateDataSummaryInputs(dataSetId, userId);
    }

    public bool ValidateChartConfiguration(ChartType chartType, ChartConfigurationDto? configuration)
    {
        return _chartGenerationService.ValidateChartConfiguration(chartType, configuration);
    }
}