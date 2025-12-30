namespace Normaize.DataNormalization.Application.DTOs;

/// <summary>
/// Validation result DTO
/// </summary>
public class ValidationResultDto
{
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Validated columns with their assigned types
    /// </summary>
    public Dictionary<string, string> ValidatedColumns { get; set; } = new();

    /// <summary>
    /// Recommended configuration adjustments
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}