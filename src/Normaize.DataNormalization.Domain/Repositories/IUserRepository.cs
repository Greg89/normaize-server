using Normaize.DataNormalization.Domain.Entities;

namespace Normaize.DataNormalization.Domain.Repositories;

/// <summary>
/// Repository interface for User aggregate.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by internal ID.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by Auth0 user ID.
    /// </summary>
    Task<User?> GetByAuth0UserIdAsync(string auth0UserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user exists by Auth0 user ID.
    /// </summary>
    Task<bool> ExistsByAuth0UserIdAsync(string auth0UserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user (soft delete).
    /// </summary>
    Task DeleteAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users (for admin purposes, includes deleted).
    /// </summary>
    Task<List<User>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);
}
