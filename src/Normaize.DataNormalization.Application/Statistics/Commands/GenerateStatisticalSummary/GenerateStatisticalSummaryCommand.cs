using MediatR;
using Normaize.DataNormalization.Application.Common.DTOs;

namespace Normaize.DataNormalization.Application.Statistics.Commands.GenerateStatisticalSummary;

/// <summary>
/// Command to generate comprehensive statistical summary for a dataset
/// </summary>
public record GenerateStatisticalSummaryCommand(
    Guid DataSetId,
    string UserId) : IRequest<StatisticalSummaryDto>;