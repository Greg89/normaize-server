using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Normaize.DataNormalization.Infrastructure.Data;

namespace Normaize.DataNormalization.API.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class ApiTestApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            services.RemoveAll(typeof(DbContextOptions<DataNormalizationDbContext>));
            services.RemoveAll(typeof(DataNormalizationDbContext));

            // Add in-memory database for testing
            services.AddDbContext<DataNormalizationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
            });

            // Build the service provider
            var serviceProvider = services.BuildServiceProvider();

            // Create a scope to get the database context
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataNormalizationDbContext>();

            // Ensure the database is created
            context.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}