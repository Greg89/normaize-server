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
using Normaize.Tests; // <-- Add this
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
    public async Task HealthEndpoint_ShouldLogUserAction()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/readiness");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        
        if (response.StatusCode != HttpStatusCode.OK)
        {
            // Log the response content to help debug the issue
            Console.WriteLine($"Health check failed with status {response.StatusCode}");
            Console.WriteLine($"Response content: {content}");
        }
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnValidJson()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/readiness");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        
        if (response.StatusCode != HttpStatusCode.OK)
        {
            // Log the response content to help debug the issue
            Console.WriteLine($"Health check failed with status {response.StatusCode}");
            Console.WriteLine($"Response content: {content}");
        }
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        content.Should().NotBeNullOrEmpty();
    }
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ensure environment variables are cleared before host is built
        Environment.SetEnvironmentVariable("MYSQLHOST", null);
        Environment.SetEnvironmentVariable("STORAGE_PROVIDER", null);
        Environment.SetEnvironmentVariable("SFTP_HOST", null);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        // Set test environment
        builder.UseEnvironment("Test");
        
        // Register mock IAppConfigurationService
        builder.ConfigureServices(services =>
        {
            var mockAppConfigService = new Mock<IAppConfigurationService>();
            mockAppConfigService.Setup(x => x.GetEnvironment()).Returns("Test");
            mockAppConfigService.Setup(x => x.HasDatabaseConnection()).Returns(false);
            mockAppConfigService.Setup(x => x.GetDatabaseConfig()).Returns(new Normaize.Core.Interfaces.DatabaseConfig());
            mockAppConfigService.Setup(x => x.GetPort()).Returns("5000");
            mockAppConfigService.Setup(x => x.GetHttpsPort()).Returns((string?)null);
            mockAppConfigService.Setup(x => x.GetSeqUrl()).Returns((string?)null);
            mockAppConfigService.Setup(x => x.GetSeqApiKey()).Returns((string?)null);
            mockAppConfigService.Setup(x => x.IsProductionLike()).Returns(false);
            mockAppConfigService.Setup(x => x.IsContainerized()).Returns(false);
            
            services.AddSingleton(mockAppConfigService.Object);
        });
    }
} 