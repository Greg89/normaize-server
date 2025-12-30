namespace Normaize.DataNormalization.API.DTOs;

/// <summary>
/// User Profile DTO matching client expectations
/// </summary>
public class UserProfileResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Picture { get; set; }
    public bool EmailVerified { get; set; }
    public UserSettingsResponse Settings { get; set; } = null!;
}

/// <summary>
/// User Settings DTO matching client expectations (merged from Preferences, Notifications, Processing, Privacy)
/// </summary>
public class UserSettingsResponse
{
    public string Id { get; set; } = string.Empty; // GUID as string
    public string UserId { get; set; } = string.Empty;

    // Notification Settings
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool ProcessingCompleteNotifications { get; set; }
    public bool ErrorNotifications { get; set; }
    public bool WeeklyDigestEnabled { get; set; }

    // UI/UX Preferences
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public int DefaultPageSize { get; set; } = 25;
    public bool ShowTutorials { get; set; } = true;
    public bool CompactMode { get; set; } = false;

    // Data Processing Preferences
    public bool AutoProcessUploads { get; set; } = false;
    public int MaxPreviewRows { get; set; } = 100;
    public string DefaultFileType { get; set; } = "CSV";
    public bool EnableDataValidation { get; set; } = true;
    public bool EnableSchemaInference { get; set; } = true;
    public int RetentionDays { get; set; } = 365;

    // Privacy Settings
    public bool ShareAnalytics { get; set; } = false;
    public bool AllowDataUsageForImprovement { get; set; } = false;
    public bool ShowProcessingTime { get; set; } = true;

    // Account Information (non-sensitive)
    public string? DisplayName { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }

    public string CreatedAt { get; set; } = string.Empty; // ISO date string
    public string UpdatedAt { get; set; } = string.Empty; // ISO date string
}

/// <summary>
/// Request DTO for updating user settings (matches client UserSettingsDto)
/// </summary>
public class UpdateUserSettingsRequest
{
    // Notification Settings
    public bool? EmailNotificationsEnabled { get; set; }
    public bool? PushNotificationsEnabled { get; set; }
    public bool? ProcessingCompleteNotifications { get; set; }
    public bool? ErrorNotifications { get; set; }
    public bool? WeeklyDigestEnabled { get; set; }

    // UI/UX Preferences
    public string? Theme { get; set; }
    public string? Language { get; set; }
    public int? DefaultPageSize { get; set; }
    public bool? ShowTutorials { get; set; }
    public bool? CompactMode { get; set; }

    // Data Processing Preferences
    public bool? AutoProcessUploads { get; set; }
    public int? MaxPreviewRows { get; set; }
    public string? DefaultFileType { get; set; }
    public bool? EnableDataValidation { get; set; }
    public bool? EnableSchemaInference { get; set; }
    public int? RetentionDays { get; set; }

    // Privacy Settings
    public bool? ShareAnalytics { get; set; }
    public bool? AllowDataUsageForImprovement { get; set; }
    public bool? ShowProcessingTime { get; set; }

    // Account Information
    public string? DisplayName { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
}

