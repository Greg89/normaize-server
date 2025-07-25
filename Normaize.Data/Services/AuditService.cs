using Microsoft.EntityFrameworkCore;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data;
using System.Text.Json;

namespace Normaize.Data.Services;

public class AuditService : IAuditService
{
    private readonly NormaizeContext _context;

    public AuditService(NormaizeContext context)
    {
        _context = context;
    }

    public async Task LogDataSetActionAsync(int dataSetId, string userId, string action, object? changes = null, string? ipAddress = null, string? userAgent = null)
    {
        var auditLog = new DataSetAuditLog
        {
            DataSetId = dataSetId,
            UserId = userId,
            Action = action,
            Changes = changes != null ? JsonSerializer.Serialize(changes) : null,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        _context.DataSetAuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<DataSetAuditLog>> GetDataSetAuditLogsAsync(int dataSetId, int skip = 0, int take = 50)
    {
        return await _context.DataSetAuditLogs
            .Where(a => a.DataSetId == dataSetId)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSetAuditLog>> GetUserAuditLogsAsync(string userId, int skip = 0, int take = 50)
    {
        return await _context.DataSetAuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<DataSetAuditLog>> GetAuditLogsByActionAsync(string action, int skip = 0, int take = 50)
    {
        return await _context.DataSetAuditLogs
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.Timestamp)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }
}