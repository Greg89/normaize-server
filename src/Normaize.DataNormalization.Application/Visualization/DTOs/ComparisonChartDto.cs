using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Visualization.DTOs;

/// <summary>
/// Data transfer object for comparison chart data between two datasets.
/// </summary>
public class ComparisonChartDto
{
    public Guid DataSetId1 { get; set; }
    public Guid DataSetId2 { get; set; }
    public ChartType ChartType { get; set; }
    public ChartConfigurationDto? Configuration { get; set; }
    public List<ChartSeriesDto> Series { get; set; } = new();
    public List<string> Labels { get; set; } = new();
    public double SimilarityScore { get; set; }
    public List<string> Differences { get; set; } = new();
    public List<string> CommonColumns { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}
