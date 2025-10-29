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

        // Default connection string for design-time
        var connectionString = "Host=localhost;Port=5432;Database=normaize;Username=normaize_user;Password=normaize_password";

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
}
