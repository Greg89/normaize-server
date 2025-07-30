using Normaize.Core.Models;

namespace Normaize.Data.Repositories;

public interface IUserSettingsRepository
{
    /// <summary>
    /// Get user settings by Auth0 user ID
    /// </summary>
    Task<UserSettings?> GetByUserIdAsync(string userId);

    /// <summary>
    /// Create new user settings
    /// </summary>
    Task<UserSettings> CreateAsync(UserSettings settings);

    /// <summary>
    /// Update existing user settings
    /// </summary>
    Task<UserSettings> UpdateAsync(UserSettings settings);

    /// <summary>
    /// Delete user settings (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(string userId);

    /// <summary>
    /// Check if user settings exist
    /// </summary>
    Task<bool> ExistsAsync(string userId);
}