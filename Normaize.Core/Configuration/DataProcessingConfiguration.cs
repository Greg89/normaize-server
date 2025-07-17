using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Configuration;

public class DataProcessingConfiguration
{
    public const string SectionName = "DataProcessing";
    
    [Range(1000, 1000000, ErrorMessage = "MaxRowsPerDataset must be between 1,000 and 1,000,000")]
    public int MaxRowsPerDataset { get; set; } = 10000;
    
    [Range(1, 1000, ErrorMessage = "MaxColumnsPerDataset must be between 1 and 1,000")]
    public int MaxColumnsPerDataset { get; set; } = 100;
    
    [Range(1, 100, ErrorMessage = "MaxPreviewRows must be between 1 and 100")]
    public int MaxPreviewRows { get; set; } = 100;
    
    public bool EnableDataValidation { get; set; } = true;
    
    public bool EnableSchemaInference { get; set; } = true;
    
    [Range(1, 100, ErrorMessage = "MaxProcessingTimeSeconds must be between 1 and 100")]
    public int MaxProcessingTimeSeconds { get; set; } = 30;
} 