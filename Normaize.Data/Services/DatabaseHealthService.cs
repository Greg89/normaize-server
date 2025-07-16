using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.Core.Interfaces;
using Normaize.Data;
using System.ComponentModel.DataAnnotations;

namespace Normaize.Data.Services;

public class DatabaseHealthConfiguration
{
    [Required]
    public string[] CriticalColumns { get; set; } = new[] { "DataHash", "UserId", "FilePath", "StorageProvider" };
}

public class DatabaseHealthService : IDatabaseHealthService
{
    private readonly NormaizeContext _context;
    private readonly ILogger<DatabaseHealthService> _logger;
    private readonly DatabaseHealthConfiguration _config;

    public DatabaseHealthService(
        NormaizeContext context,
        ILogger<DatabaseHealthService> logger,
        IOptions<DatabaseHealthConfiguration> config)
    {
        _context = context;
        _logger = logger;
        _config = config?.Value ?? new DatabaseHealthConfiguration();
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(_config);
        if (!Validator.TryValidateObject(_config, context, results, true))
        {
            var errors = string.Join(", ", results.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"DatabaseHealth configuration validation failed: {errors}");
        }
    }

    /// <summary>
    /// Checks the health of the database, including connectivity and critical columns.
    /// </summary>
    public async Task<DatabaseHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = new DatabaseHealthResult();
        _logger.LogInformation("Starting database health check at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                result.IsHealthy = false;
                result.Status = "unhealthy";
                result.ErrorMessage = "Cannot connect to database";
                _logger.LogWarning("Database connectivity check failed");
                return result;
            }

            // Check if we're using an in-memory database
            var isInMemory = _context.Database.ProviderName?.Contains("InMemory") == true;
            if (isInMemory)
            {
                result.IsHealthy = true;
                result.Status = "healthy";
                result.ErrorMessage = null;
                _logger.LogInformation("In-memory database detected, skipping schema checks");
                return result;
            }

            // Batch check for all critical columns in a single query
            var foundColumns = new List<string>();
            
            // Use provider-specific queries
            var providerName = _context.Database.ProviderName ?? string.Empty;
            if (providerName.Contains("Sqlite"))
            {
                // SQLite uses PRAGMA table_info
                var sql = "PRAGMA table_info(DataSets)";
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync(cancellationToken);
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var columnName = reader.GetString(1); // Column name is at index 1
                        foundColumns.Add(columnName);
                    }
                }
            }
            else
            {
                // Other databases use INFORMATION_SCHEMA
                var columnsList = string.Join(",", _config.CriticalColumns.Select(c => $"'" + c.Replace("'", "''") + "'"));
                var sql = $@"
                    SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'DataSets' AND COLUMN_NAME IN ({columnsList})
                ";
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    if (command.Connection.State != System.Data.ConnectionState.Open)
                        await command.Connection.OpenAsync(cancellationToken);
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        foundColumns.Add(reader.GetString(0));
                    }
                }
            }
            
            var missingColumns = _config.CriticalColumns.Except(foundColumns).ToList();
            if (missingColumns.Any())
            {
                result.IsHealthy = false;
                result.Status = "unhealthy";
                result.ErrorMessage = "Missing critical columns";
                result.MissingColumns = missingColumns;
                _logger.LogWarning("Database missing critical columns: {Columns}", string.Join(", ", missingColumns));
                return result;
            }

            result.IsHealthy = true;
            result.Status = "healthy";
            _logger.LogInformation("Database health check passed");
            return result;
        }
        catch (Exception ex)
        {
            result.IsHealthy = false;
            result.Status = "unhealthy";
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Database health check failed: {Message}", ex.Message);
            return result;
        }
    }
} 