using MediatR;
using Normaize.DataNormalization.Application.Common.DTOs;

namespace Normaize.DataNormalization.Application.Statistics.Queries.GetStatistics;

/// <summary>
/// Query to get statistics by dataset ID
/// </summary>
public record GetStatisticsByDataSetIdQuery(
    Guid DataSetId,
    string UserId) : IRequest<StatisticalSummaryDto?>;