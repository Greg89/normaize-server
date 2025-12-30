using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Users.Commands.UpdateUserPreferences;

/// <summary>
/// Handler for updating user preferences
/// </summary>
public class UpdateUserPreferencesCommandHandler : IRequestHandler<UpdateUserPreferencesCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateUserPreferencesCommandHandler> _logger;

    public UpdateUserPreferencesCommandHandler(
        IUserRepository userRepository,
        ILogger<UpdateUserPreferencesCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateUserPreferencesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating preferences for user with Auth0UserId: {Auth0UserId}", request.Auth0UserId);

        // Get user by Auth0UserId
        var user = await _userRepository.GetByAuth0UserIdAsync(request.Auth0UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with Auth0UserId '{request.Auth0UserId}' not found");
        }

        // Ensure access (validates Auth0UserId matches)
        user.EnsureUserAccess(request.Auth0UserId);

        // Build new preferences using With() pattern, only updating provided values
        var updatedPreferences = user.Preferences.With(
            theme: request.Theme ?? user.Preferences.Theme,
            language: request.Language ?? user.Preferences.Language,
            timeZone: request.TimeZone ?? user.Preferences.TimeZone,
            dateFormat: request.DateFormat ?? user.Preferences.DateFormat,
            timeFormat: request.TimeFormat ?? user.Preferences.TimeFormat,
            defaultPageSize: request.DefaultPageSize ?? user.Preferences.DefaultPageSize,
            showTutorials: request.ShowTutorials ?? user.Preferences.ShowTutorials,
            compactMode: request.CompactMode ?? user.Preferences.CompactMode
        );

        // Update user preferences (triggers domain event)
        user.UpdatePreferences(updatedPreferences, request.Auth0UserId);

        // Save changes
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Successfully updated preferences for user {UserId}", user.Id);

        return Unit.Value;
    }
}
