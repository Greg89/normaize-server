using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Users.Commands.UpdateAllSettings;

/// <summary>
/// Handler for updating all user settings at once
/// </summary>
public class UpdateAllSettingsCommandHandler : IRequestHandler<UpdateAllSettingsCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateAllSettingsCommandHandler> _logger;

    public UpdateAllSettingsCommandHandler(
        IUserRepository userRepository,
        ILogger<UpdateAllSettingsCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateAllSettingsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating all settings for user with Auth0UserId: {Auth0UserId}", request.Auth0UserId);

        // Get user by Auth0UserId
        var user = await _userRepository.GetByAuth0UserIdAsync(request.Auth0UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with Auth0UserId '{request.Auth0UserId}' not found");
        }

        // Ensure access (validates Auth0UserId matches)
        user.EnsureUserAccess(request.Auth0UserId);

        // Create new value objects with provided values
        var preferences = UserPreferences.Create(
            request.Theme,
            request.Language,
            request.TimeZone,
            request.DateFormat,
            request.TimeFormat,
            request.DefaultPageSize,
            request.ShowTutorials,
            request.CompactMode
        );

        var notificationSettings = NotificationSettings.Create(
            request.EmailNotificationsEnabled,
            request.PushNotificationsEnabled,
            request.ProcessingCompleteNotifications,
            request.ErrorNotifications,
            request.WeeklyDigestEnabled
        );

        var processingDefaults = ProcessingDefaults.Create(
            request.AutoProcessUploads,
            request.MaxPreviewRows,
            request.DefaultFileType,
            request.EnableDataValidation,
            request.EnableSchemaInference,
            request.RetentionDays
        );

        var privacySettings = PrivacySettings.Create(
            request.ShareAnalytics,
            request.AllowDataUsageForImprovement,
            request.ShowProcessingTime
        );

        // Update all settings (triggers domain event)
        user.UpdateAllSettings(
            user.DisplayName, // Keep current display name
            preferences,
            notificationSettings,
            processingDefaults,
            privacySettings,
            request.Auth0UserId
        );

        // Save changes
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Successfully updated all settings for user {UserId}", user.Id);

        return Unit.Value;
    }
}
