using MediatR;

namespace Normaize.DataNormalization.Application.Users.Commands.ResetUserSettings;

/// <summary>
/// Command to reset all user settings to defaults
/// </summary>
public record ResetUserSettingsCommand(
    string Auth0UserId) : IRequest<Unit>;
