using MediatR;

namespace Normaize.DataNormalization.Application.Users.Commands.DeleteUser;

/// <summary>
/// Command to soft delete a user
/// </summary>
public record DeleteUserCommand(
    string Auth0UserId) : IRequest<Unit>;
