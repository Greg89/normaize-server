using MediatR;
using Normaize.DataNormalization.Application.Common.DTOs;

namespace Normaize.DataNormalization.Application.Statistics.Commands.GenerateDataSummary;

/// <summary>
/// Command to generate basic data summary statistics for a dataset
/// </summary>
public record GenerateDataSummaryCommand(
    Guid DataSetId,
    string UserId) : IRequest<DataSummaryDto>;