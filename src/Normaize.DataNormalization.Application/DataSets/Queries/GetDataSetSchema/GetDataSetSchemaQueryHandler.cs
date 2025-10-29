using MediatR;
using Normaize.DataNormalization.Domain.Repositories;
using System.Text.Json;

namespace Normaize.DataNormalization.Application.DataSets.Queries.GetDataSetSchema;

/// <summary>
/// Handler for retrieving dataset schema information.
/// </summary>
public class GetDataSetSchemaQueryHandler : IRequestHandler<GetDataSetSchemaQuery, object?>
{
    private readonly IDataSetRepository _dataSetRepository;

    public GetDataSetSchemaQueryHandler(IDataSetRepository dataSetRepository)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
    }

    public async Task<object?> Handle(GetDataSetSchemaQuery request, CancellationToken cancellationToken)
    {
        // Validate inputs
        if (request.DataSetId == Guid.Empty)
            throw new ArgumentException("DataSet ID cannot be empty.", nameof(request.DataSetId));

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(request.UserId));

        // Retrieve dataset
        var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);
        if (dataSet == null)
            throw new InvalidOperationException($"DataSet with ID {request.DataSetId} not found.");

        // Verify access control
        if (dataSet.UserId != request.UserId)
            throw new UnauthorizedAccessException($"User {request.UserId} is not authorized to access DataSet {request.DataSetId}.");

        // Check if dataset is processed
        if (!dataSet.IsProcessed)
            return null;

        // Check if schema exists
        if (string.IsNullOrWhiteSpace(dataSet.Schema))
            return null;

        // Deserialize schema - try as List<string> first
        try
        {
            var schemaList = JsonSerializer.Deserialize<List<string>>(
                dataSet.Schema,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (schemaList != null)
                return schemaList;

            // If that fails, try as generic object
            return JsonSerializer.Deserialize<object>(
                dataSet.Schema,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            // Invalid JSON format
            return null;
        }
    }
}
