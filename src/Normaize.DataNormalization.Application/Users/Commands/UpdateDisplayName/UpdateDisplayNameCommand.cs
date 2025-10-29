using MediatR;

namespace Normaize.DataNormalization.Application.Users.Commands.UpdateDisplayName;

/// <summary>
/// Command to update user display name
/// </summary>
public record UpdateDisplayNameCommand(
    string Auth0UserId,
    string DisplayName) : IRequest<Unit>;
