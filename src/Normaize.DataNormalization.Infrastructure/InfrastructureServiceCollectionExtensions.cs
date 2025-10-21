using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Infrastructure.Repositories;
using Normaize.DataNormalization.Infrastructure.Services;

namespace Normaize.DataNormalization.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDataNormalizationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<DataNormalizationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(DataNormalizationDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
            
            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Repositories
        services.AddScoped<INormalizationJobRepository, NormalizationJobRepository>();

        // Services
        services.AddScoped<IJobQueue, EfCoreJobQueue>();
        services.AddScoped<IJobProgress, EfCoreJobProgress>();

        return services;
    }
}
