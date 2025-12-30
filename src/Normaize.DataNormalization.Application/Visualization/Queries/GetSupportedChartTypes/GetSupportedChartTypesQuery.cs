using MediatR;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Visualization.Queries.GetSupportedChartTypes;

/// <summary>
/// Query to get all supported chart types.
/// </summary>
public record GetSupportedChartTypesQuery : IRequest<IEnumerable<ChartType>>
{
}
