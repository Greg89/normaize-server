using MediatR;

namespace Normaize.DataNormalization.Application.Users.Commands.UpdateUserPreferences;

/// <summary>
/// Command to update user preferences
/// </summary>
public record UpdateUserPreferencesCommand(
    string Auth0UserId,
    string? Theme = null,
    string? Language = null,
    string? TimeZone = null,
    string? DateFormat = null,
    string? TimeFormat = null,
    int? DefaultPageSize = null,
    bool? ShowTutorials = null,
    bool? CompactMode = null) : IRequest<Unit>;
