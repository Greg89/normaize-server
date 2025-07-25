using Microsoft.EntityFrameworkCore;
using Normaize.Core.Models;
using Normaize.Data.Repositories;

namespace Normaize.Data.Repositories;

public class UserSettingsRepository : IUserSettingsRepository
{
    private readonly NormaizeContext _context;

    public UserSettingsRepository(NormaizeContext context)
    {
        _context = context;
    }

    public async Task<UserSettings?> GetByUserIdAsync(string userId)
    {
        return await _context.UserSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);
    }

    public async Task<UserSettings> CreateAsync(UserSettings settings)
    {
        settings.CreatedAt = DateTime.UtcNow;
        settings.UpdatedAt = DateTime.UtcNow;
        
        _context.UserSettings.Add(settings);
        await _context.SaveChangesAsync();
        
        return settings;
    }

    public async Task<UserSettings> UpdateAsync(UserSettings settings)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        
        _context.UserSettings.Update(settings);
        await _context.SaveChangesAsync();
        
        return settings;
    }

    public async Task<bool> DeleteAsync(string userId)
    {
        var settings = await GetByUserIdAsync(userId);
        if (settings == null)
            return false;

        settings.IsDeleted = true;
        settings.DeletedAt = DateTime.UtcNow;
        settings.UpdatedAt = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        return await _context.UserSettings
            .AnyAsync(s => s.UserId == userId && !s.IsDeleted);
    }
} 