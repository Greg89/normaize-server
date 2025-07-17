using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

public interface IDataVisualizationService
{
    Task<ChartDataDto> GenerateChartAsync(int dataSetId, ChartType chartType, ChartConfigurationDto? configuration, string userId);
    Task<ComparisonChartDto> GenerateComparisonChartAsync(int dataSetId1, int dataSetId2, ChartType chartType, ChartConfigurationDto? configuration, string userId);
    Task<DataSummaryDto> GetDataSummaryAsync(int dataSetId, string userId);
    Task<StatisticalSummaryDto> GetStatisticalSummaryAsync(int dataSetId, string userId);
    Task<IEnumerable<ChartType>> GetSupportedChartTypesAsync();
    bool ValidateChartConfiguration(ChartType chartType, ChartConfigurationDto? configuration);
} 