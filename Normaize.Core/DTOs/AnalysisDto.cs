namespace Normaize.Core.DTOs;

public enum AnalysisStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public enum AnalysisType
{
    Normalization,
    Comparison,
    Statistical,
    DataCleaning,
    OutlierDetection,
    CorrelationAnalysis,
    TrendAnalysis,
    Custom
}

public class AnalysisDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AnalysisType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public AnalysisStatus Status { get; set; }
    public string? Results { get; set; }
    public string? ErrorMessage { get; set; }
    public int DataSetId { get; set; }
    public int? ComparisonDataSetId { get; set; }
}

public class CreateAnalysisDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AnalysisType Type { get; set; }
    public int DataSetId { get; set; }
    public int? ComparisonDataSetId { get; set; }
    public string? Configuration { get; set; }
}

public class AnalysisResultDto
{
    public int AnalysisId { get; set; }
    public AnalysisStatus Status { get; set; }
    public object? Results { get; set; }
    public string? ErrorMessage { get; set; }
} 