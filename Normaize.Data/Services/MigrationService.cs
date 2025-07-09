using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;
using Normaize.Data;

namespace Normaize.Data.Services;

public class MigrationService : IMigrationService
{
    private readonly NormaizeContext _context;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(NormaizeContext context, ILogger<MigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MigrationResult> ApplyMigrationsAsync()
    {
        var result = new MigrationResult();

        try
        {
            // Check if database exists and is accessible
            if (!_context.Database.CanConnect())
            {
                result.Success = false;
                result.ErrorMessage = "Cannot connect to database. Please check connection string and database availability.";
                _logger.LogError(result.ErrorMessage);
                return result;
            }

            // Get pending migrations
            var pendingMigrations = _context.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Any())
            {
                result.PendingMigrations = pendingMigrations;
                _logger.LogInformation("Found {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count, string.Join(", ", pendingMigrations));
            }
            else
            {
                _logger.LogInformation("No pending migrations found");
            }

            // Apply migrations
            _context.Database.Migrate();
            result.Success = true;
            result.Message = "Database migrations applied successfully";
            _logger.LogInformation(result.Message);

            // Verify schema after migration
            var schemaResult = await VerifySchemaAsync();
            if (!schemaResult.Success)
            {
                result.MissingColumns = schemaResult.MissingColumns;
                _logger.LogWarning("Schema verification found missing columns: {Columns}", 
                    string.Join(", ", schemaResult.MissingColumns));
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error applying database migrations");

            // Log specific migration error details
            if (ex.Message.Contains("Unknown column"))
            {
                _logger.LogError("Database schema mismatch detected. This may indicate a failed or incomplete migration.");
                _logger.LogError("Please check if all migrations have been applied correctly.");
            }

            return result;
        }
    }

    public async Task<MigrationResult> VerifySchemaAsync()
    {
        var result = new MigrationResult();

        try
        {
            // Check for critical columns
            var criticalColumns = new[] { "DataHash", "UserId", "FilePath", "StorageProvider" };
            var missingColumns = new List<string>();

            foreach (var column in criticalColumns)
            {
                var columnExists = await _context.Database.ExecuteSqlRawAsync(
                    $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'DataSets' AND COLUMN_NAME = '{column}'");
                
                if (columnExists == 0)
                {
                    missingColumns.Add(column);
                    _logger.LogWarning("Critical column '{Column}' is missing from DataSets table", column);
                }
                else
                {
                    _logger.LogInformation("Column '{Column}' exists in DataSets table", column);
                }
            }

            // Check if DataSetRows table exists
            var tableExists = await _context.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataSetRows'");
            
            if (tableExists == 0)
            {
                missingColumns.Add("DataSetRows table");
                _logger.LogWarning("DataSetRows table is missing");
            }
            else
            {
                _logger.LogInformation("DataSetRows table exists");
            }

            result.Success = missingColumns.Count == 0;
            result.MissingColumns = missingColumns;
            result.Message = missingColumns.Count == 0 ? "Schema verification passed" : "Schema verification found missing elements";

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogWarning(ex, "Error verifying database schema");
            return result;
        }
    }
} 