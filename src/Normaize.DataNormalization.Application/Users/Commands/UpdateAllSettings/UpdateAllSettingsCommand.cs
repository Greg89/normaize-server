using MediatR;

namespace Normaize.DataNormalization.Application.Users.Commands.UpdateAllSettings;

/// <summary>
/// Command to update all user settings at once
/// </summary>
public record UpdateAllSettingsCommand(
    string Auth0UserId,
    // Preferences
    string Theme,
    string Language,
    string TimeZone,
    string DateFormat,
    string TimeFormat,
    int DefaultPageSize,
    bool ShowTutorials,
    bool CompactMode,
    // Notification Settings
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool ProcessingCompleteNotifications,
    bool ErrorNotifications,
    bool WeeklyDigestEnabled,
    // Processing Defaults
    bool AutoProcessUploads,
    int MaxPreviewRows,
    string DefaultFileType,
    bool EnableDataValidation,
    bool EnableSchemaInference,
    int RetentionDays,
    // Privacy Settings
    bool ShareAnalytics,
    bool AllowDataUsageForImprovement,
    bool ShowProcessingTime) : IRequest<Unit>;
