using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Visualization.DTOs;

/// <summary>
/// Data transfer object for chart data.
/// </summary>
public class ChartDataDto
{
    public Guid DataSetId { get; set; }
    public ChartType ChartType { get; set; }
    public ChartConfigurationDto? Configuration { get; set; }
    public List<ChartSeriesDto> Series { get; set; } = new();
    public List<string> Labels { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Data transfer object for chart configuration.
/// </summary>
public class ChartConfigurationDto
{
    public string? Title { get; set; }
    public string? XAxisLabel { get; set; }
    public string? YAxisLabel { get; set; }
    public bool ShowLegend { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public string? ColorScheme { get; set; }
    public int? MaxDataPoints { get; set; }
    public DataAggregationType? AggregationType { get; set; }
    public Dictionary<string, object>? CustomOptions { get; set; }

    /// <summary>
    /// Converts DTO to domain value object.
    /// </summary>
    public ChartConfiguration ToDomain()
    {
        return ChartConfiguration.Create(
            title: Title,
            xAxisLabel: XAxisLabel,
            yAxisLabel: YAxisLabel,
            showLegend: ShowLegend,
            showGrid: ShowGrid,
            colorScheme: ColorScheme,
            maxDataPoints: MaxDataPoints,
            aggregationType: AggregationType,
            customOptions: CustomOptions
        );
    }
}

/// <summary>
/// Data transfer object for chart series.
/// </summary>
public class ChartSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public List<object> Data { get; set; } = new();
    public string? Color { get; set; }
    public string? Type { get; set; }
}
