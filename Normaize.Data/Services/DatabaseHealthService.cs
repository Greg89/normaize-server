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
        _logger.LogInformation("Starting database health check at {Timestamp}", DateTime.UtcNow);

        try
        {
            if (!await CheckConnectivityAsync(cancellationToken))
                return CreateUnhealthyResult("Cannot connect to database");

            if (IsInMemoryDatabase())
                return CreateHealthyResult();

            var foundColumns = await GetExistingColumnsAsync(cancellationToken);
            var missingColumns = _config.CriticalColumns.Except(foundColumns).ToList();
            
            if (missingColumns.Count > 0)
            {
                _logger.LogWarning("Database missing critical columns: {Columns}", string.Join(", ", missingColumns));
                return CreateUnhealthyResult("Missing critical columns", missingColumns);
            }

            return CreateHealthyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed: {Message}", ex.Message);
            return CreateUnhealthyResult(ex.Message);
        }
    }

    private async Task<bool> CheckConnectivityAsync(CancellationToken cancellationToken)
    {
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            _logger.LogWarning("Database connectivity check failed");
        }
        return canConnect;
    }

    private bool IsInMemoryDatabase()
    {
        var isInMemory = _context.Database.ProviderName?.Contains("InMemory") == true;
        if (isInMemory)
        {
            _logger.LogInformation("In-memory database detected, skipping schema checks");
        }
        return isInMemory;
    }

    private async Task<List<string>> GetExistingColumnsAsync(CancellationToken cancellationToken)
    {
        var providerName = _context.Database.ProviderName ?? string.Empty;
        
        if (providerName.Contains("Sqlite"))
            return await GetSqliteColumnsAsync(cancellationToken);
        
        return await GetStandardColumnsAsync(cancellationToken);
    }

    private async Task<List<string>> GetSqliteColumnsAsync(CancellationToken cancellationToken)
    {
        var foundColumns = new List<string>();
        var sql = "PRAGMA table_info(DataSets)";
        
        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        
        if (command.Connection?.State != System.Data.ConnectionState.Open)
            await command.Connection!.OpenAsync(cancellationToken);
            
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var columnName = reader.GetString(1); // Column name is at index 1
            foundColumns.Add(columnName);
        }
        
        return foundColumns;
    }

    private async Task<List<string>> GetStandardColumnsAsync(CancellationToken cancellationToken)
    {
        var foundColumns = new List<string>();
        var sqlBuilder = new System.Text.StringBuilder(@"
            SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = 'DataSets' AND COLUMN_NAME IN (");
        
        using var command = _context.Database.GetDbConnection().CreateCommand();
        
        // Build parameterized query with proper parameters
        var parameters = new List<System.Data.Common.DbParameter>();
        for (int i = 0; i < _config.CriticalColumns.Length; i++)
        {
            if (i > 0) sqlBuilder.Append(',');
            var paramName = $"@column{i}";
            sqlBuilder.Append(paramName);
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = paramName;
            parameter.Value = _config.CriticalColumns[i];
            parameters.Add(parameter);
        }
        sqlBuilder.Append(')');
        var sql = sqlBuilder.ToString();
        
        command.CommandText = sql;
        command.Parameters.AddRange(parameters.ToArray());
        
        if (command.Connection?.State != System.Data.ConnectionState.Open)
            await command.Connection!.OpenAsync(cancellationToken);
            
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            foundColumns.Add(reader.GetString(0));
        }
        
        return foundColumns;
    }

    private static DatabaseHealthResult CreateHealthyResult()
    {
        return new DatabaseHealthResult
        {
            IsHealthy = true,
            Status = "healthy",
            ErrorMessage = null
        };
    }

    private static DatabaseHealthResult CreateUnhealthyResult(string errorMessage, List<string>? missingColumns = null)
    {
        return new DatabaseHealthResult
        {
            IsHealthy = false,
            Status = "unhealthy",
            ErrorMessage = errorMessage,
            MissingColumns = missingColumns ?? []
        };
    }
} 