using MediatR;

namespace Normaize.DataNormalization.Application.DataSets.Queries.GetDataSetSchema;

/// <summary>
/// Query to retrieve schema information for a dataset.
/// </summary>
public record GetDataSetSchemaQuery(
    Guid DataSetId,
    string UserId) : IRequest<object?>;
