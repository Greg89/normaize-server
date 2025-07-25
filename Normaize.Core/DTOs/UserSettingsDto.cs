using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.DTOs;

public class UserSettingsDto
{
    public int Id { get; set; }
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    // Notification Settings
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public bool ProcessingCompleteNotifications { get; set; } = true;
    public bool ErrorNotifications { get; set; } = true;
    public bool WeeklyDigestEnabled { get; set; } = false;
    
    // UI/UX Preferences
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "en";
    public int DefaultPageSize { get; set; } = 20;
    public bool ShowTutorials { get; set; } = true;
    public bool CompactMode { get; set; } = false;
    
    // Data Processing Preferences
    public bool AutoProcessUploads { get; set; } = true;
    public int MaxPreviewRows { get; set; } = 100;
    public string DefaultFileType { get; set; } = "CSV";
    public bool EnableDataValidation { get; set; } = true;
    public bool EnableSchemaInference { get; set; } = true;
    
    // Privacy Settings
    public bool ShareAnalytics { get; set; } = true;
    public bool AllowDataUsageForImprovement { get; set; } = false;
    public bool ShowProcessingTime { get; set; } = true;
    
    // Account Information (non-sensitive)
    public string? DisplayName { get; set; }
    public string? TimeZone { get; set; } = "UTC";
    public string? DateFormat { get; set; } = "MM/dd/yyyy";
    public string? TimeFormat { get; set; } = "12h";
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
} 