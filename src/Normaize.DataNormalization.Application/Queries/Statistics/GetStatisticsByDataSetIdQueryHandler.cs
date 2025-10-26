using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Queries.Statistics;

/// <summary>
/// Handler for getting statistics by dataset ID
/// </summary>
public class GetStatisticsByDataSetIdQueryHandler : IRequestHandler<GetStatisticsByDataSetIdQuery, StatisticsDto?>
{
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IStatisticsMapper _statisticsMapper;
    private readonly ILogger<GetStatisticsByDataSetIdQueryHandler> _logger;

    public GetStatisticsByDataSetIdQueryHandler(
        IStatisticsRepository statisticsRepository,
        IStatisticsMapper statisticsMapper,
        ILogger<GetStatisticsByDataSetIdQueryHandler> logger)
    {
        _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
        _statisticsMapper = statisticsMapper ?? throw new ArgumentNullException(nameof(statisticsMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<StatisticsDto?> Handle(GetStatisticsByDataSetIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving statistics for dataset {DataSetId}", request.DataSetId);

        try
        {
            var statistics = await _statisticsRepository.GetByDataSetIdAsync(request.DataSetId, cancellationToken);
            
            if (statistics == null)
            {
                _logger.LogInformation("No statistics found for dataset {DataSetId}", request.DataSetId);
                return null;
            }

            var dto = _statisticsMapper.MapToStatisticsDto(statistics);
            
            _logger.LogInformation("Successfully retrieved statistics for dataset {DataSetId}", request.DataSetId);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for dataset {DataSetId}", request.DataSetId);
            throw;
        }
    }
}