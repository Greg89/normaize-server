using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Visualization.Commands.GenerateChart;

/// <summary>
/// Handler for GenerateChartCommand
/// </summary>
public class GenerateChartCommandHandler : IRequestHandler<GenerateChartCommand, ChartDataDto>
{
    private readonly IDataSetRepository _dataSetRepository;
    private readonly IChartGenerationService _chartGenerationService;
    private readonly ILogger<GenerateChartCommandHandler> _logger;

    public GenerateChartCommandHandler(
        IDataSetRepository dataSetRepository,
        IChartGenerationService chartGenerationService,
        ILogger<GenerateChartCommandHandler> logger)
    {
        _dataSetRepository = dataSetRepository;
        _chartGenerationService = chartGenerationService;
        _logger = logger;
    }

    public async Task<ChartDataDto> Handle(GenerateChartCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // Validate inputs
        ValidateInputs(request);

        // Retrieve dataset
        var dataSet = await _dataSetRepository.GetByIdAsync(request.DataSetId, cancellationToken);

        if (dataSet == null)
        {
            _logger.LogWarning("Dataset {DataSetId} not found", request.DataSetId);
            throw new ArgumentException($"Dataset with ID {request.DataSetId} not found", nameof(request.DataSetId));
        }

        // Verify user access
        try
        {
            dataSet.EnsureUserAccess(request.UserId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "User {UserId} does not have access to dataset {DataSetId}", request.UserId, request.DataSetId);
            throw new UnauthorizedAccessException($"User {request.UserId} does not have access to dataset {request.DataSetId}", ex);
        }

        // Check if dataset is deleted
        if (dataSet.IsDeleted)
        {
            _logger.LogWarning("Attempted to generate chart for deleted dataset {DataSetId}", request.DataSetId);
            throw new ArgumentException($"Dataset {request.DataSetId} has been deleted", nameof(request.DataSetId));
        }

        // Extract and deserialize dataset data
        var data = ExtractDataSetData(dataSet);

        // Convert configuration DTO to domain object
        var configuration = request.Configuration?.ToDomain();

        // Generate chart data
        var chartData = _chartGenerationService.GenerateChartData(
            dataSet,
            data,
            request.ChartType,
            configuration);

        chartData.ProcessingTime = stopwatch.Elapsed;
        chartData.GeneratedAt = DateTime.UtcNow;

        _logger.LogInformation("Successfully generated {ChartType} chart for dataset {DataSetId} in {ElapsedMs}ms",
            request.ChartType, request.DataSetId, stopwatch.ElapsedMilliseconds);

        return chartData;
    }

    private void ValidateInputs(GenerateChartCommand request)
    {
        if (request.DataSetId == Guid.Empty)
        {
            throw new ArgumentException("DataSetId cannot be empty", nameof(request.DataSetId));
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
