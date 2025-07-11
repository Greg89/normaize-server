using Microsoft.EntityFrameworkCore;
using Normaize.Core.Interfaces;
using Normaize.Data;

namespace Normaize.Data.Services;

public class DatabaseHealthService : IDatabaseHealthService
{
    private readonly NormaizeContext _context;

    public DatabaseHealthService(NormaizeContext context)
    {
        _context = context;
    }

    public async Task<DatabaseHealthResult> CheckHealthAsync()
    {
        var result = new DatabaseHealthResult();

        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                result.IsHealthy = false;
                result.Status = "unhealthy";
                result.ErrorMessage = "Cannot connect to database";
                return result;
            }

            // Check if we're using an in-memory database
            var isInMemory = _context.Database.ProviderName?.Contains("InMemory") == true;
            
            if (isInMemory)
            {
                // For in-memory databases, just check connectivity
                result.IsHealthy = true;
                result.Status = "healthy";
                result.ErrorMessage = null;
                return result;
            }

            // For real databases, check for critical columns
            var criticalColumns = new[] { "DataHash", "UserId", "FilePath", "StorageProvider" };
            var missingColumns = new List<string>();

            foreach (var column in criticalColumns)
            {
                var columnExists = await _context.Database.ExecuteSqlAsync(
                    $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DataSets' AND COLUMN_NAME = '{column}'");
                
                if (columnExists == 0)
                {
                    missingColumns.Add(column);
                }
            }

            if (missingColumns.Any())
            {
                result.IsHealthy = false;
                result.Status = "unhealthy";
                result.ErrorMessage = "Missing critical columns";
                result.MissingColumns = missingColumns;
                return result;
            }

            result.IsHealthy = true;
            result.Status = "healthy";
            return result;
        }
        catch (Exception ex)
        {
            result.IsHealthy = false;
            result.Status = "unhealthy";
            result.ErrorMessage = ex.Message;
            return result;
        }
    }
} 