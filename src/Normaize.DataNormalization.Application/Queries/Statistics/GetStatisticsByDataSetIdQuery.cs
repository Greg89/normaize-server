using MediatR;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.Application.Queries.Statistics;

/// <summary>
/// Query to get statistics by dataset ID
/// </summary>
public record GetStatisticsByDataSetIdQuery(Guid DataSetId) : IRequest<StatisticsDto?>;