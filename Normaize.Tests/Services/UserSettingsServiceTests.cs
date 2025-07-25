using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Data.Repositories;
using Normaize.Data.Services;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Services;

public class UserSettingsServiceTests
{
    private readonly Mock<IUserSettingsRepository> _mockRepository;
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly UserSettingsService _service;

    public UserSettingsServiceTests()
    {
        _mockRepository = new Mock<IUserSettingsRepository>();
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _service = new UserSettingsService(_mockRepository.Object, _mockLoggingService.Object);
    }

    [Fact]
    public async Task GetUserSettingsAsync_WithExistingSettings_ShouldReturnSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userSettings);

        // Act
        var result = await _service.GetUserSettingsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Theme.Should().Be("dark");
        result.Language.Should().Be("es");
        result.EmailNotificationsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserSettingsAsync_WithNonExistingSettings_ShouldReturnNull()
    {
        // Arrange
        var userId = "auth0|123456789";
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserSettings?)null);

        // Act
        var result = await _service.GetUserSettingsAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveUserSettingsAsync_WithNewUser_ShouldCreateSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        var updateDto = new UpdateUserSettingsDto
        {
            Theme = "dark",
            Language = "es",
            EmailNotificationsEnabled = false
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserSettings?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UserSettings>()))
            .ReturnsAsync((UserSettings settings) => settings!);

        // Act
        var result = await _service.SaveUserSettingsAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Theme.Should().Be("dark");
        result.Language.Should().Be("es");
        result.EmailNotificationsEnabled.Should().BeFalse();
        
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserSettings>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSettings>()), Times.Never);
    }

    [Fact]
    public async Task SaveUserSettingsAsync_WithExistingUser_ShouldUpdateSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        var existingSettings = CreateTestUserSettings(userId);
        var updateDto = new UpdateUserSettingsDto
        {
            Theme = "light",
            Language = "en"
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(existingSettings);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserSettings>()))
            .ReturnsAsync((UserSettings settings) => settings);

        // Act
        var result = await _service.SaveUserSettingsAsync(userId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result.Theme.Should().Be("light");
        result.Language.Should().Be("en");
        // Other settings should remain unchanged
        result.EmailNotificationsEnabled.Should().BeFalse();
        
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSettings>()), Times.Once);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserSettings>()), Times.Never);
    }

    [Fact]
    public async Task GetUserProfileAsync_WithExistingSettings_ShouldReturnProfile()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userSettings);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Settings.Should().NotBeNull();
        result.Settings.Theme.Should().Be("dark");
    }

    [Fact]
    public async Task GetUserProfileAsync_WithNonExistingSettings_ShouldReturnNull()
    {
        // Arrange
        var userId = "auth0|123456789";
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserSettings?)null);

        // Act
        var result = await _service.GetUserProfileAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InitializeUserSettingsAsync_ShouldCreateDefaultSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UserSettings>()))
            .ReturnsAsync((UserSettings settings) => settings!);

        // Act
        var result = await _service.InitializeUserSettingsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Theme.Should().Be("light"); // Default value
        result.Language.Should().Be("en"); // Default value
        result.EmailNotificationsEnabled.Should().BeTrue(); // Default value
        
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserSettings>()), Times.Once);
        _mockLoggingService.Verify(l => l.LogUserAction("User settings initialized", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserSettingsAsync_WithExistingSettings_ShouldReturnTrue()
    {
        // Arrange
        var userId = "auth0|123456789";
        _mockRepository.Setup(r => r.DeleteAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteUserSettingsAsync(userId);

        // Assert
        result.Should().BeTrue();
        _mockLoggingService.Verify(l => l.LogUserAction("User settings deleted", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserSettingsAsync_WithNonExistingSettings_ShouldReturnFalse()
    {
        // Arrange
        var userId = "auth0|123456789";
        _mockRepository.Setup(r => r.DeleteAsync(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.DeleteUserSettingsAsync(userId);

        // Assert
        result.Should().BeFalse();
        _mockLoggingService.Verify(l => l.LogUserAction("User settings deleted", It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task GetSettingValueAsync_WithExistingSetting_ShouldReturnValue()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userSettings);

        // Act
        var result = await _service.GetSettingValueAsync<string>(userId, "Theme");

        // Assert
        result.Should().Be("dark");
    }

    [Fact]
    public async Task GetSettingValueAsync_WithNonExistingSetting_ShouldReturnNull()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userSettings);

        // Act
        var result = await _service.GetSettingValueAsync<string>(userId, "NonExistentSetting");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSettingValueAsync_WithNonExistingUser_ShouldReturnNull()
    {
        // Arrange
        var userId = "auth0|123456789";
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserSettings?)null);

        // Act
        var result = await _service.GetSettingValueAsync<string>(userId, "Theme");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateSettingValueAsync_WithValidSetting_ShouldReturnTrue()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userSettings);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserSettings>()))
            .ReturnsAsync((UserSettings settings) => settings);

        // Act
        var result = await _service.UpdateSettingValueAsync(userId, "Theme", "light");

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSettings>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingValueAsync_WithInvalidSetting_ShouldReturnFalse()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userSettings);

        // Act
        var result = await _service.UpdateSettingValueAsync(userId, "InvalidSetting", "value");

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserSettings>()), Times.Never);
    }

    [Fact]
    public async Task SaveUserSettingsAsync_WhenRepositoryThrowsException_ShouldLogAndRethrow()
    {
        // Arrange
        var userId = "auth0|123456789";
        var updateDto = new UpdateUserSettingsDto { Theme = "dark" };
        var exception = new Exception("Database error");
        
        _mockRepository.Setup(r => r.GetByUserIdAsync(userId))
            .ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.SaveUserSettingsAsync(userId, updateDto));
        
        _mockLoggingService.Verify(l => l.LogException(exception, It.IsAny<string>()), Times.Once);
    }

    private static UserSettings CreateTestUserSettings(string userId)
    {
        return new UserSettings
        {
            Id = 1,
            UserId = userId,
            Theme = "dark",
            Language = "es",
            EmailNotificationsEnabled = false,
            PushNotificationsEnabled = true,
            ProcessingCompleteNotifications = true,
            ErrorNotifications = true,
            WeeklyDigestEnabled = false,
            DefaultPageSize = 25,
            ShowTutorials = false,
            CompactMode = true,
            AutoProcessUploads = true,
            MaxPreviewRows = 50,
            DefaultFileType = "JSON",
            EnableDataValidation = true,
            EnableSchemaInference = false,
            ShareAnalytics = false,
            AllowDataUsageForImprovement = true,
            ShowProcessingTime = false,
            DisplayName = "Test User",
            TimeZone = "America/New_York",
            DateFormat = "MM/dd/yyyy",
            TimeFormat = "24h",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }
} 