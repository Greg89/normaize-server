using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Mapping;
using Normaize.Data.Repositories;
using System.ComponentModel.DataAnnotations;

namespace Normaize.Data.Services;

/// <summary>
/// Service for managing user settings and preferences in the Normaize application.
/// </summary>
/// <remarks>
/// This service provides comprehensive user settings management including:
/// - CRUD operations for user settings
/// - User profile management with Auth0 integration
/// - Dynamic setting value retrieval and updates
/// - Default settings initialization for new users
/// - Soft delete functionality for data preservation
/// 
/// The service integrates with the UserSettingsRepository for data persistence
/// and uses structured logging for comprehensive audit trails.
/// </remarks>
public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _repository;
    private readonly IStructuredLoggingService _loggingService;

    /// <summary>
    /// Initializes a new instance of the UserSettingsService.
    /// </summary>
    /// <param name="repository">The user settings repository for data access.</param>
    /// <param name="loggingService">The structured logging service for audit trails.</param>
    /// <exception cref="ArgumentNullException">Thrown when repository or loggingService is null.</exception>
    public UserSettingsService(
        IUserSettingsRepository repository,
        IStructuredLoggingService loggingService)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <summary>
    /// Retrieves user settings for the specified user ID.
    /// </summary>
    /// <param name="userId">The Auth0 user ID to retrieve settings for.</param>
    /// <returns>
    /// The user settings DTO if found; otherwise, null if no settings exist for the user.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when userId is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there's an error retrieving settings from the repository.</exception>
    public async Task<UserSettingsDto?> GetUserSettingsAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            _loggingService.LogUserAction("Retrieving user settings", new { UserId = userId });
            
            var settings = await _repository.GetByUserIdAsync(userId);
            var result = settings?.ToDto();
            
            _loggingService.LogUserAction("User settings retrieved", new { UserId = userId, SettingsFound = result != null });
            
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Error retrieving user settings for user {userId}");
            throw new InvalidOperationException($"Failed to retrieve user settings for user {userId}", ex);
        }
    }

    /// <summary>
    /// Creates or updates user settings for the specified user ID.
    /// </summary>
    /// <param name="userId">The Auth0 user ID to save settings for.</param>
    /// <param name="settings">The settings to save or update.</param>
    /// <returns>
    /// The updated user settings DTO after saving.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when userId is null, empty, or whitespace, or when settings is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there's an error saving settings to the repository.</exception>
    public async Task<UserSettingsDto> SaveUserSettingsAsync(string userId, UpdateUserSettingsDto settings)
    {
        ValidateUserId(userId);
        ValidateSettings(settings);

        try
        {
            _loggingService.LogUserAction("Saving user settings", new { UserId = userId, Settings = settings });
            
            var existingSettings = await _repository.GetByUserIdAsync(userId);

            if (existingSettings == null)
            {
                // Create new settings with defaults
                var newSettings = CreateDefaultUserSettings(userId);
                ApplyUpdates(newSettings, settings);
                
                var created = await _repository.CreateAsync(newSettings);
                var result = created.ToDto();
                
                _loggingService.LogUserAction("User settings created", new { UserId = userId, SettingsId = result.Id });
                
                return result;
            }
            else
            {
                // Update existing settings
                ApplyUpdates(existingSettings, settings);
                var updated = await _repository.UpdateAsync(existingSettings);
                var result = updated.ToDto();
                
                _loggingService.LogUserAction("User settings updated", new { UserId = userId, SettingsId = result.Id });
                
                return result;
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Error saving user settings for user {userId}");
            throw new InvalidOperationException($"Failed to save user settings for user {userId}", ex);
        }
    }

    /// <summary>
    /// Retrieves the complete user profile including Auth0 information and application settings.
    /// </summary>
    /// <param name="userId">The Auth0 user ID to retrieve the profile for.</param>
    /// <returns>
    /// The complete user profile DTO if settings exist; otherwise, null if no settings exist for the user.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when userId is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there's an error retrieving the user profile.</exception>
    public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            _loggingService.LogUserAction("Retrieving user profile", new { UserId = userId });
            
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
            {
                _loggingService.LogUserAction("User profile not found - no settings exist", new { UserId = userId });
                return null;
            }

            // Note: In a real implementation, you might want to fetch Auth0 user info
            // from Auth0 Management API or cache it. For now, we'll return what we have.
            var profile = new UserProfileDto
            {
                Version = 1,
                LastUpdated = DateTime.UtcNow,
                UserId = userId,
                Settings = settings
                // Auth0 info would be populated here from Auth0 Management API
            };
            
            _loggingService.LogUserAction("User profile retrieved", new { UserId = userId, ProfileFound = true });
            
            return profile;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Error retrieving user profile for user {userId}");
            throw new InvalidOperationException($"Failed to retrieve user profile for user {userId}", ex);
        }
    }

    /// <summary>
    /// Initializes default user settings for a new user.
    /// </summary>
    /// <param name="userId">The Auth0 user ID to initialize settings for.</param>
    /// <returns>
    /// The newly created user settings DTO with default values.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when userId is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there's an error initializing user settings.</exception>
    public async Task<UserSettingsDto> InitializeUserSettingsAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            _loggingService.LogUserAction("Initializing user settings", new { UserId = userId });
            
            var settings = CreateDefaultUserSettings(userId);
            var created = await _repository.CreateAsync(settings);
            var result = created.ToDto();
            
            _loggingService.LogUserAction("User settings initialized", new { UserId = userId, SettingsId = result.Id });
            
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Error initializing user settings for user {userId}");
            throw new InvalidOperationException($"Failed to initialize user settings for user {userId}", ex);
        }
    }

    /// <summary>
    /// Deletes user settings for the specified user ID (soft delete).
    /// </summary>
    /// <param name="userId">The Auth0 user ID to delete settings for.</param>
    /// <returns>
    /// True if the settings were successfully deleted; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when userId is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there's an error deleting user settings.</exception>
    public async Task<bool> DeleteUserSettingsAsync(string userId)
    {
        ValidateUserId(userId);

        try
        {
            _loggingService.LogUserAction("Deleting user settings", new { UserId = userId });
            
            var result = await _repository.DeleteAsync(userId);
            
            if (result)
            {
                _loggingService.LogUserAction("User settings deleted", new { UserId = userId });
            }
            else
            {
                _loggingService.LogUserAction("User settings deletion failed - no settings found", new { UserId = userId });
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Error deleting user settings for user {userId}");
            throw new InvalidOperationException($"Failed to delete user settings for user {userId}", ex);
        }
    }

    /// <summary>
    /// Retrieves a specific setting value for the specified user.
    /// </summary>
    /// <typeparam name="T">The type of the setting value to retrieve.</typeparam>
    /// <param name="userId">The Auth0 user ID to retrieve the setting for.</param>
    /// <param name="settingName">The name of the setting to retrieve.</param>
    /// <returns>
    /// The setting value if found and of the correct type; otherwise, null.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when userId or settingName is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there's an error retrieving the setting value.</exception>
    public async Task<T?> GetSettingValueAsync<T>(string userId, string settingName) where T : class
    {
        ValidateUserId(userId);
        ValidateSettingName(settingName);

        try
        {
            _loggingService.LogUserAction("Retrieving setting value", new { UserId = userId, SettingName = settingName, SettingType = typeof(T).Name });
            
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
            {
                _loggingService.LogUserAction("Setting value not found - no user settings exist", new { UserId = userId, SettingName = settingName });
                return null;
            }

            var property = typeof(UserSettingsDto).GetProperty(settingName);
            if (property == null)
            {
                _loggingService.LogUserAction("Setting value not found - invalid setting name", new { UserId = userId, SettingName = settingName });
                return null;
            }

            var value = property.GetValue(settings);
            var result = value as T;
            
            _loggingService.LogUserAction("Setting value retrieved", new { UserId = userId, SettingName = settingName, ValueFound = result != null });
            
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Error retrieving setting value for user {userId}, setting {settingName}");
            throw new InvalidOperationException($"Failed to retrieve setting value for user {userId}, setting {settingName}", ex);
        }
    }

    /// <summary>
    /// Updates a specific setting value for the specified user.
    /// </summary>
    /// <typeparam name="T">The type of the setting value to update.</typeparam>
    /// <param name="userId">The Auth0 user ID to update the setting for.</param>
    /// <param name="settingName">The name of the setting to update.</param>
    /// <param name="value">The new value for the setting.</param>
    /// <returns>
    /// True if the setting was successfully updated; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when userId or settingName is null, empty, or whitespace, or when value is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there's an error updating the setting value.</exception>
    public async Task<bool> UpdateSettingValueAsync<T>(string userId, string settingName, T value) where T : class
    {
        ValidateUserId(userId);
        ValidateSettingName(settingName);
        
        if (value == null)
        {
            throw new ArgumentException("Setting value cannot be null", nameof(value));
        }

        try
        {
            _loggingService.LogUserAction("Updating setting value", new { UserId = userId, SettingName = settingName, SettingType = typeof(T).Name });
            
            var updateDto = new UpdateUserSettingsDto();
            var property = typeof(UpdateUserSettingsDto).GetProperty(settingName);

            if (property == null)
            {
                _loggingService.LogUserAction("Setting update failed - invalid setting name", new { UserId = userId, SettingName = settingName });
                return false;
            }

            property.SetValue(updateDto, value);
            await SaveUserSettingsAsync(userId, updateDto);
            
            _loggingService.LogUserAction("Setting value updated", new { UserId = userId, SettingName = settingName });
            
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"Error updating setting value for user {userId}, setting {settingName}");
            throw new InvalidOperationException($"Failed to update setting value for user {userId}, setting {settingName}", ex);
        }
    }

    #region Private Methods

    /// <summary>
    /// Creates a new UserSettings entity with default values for the specified user.
    /// </summary>
    /// <param name="userId">The Auth0 user ID for the new settings.</param>
    /// <returns>A new UserSettings entity with default values.</returns>
    private static UserSettings CreateDefaultUserSettings(string userId)
    {
        return new UserSettings
        {
            UserId = userId,
            // All other properties will use their default values from the model
        };
    }

    /// <summary>
    /// Applies updates from an UpdateUserSettingsDto to an existing UserSettings entity.
    /// </summary>
    /// <param name="settings">The existing UserSettings entity to update.</param>
    /// <param name="updateDto">The DTO containing the updates to apply.</param>
    private static void ApplyUpdates(UserSettings settings, UpdateUserSettingsDto updateDto)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(updateDto);

        // Use the ManualMapper for consistent mapping behavior
        var updatedSettings = updateDto.ToEntity(settings);
        
        // Copy the updated values back to the original settings object
        var updateType = typeof(UpdateUserSettingsDto);
        var settingsType = typeof(UserSettings);

        foreach (var property in updateType.GetProperties())
        {
            var value = property.GetValue(updateDto);
            if (value != null)
            {
                var settingsProperty = settingsType.GetProperty(property.Name);
                if (settingsProperty != null && settingsProperty.CanWrite)
                {
                    settingsProperty.SetValue(settings, value);
                }
            }
        }

        // Ensure UpdatedAt is always set
        settings.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates that the user ID is not null, empty, or whitespace.
    /// </summary>
    /// <param name="userId">The user ID to validate.</param>
    /// <exception cref="ArgumentException">Thrown when userId is invalid.</exception>
    private static void ValidateUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null, empty, or whitespace", nameof(userId));
        }
    }

    /// <summary>
    /// Validates that the settings object is not null.
    /// </summary>
    /// <param name="settings">The settings object to validate.</param>
    /// <exception cref="ArgumentException">Thrown when settings is null.</exception>
    private static void ValidateSettings(UpdateUserSettingsDto settings)
    {
        if (settings == null)
        {
            throw new ArgumentException("Settings cannot be null", nameof(settings));
        }
    }

    /// <summary>
    /// Validates that the setting name is not null, empty, or whitespace.
    /// </summary>
    /// <param name="settingName">The setting name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when settingName is invalid.</exception>
    private static void ValidateSettingName(string settingName)
    {
        if (string.IsNullOrWhiteSpace(settingName))
        {
            throw new ArgumentException("Setting name cannot be null, empty, or whitespace", nameof(settingName));
        }
    }

    #endregion
}