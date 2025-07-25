using Microsoft.Extensions.DependencyInjection;
using Normaize.API.Configuration;
using FluentAssertions;
using Xunit;

namespace Normaize.Tests.Integration;

public class ConfigurationIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public ConfigurationIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void AppConfiguration_ShouldLoadEnvironmentVariables()
    {
        // Arrange & Act
        var environment = AppConfiguration.GetEnvironment();
        var port = AppConfiguration.GetPort();

        // Assert
        environment.Should().NotBeNullOrEmpty();
        port.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AppConfiguration_ShouldHandleDatabaseConfig()
    {
        // Arrange & Act
        var dbConfig = AppConfiguration.GetDatabaseConfig();

        // Assert
        dbConfig.Should().NotBeNull();
    }

    [Fact]
    public void ServiceConfiguration_ShouldRegisterRequiredServices()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var services = _factory.Services;

        // Assert
        services.Should().NotBeNull();

        // Verify key services are registered
        var httpClientFactory = services.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();
    }
}