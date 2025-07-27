using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data.Repositories;
using System.Reflection;
using System.Text.Json;

namespace Normaize.Data.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _repository;
    private readonly IStructuredLoggingService _loggingService;

    public UserSettingsService(
        IUserSettingsRepository repository,
        IStructuredLoggingService loggingService)
    {
        _repository = repository;
        _loggingService = loggingService;
    }

    public async Task<UserSettingsDto?> GetUserSettingsAsync(string userId)
    {
        try
        {
            var settings = await _repository.GetByUserIdAsync(userId);
            return settings != null ? MapToDto(settings) : null;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetUserSettingsAsync({userId})");
            throw;
        }
    }

    public async Task<UserSettingsDto> SaveUserSettingsAsync(string userId, UpdateUserSettingsDto settings)
    {
        try
        {
            var existingSettings = await _repository.GetByUserIdAsync(userId);

            if (existingSettings == null)
            {
                // Create new settings
                var newSettings = new UserSettings
                {
                    UserId = userId
                };

                ApplyUpdates(newSettings, settings);
                var created = await _repository.CreateAsync(newSettings);
                return MapToDto(created);
            }
            else
            {
                // Update existing settings
                ApplyUpdates(existingSettings, settings);
                var updated = await _repository.UpdateAsync(existingSettings);
                return MapToDto(updated);
            }
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"SaveUserSettingsAsync({userId})");
            throw;
        }
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
    {
        try
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
                return null;

            // Note: In a real implementation, you might want to fetch Auth0 user info
            // from Auth0 Management API or cache it. For now, we'll return what we have.
            return new UserProfileDto
            {
                UserId = userId,
                Settings = settings
                // Auth0 info would be populated here from Auth0 Management API
            };
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetUserProfileAsync({userId})");
            throw;
        }
    }

    public async Task<UserSettingsDto> InitializeUserSettingsAsync(string userId)
    {
        try
        {
            var settings = new UserSettings
            {
                UserId = userId
                // All other properties will use their default values
            };

            var created = await _repository.CreateAsync(settings);
            _loggingService.LogUserAction("User settings initialized", new { UserId = userId });

            return MapToDto(created);
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"InitializeUserSettingsAsync({userId})");
            throw;
        }
    }

    public async Task<bool> DeleteUserSettingsAsync(string userId)
    {
        try
        {
            var result = await _repository.DeleteAsync(userId);
            if (result)
            {
                _loggingService.LogUserAction("User settings deleted", new { UserId = userId });
            }
            return result;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"DeleteUserSettingsAsync({userId})");
            throw;
        }
    }

    public async Task<T?> GetSettingValueAsync<T>(string userId, string settingName) where T : class
    {
        try
        {
            var settings = await GetUserSettingsAsync(userId);
            if (settings == null)
                return null;

            var property = typeof(UserSettingsDto).GetProperty(settingName);
            if (property == null)
                return null;

            var value = property.GetValue(settings);
            return value as T;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetSettingValueAsync({userId}, {settingName})");
            throw;
        }
    }

    public async Task<bool> UpdateSettingValueAsync<T>(string userId, string settingName, T value)
    {
        try
        {
            var updateDto = new UpdateUserSettingsDto();
            var property = typeof(UpdateUserSettingsDto).GetProperty(settingName);

            if (property == null)
                return false;

            property.SetValue(updateDto, value);

            await SaveUserSettingsAsync(userId, updateDto);
            return true;
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"UpdateSettingValueAsync({userId}, {settingName})");
            throw;
        }
    }

    private static void ApplyUpdates(UserSettings settings, UpdateUserSettingsDto updateDto)
    {
        var updateType = typeof(UpdateUserSettingsDto);
        var settingsType = typeof(UserSettings);

        foreach (var property in updateType.GetProperties())
        {
            var value = property.GetValue(updateDto);
            if (value != null)
            {
                var settingsProperty = settingsType.GetProperty(property.Name);
                if (settingsProperty != null)
                {
                    settingsProperty.SetValue(settings, value);
                }
            }
        }
    }

    private static UserSettingsDto MapToDto(UserSettings settings)
    {
        return new UserSettingsDto
        {
            Id = settings.Id,
            UserId = settings.UserId,
            EmailNotificationsEnabled = settings.EmailNotificationsEnabled,
            PushNotificationsEnabled = settings.PushNotificationsEnabled,
            ProcessingCompleteNotifications = settings.ProcessingCompleteNotifications,
            ErrorNotifications = settings.ErrorNotifications,
            WeeklyDigestEnabled = settings.WeeklyDigestEnabled,
            Theme = settings.Theme,
            Language = settings.Language,
            DefaultPageSize = settings.DefaultPageSize,
            ShowTutorials = settings.ShowTutorials,
            CompactMode = settings.CompactMode,
            AutoProcessUploads = settings.AutoProcessUploads,
            MaxPreviewRows = settings.MaxPreviewRows,
            DefaultFileType = settings.DefaultFileType,
            EnableDataValidation = settings.EnableDataValidation,
            EnableSchemaInference = settings.EnableSchemaInference,
            ShareAnalytics = settings.ShareAnalytics,
            AllowDataUsageForImprovement = settings.AllowDataUsageForImprovement,
            ShowProcessingTime = settings.ShowProcessingTime,
            DisplayName = settings.DisplayName,
            TimeZone = settings.TimeZone,
            DateFormat = settings.DateFormat,
            TimeFormat = settings.TimeFormat,
            CreatedAt = settings.CreatedAt,
            UpdatedAt = settings.UpdatedAt
        };
    }
}