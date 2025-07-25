using Microsoft.Extensions.DependencyInjection;
using Normaize.Core.DTOs;
using Normaize.Data;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Integration;

public class UserSettingsIntegrationTests : IClassFixture<TestWebApplicationFactory>, IDisposable
{
    // Force static constructor to run by referencing a static readonly field
    private static readonly object _ = InitTestEnv();
    private static object InitTestEnv() { var _ = typeof(TestSetup); return null!; }

    private readonly TestWebApplicationFactory _factory;
    private readonly NormaizeContext _context;

    public UserSettingsIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        
        // Get the database context from the factory
        var scope = _factory.Services.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<NormaizeContext>();
        
        // Ensure database is created
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetUserSettings_WhenAuthenticated_ShouldReturnUserSettings()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Add authentication header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.GetAsync("/api/usersettings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateUserSettings_WhenAuthenticated_ShouldUpdateSettings()
    {
        // Arrange
        var client = _factory.CreateClient();
        var updateDto = new UpdateUserSettingsDto
        {
            Theme = "dark",
            Language = "en",
            TimeZone = "UTC"
        };
        
        var json = JsonSerializer.Serialize(updateDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Add authentication header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.PutAsync("/api/usersettings", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSettingValue_WhenAuthenticated_ShouldReturnSettingValue()
    {
        // Arrange
        var client = _factory.CreateClient();
        var settingName = "Theme";
        
        // Add authentication header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.GetAsync($"/api/usersettings/setting/{settingName}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateSettingValue_WhenAuthenticated_ShouldUpdateSetting()
    {
        // Arrange
        var client = _factory.CreateClient();
        var settingName = "Theme";
        var settingValue = "light";
        
        // Send the value as a JSON string, not as a JSON object
        var json = $"\"{settingValue}\"";
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        // Add authentication header
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await client.PutAsync($"/api/usersettings/setting/{settingName}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
} 