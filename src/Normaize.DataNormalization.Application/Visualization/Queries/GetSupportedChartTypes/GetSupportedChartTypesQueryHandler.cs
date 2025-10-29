using MediatR;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Visualization.Queries.GetSupportedChartTypes;

/// <summary>
/// Handler for getting all supported chart types.
/// </summary>
public class GetSupportedChartTypesQueryHandler : IRequestHandler<GetSupportedChartTypesQuery, IEnumerable<ChartType>>
{
    public Task<IEnumerable<ChartType>> Handle(GetSupportedChartTypesQuery request, CancellationToken cancellationToken)
    {
        var chartTypes = Enum.GetValues<ChartType>();
        return Task.FromResult<IEnumerable<ChartType>>(chartTypes);
    }
}
