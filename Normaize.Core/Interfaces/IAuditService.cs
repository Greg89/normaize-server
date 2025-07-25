using Normaize.Core.Models;

namespace Normaize.Core.Interfaces;

public interface IAuditService
{
    Task LogDataSetActionAsync(int dataSetId, string userId, string action, object? changes = null, string? ipAddress = null, string? userAgent = null);
    Task<IEnumerable<DataSetAuditLog>> GetDataSetAuditLogsAsync(int dataSetId, int skip = 0, int take = 50);
    Task<IEnumerable<DataSetAuditLog>> GetUserAuditLogsAsync(string userId, int skip = 0, int take = 50);
    Task<IEnumerable<DataSetAuditLog>> GetAuditLogsByActionAsync(string action, int skip = 0, int take = 50);
}