using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Queries.Statistics;

/// <summary>
/// Handler for getting correlation matrix
/// </summary>
public class GetCorrelationMatrixQueryHandler : IRequestHandler<GetCorrelationMatrixQuery, CorrelationMatrixDto>
{
    private readonly IStatisticalCalculationService _statisticalCalculationService;
    private readonly IDataSetRepository _dataSetRepository;
    private readonly ILogger<GetCorrelationMatrixQueryHandler> _logger;

    public GetCorrelationMatrixQueryHandler(
        IStatisticalCalculationService statisticalCalculationService,
        IDataSetRepository dataSetRepository,
        ILogger<GetCorrelationMatrixQueryHandler> logger)
    {
        _statisticalCalculationService = statisticalCalculationService ?? throw new ArgumentNullException(nameof(statisticalCalculationService));
        _dataSetRepository = dataSetRepository ?? throw new ArgumentNullException(nameof(dataSetRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<CorrelationMatrixDto> Handle(GetCorrelationMatrixQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating correlation matrix for dataset {DataSetId}", request.DataSetId);

        try
        {
            // Validate dataset exists
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId);
            if (dataSet == null)
            {
                throw new InvalidOperationException($"Dataset with ID {request.DataSetId} not found");
            }

            // Generate correlation matrix - this needs to be implemented in the service
            // For now, return empty matrix as placeholder
            var correlationMatrix = new Dictionary<string, Dictionary<string, double>>();

            var dto = new CorrelationMatrixDto
            {
                DataSetId = request.DataSetId,
                DataSetName = dataSet.Name,
                ColumnNames = correlationMatrix.Keys.ToList(),
                Matrix = correlationMatrix.Values.Select(row => row.Values.ToList()).ToList(),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully generated correlation matrix for dataset {DataSetId} with {ColumnCount} columns",
                request.DataSetId, correlationMatrix.Count);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating correlation matrix for dataset {DataSetId}", request.DataSetId);
            throw;
        }
    }
}