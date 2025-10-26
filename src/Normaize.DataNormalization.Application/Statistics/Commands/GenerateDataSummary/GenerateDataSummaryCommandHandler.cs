using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Application.Common.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Statistics.Commands.GenerateDataSummary;

/// <summary>
/// Handler for generating basic data summary statistics
/// </summary>
public class GenerateDataSummaryCommandHandler : IRequestHandler<GenerateDataSummaryCommand, DataSummaryDto>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IStatisticalCalculationService _calculationService;
    private readonly IMapper _mapper;
    private readonly ILogger<GenerateDataSummaryCommandHandler> _logger;

    public GenerateDataSummaryCommandHandler(
        IDataSetRepository dataSetRepository,
        IStatisticsRepository statisticsRepository,
        IStatisticalCalculationService calculationService,
        IMapper mapper,
        ILogger<GenerateDataSummaryCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _statisticsRepository = statisticsRepository;
        _calculationService = calculationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DataSummaryDto> Handle(GenerateDataSummaryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating data summary for DataSet {DataSetId} by user {UserId}", 
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
                return CreateEmptyDataSummary(request.DataSetId);
            }

            // Generate statistics using the infrastructure service
            var statistics = await _calculationService.GenerateDataSummaryAsync(
                dataSet, 
                data.ToList(), 
                cancellationToken);

            // Save statistics to repository
            await _statisticsRepository.AddAsync(statistics, cancellationToken);

            var processingTime = DateTime.UtcNow - startTime;
            
            // Convert to DTO
            var result = _mapper.MapToDataSummaryDto(statistics, processingTime);

            _logger.LogInformation("Successfully generated data summary for DataSet {DataSetId} in {ProcessingTime}ms", 
                request.DataSetId, processingTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data summary for DataSet {DataSetId}", request.DataSetId);
            throw;
        }
    }

    private static DataSummaryDto CreateEmptyDataSummary(Guid dataSetId)
    {
        return new DataSummaryDto
        {
            DataSetId = (int)dataSetId.GetHashCode(),
            TotalRows = 0,
            TotalColumns = 0,
            MissingValues = 0,
            DuplicateRows = 0,
            ColumnSummaries = new Dictionary<string, BasicColumnSummaryDto>(),
            GeneratedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.Zero,
            QualityScore = new DataQualityScoreDto
            {
                OverallScore = 100,
                HasQualityIssues = false,
                HasSeriousIssues = false
            }
        };
    }
}