using MediatR;

namespace Normaize.DataNormalization.Application.Commands.Statistics;

/// <summary>
/// Command to delete statistics for a dataset
/// </summary>
public record DeleteStatisticsCommand(Guid DataSetId) : IRequest;