using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.DTOs;

public class UpdateUserSettingsDto
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
    public bool? AutoProcessUploads { get; set; }
    public int? MaxPreviewRows { get; set; }
    public string? DefaultFileType { get; set; }
    public bool? EnableDataValidation { get; set; }
    public bool? EnableSchemaInference { get; set; }
    public bool? ShareAnalytics { get; set; }
    public bool? AllowDataUsageForImprovement { get; set; }
    public bool? ShowProcessingTime { get; set; }
    public string? DisplayName { get; set; }
    public string? TimeZone { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
} 