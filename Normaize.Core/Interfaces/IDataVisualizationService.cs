namespace Normaize.Core.Interfaces;

public interface IDataVisualizationService
{
    Task<object> GenerateChartAsync(int dataSetId, string chartType, string? configuration);
    Task<object> GenerateComparisonChartAsync(int dataSetId1, int dataSetId2, string chartType, string? configuration);
    Task<object> GetDataSummaryAsync(int dataSetId);
    Task<object> GetStatisticalSummaryAsync(int dataSetId);
} 