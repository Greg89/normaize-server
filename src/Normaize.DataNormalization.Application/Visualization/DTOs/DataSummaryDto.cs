namespace Normaize.DataNormalization.Application.Visualization.DTOs;

/// <summary>
/// Data transfer object for dataset summary information.
/// </summary>
public class DataSummaryDto
{
    public Guid DataSetId { get; set; }
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public int MissingValues { get; set; }
    public int DuplicateRows { get; set; }
    public Dictionary<string, ColumnSummaryDto> ColumnSummaries { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Data transfer object for column summary information.
/// </summary>
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
