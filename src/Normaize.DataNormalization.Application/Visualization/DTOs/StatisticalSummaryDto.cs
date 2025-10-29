namespace Normaize.DataNormalization.Application.Visualization.DTOs;

/// <summary>
/// Data transfer object for statistical summary information.
/// </summary>
public class StatisticalSummaryDto
{
    public Guid DataSetId { get; set; }
    public Dictionary<string, ColumnStatisticsDto> ColumnStatistics { get; set; } = new();
    public Dictionary<string, double> CorrelationMatrix { get; set; } = new();
    public List<string> OutlierColumns { get; set; } = new();
    public List<int> OutlierIndices { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Data transfer object for column statistics.
/// </summary>
public class ColumnStatisticsDto
{
    public string ColumnName { get; set; } = string.Empty;
    public double Mean { get; set; }
    public double Median { get; set; }
    public double StandardDeviation { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Q1 { get; set; }
    public double Q2 { get; set; }
    public double Q3 { get; set; }
    public double Skewness { get; set; }
    public double Kurtosis { get; set; }
    public int OutlierCount { get; set; }
}
