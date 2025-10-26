using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Statistics.Commands.GenerateStatisticalSummary;

/// <summary>
/// Handler for generating comprehensive statistical summary
/// </summary>
public class GenerateStatisticalSummaryCommandHandler : IRequestHandler<GenerateStatisticalSummaryCommand, StatisticalSummaryDto>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IStatisticalCalculationService _calculationService;
    private readonly IMapper _mapper;
    private readonly ILogger<GenerateStatisticalSummaryCommandHandler> _logger;

    public GenerateStatisticalSummaryCommandHandler(
        IDataSetRepository dataSetRepository,
        IStatisticsRepository statisticsRepository,
        IStatisticalCalculationService calculationService,
        IMapper mapper,
        ILogger<GenerateStatisticalSummaryCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _statisticsRepository = statisticsRepository;
        _calculationService = calculationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<StatisticalSummaryDto> Handle(GenerateStatisticalSummaryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating statistical summary for DataSet {DataSetId} by user {UserId}", 
            request.DataSetId, request.UserId);

        var startTime = DateTime.UtcNow;

        try
        {
            // Get the dataset
            var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId);
            if (dataSet == null)
            {
                _logger.LogWarning("DataSet {DataSetId} not found", request.DataSetId);
                throw new ArgumentException($"DataSet with ID {request.DataSetId} not found");
            }

            // Validate user access
            if (dataSet.UserId != request.UserId)
            {
                _logger.LogWarning("User {UserId} attempted to access DataSet {DataSetId} owned by {OwnerId}", 
                    request.UserId, request.DataSetId, dataSet.UserId);
                throw new UnauthorizedAccessException("User does not have access to this dataset");
            }

            // Get dataset data - placeholder for now since GetDataAsync doesn't exist in current interface
            var data = new List<Dictionary<string, object?>>();  // TODO: Implement data retrieval
            if (!data.Any())
            {
                _logger.LogInformation("No data found for DataSet {DataSetId}, returning empty summary", request.DataSetId);
                return CreateEmptyStatisticalSummary(request.DataSetId);
            }

            // Generate comprehensive statistics using the infrastructure service
            var statistics = await _calculationService.GenerateStatisticalSummaryAsync(
                dataSet, 
                data.ToList(), 
                cancellationToken);

            // Save statistics to repository
            await _statisticsRepository.AddAsync(statistics, cancellationToken);

            var processingTime = DateTime.UtcNow - startTime;
            
            // Convert to DTO
            var result = _mapper.MapToStatisticalSummaryDto(statistics, processingTime);

            _logger.LogInformation("Successfully generated statistical summary for DataSet {DataSetId} in {ProcessingTime}ms", 
                request.DataSetId, processingTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating statistical summary for DataSet {DataSetId}", request.DataSetId);
            throw;
        }
    }

    private static StatisticalSummaryDto CreateEmptyStatisticalSummary(Guid dataSetId)
    {
        return new StatisticalSummaryDto
        {
            DataSetId = (int)dataSetId.GetHashCode(),
            ColumnStatistics = new Dictionary<string, ColumnStatisticsDto>(),
            CorrelationMatrix = new Dictionary<string, double>(),
            OutlierColumns = new List<string>(),
            OutlierIndices = new List<int>(),
            GeneratedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.Zero,
            Insights = new StatisticalInsightsDto
            {
                NumericColumnCount = 0,
                SkewedColumnCount = 0,
                HighKurtosisColumnCount = 0,
                RecommendedTransformations = new List<string>(),
                DataQualityWarnings = new List<string>()
            }
        };
    }
}