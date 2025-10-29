using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Normaize.DataNormalization.Infrastructure.Data;

/// <summary>
/// Design-time factory for DataNormalizationDbContext
/// </summary>
public class DataNormalizationDbContextFactory : IDesignTimeDbContextFactory<DataNormalizationDbContext>
{
    public DataNormalizationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DataNormalizationDbContext>();

        // Try to read from environment variable first (for CI/CD)
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        
        // Fall back to default connection string for local development
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "Host=localhost;Port=5432;Database=normaize;Username=normaize_user;Password=normaize_password";
        }

        Console.WriteLine($"Using connection string: {MaskPassword(connectionString)}");

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(DataNormalizationDbContext).Assembly.FullName);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });

        return new DataNormalizationDbContext(optionsBuilder.Options);
    }

    private static string MaskPassword(string connectionString)
    {
        var parts = connectionString.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase))
            {
                parts[i] = "Password=***";
            }
        }
        return string.Join(";", parts);
    }
}
