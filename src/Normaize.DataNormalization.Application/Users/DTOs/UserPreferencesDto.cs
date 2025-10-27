namespace Normaize.DataNormalization.Application.Users.DTOs;

/// <summary>
/// DTO for user preferences
/// </summary>
public class UserPreferencesDto
{
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC";
    public string DateFormat { get; set; } = "MM/dd/yyyy";
    public string TimeFormat { get; set; } = "12h";
    public int DefaultPageSize { get; set; } = 25;
    public bool ShowTutorials { get; set; } = true;
    public bool CompactMode { get; set; } = false;
}
