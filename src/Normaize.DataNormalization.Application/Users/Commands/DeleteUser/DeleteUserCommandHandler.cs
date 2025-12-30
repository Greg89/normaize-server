using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Users.Commands.DeleteUser;

/// <summary>
/// Handler for soft deleting a user
/// </summary>
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DeleteUserCommandHandler> _logger;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        ILogger<DeleteUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user with Auth0UserId: {Auth0UserId}", request.Auth0UserId);

        // Get user by Auth0UserId
        var user = await _userRepository.GetByAuth0UserIdAsync(request.Auth0UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with Auth0UserId '{request.Auth0UserId}' not found");
        }

        // Ensure access (validates Auth0UserId matches)
        user.EnsureUserAccess(request.Auth0UserId);

        // Soft delete
        user.Delete();

        // Save changes
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Successfully deleted user {UserId}", user.Id);

        return Unit.Value;
    }
}
