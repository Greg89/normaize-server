using Normaize.Core.DTOs;

namespace Normaize.Core.Interfaces;

public interface IUserSettingsService
{
    /// <summary>
    /// Get user settings by Auth0 user ID
    /// </summary>
    Task<UserSettingsDto?> GetUserSettingsAsync(string userId);

    /// <summary>
    /// Create or update user settings
    /// </summary>
    Task<UserSettingsDto> SaveUserSettingsAsync(string userId, UpdateUserSettingsDto settings);

    /// <summary>
    /// Get complete user profile including Auth0 info and application settings
    /// </summary>
    Task<UserProfileDto?> GetUserProfileAsync(string userId);

    /// <summary>
    /// Initialize default settings for a new user
    /// </summary>
    Task<UserSettingsDto> InitializeUserSettingsAsync(string userId);

    /// <summary>
    /// Delete user settings (soft delete)
    /// </summary>
    Task<bool> DeleteUserSettingsAsync(string userId);

    /// <summary>
    /// Get a specific setting value
    /// </summary>
    Task<T?> GetSettingValueAsync<T>(string userId, string settingName) where T : class;

    /// <summary>
    /// Update a specific setting value
    /// </summary>
    Task<bool> UpdateSettingValueAsync<T>(string userId, string settingName, T value);
}