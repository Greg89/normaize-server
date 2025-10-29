using MediatR;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Repositories;
using System.Text.Json;

namespace Normaize.DataNormalization.Application.DataSets.Queries.GetDataSetPreview;

/// <summary>
/// Handler for retrieving dataset preview data.
/// </summary>
public class GetDataSetPreviewQueryHandler : IRequestHandler<GetDataSetPreviewQuery, DataSetPreviewDto?>
{
    private const int MaxPreviewRows = 1000;
    private readonly IDataSetRepository _dataSetRepository;

    public GetDataSetPreviewQueryHandler(IDataSetRepository dataSetRepository)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
    }

    public async Task<DataSetPreviewDto?> Handle(GetDataSetPreviewQuery request, CancellationToken cancellationToken)
    {
        // Validate inputs
        if (request.DataSetId == Guid.Empty)
            throw new ArgumentException("DataSet ID cannot be empty.", nameof(request.DataSetId));

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(request.UserId));

        if (request.Rows <= 0)
            throw new ArgumentException("Rows must be greater than zero.", nameof(request.Rows));

        if (request.Rows > MaxPreviewRows)
            throw new ArgumentException($"Rows cannot exceed {MaxPreviewRows}.", nameof(request.Rows));

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

        // Check if preview data exists
        if (string.IsNullOrWhiteSpace(dataSet.PreviewData))
            return null;

        // Deserialize preview data
        try
        {
            // Deserialize as an intermediate object matching the stored format
            var previewData = JsonSerializer.Deserialize<PreviewDataDto>(
                dataSet.PreviewData,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (previewData == null)
                return null;

            // Limit rows to requested amount
            var limitedRows = previewData.Rows?.Take(request.Rows).ToList() ?? new List<Dictionary<string, object>>();
            
            return new DataSetPreviewDto(
                DataSetId: request.DataSetId,
                PreviewRowCount: request.Rows,
                TotalRows: previewData.TotalRows,
                Rows: limitedRows,
                Columns: previewData.Columns ?? new List<string>());
        }
        catch (JsonException)
        {
            // Invalid JSON format
            return null;
        }
    }

    // Internal DTO for deserializing stored preview data
    private class PreviewDataDto
    {
        public List<string>? Columns { get; set; }
        public List<Dictionary<string, object>>? Rows { get; set; }
        public int TotalRows { get; set; }
        public int PreviewRowCount { get; set; }
        public int MaxPreviewRows { get; set; }
    }
}
