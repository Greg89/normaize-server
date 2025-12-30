namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing user notification preferences.
/// </summary>
public sealed record NotificationSettings
{
    public bool EmailNotificationsEnabled { get; init; }
    public bool PushNotificationsEnabled { get; init; }
    public bool ProcessingCompleteNotifications { get; init; }
    public bool ErrorNotifications { get; init; }
    public bool WeeklyDigestEnabled { get; init; }

    private NotificationSettings() // EF Core
    {
        EmailNotificationsEnabled = true;
        PushNotificationsEnabled = true;
        ProcessingCompleteNotifications = true;
        ErrorNotifications = true;
        WeeklyDigestEnabled = false;
    }

    private NotificationSettings(
        bool emailNotificationsEnabled,
        bool pushNotificationsEnabled,
        bool processingCompleteNotifications,
        bool errorNotifications,
        bool weeklyDigestEnabled)
    {
        EmailNotificationsEnabled = emailNotificationsEnabled;
        PushNotificationsEnabled = pushNotificationsEnabled;
        ProcessingCompleteNotifications = processingCompleteNotifications;
        ErrorNotifications = errorNotifications;
        WeeklyDigestEnabled = weeklyDigestEnabled;
    }

    /// <summary>
    /// Creates default notification settings for new users.
    /// </summary>
    public static NotificationSettings Default() => new(
        emailNotificationsEnabled: true,
        pushNotificationsEnabled: true,
        processingCompleteNotifications: true,
        errorNotifications: true,
        weeklyDigestEnabled: false
    );

    /// <summary>
    /// Creates notification settings with specified values.
    /// </summary>
    public static NotificationSettings Create(
        bool emailNotificationsEnabled,
        bool pushNotificationsEnabled,
        bool processingCompleteNotifications,
        bool errorNotifications,
        bool weeklyDigestEnabled)
    {
        return new NotificationSettings(
            emailNotificationsEnabled,
            pushNotificationsEnabled,
            processingCompleteNotifications,
            errorNotifications,
            weeklyDigestEnabled
        );
    }

    /// <summary>
    /// Creates a copy with updated values.
    /// </summary>
    public NotificationSettings With(
        bool? emailNotificationsEnabled = null,
        bool? pushNotificationsEnabled = null,
        bool? processingCompleteNotifications = null,
        bool? errorNotifications = null,
        bool? weeklyDigestEnabled = null)
    {
        return Create(
            emailNotificationsEnabled ?? EmailNotificationsEnabled,
            pushNotificationsEnabled ?? PushNotificationsEnabled,
            processingCompleteNotifications ?? ProcessingCompleteNotifications,
            errorNotifications ?? ErrorNotifications,
            weeklyDigestEnabled ?? WeeklyDigestEnabled
        );
    }

    /// <summary>
    /// Disables all notifications.
    /// </summary>
    public NotificationSettings DisableAll() => Create(
        emailNotificationsEnabled: false,
        pushNotificationsEnabled: false,
        processingCompleteNotifications: false,
        errorNotifications: false,
        weeklyDigestEnabled: false
    );

    /// <summary>
    /// Enables all notifications.
    /// </summary>
    public NotificationSettings EnableAll() => Create(
        emailNotificationsEnabled: true,
        pushNotificationsEnabled: true,
        processingCompleteNotifications: true,
        errorNotifications: true,
        weeklyDigestEnabled: true
    );

    /// <summary>
    /// Checks if any notifications are enabled.
    /// </summary>
    public bool HasAnyEnabled() =>
        EmailNotificationsEnabled ||
        PushNotificationsEnabled ||
        ProcessingCompleteNotifications ||
        ErrorNotifications ||
        WeeklyDigestEnabled;
}
