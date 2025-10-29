using MediatR;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.Application.Queries.Statistics;

/// <summary>
/// Query to get correlation matrix for a dataset
/// </summary>
public record GetCorrelationMatrixQuery(Guid DataSetId) : IRequest<CorrelationMatrixDto>;