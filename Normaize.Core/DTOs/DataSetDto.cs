namespace Normaize.Core.DTOs;

public class DataSetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? PreviewData { get; set; }
}

public class CreateDataSetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UserId { get; set; } = string.Empty;
}

public class DataSetUploadResponse
{
    public int DataSetId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
} 