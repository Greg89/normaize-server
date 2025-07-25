using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Microsoft.Extensions.Options;

namespace Normaize.Core.Services.Visualization;

/// <summary>
/// Service for generating charts from dataset data.
/// Extracted from DataVisualizationService to follow single responsibility principle.
/// </summary>
public class ChartGenerationService : IChartGenerationService
{
    private readonly IStatisticalCalculationService _statisticalCalculationService;
    private readonly DataVisualizationOptions _options;
    private readonly IDataProcessingInfrastructure _infrastructure;

    public ChartGenerationService(
        IStatisticalCalculationService statisticalCalculationService,
        IOptions<DataVisualizationOptions> options,
        IDataProcessingInfrastructure infrastructure)
    {
        ArgumentNullException.ThrowIfNull(statisticalCalculationService);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(infrastructure);

        _statisticalCalculationService = statisticalCalculationService;
        _options = options.Value;
        _infrastructure = infrastructure;
    }

    public ChartDataDto GenerateChartData(DataSet dataSet, List<Dictionary<string, object>> data, ChartType chartType, ChartConfigurationDto? configuration, IOperationContext context)
    {
        if (data.Count == 0)
        {
            _infrastructure.StructuredLogging.LogStep(context, "No data available for chart generation", new Dictionary<string, object>
            {
                [AppConstants.DataStructures.DATASETID] = dataSet.Id,
                [AppConstants.DataStructures.CHART_TYPE] = chartType.ToString()
            });
            return new ChartDataDto
            {
                DataSetId = dataSet.Id,
                ChartType = chartType,
                Labels = [],
                Series = [],
                Configuration = configuration
            };
        }

        var maxDataPoints = configuration?.MaxDataPoints ?? _options.MaxDataPoints;
        var limitedData = data.Take(maxDataPoints).ToList();

        var labels = new List<string>();
        var series = new List<ChartSeriesDto>();

        switch (chartType)
        {
            case ChartType.Bar:
            case ChartType.Line:
            case ChartType.Area:
                GenerateBarLineAreaChart(limitedData, labels, series, context);
                break;

            case ChartType.Pie:
            case ChartType.Donut:
                GeneratePieDonutChart(limitedData, labels, series, context);
                break;

            case ChartType.Scatter:
            case ChartType.Bubble:
                GenerateScatterBubbleChart(limitedData, labels, series, context);
                break;

            default:
                _infrastructure.StructuredLogging.LogStep(context, "Unsupported chart type", new Dictionary<string, object>
                {
                    [AppConstants.DataStructures.DATASETID] = dataSet.Id,
                    [AppConstants.DataStructures.CHART_TYPE] = chartType.ToString()
                });
                break;
        }

        return new ChartDataDto
        {
            DataSetId = dataSet.Id,
            ChartType = chartType,
            Labels = labels,
            Series = series,
            Configuration = configuration
        };
    }

    public ComparisonChartDto GenerateComparisonChartData(DataSet dataSet1, DataSet dataSet2, List<Dictionary<string, object>> data1, List<Dictionary<string, object>> data2, ChartType chartType, ChartConfigurationDto? configuration, IOperationContext context)
    {
        var chart1 = GenerateChartData(dataSet1, data1, chartType, configuration, context);
        var chart2 = GenerateChartData(dataSet2, data2, chartType, configuration, context);

        return new ComparisonChartDto
        {
            DataSetId1 = dataSet1.Id,
            DataSetId2 = dataSet2.Id,
            ChartType = chartType,
            Series = chart1.Series.Concat(chart2.Series).ToList(),
            Labels = chart1.Labels,
            Configuration = configuration
        };
    }

    public bool ValidateChartConfiguration(ChartType chartType, ChartConfigurationDto? configuration)
    {
        return ValidateChartConfigurationInternal(chartType, configuration);
    }

    #region Private Chart Generation Methods

    private void GenerateBarLineAreaChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, IOperationContext context)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => _statisticalCalculationService.IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _infrastructure.StructuredLogging.LogStep(context, "No numeric columns found for chart. Using fallback data");

            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = AppConstants.DataProcessing.FALLBACK_SERIES_NAME,
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first column as labels (if it's not numeric)
        var labelColumn = columns.FirstOrDefault(col => !numericColumns.Contains(col)) ?? columns[0];
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? AppConstants.Messages.UNKNOWN));

        // Create series for each numeric column
        foreach (var column in numericColumns)
        {
            series.Add(new ChartSeriesDto
            {
                Name = column,
                Data = data.Select(row => ExtractDouble(row.GetValueOrDefault(column))).Cast<object>().ToList()
            });
        }
    }

    private void GeneratePieDonutChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, IOperationContext context)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => _statisticalCalculationService.IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count == 0)
        {
            _infrastructure.StructuredLogging.LogStep(context, "No numeric columns found for pie/donut chart. Using fallback data");

            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = AppConstants.DataProcessing.FALLBACK_SERIES_NAME,
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first column as labels
        var labelColumn = columns[0];
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? AppConstants.Messages.UNKNOWN));

        // Use first numeric column as data
        var dataColumn = numericColumns[0];
        series.Add(new ChartSeriesDto
        {
            Name = dataColumn,
            Data = data.Select(row => ExtractDouble(row.GetValueOrDefault(dataColumn))).Cast<object>().ToList()
        });
    }

    private void GenerateScatterBubbleChart(List<Dictionary<string, object>> data, List<string> labels, List<ChartSeriesDto> series, IOperationContext context)
    {
        if (data.Count == 0) return;

        var columns = data[0].Keys.ToList();
        var numericColumns = columns.Where(col => _statisticalCalculationService.IsNumericColumn(data.Select(row => row.GetValueOrDefault(col)).ToList())).ToList();

        if (numericColumns.Count < 2)
        {
            _infrastructure.StructuredLogging.LogStep(context, "Insufficient numeric columns for scatter/bubble chart. Using fallback data");

            // Fallback: use row indices as labels and data
            labels.AddRange(data.Select((_, index) => $"Row {index + 1}"));
            series.Add(new ChartSeriesDto
            {
                Name = AppConstants.DataProcessing.FALLBACK_SERIES_NAME,
                Data = data.Select((_, index) => (object)(index + 1)).ToList()
            });
            return;
        }

        // Use first column as labels
        var labelColumn = columns[0];
        labels.AddRange(data.Select(row => row.GetValueOrDefault(labelColumn)?.ToString() ?? AppConstants.Messages.UNKNOWN));

        // Use first two numeric columns as X and Y
        var xColumn = numericColumns[0];
        var yColumn = numericColumns[1];

        series.Add(new ChartSeriesDto
        {
            Name = $"{xColumn} vs {yColumn}",
            Data = data.Select(row => new
            {
                X = ExtractDouble(row.GetValueOrDefault(xColumn)),
                Y = ExtractDouble(row.GetValueOrDefault(yColumn))
            }).Cast<object>().ToList()
        });
    }

    #endregion

    #region Private Utility Methods

    private static bool ValidateChartConfigurationInternal(ChartType chartType, ChartConfigurationDto? configuration)
    {
        if (configuration == null) return true;

        // Validate max data points
        if (configuration.MaxDataPoints.HasValue && configuration.MaxDataPoints.Value <= 0)
        {
            throw new ArgumentException("MaxDataPoints must be greater than 0", nameof(configuration));
        }

        // Validate chart-specific configurations
        switch (chartType)
        {
            case ChartType.Pie:
            case ChartType.Donut:
                if (!string.IsNullOrEmpty(configuration.XAxisLabel) || !string.IsNullOrEmpty(configuration.YAxisLabel))
                {
                    // Log warning but don't throw - this is a validation warning, not an error
                }
                break;

            case ChartType.Scatter:
            case ChartType.Bubble:
                if (string.IsNullOrEmpty(configuration.XAxisLabel) || string.IsNullOrEmpty(configuration.YAxisLabel))
                {
                    // Log warning but don't throw - this is a validation warning, not an error
                }
                break;
        }

        return true;
    }

    private static double ExtractDouble(object? value, double fallback = 0)
    {
        if (value == null) return fallback;

        return value switch
        {
            double d => d,
            int i => i,
            long l => l,
            float f => f,
            decimal dec => (double)dec,
            string s when double.TryParse(s, out var result) => result,
            _ => fallback
        };
    }

    #endregion
}