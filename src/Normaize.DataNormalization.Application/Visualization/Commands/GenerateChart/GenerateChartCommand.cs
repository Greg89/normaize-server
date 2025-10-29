using MediatR;
using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Visualization.Commands.GenerateChart;

/// <summary>
/// Command to generate a chart for a specific dataset.
/// </summary>
public record GenerateChartCommand : IRequest<ChartDataDto>
{
    public Guid DataSetId { get; init; }
    public ChartType ChartType { get; init; }
    public ChartConfigurationDto? Configuration { get; init; }
    public string UserId { get; init; }

    public GenerateChartCommand(Guid dataSetId, ChartType chartType, string userId, ChartConfigurationDto? configuration = null)
    {
        DataSetId = dataSetId;
        ChartType = chartType;
        UserId = userId;
        Configuration = configuration;
    }
}
