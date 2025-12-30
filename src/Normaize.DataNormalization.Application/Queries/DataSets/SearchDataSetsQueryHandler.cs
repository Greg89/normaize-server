using MediatR;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Queries.DataSets;

/// <summary>
/// Handler for searching datasets
/// </summary>
public class SearchDataSetsQueryHandler : IRequestHandler<SearchDataSetsQuery, IReadOnlyList<DataSetDto>>
{
    private readonly IDataSetRepository _dataSetRepository;

    public SearchDataSetsQueryHandler(IDataSetRepository dataSetRepository)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
    }

    public async Task<IReadOnlyList<DataSetDto>> Handle(SearchDataSetsQuery request, CancellationToken cancellationToken)
    {
        // Get all datasets for user
        var dataSets = await _dataSetRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        // Filter by search query (case-insensitive)
        var searchLower = request.SearchTerm.ToLowerInvariant();
        var filteredDataSets = dataSets
            .Where(ds => !ds.IsDeleted &&
                         (ds.Name.ToLowerInvariant().Contains(searchLower) ||
                          (ds.Description != null && ds.Description.ToLowerInvariant().Contains(searchLower))))
            .ToList();

        // Apply pagination
        var paginatedDataSets = filteredDataSets
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return paginatedDataSets
            .Select(MapToDto)
            .ToList();
    }

    private static DataSetDto MapToDto(Domain.Entities.DataSet dataSet)
    {
        return new DataSetDto(
            dataSet.Id,
            dataSet.Name,
            dataSet.Description,
            dataSet.UserId,
            dataSet.FileInfo?.FileName ?? string.Empty,
            dataSet.FileInfo?.FilePath ?? string.Empty,
            dataSet.FileInfo?.FileSize ?? 0,
            dataSet.FileInfo?.FileType.Value ?? "Unknown",
            dataSet.FileInfo?.StorageProvider.Value ?? "Local",
            dataSet.Statistics?.RowCount ?? 0,
            dataSet.Statistics?.ColumnCount ?? 0,
            dataSet.ProcessingStatus?.IsProcessed ?? false,
            dataSet.ProcessingStatus?.ProcessedAt,
            dataSet.UploadedAt,
            dataSet.RetentionExpiryDate,
            dataSet.IsDeleted,
            dataSet.DeletedAt,
            dataSet.DeletedBy,
            dataSet.LastModifiedAt,
            dataSet.LastModifiedBy);
    }
}
