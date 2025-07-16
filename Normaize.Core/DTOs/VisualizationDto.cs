namespace Normaize.Core.DTOs;

public enum ChartType
{
    Bar,
    Line,
    Pie,
    Scatter,
    Area,
    Histogram,
    BoxPlot,
    Heatmap,
    Bubble,
    Radar,
    Donut,
    Column
}

public enum DataAggregationType
{
    Sum,
    Average,
    Count,
    Min,
    Max,
    Median,
    StandardDeviation
}

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
}

public class ChartDataDto
{
    public int DataSetId { get; set; }
    public ChartType ChartType { get; set; }
    public ChartConfigurationDto? Configuration { get; set; }
    public List<ChartSeriesDto> Series { get; set; } = new();
    public List<string> Labels { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

public class ChartSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public List<object> Data { get; set; } = new();
    public string? Color { get; set; }
    public string? Type { get; set; }
}

public class ComparisonChartDto
{
    public int DataSetId1 { get; set; }
    public int DataSetId2 { get; set; }
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

public class DataSummaryDto
{
    public int DataSetId { get; set; }
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public int MissingValues { get; set; }
    public int DuplicateRows { get; set; }
    public Dictionary<string, ColumnSummaryDto> ColumnSummaries { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

public class ColumnSummaryDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int NonNullCount { get; set; }
    public int NullCount { get; set; }
    public int UniqueCount { get; set; }
    public List<object> SampleValues { get; set; } = new();
    public object? MinValue { get; set; }
    public object? MaxValue { get; set; }
    public double? Mean { get; set; }
    public double? Median { get; set; }
    public double? StandardDeviation { get; set; }
}

public class StatisticalSummaryDto
{
    public int DataSetId { get; set; }
    public Dictionary<string, ColumnStatisticsDto> ColumnStatistics { get; set; } = new();
    public Dictionary<string, double> CorrelationMatrix { get; set; } = new();
    public List<string> OutlierColumns { get; set; } = new();
    public List<int> OutlierIndices { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

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

public class VisualizationErrorDto
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
} 