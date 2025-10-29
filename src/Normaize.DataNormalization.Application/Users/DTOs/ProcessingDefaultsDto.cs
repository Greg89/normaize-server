namespace Normaize.DataNormalization.Application.Users.DTOs;

/// <summary>
/// DTO for processing defaults
/// </summary>
public class ProcessingDefaultsDto
{
    public bool AutoProcessUploads { get; set; } = false;
    public int MaxPreviewRows { get; set; } = 100;
    public string DefaultFileType { get; set; } = "CSV";
    public bool EnableDataValidation { get; set; } = true;
    public bool EnableSchemaInference { get; set; } = true;
    public int RetentionDays { get; set; } = 365;
}
