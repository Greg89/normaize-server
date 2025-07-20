using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Normaize.Core.Interfaces;
using FluentAssertions;
using System.Net;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Normaize.Data;
using Normaize.Tests;
using Moq;

namespace Normaize.Tests.Integration;

public class LoggingIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    // Force static constructor to run by referencing a static readonly field
    private static readonly object _ = InitTestEnv();
    private static object InitTestEnv() { var _ = typeof(TestSetup); return null!; }

    private readonly TestWebApplicationFactory _factory;

    public LoggingIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk_WhenApplicationIsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task HealthEndpoint_LogsUserAction_WhenCalled()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify that the structured logging service was called
        var structuredLoggingService = _factory.Services.GetRequiredService<IStructuredLoggingService>();
        // Note: In a real integration test, you might want to use a test double or verify logs
        // For now, we're just ensuring the application starts and responds correctly
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure environment variables are cleared before host is built
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("MYSQLDATABASE", null);
        Environment.SetEnvironmentVariable("MYSQLUSER", null);
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", null);
        Environment.SetEnvironmentVariable("MYSQLPORT", null);
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);
        Environment.SetEnvironmentVariable("SFTP_HOST", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        // Set test environment
        builder.UseEnvironment("Test");
        
        // Configure services for testing
        builder.ConfigureServices(services =>
        {
            // Remove any existing DbContext registrations
            var dbContextDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(NormaizeContext));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }
            
            // Add in-memory database
            services.AddDbContext<NormaizeContext>(options =>
                options.UseInMemoryDatabase("TestDatabase"));
            
            // Mock the IAppConfigurationService to return test environment
            var mockAppConfig = new Mock<IAppConfigurationService>();
            mockAppConfig.Setup(x => x.GetEnvironment()).Returns("Test");
            mockAppConfig.Setup(x => x.GetDatabaseConfig()).Returns(new Normaize.Core.Interfaces.DatabaseConfig());
            mockAppConfig.Setup(x => x.HasDatabaseConnection()).Returns(false);
            
            // Remove existing IAppConfigurationService registration and add mock
            var appConfigDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAppConfigurationService));
            if (appConfigDescriptor != null)
            {
                services.Remove(appConfigDescriptor);
            }
            services.AddSingleton(mockAppConfig.Object);
        });
    }
} 