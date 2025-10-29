namespace Normaize.DataNormalization.Application.Users.DTOs;

/// <summary>
/// DTO for notification settings
/// </summary>
public class NotificationSettingsDto
{
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool ProcessingCompleteNotifications { get; set; } = true;
    public bool ErrorNotifications { get; set; } = true;
    public bool WeeklyDigestEnabled { get; set; } = false;
}
