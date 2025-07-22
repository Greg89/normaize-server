using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Normaize.Core.Interfaces;

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

    public Task<MigrationResult> ApplyMigrations()
    {
        var result = new MigrationResult();

        try
        {
            _logger.LogInformation("Starting database migration process...");

            // Check if database exists and is accessible
            if (!_context.Database.CanConnect())
            {
                result.Success = false;
                result.ErrorMessage = "Cannot connect to database. Please check connection string and database availability.";
                _logger.LogError("Database migration failed: {ErrorMessage}", result.ErrorMessage);
                return Task.FromResult(result);
            }

            // Get pending migrations
            var pendingMigrations = _context.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Count > 0)
            {
                result.PendingMigrations = pendingMigrations;
                _logger.LogInformation("Found {Count} pending migrations: {Migrations}", 
                    pendingMigrations.Count, string.Join(", ", pendingMigrations));
            }
            else
            {
                _logger.LogInformation("No pending migrations found");
            }

            // Apply migrations using EF Core's built-in mechanism
            // This handles all the complexity internally
            _context.Database.Migrate();
            
            result.Success = true;
            result.Message = "Database migrations applied successfully";
            _logger.LogInformation("Database migration completed: {Message}", result.Message);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error applying database migrations");

            // Provide helpful error messages for common issues
            if (ex.Message.Contains("already exists"))
            {
                _logger.LogError("Table already exists error detected. This indicates a database state conflict.");
                _logger.LogError("Manual intervention required: Drop existing tables or reset database.");
                result.ErrorMessage = "Database state conflict detected. Manual intervention required.";
            }
            else if (ex.Message.Contains("Unknown column"))
            {
                _logger.LogError("Database schema mismatch detected. This may indicate a failed or incomplete migration.");
                result.ErrorMessage = "Database schema mismatch detected. Manual intervention required.";
            }

            return Task.FromResult(result);
        }
    }

    public async Task<MigrationResult> VerifySchemaAsync()
    {
        var result = new MigrationResult();

        try
        {
            // Simple schema verification - just check if critical tables exist
            var dataSetsTableExists = await _context.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataSets'");
            
            var dataSetRowsTableExists = await _context.Database.ExecuteSqlRawAsync(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DataSetRows'");

            var missingTables = new List<string>();
            if (dataSetsTableExists == 0) missingTables.Add("DataSets");
            if (dataSetRowsTableExists == 0) missingTables.Add("DataSetRows");

            result.Success = missingTables.Count == 0;
            result.MissingColumns = missingTables; // Reusing for missing tables
            result.Message = missingTables.Count == 0 ? "Schema verification passed" : $"Missing tables: {string.Join(", ", missingTables)}";

            _logger.LogInformation("Schema verification: {Result}", result.Message);

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