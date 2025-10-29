namespace Normaize.DataNormalization.Application.Users.DTOs;

/// <summary>
/// DTO for privacy settings
/// </summary>
public class PrivacySettingsDto
{
    public bool ShareAnalytics { get; set; } = false;
    public bool AllowDataUsageForImprovement { get; set; } = false;
    public bool ShowProcessingTime { get; set; } = true;
}
