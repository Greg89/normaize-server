using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Users.Commands.ResetUserSettings;

/// <summary>
/// Handler for resetting user settings to defaults
/// </summary>
public class ResetUserSettingsCommandHandler : IRequestHandler<ResetUserSettingsCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ResetUserSettingsCommandHandler> _logger;

    public ResetUserSettingsCommandHandler(
        IUserRepository userRepository,
        ILogger<ResetUserSettingsCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResetUserSettingsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resetting settings to defaults for user with Auth0UserId: {Auth0UserId}", request.Auth0UserId);

        // Get user by Auth0UserId
        var user = await _userRepository.GetByAuth0UserIdAsync(request.Auth0UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with Auth0UserId '{request.Auth0UserId}' not found");
        }

        // Ensure access (validates Auth0UserId matches)
        user.EnsureUserAccess(request.Auth0UserId);

        // Reset to defaults (triggers domain event)
        user.ResetToDefaults(request.Auth0UserId);

        // Save changes
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Successfully reset settings to defaults for user {UserId}", user.Id);

        return Unit.Value;
    }
}
