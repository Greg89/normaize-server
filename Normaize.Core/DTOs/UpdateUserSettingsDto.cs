using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Normaize.Core.DTOs;

/// <summary>
/// Data Transfer Object for updating user settings with selective property updates
/// </summary>
/// <remarks>
/// This DTO allows for partial updates of user settings by using nullable properties.
/// Only properties with non-null values will be updated, enabling selective updates
/// without requiring all settings to be specified. This is used by the UserSettingsController
/// for both individual setting updates and bulk setting updates.
/// 
/// Unlike UserSettingsDto which represents the complete settings state, this DTO
/// is specifically designed for update operations where only specific properties
/// need to be modified.
/// </remarks>
public class UpdateUserSettingsDto
{
    #region Notification Settings

    /// <summary>
    /// Gets or sets whether email notifications are enabled
    /// </summary>
    /// <remarks>
    /// Controls whether the user receives email notifications for various events.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("emailNotificationsEnabled")]
    public bool? EmailNotificationsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether push notifications are enabled
    /// </summary>
    /// <remarks>
    /// Controls whether the user receives push notifications for various events.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("pushNotificationsEnabled")]
    public bool? PushNotificationsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether processing complete notifications are enabled
    /// </summary>
    /// <remarks>
    /// Controls whether the user receives notifications when data processing is complete.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("processingCompleteNotifications")]
    public bool? ProcessingCompleteNotifications { get; set; }

    /// <summary>
    /// Gets or sets whether error notifications are enabled
    /// </summary>
    /// <remarks>
    /// Controls whether the user receives notifications when errors occur during processing.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("errorNotifications")]
    public bool? ErrorNotifications { get; set; }

    /// <summary>
    /// Gets or sets whether weekly digest notifications are enabled
    /// </summary>
    /// <remarks>
    /// Controls whether the user receives weekly summary notifications.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("weeklyDigestEnabled")]
    public bool? WeeklyDigestEnabled { get; set; }

    #endregion

    #region UI/UX Preferences

    /// <summary>
    /// Gets or sets the user's preferred theme
    /// </summary>
    /// <remarks>
    /// The visual theme for the application interface (e.g., "light", "dark", "auto").
    /// When null, this setting will not be updated.
    /// </remarks>
    [StringLength(50)]
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }

    /// <summary>
    /// Gets or sets the user's preferred language
    /// </summary>
    /// <remarks>
    /// The language code for the application interface (e.g., "en-US", "es-ES").
    /// When null, this setting will not be updated.
    /// </remarks>
    [StringLength(10)]
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the default page size for data displays
    /// </summary>
    /// <remarks>
    /// The number of items to display per page in data tables and lists.
    /// When null, this setting will not be updated.
    /// </remarks>
    [Range(1, 1000)]
    [JsonPropertyName("defaultPageSize")]
    public int? DefaultPageSize { get; set; }

    /// <summary>
    /// Gets or sets whether tutorials should be shown
    /// </summary>
    /// <remarks>
    /// Controls whether the application shows tutorial overlays and help content.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("showTutorials")]
    public bool? ShowTutorials { get; set; }

    /// <summary>
    /// Gets or sets whether compact mode is enabled
    /// </summary>
    /// <remarks>
    /// Controls whether the interface uses a more compact layout with reduced spacing.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("compactMode")]
    public bool? CompactMode { get; set; }

    #endregion

    #region Data Processing Preferences

    /// <summary>
    /// Gets or sets whether uploads should be automatically processed
    /// </summary>
    /// <remarks>
    /// Controls whether uploaded files are automatically processed without user confirmation.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("autoProcessUploads")]
    public bool? AutoProcessUploads { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of preview rows to display
    /// </summary>
    /// <remarks>
    /// The maximum number of rows to show in data previews and sample displays.
    /// When null, this setting will not be updated.
    /// </remarks>
    [Range(1, 10000)]
    [JsonPropertyName("maxPreviewRows")]
    public int? MaxPreviewRows { get; set; }

    /// <summary>
    /// Gets or sets the default file type preference
    /// </summary>
    /// <remarks>
    /// The preferred file type for data uploads and exports (e.g., "CSV", "JSON", "Excel").
    /// When null, this setting will not be updated.
    /// </remarks>
    [StringLength(20)]
    [JsonPropertyName("defaultFileType")]
    public string? DefaultFileType { get; set; }

    /// <summary>
    /// Gets or sets whether data validation is enabled
    /// </summary>
    /// <remarks>
    /// Controls whether uploaded data should be validated for format and content.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("enableDataValidation")]
    public bool? EnableDataValidation { get; set; }

    /// <summary>
    /// Gets or sets whether schema inference is enabled
    /// </summary>
    /// <remarks>
    /// Controls whether the application should automatically infer data schemas.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("enableSchemaInference")]
    public bool? EnableSchemaInference { get; set; }

    #endregion

    #region Privacy Settings

    /// <summary>
    /// Gets or sets whether analytics sharing is enabled
    /// </summary>
    /// <remarks>
    /// Controls whether the user's usage data is shared for analytics and improvement purposes.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("shareAnalytics")]
    public bool? ShareAnalytics { get; set; }

    /// <summary>
    /// Gets or sets whether data usage for improvement is allowed
    /// </summary>
    /// <remarks>
    /// Controls whether the user's data can be used to improve the application.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("allowDataUsageForImprovement")]
    public bool? AllowDataUsageForImprovement { get; set; }

    /// <summary>
    /// Gets or sets whether processing time should be displayed
    /// </summary>
    /// <remarks>
    /// Controls whether processing time information is shown to the user.
    /// When null, this setting will not be updated.
    /// </remarks>
    [JsonPropertyName("showProcessingTime")]
    public bool? ShowProcessingTime { get; set; }

    #endregion

    #region Account Information

    /// <summary>
    /// Gets or sets the user's display name
    /// </summary>
    /// <remarks>
    /// The custom display name for the user within the application.
    /// When null, this setting will not be updated.
    /// </remarks>
    [StringLength(100)]
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the user's timezone
    /// </summary>
    /// <remarks>
    /// The user's preferred timezone for date and time displays (e.g., "UTC", "America/New_York").
    /// When null, this setting will not be updated.
    /// </remarks>
    [StringLength(50)]
    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }

    /// <summary>
    /// Gets or sets the user's preferred date format
    /// </summary>
    /// <remarks>
    /// The preferred format for displaying dates (e.g., "MM/DD/YYYY", "DD/MM/YYYY").
    /// When null, this setting will not be updated.
    /// </remarks>
    [StringLength(20)]
    [JsonPropertyName("dateFormat")]
    public string? DateFormat { get; set; }

    /// <summary>
    /// Gets or sets the user's preferred time format
    /// </summary>
    /// <remarks>
    /// The preferred format for displaying times (e.g., "12-hour", "24-hour").
    /// When null, this setting will not be updated.
    /// </remarks>
    [StringLength(20)]
    [JsonPropertyName("timeFormat")]
    public string? TimeFormat { get; set; }

    #endregion
}