using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Models;

public class DataSet
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string FileType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    public string? Schema { get; set; }
    
    public int RowCount { get; set; }
    
    public int ColumnCount { get; set; }
    
    public string? PreviewData { get; set; }
    
    public bool IsProcessed { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    public List<Analysis> Analyses { get; set; } = new();
} 