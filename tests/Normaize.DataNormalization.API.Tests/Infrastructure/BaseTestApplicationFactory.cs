using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.API.Tests.Infrastructure;

/// <summary>
/// Base test application factory for integration tests with proper isolation
/// </summary>
public abstract class BaseTestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    protected BaseTestApplicationFactory()
    {
        _databaseName = $"TestDB_{Guid.NewGuid():N}_{GetType().Name}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        // Configure app settings to avoid PostgreSQL configuration loading
        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.UseSetting("SkipInfrastructureRegistration", "true");
        builder.UseSetting("Features:EnableSwagger", "true");
        
        // Override the configuration to prevent the main Program from registering PostgreSQL services
        builder.ConfigureServices((context, services) =>
        {
            // Remove PostgreSQL DbContext registration
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<DataNormalizationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Remove any existing DataNormalizationDbContext registration
            var contextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DataNormalizationDbContext));
            if (contextDescriptor != null)
            {
                services.Remove(contextDescriptor);
            }

            // Add clean in-memory database
            services.AddDbContext<DataNormalizationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            });
            
            // Add our test infrastructure services
            services.AddTestDataNormalizationInfrastructureWithoutDatabase();

            // Ensures [Authorize] endpoints do not return 401 unless a test opts into that explicitly.
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", _ => { });
            services.AddAuthorization();
        });
    }

    public async Task SeedTestDataAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();
        
        await context.Database.EnsureCreatedAsync();
        
        if (!context.DataSets.Any())
        {
            await SeedDataSetsAsync(context);
            await context.SaveChangesAsync();
        }
    }

    private async Task SeedDataSetsAsync(DataNormalizationDbContext context)
    {
        var fileInfo = FileMetadata.Create("test.csv", "/test/test.csv", FileType.CSV, 1024);
        var stats = DatasetStatistics.Create(100, 5);

        var testDataSets = new[]
        {
            DataSet.Create(
                name: "Test Dataset 1",
                description: "Test dataset for integration tests",
                userId: "test-user-id",
                fileInfo: fileInfo,
                statistics: stats),
            DataSet.Create(
                name: "Test Dataset 2", 
                description: "Another test dataset",
                userId: "test-user-id",
                fileInfo: fileInfo,
                statistics: stats),
            DataSet.Create(
                name: "Test Dataset 3",
                description: "Third test dataset", 
                userId: "test-user-id",
                fileInfo: fileInfo,
                statistics: stats)
        };

        await context.DataSets.AddRangeAsync(testDataSets);
    }
}

/// <summary>
/// Test application factory that auto-seeds data for each test
/// </summary>
public class SeededApiTestApplicationFactory : BaseTestApplicationFactory, IAsyncLifetime
{
    async Task IAsyncLifetime.InitializeAsync() => await SeedTestDataAsync();

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();
}