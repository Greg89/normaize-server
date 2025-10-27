using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Application.Users.DTOs;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Users.Queries.GetUserPreferences;

/// <summary>
/// Handler for getting user preferences only
/// </summary>
public class GetUserPreferencesQueryHandler : IRequestHandler<GetUserPreferencesQuery, UserPreferencesDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserPreferencesQueryHandler> _logger;

    public GetUserPreferencesQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserPreferencesQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserPreferencesDto?> Handle(GetUserPreferencesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user preferences for Auth0UserId: {Auth0UserId}", request.Auth0UserId);

        var user = await _userRepository.GetByAuth0UserIdAsync(request.Auth0UserId);
        if (user == null)
        {
            _logger.LogInformation("User with Auth0UserId {Auth0UserId} not found", request.Auth0UserId);
            return null;
        }

        // Ensure access (validates Auth0UserId matches)
        user.EnsureUserAccess(request.Auth0UserId);

        // Map to DTO
        return new UserPreferencesDto
        {
            Theme = user.Preferences.Theme,
            Language = user.Preferences.Language,
            TimeZone = user.Preferences.TimeZone,
            DateFormat = user.Preferences.DateFormat,
            TimeFormat = user.Preferences.TimeFormat,
            DefaultPageSize = user.Preferences.DefaultPageSize,
            ShowTutorials = user.Preferences.ShowTutorials,
            CompactMode = user.Preferences.CompactMode
        };
    }
}
