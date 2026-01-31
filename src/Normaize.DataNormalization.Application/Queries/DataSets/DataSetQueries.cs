using MediatR;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.Application.Queries.DataSets;

/// <summary>
/// Query to get a dataset by ID
/// </summary>
public record GetDataSetByIdQuery(
    Guid DataSetId,
    string UserId) : IRequest<DataSetDto?>;

/// <summary>
/// Query to get all datasets for a user
/// </summary>
public record GetDataSetsByUserQuery(
    string UserId,
    int Page = 1,
    int PageSize = 20,
    bool IncludeDeleted = false) : IRequest<PaginatedResult<DataSetDto>>;

/// <summary>
/// Query to search datasets
/// </summary>
public record SearchDataSetsQuery(
    string SearchTerm,
    string UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedResult<DataSetDto>>;

/// <summary>
/// Query to get dataset preview
/// </summary>
public record GetDataSetPreviewQuery(
    Guid DataSetId,
    string UserId,
    int Rows = 10) : IRequest<DataSetPreviewDto?>;

/// <summary>
/// Query to get dataset schema
/// </summary>
public record GetDataSetSchemaQuery(
    Guid DataSetId,
    string UserId) : IRequest<DataSetSchemaDto?>;
