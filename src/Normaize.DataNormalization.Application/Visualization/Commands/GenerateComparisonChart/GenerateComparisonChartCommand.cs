using MediatR;
using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Visualization.Commands.GenerateComparisonChart;

/// <summary>
/// Command to generate a comparison chart between two datasets.
/// </summary>
public record GenerateComparisonChartCommand : IRequest<ComparisonChartDto>
{
    public Guid DataSetId1 { get; init; }
    public Guid DataSetId2 { get; init; }
    public ChartType ChartType { get; init; }
    public ChartConfigurationDto? Configuration { get; init; }
    public string UserId { get; init; }

    public GenerateComparisonChartCommand(Guid dataSetId1, Guid dataSetId2, ChartType chartType, string userId, ChartConfigurationDto? configuration = null)
    {
        DataSetId1 = dataSetId1;
        DataSetId2 = dataSetId2;
        ChartType = chartType;
        UserId = userId;
        Configuration = configuration;
    }
}
