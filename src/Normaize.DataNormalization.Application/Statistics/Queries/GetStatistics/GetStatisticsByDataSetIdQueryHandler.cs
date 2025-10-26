using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Statistics.Queries.GetStatistics;

/// <summary>
/// Handler for getting statistics by dataset ID
/// </summary>
public class GetStatisticsByDataSetIdQueryHandler : IRequestHandler<GetStatisticsByDataSetIdQuery, StatisticalSummaryDto?>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetStatisticsByDataSetIdQueryHandler> _logger;

    public GetStatisticsByDataSetIdQueryHandler(
        IDataSetRepository dataSetRepository,
        IStatisticsRepository statisticsRepository,
        IMapper mapper,
        ILogger<GetStatisticsByDataSetIdQueryHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _statisticsRepository = statisticsRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<StatisticalSummaryDto?> Handle(GetStatisticsByDataSetIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting statistics for DataSet {DataSetId} by user {UserId}", 
            request.DataSetId, request.UserId);

        try
        {
            // Verify dataset exists and user has access
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId);
            if (dataSet == null)
            {
                _logger.LogWarning("DataSet {DataSetId} not found", request.DataSetId);
                return null;
            }

            if (dataSet.UserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to access DataSet {DataSetId} owned by {OwnerId}", 
                    request.UserId, request.DataSetId, dataSet.UserId);
                throw new UnauthorizedAccessException("User does not have access to this dataset");
            }

            // Get statistics
            var statistics = await _statisticsRepository.GetByDataSetIdAsync(request.DataSetId, cancellationToken);
            if (statistics == null)
            {
                _logger.LogInformation("No statistics found for DataSet {DataSetId}", request.DataSetId);
                return null;
            }

            // Convert to DTO
            var result = _mapper.MapToStatisticalSummaryDto(statistics, statistics.ProcessingTime);

            _logger.LogInformation("Successfully retrieved statistics for DataSet {DataSetId}", request.DataSetId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for DataSet {DataSetId}", request.DataSetId);
            throw;
        }
    }
}