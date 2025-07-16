using System.ComponentModel.DataAnnotations;
using Normaize.Core.DTOs;

namespace Normaize.Core.Models;

public class Analysis
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Required]
    public AnalysisType Type { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    public AnalysisStatus Status { get; set; } = AnalysisStatus.Pending;
    
    public string? Configuration { get; set; } // JSON configuration
    
    public string? Results { get; set; } // JSON results
    
    public string? ErrorMessage { get; set; }
    
    public int DataSetId { get; set; }
    public DataSet DataSet { get; set; } = null!;
    
    public int? ComparisonDataSetId { get; set; }
    public DataSet? ComparisonDataSet { get; set; }
    
    // Soft delete properties
    public bool IsDeleted { get; set; } = false;
    
    public DateTime? DeletedAt { get; set; }
    
    public string? DeletedBy { get; set; }
} 