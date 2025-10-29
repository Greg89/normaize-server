using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Visualization.Commands.GenerateComparisonChart;

/// <summary>
/// Handler for GenerateComparisonChartCommand
/// </summary>
public class GenerateComparisonChartCommandHandler : IRequestHandler<GenerateComparisonChartCommand, ComparisonChartDto>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IChartGenerationService _chartGenerationService;
    private readonly ILogger<GenerateComparisonChartCommandHandler> _logger;

    public GenerateComparisonChartCommandHandler(
        IDataSetRepository dataSetRepository,
        IChartGenerationService chartGenerationService,
        ILogger<GenerateComparisonChartCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _chartGenerationService = chartGenerationService;
        _logger = logger;
    }

    public async Task<ComparisonChartDto> Handle(GenerateComparisonChartCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Validate inputs
        ValidateInputs(request);

        // Retrieve both datasets
        var dataSet1 = await _dataSetRepository.GetByIdAsync(request.DataSetId1, cancellationToken);
        var dataSet2 = await _dataSetRepository.GetByIdAsync(request.DataSetId2, cancellationToken);

        if (dataSet1 == null)
        {
            _logger.LogWarning("Dataset {DataSetId} not found", request.DataSetId1);
            throw new ArgumentException($"Dataset with ID {request.DataSetId1} not found", nameof(request.DataSetId1));
        }

        if (dataSet2 == null)
        {
            _logger.LogWarning("Dataset {DataSetId} not found", request.DataSetId2);
            throw new ArgumentException($"Dataset with ID {request.DataSetId2} not found", nameof(request.DataSetId2));
        }

        // Verify user access to both datasets
        try
        {
            dataSet1.EnsureUserAccess(request.UserId);
            dataSet2.EnsureUserAccess(request.UserId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "User {UserId} does not have access to datasets", request.UserId);
            throw new UnauthorizedAccessException($"User {request.UserId} does not have access to one or both datasets", ex);
        }

        // Check if datasets are deleted
        if (dataSet1.IsDeleted || dataSet2.IsDeleted)
        {
            _logger.LogWarning("Attempted to generate comparison chart with deleted dataset");
            throw new ArgumentException("Cannot generate comparison chart for deleted datasets");
        }

        // Extract and deserialize dataset data
        var data1 = ExtractDataSetData(dataSet1);
        var data2 = ExtractDataSetData(dataSet2);

        // Convert configuration DTO to domain object
        var configuration = request.Configuration?.ToDomain();

        // Generate comparison chart data
        var comparisonChart = _chartGenerationService.GenerateComparisonChartData(
            dataSet1,
            dataSet2,
            data1,
            data2,
            request.ChartType,
            configuration);

        comparisonChart.ProcessingTime = stopwatch.Elapsed;
        comparisonChart.GeneratedAt = DateTime.UtcNow;

        _logger.LogInformation("Successfully generated comparison {ChartType} chart for datasets {DataSetId1} and {DataSetId2} in {ElapsedMs}ms",
            request.ChartType, request.DataSetId1, request.DataSetId2, stopwatch.ElapsedMilliseconds);

        return comparisonChart;
    }

    private void ValidateInputs(GenerateComparisonChartCommand request)
    {
        if (request.DataSetId1 == Guid.Empty)
        {
            throw new ArgumentException("DataSetId1 cannot be empty", nameof(request.DataSetId1));
        }

        if (request.DataSetId2 == Guid.Empty)
        {
            throw new ArgumentException("DataSetId2 cannot be empty", nameof(request.DataSetId2));
        }

        if (request.DataSetId1 == request.DataSetId2)
        {
            throw new ArgumentException("Dataset IDs must be different for comparison", nameof(request.DataSetId2));
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId cannot be empty", nameof(request.UserId));
        }

        // Validate configuration if provided
        if (request.Configuration != null)
        {
            var config = request.Configuration.ToDomain();
            _chartGenerationService.ValidateChartConfiguration(request.ChartType, config);
        }
    }

    private List<Dictionary<string, object?>> ExtractDataSetData(Domain.Entities.DataSet dataSet)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dataSet.ProcessedData))
            {
                _logger.LogWarning("Dataset {DataSetId} has no processed data", dataSet.Id);
                return new List<Dictionary<string, object?>>();
            }

            var data = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(dataSet.ProcessedData);

            if (data == null)
            {
                _logger.LogWarning("Failed to deserialize dataset {DataSetId} data", dataSet.Id);
                return new List<Dictionary<string, object?>>();
            }

            _logger.LogDebug("Extracted {RowCount} rows from dataset {DataSetId}", data.Count, dataSet.Id);
            return data;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse dataset {DataSetId} JSON data", dataSet.Id);
            throw new InvalidOperationException($"Failed to parse dataset {dataSet.Id} data: {ex.Message}", ex);
        }
    }
}
