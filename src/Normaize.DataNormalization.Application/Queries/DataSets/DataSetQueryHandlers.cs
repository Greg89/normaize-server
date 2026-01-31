using MediatR;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Queries.DataSets;

/// <summary>
/// Handler for getting dataset by ID
/// </summary>
public class GetDataSetByIdQueryHandler : IRequestHandler<GetDataSetByIdQuery, DataSetDto?>
{
    private readonly IDataSetRepository _dataSetRepository;

    public GetDataSetByIdQueryHandler(IDataSetRepository dataSetRepository)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
    }

    public async Task<DataSetDto?> Handle(GetDataSetByIdQuery request, CancellationToken cancellationToken)
    {
        var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);

        if (dataSet == null)
            return null;

        // Ensure user access
        dataSet.EnsureUserAccess(request.UserId);

        return MapToDto(dataSet);
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
            dataSet.FileInfo?.StorageProvider.Value ?? "S3",
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

/// <summary>
/// Handler for getting datasets by user
/// </summary>
public class GetDataSetsByUserQueryHandler : IRequestHandler<GetDataSetsByUserQuery, PaginatedResult<DataSetDto>>
{
    private readonly IDataSetRepository _dataSetRepository;

    public GetDataSetsByUserQueryHandler(IDataSetRepository dataSetRepository)
    {
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
    }

    public async Task<PaginatedResult<DataSetDto>> Handle(GetDataSetsByUserQuery request, CancellationToken cancellationToken)
    {
        var dataSets = request.IncludeDeleted
            ? await _dataSetRepository.GetAllByUserIdAsync(request.UserId, cancellationToken)
            : await _dataSetRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var totalItems = dataSets.Count;

        // Apply pagination
        var paginatedDataSets = dataSets
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = paginatedDataSets
            .Select(MapToDto)
            .ToList();

        return new PaginatedResult<DataSetDto>(items, totalItems);
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
            dataSet.FileInfo?.StorageProvider.Value ?? "S3",
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
