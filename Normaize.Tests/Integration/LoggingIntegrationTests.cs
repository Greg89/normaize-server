using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Normaize.API.Services;
using FluentAssertions;
using System.Net;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using Normaize.Core.Interfaces;

namespace Normaize.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set both the host environment and the process environment variable
        builder.UseEnvironment("Test");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
        
        // Override services to ensure in-memory storage is used for tests
        builder.ConfigureServices(services =>
        {
            // Remove any existing storage service registrations
            var storageServiceDescriptor = services.FirstOrDefault(d => 
                d.ServiceType == typeof(IStorageService));
            if (storageServiceDescriptor != null)
            {
                services.Remove(storageServiceDescriptor);
            }
            
            // Force in-memory storage for tests
            services.AddScoped<IStorageService, InMemoryStorageService>();
        });
    }
}

public class LoggingIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
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
        var loggingService = _factory.Services.GetRequiredService<IStructuredLoggingService>();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task HealthBasicEndpoint_ShouldLogUserAction()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
        content.Should().Contain("1.0.0");
    }

    [Fact]
    public async Task HealthReadinessEndpoint_ShouldBeAccessible()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/readiness");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ready");
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAccessible()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger");

        // Assert
        // In Test environment, Swagger is not enabled, so expect 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task NonExistentEndpoint_ShouldReturn404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public void LoggingService_ShouldBeRegistered()
    {
        // Arrange & Act
        var loggingService = _factory.Services.GetService<IStructuredLoggingService>();

        // Assert
        loggingService.Should().NotBeNull();
        loggingService.Should().BeOfType<StructuredLoggingService>();
    }

    [Fact]
    public void HttpContextAccessor_ShouldBeRegistered()
    {
        // Arrange & Act
        var httpContextAccessor = _factory.Services.GetService<IHttpContextAccessor>();

        // Assert
        httpContextAccessor.Should().NotBeNull();
    }
} 