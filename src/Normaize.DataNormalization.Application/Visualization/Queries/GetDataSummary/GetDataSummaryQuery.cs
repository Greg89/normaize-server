using MediatR;
using Normaize.DataNormalization.Application.Visualization.DTOs;

namespace Normaize.DataNormalization.Application.Visualization.Queries.GetDataSummary;

/// <summary>
/// Query to get a data summary for a specific dataset.
/// </summary>
public record GetDataSummaryQuery : IRequest<DataSummaryDto>
{
    public Guid DataSetId { get; init; }
    public string UserId { get; init; }

    public GetDataSummaryQuery(Guid dataSetId, string userId)
    {
        DataSetId = dataSetId;
        UserId = userId;
    }
}
