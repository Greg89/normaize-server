using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Commands.Statistics;

/// <summary>
/// Handler for deleting statistics
/// </summary>
public class DeleteStatisticsCommandHandler : IRequestHandler<DeleteStatisticsCommand>
{
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly ILogger<DeleteStatisticsCommandHandler> _logger;

    public DeleteStatisticsCommandHandler(
        IStatisticsRepository statisticsRepository,
        ILogger<DeleteStatisticsCommandHandler> logger)
    {
        _statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(DeleteStatisticsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting statistics for dataset {DataSetId}", request.DataSetId);

        try
        {
            var statistics = await _statisticsRepository.GetByDataSetIdAsync(request.DataSetId, cancellationToken);
            
            if (statistics == null)
            {
                throw new InvalidOperationException($"Statistics not found for dataset {request.DataSetId}");
            }

            await _statisticsRepository.DeleteAsync(statistics.Id.Value, cancellationToken);
            
            _logger.LogInformation("Successfully deleted statistics for dataset {DataSetId}", request.DataSetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting statistics for dataset {DataSetId}", request.DataSetId);
            throw;
        }
    }
}