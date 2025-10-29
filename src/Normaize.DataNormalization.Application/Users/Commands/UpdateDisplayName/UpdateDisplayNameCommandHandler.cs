using MediatR;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Repositories;

namespace Normaize.DataNormalization.Application.Users.Commands.UpdateDisplayName;

/// <summary>
/// Handler for updating user display name
/// </summary>
public class UpdateDisplayNameCommandHandler : IRequestHandler<UpdateDisplayNameCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UpdateDisplayNameCommandHandler> _logger;

    public UpdateDisplayNameCommandHandler(
        IUserRepository userRepository,
        ILogger<UpdateDisplayNameCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateDisplayNameCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating display name for user with Auth0UserId: {Auth0UserId}", request.Auth0UserId);

        // Validate input
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            throw new ArgumentException("DisplayName cannot be empty", nameof(request.DisplayName));
        }

        // Get user by Auth0UserId
        var user = await _userRepository.GetByAuth0UserIdAsync(request.Auth0UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with Auth0UserId '{request.Auth0UserId}' not found");
        }

        // Ensure access (validates Auth0UserId matches)
        user.EnsureUserAccess(request.Auth0UserId);

        // Update display name
        user.UpdateDisplayName(request.DisplayName);

        // Save changes
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Successfully updated display name for user {UserId}", user.Id);

        return Unit.Value;
    }
}
