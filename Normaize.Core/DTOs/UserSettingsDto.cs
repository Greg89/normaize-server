using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

/// <summary>
/// Represents user application settings and preferences for the Normaize platform.
/// This DTO contains all user-configurable settings including notifications, UI preferences,
/// data processing options, privacy settings, and account information.
/// </summary>
/// <remarks>
/// This DTO is used for both reading and writing user settings. For partial updates,
/// use <see cref="UpdateUserSettingsDto"/> which allows nullable properties for selective updates.
/// The DTO provides sensible defaults for all settings to ensure a good user experience
/// even when settings are not explicitly configured.
/// </remarks>
public class UserSettingsDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the user settings record.
    /// </summary>
    /// <remarks>
    /// This is the database primary key and is auto-generated when settings are created.
    /// </remarks>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the Auth0 user identifier associated with these settings.
    /// </summary>
    /// <remarks>
    /// This field links the settings to the authenticated user and is required for all operations.
    /// The value should match the Auth0 user ID from the JWT token.
    /// </remarks>
    [Required(ErrorMessage = "UserId is required")]
    [StringLength(255, ErrorMessage = "UserId cannot exceed 255 characters")]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    #region Notification Settings

    /// <summary>
    /// Gets or sets whether email notifications are enabled for the user.
    /// </summary>
    /// <remarks>
    /// When enabled, users will receive email notifications for important events
    /// such as data processing completion, errors, and weekly digests.
    /// </remarks>
    [JsonPropertyName("emailNotificationsEnabled")]
    public bool EmailNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether push notifications are enabled for the user.
    /// </summary>
    /// <remarks>
    /// When enabled, users will receive real-time push notifications for immediate events
    /// such as processing status updates and error alerts.
    /// </remarks>
    [JsonPropertyName("pushNotificationsEnabled")]
    public bool PushNotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether notifications are sent when data processing completes.
    /// </summary>
    /// <remarks>
    /// This setting controls whether users receive notifications when their uploaded
    /// data files finish processing, regardless of success or failure.
    /// </remarks>
    [JsonPropertyName("processingCompleteNotifications")]
    public bool ProcessingCompleteNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets whether error notifications are enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, users will be notified of any errors that occur during
    /// data processing or other critical operations.
    /// </remarks>
    [JsonPropertyName("errorNotifications")]
    public bool ErrorNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets whether weekly digest emails are enabled.
    /// </summary>
    /// <remarks>
    /// When enabled, users will receive a weekly summary email containing
    /// their activity statistics and any important updates.
    /// </remarks>
    [JsonPropertyName("weeklyDigestEnabled")]
    public bool WeeklyDigestEnabled { get; set; } = false;

    #endregion

    #region UI/UX Preferences

    /// <summary>
    /// Gets or sets the user's preferred theme for the application interface.
    /// </summary>
    /// <remarks>
    /// Valid values are: "light", "dark", "auto". The "auto" setting will
    /// automatically switch between light and dark based on system preferences.
    /// </remarks>
    [JsonPropertyName("theme")]
    [StringLength(10, ErrorMessage = "Theme cannot exceed 10 characters")]
    public string Theme { get; set; } = "light";

    /// <summary>
    /// Gets or sets the user's preferred language for the application interface.
    /// </summary>
    /// <remarks>
    /// Uses ISO 639-1 language codes (e.g., "en", "es", "fr"). The application
    /// will attempt to provide localized content based on this setting.
    /// </remarks>
    [JsonPropertyName("language")]
    [StringLength(5, ErrorMessage = "Language code cannot exceed 5 characters")]
    public string Language { get; set; } = "en";

    /// <summary>
    /// Gets or sets the default number of items to display per page in lists and tables.
    /// </summary>
    /// <remarks>
    /// This setting affects data sets, analysis results, and other paginated content.
    /// Valid range is 10-100 items per page.
    /// </remarks>
    [JsonPropertyName("defaultPageSize")]
    [Range(10, 100, ErrorMessage = "Default page size must be between 10 and 100")]
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets whether tutorial overlays and help content should be displayed.
    /// </summary>
    /// <remarks>
    /// When enabled, users will see contextual help, tooltips, and guided tours
    /// to help them learn the application features.
    /// </remarks>
    [JsonPropertyName("showTutorials")]
    public bool ShowTutorials { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the application should use compact mode for UI elements.
    /// </summary>
    /// <remarks>
    /// Compact mode reduces spacing and padding to fit more content on screen,
    /// useful for users with smaller displays or who prefer denser layouts.
    /// </remarks>
    [JsonPropertyName("compactMode")]
    public bool CompactMode { get; set; } = false;

    #endregion

    #region Data Processing Preferences

    /// <summary>
    /// Gets or sets whether uploaded files should be automatically processed.
    /// </summary>
    /// <remarks>
    /// When enabled, files will begin processing immediately upon upload.
    /// When disabled, users must manually trigger processing.
    /// </remarks>
    [JsonPropertyName("autoProcessUploads")]
    public bool AutoProcessUploads { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of rows to display in data previews.
    /// </summary>
    /// <remarks>
    /// This setting controls how many rows are shown when previewing uploaded data
    /// before processing. Valid range is 10-1000 rows.
    /// </remarks>
    [JsonPropertyName("maxPreviewRows")]
    [Range(10, 1000, ErrorMessage = "Max preview rows must be between 10 and 1000")]
    public int MaxPreviewRows { get; set; } = 100;

    /// <summary>
    /// Gets or sets the default file type to use for data exports and processing.
    /// </summary>
    /// <remarks>
    /// Valid values include: "CSV", "JSON", "XML", "Excel". This setting
    /// determines the default format for exported data and processed results.
    /// </remarks>
    [JsonPropertyName("defaultFileType")]
    [StringLength(10, ErrorMessage = "Default file type cannot exceed 10 characters")]
    public string DefaultFileType { get; set; } = "CSV";

    /// <summary>
    /// Gets or sets whether data validation should be enabled during processing.
    /// </summary>
    /// <remarks>
    /// When enabled, the system will perform comprehensive data validation
    /// including type checking, range validation, and format verification.
    /// </remarks>
    [JsonPropertyName("enableDataValidation")]
    public bool EnableDataValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether schema inference should be enabled for uploaded data.
    /// </summary>
    /// <remarks>
    /// When enabled, the system will automatically detect data types and structure
    /// from uploaded files, improving processing accuracy and user experience.
    /// </remarks>
    [JsonPropertyName("enableSchemaInference")]
    public bool EnableSchemaInference { get; set; } = true;

    #endregion

    #region Privacy Settings

    /// <summary>
    /// Gets or sets whether the user allows sharing of anonymous analytics data.
    /// </summary>
    /// <remarks>
    /// When enabled, anonymous usage statistics may be collected to improve
    /// the application. No personally identifiable information is shared.
    /// </remarks>
    [JsonPropertyName("shareAnalytics")]
    public bool ShareAnalytics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the user allows their data to be used for service improvement.
    /// </summary>
    /// <remarks>
    /// When enabled, anonymized data patterns may be used to improve
    /// processing algorithms and user experience features.
    /// </remarks>
    [JsonPropertyName("allowDataUsageForImprovement")]
    public bool AllowDataUsageForImprovement { get; set; } = false;

    /// <summary>
    /// Gets or sets whether processing time information should be displayed to the user.
    /// </summary>
    /// <remarks>
    /// When enabled, users will see detailed timing information for data processing
    /// operations, which can be useful for performance monitoring.
    /// </remarks>
    [JsonPropertyName("showProcessingTime")]
    public bool ShowProcessingTime { get; set; } = true;

    #endregion

    #region Account Information

    /// <summary>
    /// Gets or sets the user's custom display name for the application.
    /// </summary>
    /// <remarks>
    /// This is a custom name that can be different from the Auth0 profile name.
    /// If not set, the Auth0 name will be used as the display name.
    /// </remarks>
    [JsonPropertyName("displayName")]
    [StringLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the user's preferred timezone for date and time display.
    /// </summary>
    /// <remarks>
    /// Uses IANA timezone identifiers (e.g., "America/New_York", "Europe/London").
    /// All timestamps will be converted to this timezone for display purposes.
    /// </remarks>
    [JsonPropertyName("timeZone")]
    [StringLength(50, ErrorMessage = "Time zone cannot exceed 50 characters")]
    public string? TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Gets or sets the user's preferred date format for display.
    /// </summary>
    /// <remarks>
    /// Uses .NET date format strings (e.g., "MM/dd/yyyy", "dd/MM/yyyy").
    /// This format will be used for all date displays throughout the application.
    /// </remarks>
    [JsonPropertyName("dateFormat")]
    [StringLength(20, ErrorMessage = "Date format cannot exceed 20 characters")]
    public string? DateFormat { get; set; } = "MM/dd/yyyy";

    /// <summary>
    /// Gets or sets the user's preferred time format for display.
    /// </summary>
    /// <remarks>
    /// Valid values are: "12h" (12-hour format with AM/PM) or "24h" (24-hour format).
    /// This format will be used for all time displays throughout the application.
    /// </remarks>
    [JsonPropertyName("timeFormat")]
    [StringLength(3, ErrorMessage = "Time format cannot exceed 3 characters")]
    public string? TimeFormat { get; set; } = "12h";

    #endregion

    #region Timestamps

    /// <summary>
    /// Gets or sets the timestamp when these settings were first created.
    /// </summary>
    /// <remarks>
    /// This timestamp is automatically set when the user settings record is created
    /// and should not be modified by client applications.
    /// </remarks>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when these settings were last updated.
    /// </summary>
    /// <remarks>
    /// This timestamp is automatically updated whenever any setting is modified
    /// and should not be modified by client applications.
    /// </remarks>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    #endregion
}

/// <summary>
/// Defines the available theme options for the application interface.
/// </summary>
public static class ThemeOptions
{
    /// <summary>
    /// Light theme with light backgrounds and dark text.
    /// </summary>
    public const string Light = "light";

    /// <summary>
    /// Dark theme with dark backgrounds and light text.
    /// </summary>
    public const string Dark = "dark";

    /// <summary>
    /// Automatic theme that follows system preferences.
    /// </summary>
    public const string Auto = "auto";
}

/// <summary>
/// Defines the available time format options for time display.
/// </summary>
public static class TimeFormatOptions
{
    /// <summary>
    /// 12-hour format with AM/PM indicators.
    /// </summary>
    public const string TwelveHour = "12h";

    /// <summary>
    /// 24-hour format without AM/PM indicators.
    /// </summary>
    public const string TwentyFourHour = "24h";
}

/// <summary>
/// Defines common file type options for data processing and export.
/// </summary>
public static class FileTypeOptions
{
    /// <summary>
    /// Comma-separated values format.
    /// </summary>
    public const string Csv = "CSV";

    /// <summary>
    /// JavaScript Object Notation format.
    /// </summary>
    public const string Json = "JSON";

    /// <summary>
    /// Extensible Markup Language format.
    /// </summary>
    public const string Xml = "XML";

    /// <summary>
    /// Microsoft Excel format.
    /// </summary>
    public const string Excel = "Excel";
}