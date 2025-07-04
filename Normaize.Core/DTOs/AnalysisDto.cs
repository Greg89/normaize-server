namespace Normaize.Core.DTOs;

public class AnalysisDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Results { get; set; }
    public string? ErrorMessage { get; set; }
    public int DataSetId { get; set; }
    public int? ComparisonDataSetId { get; set; }
}

public class CreateAnalysisDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public int DataSetId { get; set; }
    public int? ComparisonDataSetId { get; set; }
    public string? Configuration { get; set; }
}

public class AnalysisResultDto
{
    public int AnalysisId { get; set; }
    public string Status { get; set; } = string.Empty;
    public object? Results { get; set; }
    public string? ErrorMessage { get; set; }
} 