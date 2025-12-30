using MediatR;
using Normaize.DataNormalization.Application.Users.DTOs;

namespace Normaize.DataNormalization.Application.Users.Commands.RegisterUser;

/// <summary>
/// Command to register a new user with default settings
/// </summary>
public record RegisterUserCommand(
    string Auth0UserId,
    string DisplayName) : IRequest<UserProfileDto>;
