namespace Normaize.DataNormalization.Application.Common.DTOs;

/// <summary>
/// Data Transfer Object for basic data summary information
/// </summary>
public class DataSummaryDto
{
    public Guid DataSetId { get; set; }
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public int MissingValues { get; set; }
    public int DuplicateRows { get; set; }
    public Dictionary<string, BasicColumnSummaryDto> ColumnSummaries { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
    public DataQualityScoreDto QualityScore { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for basic column summary information
/// </summary>
public class BasicColumnSummaryDto
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

/// <summary>
/// Data Transfer Object for comprehensive statistical summary
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
    public StatisticalInsightsDto Insights { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for detailed column statistics
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
    public double Range { get; set; }
    public double InterquartileRange { get; set; }
    public bool IsSignificantlySkewed { get; set; }
    public bool IsHighKurtosis { get; set; }
}

/// <summary>
/// Data Transfer Object for data quality scoring
/// </summary>
public class DataQualityScoreDto
{
    public int OverallScore { get; set; }
    public int ColumnsWithHighNullRate { get; set; }
    public int HighCardinalityColumns { get; set; }
    public double MissingDataPercentage { get; set; }
    public double DuplicateRowsPercentage { get; set; }
    public bool HasQualityIssues { get; set; }
    public bool HasSeriousIssues { get; set; }
}

/// <summary>
/// Data Transfer Object for statistical insights
/// </summary>
public class StatisticalInsightsDto
{
    public int NumericColumnCount { get; set; }
    public int SkewedColumnCount { get; set; }
    public int HighKurtosisColumnCount { get; set; }
    public List<string> RecommendedTransformations { get; set; } = new();
    public List<string> DataQualityWarnings { get; set; } = new();
}