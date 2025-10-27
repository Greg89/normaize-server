using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Users.DTOs;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Users.Commands.RegisterUser;

/// <summary>
/// Handler for registering a new user
/// </summary>
public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, UserProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserProfileDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering new user with Auth0UserId: {Auth0UserId}", request.Auth0UserId);

        // Validate input
        if (string.IsNullOrWhiteSpace(request.Auth0UserId))
        {
            throw new ArgumentException("Auth0UserId cannot be empty", nameof(request.Auth0UserId));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("DisplayName cannot be empty", nameof(request.DisplayName));
        }

        // Check if user already exists
        var existingUser = await _userRepository.GetByAuth0UserIdAsync(request.Auth0UserId);
        if (existingUser != null)
        {
            _logger.LogWarning("User with Auth0UserId {Auth0UserId} already exists", request.Auth0UserId);
            throw new InvalidOperationException($"User with Auth0UserId '{request.Auth0UserId}' already exists");
        }

        // Create user with default settings
        var user = User.Register(request.Auth0UserId, request.DisplayName);

        // Save to repository
        await _userRepository.CreateAsync(user);

        _logger.LogInformation("Successfully registered user {UserId} with Auth0UserId {Auth0UserId}",
            user.Id, request.Auth0UserId);

        // Map to DTO
        return MapToUserProfileDto(user);
    }

    private static UserProfileDto MapToUserProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Auth0UserId = user.Auth0UserId,
            DisplayName = user.DisplayName,
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
