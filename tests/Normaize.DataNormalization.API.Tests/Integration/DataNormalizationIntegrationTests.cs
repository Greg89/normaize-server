using System.Net;
using FluentAssertions;
using Xunit;
using Normaize.DataNormalization.API.Tests.Infrastructure;

namespace Normaize.DataNormalization.API.Tests.Integration;

public class DataNormalizationIntegrationTests : IClassFixture<ApiTestApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ApiTestApplicationFactory _factory;

    public DataNormalizationIntegrationTests(ApiTestApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task Swagger_IsAccessible()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiDocumentation_IsAvailable()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Normaize Data Normalization API");
    }
}