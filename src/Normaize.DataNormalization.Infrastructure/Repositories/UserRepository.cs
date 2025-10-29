using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of User repository
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly DataNormalizationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        DataNormalizationDbContext context,
        ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user by ID {UserId}", id);

        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

            if (user != null)
            {
                _logger.LogInformation("Found user {UserId}", id);
            }
            else
            {
                _logger.LogInformation("User {UserId} not found", id);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            throw;
        }
    }

    public async Task<User?> GetByAuth0UserIdAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user by Auth0UserId {Auth0UserId}", auth0UserId);

        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Auth0UserId == auth0UserId && !u.IsDeleted, cancellationToken);

            if (user != null)
            {
                _logger.LogInformation("Found user {UserId} with Auth0UserId {Auth0UserId}", 
                    user.Id, auth0UserId);
            }
            else
            {
                _logger.LogInformation("User with Auth0UserId {Auth0UserId} not found", auth0UserId);
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by Auth0UserId {Auth0UserId}", auth0UserId);
            throw;
        }
    }

    public async Task<bool> ExistsByAuth0UserIdAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _context.Users
                .AnyAsync(u => u.Auth0UserId == auth0UserId && !u.IsDeleted, cancellationToken);

            _logger.LogInformation("User with Auth0UserId {Auth0UserId} exists: {Exists}", 
                auth0UserId, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user exists with Auth0UserId {Auth0UserId}", 
                auth0UserId);
            throw;
        }
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user with Auth0UserId {Auth0UserId}", user.Auth0UserId);

        try
        {
            var entity = _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created user {UserId}", entity.Entity.Id);
            return entity.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with Auth0UserId {Auth0UserId}", 
                user.Auth0UserId);
            throw;
        }
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user {UserId}", user.Id);

        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated user {UserId}", user.Id);
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user {UserId}", user.Id);

        try
        {
            // Soft delete is already called on the user entity
            // Just save the changes
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted user {UserId}", user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<List<User>> GetAllAsync(bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all users (includeDeleted: {IncludeDeleted})", includeDeleted);

        try
        {
            var query = _context.Users.AsQueryable();

            if (!includeDeleted)
            {
                query = query.Where(u => !u.IsDeleted);
            }

            var users = await query
                .OrderBy(u => u.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} users", users.Count);
            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            throw;
        }
    }
}
