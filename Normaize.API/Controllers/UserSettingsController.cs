using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Extensions;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserSettingsController(
    IUserSettingsService _userSettingsService,
    IStructuredLoggingService _loggingService
) : BaseApiController(_loggingService)
{
    private string GetCurrentUserId()
    {
        return User.GetUserId();
    }

    /// <summary>
    /// Get current user's settings
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> GetUserSettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            var settings = await _userSettingsService.GetUserSettingsAsync(userId);

            if (settings == null)
            {
                // Initialize default settings for new user
                settings = await _userSettingsService.InitializeUserSettingsAsync(userId);
            }

            return Success(settings);
        }
        catch (Exception ex)
        {
            return HandleException<UserSettingsDto>(ex, "GetUserSettings");
        }
    }

    /// <summary>
    /// Update current user's settings
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> UpdateUserSettings([FromBody] UpdateUserSettingsDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var updatedSettings = await _userSettingsService.SaveUserSettingsAsync(userId, updateDto);

            return Success(updatedSettings, "User settings updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException<UserSettingsDto>(ex, "UpdateUserSettings");
        }
    }

    /// <summary>
    /// Get complete user profile including Auth0 info and application settings
    /// </summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetUserProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var profile = await _userSettingsService.GetUserProfileAsync(userId);

            if (profile == null)
            {
                // Initialize settings and create profile
                var settings = await _userSettingsService.InitializeUserSettingsAsync(userId);
                profile = new UserProfileDto
                {
                    UserId = userId,
                    Settings = settings
                };
            }

            return Success(profile);
        }
        catch (Exception ex)
        {
            return HandleException<UserProfileDto>(ex, "GetUserProfile");
        }
    }

    /// <summary>
    /// Update user profile settings
    /// </summary>
    [HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto?>>> UpdateUserProfile([FromBody] UpdateUserSettingsDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var updatedSettings = await _userSettingsService.SaveUserSettingsAsync(userId, updateDto);

            // Return the updated profile
            var updatedProfile = await _userSettingsService.GetUserProfileAsync(userId);
            return Success(updatedProfile, "User profile updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException<UserProfileDto?>(ex, "UpdateUserProfile");
        }
    }

    /// <summary>
    /// Get a specific setting value
    /// </summary>
    [HttpGet("setting/{settingName}")]
    public async Task<ActionResult<ApiResponse<object>>> GetSettingValue(string settingName)
    {
        try
        {
            var userId = GetCurrentUserId();
            var value = await _userSettingsService.GetSettingValueAsync<object>(userId, settingName);

            if (value == null)
                return NotFound<object>($"Setting '{settingName}' not found");

            return Success(value);
        }
        catch (Exception ex)
        {
            return HandleException<object>(ex, $"GetSettingValue({settingName})");
        }
    }

    /// <summary>
    /// Update a specific setting value
    /// </summary>
    [HttpPut("setting/{settingName}")]
    public async Task<ActionResult<ApiResponse<object?>>> UpdateSettingValue(string settingName, [FromBody] string value)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _userSettingsService.UpdateSettingValueAsync(userId, settingName, value);

            if (!success)
                return BadRequest<object?>($"Invalid setting name: {settingName}");

            return Success<object?>(null, "Setting updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException<object?>(ex, $"UpdateSettingValue({settingName})");
        }
    }

    /// <summary>
    /// Reset user settings to defaults
    /// </summary>
    [HttpPost("reset")]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> ResetUserSettings()
    {
        try
        {
            var userId = GetCurrentUserId();

            // Delete existing settings
            await _userSettingsService.DeleteUserSettingsAsync(userId);

            // Initialize new default settings
            var newSettings = await _userSettingsService.InitializeUserSettingsAsync(userId);

            return Success(newSettings, "User settings reset to defaults successfully");
        }
        catch (Exception ex)
        {
            return HandleException<UserSettingsDto>(ex, "ResetUserSettings");
        }
    }

    /// <summary>
    /// Test endpoint to verify camelCase JSON serialization
    /// </summary>
    [HttpGet("test-serialization")]
    public ActionResult<ApiResponse<object>> TestSerialization()
    {
        var testObject = new
        {
            UserId = "test-user-123",
            EmailAddress = "test@example.com",
            DisplayName = "Test User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Settings = new
            {
                Theme = "dark",
                Language = "en",
                EmailNotificationsEnabled = true
            }
        };

        return Success((object)testObject);
    }
}