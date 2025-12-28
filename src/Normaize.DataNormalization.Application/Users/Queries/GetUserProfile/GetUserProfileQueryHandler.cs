using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Users.DTOs;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Users.Queries.GetUserProfile;

/// <summary>
/// Handler for getting user profile
/// </summary>
public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, UserProfileDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserProfileDto?> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user profile for Auth0UserId: {Auth0UserId}", request.Auth0UserId);

        var user = await _userRepository.GetByAuth0UserIdAsync(request.Auth0UserId);
        if (user == null)
        {
            _logger.LogInformation("User with Auth0UserId {Auth0UserId} not found", request.Auth0UserId);
            return null;
        }

        // Ensure access (validates Auth0UserId matches)
        user.EnsureUserAccess(request.Auth0UserId);

        // Map to DTO
        return MapToUserProfileDto(user);
    }

    private static UserProfileDto MapToUserProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Auth0UserId = user.Auth0UserId,
            DisplayName = user.DisplayName ?? string.Empty,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Preferences = new UserPreferencesDto
            {
                Theme = user.Preferences.Theme,
                Language = user.Preferences.Language,
                TimeZone = user.Preferences.TimeZone,
                DateFormat = user.Preferences.DateFormat,
                TimeFormat = user.Preferences.TimeFormat,
                DefaultPageSize = user.Preferences.DefaultPageSize,
                ShowTutorials = user.Preferences.ShowTutorials,
                CompactMode = user.Preferences.CompactMode
            },
            NotificationSettings = new NotificationSettingsDto
            {
                EmailNotificationsEnabled = user.NotificationSettings.EmailNotificationsEnabled,
                PushNotificationsEnabled = user.NotificationSettings.PushNotificationsEnabled,
                ProcessingCompleteNotifications = user.NotificationSettings.ProcessingCompleteNotifications,
                ErrorNotifications = user.NotificationSettings.ErrorNotifications,
                WeeklyDigestEnabled = user.NotificationSettings.WeeklyDigestEnabled
            },
            ProcessingDefaults = new ProcessingDefaultsDto
            {
                AutoProcessUploads = user.ProcessingDefaults.AutoProcessUploads,
                MaxPreviewRows = user.ProcessingDefaults.MaxPreviewRows,
                DefaultFileType = user.ProcessingDefaults.DefaultFileType,
                EnableDataValidation = user.ProcessingDefaults.EnableDataValidation,
                EnableSchemaInference = user.ProcessingDefaults.EnableSchemaInference,
                RetentionDays = user.ProcessingDefaults.RetentionDays
            },
            PrivacySettings = new PrivacySettingsDto
            {
                ShareAnalytics = user.PrivacySettings.ShareAnalytics,
                AllowDataUsageForImprovement = user.PrivacySettings.AllowDataUsageForImprovement,
                ShowProcessingTime = user.PrivacySettings.ShowProcessingTime
            }
        };
    }
}
