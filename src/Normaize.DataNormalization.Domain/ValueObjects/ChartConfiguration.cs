namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Configuration settings for chart generation and display.
/// </summary>
public class ChartConfiguration
{
    public string? Title { get; init; }
    public string? XAxisLabel { get; init; }
    public string? YAxisLabel { get; init; }
    public bool ShowLegend { get; init; } = true;
    public bool ShowGrid { get; init; } = true;
    public string? ColorScheme { get; init; }
    public int? MaxDataPoints { get; init; }
    public DataAggregationType? AggregationType { get; init; }
    public Dictionary<string, object>? CustomOptions { get; init; }

    private ChartConfiguration() { }

    public static ChartConfiguration Create(
        string? title = null,
        string? xAxisLabel = null,
        string? yAxisLabel = null,
        bool showLegend = true,
        bool showGrid = true,
        string? colorScheme = null,
        int? maxDataPoints = null,
        DataAggregationType? aggregationType = null,
        Dictionary<string, object>? customOptions = null)
    {
        if (maxDataPoints.HasValue && maxDataPoints.Value <= 0)
        {
            throw new ArgumentException("MaxDataPoints must be greater than 0", nameof(maxDataPoints));
        }

        return new ChartConfiguration
        {
            Title = title,
            XAxisLabel = xAxisLabel,
            YAxisLabel = yAxisLabel,
            ShowLegend = showLegend,
            ShowGrid = showGrid,
            ColorScheme = colorScheme,
            MaxDataPoints = maxDataPoints,
            AggregationType = aggregationType,
            CustomOptions = customOptions
        };
    }

    public static ChartConfiguration CreateDefault()
    {
        return Create();
    }
}
