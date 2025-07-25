using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Extensions;

namespace Normaize.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserSettingsController : ControllerBase
{
    private readonly IUserSettingsService _userSettingsService;
    private readonly IStructuredLoggingService _loggingService;

    public UserSettingsController(
        IUserSettingsService userSettingsService,
        IStructuredLoggingService loggingService)
    {
        _userSettingsService = userSettingsService;
        _loggingService = loggingService;
    }

    private string GetCurrentUserId()
    {
        return User.GetUserId();
    }

    /// <summary>
    /// Get current user's settings
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserSettings()
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
            
            return Ok(settings);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetUserSettings");
            return StatusCode(500, "Error retrieving user settings");
        }
    }

    /// <summary>
    /// Update current user's settings
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateUserSettings([FromBody] UpdateUserSettingsDto updateDto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var updatedSettings = await _userSettingsService.SaveUserSettingsAsync(userId, updateDto);
            
            _loggingService.LogUserAction("User settings updated", new { UserId = userId });
            
            return Ok(updatedSettings);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "UpdateUserSettings");
            return StatusCode(500, "Error updating user settings");
        }
    }

    /// <summary>
    /// Get complete user profile including Auth0 info and application settings
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetUserProfile()
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
            
            return Ok(profile);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "GetUserProfile");
            return StatusCode(500, "Error retrieving user profile");
        }
    }

    /// <summary>
    /// Get a specific setting value
    /// </summary>
    [HttpGet("setting/{settingName}")]
    public async Task<IActionResult> GetSettingValue(string settingName)
    {
        try
        {
            var userId = GetCurrentUserId();
            var value = await _userSettingsService.GetSettingValueAsync<object>(userId, settingName);
            
            if (value == null)
                return NotFound($"Setting '{settingName}' not found");
            
            return Ok(new { SettingName = settingName, Value = value });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"GetSettingValue({settingName})");
            return StatusCode(500, "Error retrieving setting value");
        }
    }

    /// <summary>
    /// Update a specific setting value
    /// </summary>
    [HttpPut("setting/{settingName}")]
    public async Task<IActionResult> UpdateSettingValue(string settingName, [FromBody] string value)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _userSettingsService.UpdateSettingValueAsync(userId, settingName, value);
            
            if (!success)
                return BadRequest($"Invalid setting name: {settingName}");
            
            _loggingService.LogUserAction("Setting value updated", new { UserId = userId, SettingName = settingName });
            
            return Ok(new { Message = "Setting updated successfully" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, $"UpdateSettingValue({settingName})");
            return StatusCode(500, "Error updating setting value");
        }
    }

    /// <summary>
    /// Reset user settings to defaults
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetUserSettings()
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // Delete existing settings
            await _userSettingsService.DeleteUserSettingsAsync(userId);
            
            // Initialize new default settings
            var newSettings = await _userSettingsService.InitializeUserSettingsAsync(userId);
            
            _loggingService.LogUserAction("User settings reset to defaults", new { UserId = userId });
            
            return Ok(newSettings);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _loggingService.LogException(ex, "ResetUserSettings");
            return StatusCode(500, "Error resetting user settings");
        }
    }
} 