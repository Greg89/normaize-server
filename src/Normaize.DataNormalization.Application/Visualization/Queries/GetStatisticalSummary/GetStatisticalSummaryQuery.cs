using MediatR;
using Normaize.DataNormalization.Application.Visualization.DTOs;

namespace Normaize.DataNormalization.Application.Visualization.Queries.GetStatisticalSummary;

/// <summary>
/// Query to get statistical summary for a specific dataset.
/// </summary>
public record GetStatisticalSummaryQuery : IRequest<StatisticalSummaryDto>
{
    public Guid DataSetId { get; init; }
    public string UserId { get; init; }

    public GetStatisticalSummaryQuery(Guid dataSetId, string userId)
    {
        DataSetId = dataSetId;
        UserId = userId;
    }
}
