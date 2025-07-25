namespace Normaize.Core.DTOs;

public class UserProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public bool EmailVerified { get; set; }
    public UserSettingsDto Settings { get; set; } = new();
} 