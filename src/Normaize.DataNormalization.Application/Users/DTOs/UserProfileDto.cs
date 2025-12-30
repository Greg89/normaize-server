namespace Normaize.DataNormalization.Application.Users.DTOs;

/// <summary>
/// DTO for complete user profile
/// </summary>
public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Auth0UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public UserPreferencesDto Preferences { get; set; } = null!;
    public NotificationSettingsDto NotificationSettings { get; set; } = null!;
    public ProcessingDefaultsDto ProcessingDefaults { get; set; } = null!;
    public PrivacySettingsDto PrivacySettings { get; set; } = null!;
}
