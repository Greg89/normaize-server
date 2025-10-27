using MediatR;
using Normaize.DataNormalization.Application.DTOs;

namespace Normaize.DataNormalization.Application.DataSets.Queries.GetDataSetPreview;

/// <summary>
/// Query to retrieve preview data for a dataset.
/// </summary>
public record GetDataSetPreviewQuery(
    Guid DataSetId,
    int Rows,
    string UserId) : IRequest<DataSetPreviewDto?>;
