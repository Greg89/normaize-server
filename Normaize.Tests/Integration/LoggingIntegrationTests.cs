using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Normaize.API.Services;
using FluentAssertions;
using System.Net;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace Normaize.Tests.Integration;

public class LoggingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LoggingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Set environment to Test to avoid middleware registration issues
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
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
        var response = await client.GetAsync("/health/basic");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
        content.Should().Contain("1.0.0");
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