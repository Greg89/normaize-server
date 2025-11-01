using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.API.Extensions;
using Normaize.DataNormalization.Application.Users.Commands.RegisterUser;
using Normaize.DataNormalization.Application.Users.Commands.UpdateAllSettings;
using Normaize.DataNormalization.Application.Users.Queries.GetUserProfile;
using Normaize.DataNormalization.Application.Users.DTOs;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.API.Controllers;

/// <summary>
/// Controller for user settings and profile management
/// Provides endpoints matching legacy /api/UserSettings routes for client compatibility
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserSettingsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserSettingsController> _logger;

    public UserSettingsController(
        IMediator mediator,
        IUserRepository userRepository,
        ILogger<UserSettingsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get complete user profile including Auth0 info and application settings
    /// Matches legacy endpoint: GET /api/UserSettings/profile
    /// </summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetUserProfile()
    {
        try
        {
            var auth0UserId = GetCurrentUserId();
            _logger.LogDebug("Getting user profile for Auth0UserId: {Auth0UserId}", auth0UserId);

            // Get user profile from application layer
            var query = new GetUserProfileQuery(auth0UserId);
            var userProfileDto = await _mediator.Send(query);

            // If user doesn't exist, auto-register them
            if (userProfileDto == null)
            {
                _logger.LogInformation("User not found, auto-registering user with Auth0UserId: {Auth0UserId}", auth0UserId);

                // Get display name from claims (or use auth0UserId as fallback)
                var displayName = User.GetUserName() ?? User.GetUserId() ?? auth0UserId;

                var registerCommand = new RegisterUserCommand(auth0UserId, displayName);
                userProfileDto = await _mediator.Send(registerCommand);
            }

            // Map to client-expected format with Auth0 claims
            var response = MapToUserProfileResponse(userProfileDto);

            return Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return HandleException(ex, nameof(GetUserProfile));
        }
    }

    /// <summary>
    /// Update user profile settings
    /// Matches legacy endpoint: PUT /api/UserSettings/profile
    /// </summary>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileResponse>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserSettingsRequest request)
    {
        try
        {
            var auth0UserId = GetCurrentUserId();
            _logger.LogInformation("Updating user profile for Auth0UserId: {Auth0UserId}", auth0UserId);

            // Get current user profile to merge settings
            var query = new GetUserProfileQuery(auth0UserId);
            var currentProfile = await _mediator.Send(query);

            // If user doesn't exist, register them first
            if (currentProfile == null)
            {
                _logger.LogInformation("User not found, auto-registering user with Auth0UserId: {Auth0UserId}", auth0UserId);

                var displayName = User.GetUserName() ?? User.GetUserId() ?? auth0UserId;
                var registerCommand = new RegisterUserCommand(auth0UserId, displayName);
                currentProfile = await _mediator.Send(registerCommand);
            }

            // Merge request with current settings (only update provided values)
            var updateCommand = new UpdateAllSettingsCommand(
                auth0UserId,
                // Preferences
                Theme: request.Theme ?? currentProfile.Preferences.Theme,
                Language: request.Language ?? currentProfile.Preferences.Language,
                TimeZone: request.TimeZone ?? currentProfile.Preferences.TimeZone,
                DateFormat: request.DateFormat ?? currentProfile.Preferences.DateFormat,
                TimeFormat: request.TimeFormat ?? currentProfile.Preferences.TimeFormat,
                DefaultPageSize: request.DefaultPageSize ?? currentProfile.Preferences.DefaultPageSize,
                ShowTutorials: request.ShowTutorials ?? currentProfile.Preferences.ShowTutorials,
                CompactMode: request.CompactMode ?? currentProfile.Preferences.CompactMode,
                // Notification Settings
                EmailNotificationsEnabled: request.EmailNotificationsEnabled ?? currentProfile.NotificationSettings.EmailNotificationsEnabled,
                PushNotificationsEnabled: request.PushNotificationsEnabled ?? currentProfile.NotificationSettings.PushNotificationsEnabled,
                ProcessingCompleteNotifications: request.ProcessingCompleteNotifications ?? currentProfile.NotificationSettings.ProcessingCompleteNotifications,
                ErrorNotifications: request.ErrorNotifications ?? currentProfile.NotificationSettings.ErrorNotifications,
                WeeklyDigestEnabled: request.WeeklyDigestEnabled ?? currentProfile.NotificationSettings.WeeklyDigestEnabled,
                // Processing Defaults
                AutoProcessUploads: request.AutoProcessUploads ?? currentProfile.ProcessingDefaults.AutoProcessUploads,
                MaxPreviewRows: request.MaxPreviewRows ?? currentProfile.ProcessingDefaults.MaxPreviewRows,
                DefaultFileType: request.DefaultFileType ?? currentProfile.ProcessingDefaults.DefaultFileType,
                EnableDataValidation: request.EnableDataValidation ?? currentProfile.ProcessingDefaults.EnableDataValidation,
                EnableSchemaInference: request.EnableSchemaInference ?? currentProfile.ProcessingDefaults.EnableSchemaInference,
                RetentionDays: request.RetentionDays ?? currentProfile.ProcessingDefaults.RetentionDays,
                // Privacy Settings
                ShareAnalytics: request.ShareAnalytics ?? currentProfile.PrivacySettings.ShareAnalytics,
                AllowDataUsageForImprovement: request.AllowDataUsageForImprovement ?? currentProfile.PrivacySettings.AllowDataUsageForImprovement,
                ShowProcessingTime: request.ShowProcessingTime ?? currentProfile.PrivacySettings.ShowProcessingTime
            );

            await _mediator.Send(updateCommand);

            // Update display name if provided
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                var user = await _userRepository.GetByAuth0UserIdAsync(auth0UserId);
                if (user != null)
                {
                    user.UpdateDisplayName(request.DisplayName, auth0UserId);
                    await _userRepository.UpdateAsync(user);
                }
            }

            // Fetch updated profile
            var updatedProfile = await _mediator.Send(query);
            if (updatedProfile == null)
            {
                return Error("Failed to retrieve updated profile", "UPDATE_FAILED", 500);
            }

            // Map to client-expected format
            var response = MapToUserProfileResponse(updatedProfile);

            return Success(response, "User profile updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return HandleException(ex, nameof(UpdateUserProfile));
        }
    }

    /// <summary>
    /// Maps Application UserProfileDto to client-expected UserProfileResponse
    /// Includes Auth0 claims (email, name, picture, emailVerified)
    /// </summary>
    private UserProfileResponse MapToUserProfileResponse(UserProfileDto profileDto)
    {
        // Get Auth0 claims from JWT token
        var email = User.GetUserEmail() ?? string.Empty;
        var name = User.GetUserName() ?? profileDto.DisplayName ?? profileDto.Auth0UserId;
        var picture = User.GetUserPicture();
        var emailVerified = User.IsEmailVerified();

        // Merge settings from separate DTOs into single UserSettingsResponse
        var settings = new UserSettingsResponse
        {
            Id = profileDto.Id.ToString(),
            UserId = profileDto.Auth0UserId,
            // Notification Settings
            EmailNotificationsEnabled = profileDto.NotificationSettings.EmailNotificationsEnabled,
            PushNotificationsEnabled = profileDto.NotificationSettings.PushNotificationsEnabled,
            ProcessingCompleteNotifications = profileDto.NotificationSettings.ProcessingCompleteNotifications,
            ErrorNotifications = profileDto.NotificationSettings.ErrorNotifications,
            WeeklyDigestEnabled = profileDto.NotificationSettings.WeeklyDigestEnabled,
            // UI/UX Preferences
            Theme = profileDto.Preferences.Theme,
            Language = profileDto.Preferences.Language,
            DefaultPageSize = profileDto.Preferences.DefaultPageSize,
            ShowTutorials = profileDto.Preferences.ShowTutorials,
            CompactMode = profileDto.Preferences.CompactMode,
            // Data Processing Preferences
            AutoProcessUploads = profileDto.ProcessingDefaults.AutoProcessUploads,
            MaxPreviewRows = profileDto.ProcessingDefaults.MaxPreviewRows,
            DefaultFileType = profileDto.ProcessingDefaults.DefaultFileType,
            EnableDataValidation = profileDto.ProcessingDefaults.EnableDataValidation,
            EnableSchemaInference = profileDto.ProcessingDefaults.EnableSchemaInference,
            RetentionDays = profileDto.ProcessingDefaults.RetentionDays,
            // Privacy Settings
            ShareAnalytics = profileDto.PrivacySettings.ShareAnalytics,
            AllowDataUsageForImprovement = profileDto.PrivacySettings.AllowDataUsageForImprovement,
            ShowProcessingTime = profileDto.PrivacySettings.ShowProcessingTime,
            // Account Information
            DisplayName = profileDto.DisplayName,
            TimeZone = profileDto.Preferences.TimeZone,
            DateFormat = profileDto.Preferences.DateFormat,
            TimeFormat = profileDto.Preferences.TimeFormat,
            // Timestamps as ISO strings
            CreatedAt = profileDto.CreatedAt.ToString("O"),
            UpdatedAt = profileDto.UpdatedAt.ToString("O")
        };

        return new UserProfileResponse
        {
            UserId = profileDto.Auth0UserId,
            Email = email,
            Name = name,
            Picture = picture,
            EmailVerified = emailVerified,
            Settings = settings
        };
    }
}

