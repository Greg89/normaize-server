using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.DTOs;

/// <summary>
/// Complete user profile combining Auth0 identity with application preferences.
/// Auth0 claims (userId, email, name, picture, emailVerified) are merged with
/// application-specific settings stored in the database.
/// 
/// Priority order for display name:
/// 1. User's custom DisplayName from Settings
/// 2. Auth0 name claim
/// 3. Empty string as fallback
/// 
/// Version: 1.0 - Initial implementation
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// Gets or sets the API version of this profile structure
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timestamp when this profile was last updated
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the unique identifier for the user from Auth0
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "UserId cannot exceed 100 characters")]
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address from Auth0 claims
    /// </summary>
    [Required]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name. Priority: Custom DisplayName > Auth0 name > Empty
    /// </summary>
    [Required]
    [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's profile picture URL from Auth0 claims
    /// </summary>
    [StringLength(500, ErrorMessage = "Picture URL cannot exceed 500 characters")]
    [Url(ErrorMessage = "Invalid URL format for picture")]
    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    /// <summary>
    /// Gets or sets whether the user's email has been verified in Auth0
    /// </summary>
    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Gets or sets the user's application-specific settings and preferences
    /// </summary>
    [Required]
    [JsonPropertyName("settings")]
    public UserSettingsDto Settings { get; set; } = new();
}