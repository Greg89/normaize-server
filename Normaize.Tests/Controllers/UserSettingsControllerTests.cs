using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using System.Security.Claims;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Controllers;

public class UserSettingsControllerTests
{
    private readonly Mock<IUserSettingsService> _mockUserSettingsService;
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly UserSettingsController _controller;

    public UserSettingsControllerTests()
    {
        _mockUserSettingsService = new Mock<IUserSettingsService>();
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _controller = new UserSettingsController(_mockUserSettingsService.Object, _mockLoggingService.Object);
    }

    [Fact]
    public async Task GetUserSettings_WithExistingSettings_ShouldReturnOk()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettingsDto(userId);

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetUserSettingsAsync(userId))
            .ReturnsAsync(userSettings);

        // Act
        var result = await _controller.GetUserSettings();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSettings = okResult.Value.Should().BeOfType<UserSettingsDto>().Subject;
        returnedSettings.UserId.Should().Be(userId);
        returnedSettings.Theme.Should().Be("dark");
    }

    [Fact]
    public async Task GetUserSettings_WithNonExistingSettings_ShouldInitializeAndReturnSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettingsDto(userId);

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetUserSettingsAsync(userId))
            .ReturnsAsync((UserSettingsDto?)null);
        _mockUserSettingsService.Setup(s => s.InitializeUserSettingsAsync(userId))
            .ReturnsAsync(userSettings);

        // Act
        var result = await _controller.GetUserSettings();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSettings = okResult.Value.Should().BeOfType<UserSettingsDto>().Subject;
        returnedSettings.UserId.Should().Be(userId);

        _mockUserSettingsService.Verify(s => s.InitializeUserSettingsAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetUserSettings_WithUnauthorizedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        _mockUserSettingsService.Setup(s => s.GetUserSettingsAsync(It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.GetUserSettings();

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetUserSettings_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var userId = "auth0|123456789";
        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetUserSettingsAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUserSettings();

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Error retrieving user settings");

        _mockLoggingService.Verify(l => l.LogException(It.IsAny<Exception>(), "GetUserSettings"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserSettings_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var userId = "auth0|123456789";
        var updateDto = new UpdateUserSettingsDto
        {
            Theme = "dark",
            Language = "es",
            EmailNotificationsEnabled = false
        };
        var updatedSettings = CreateTestUserSettingsDto(userId);

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.SaveUserSettingsAsync(userId, updateDto))
            .ReturnsAsync(updatedSettings);

        // Act
        var result = await _controller.UpdateUserSettings(updateDto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSettings = okResult.Value.Should().BeOfType<UserSettingsDto>().Subject;
        returnedSettings.UserId.Should().Be(userId);

        _mockLoggingService.Verify(l => l.LogUserAction("User settings updated", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserSettings_WithUnauthorizedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var updateDto = new UpdateUserSettingsDto { Theme = "dark" };
        SetupUnauthenticatedUser();
        _mockUserSettingsService.Setup(s => s.SaveUserSettingsAsync(It.IsAny<string>(), It.IsAny<UpdateUserSettingsDto>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        // Act
        var result = await _controller.UpdateUserSettings(updateDto);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetUserProfile_WithExistingSettings_ShouldReturnOk()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userProfile = CreateTestUserProfileDto(userId);

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetUserProfileAsync(userId))
            .ReturnsAsync(userProfile);

        // Act
        var result = await _controller.GetUserProfile();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfile = okResult.Value.Should().BeOfType<UserProfileDto>().Subject;
        returnedProfile.UserId.Should().Be(userId);
        returnedProfile.Settings.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserProfile_WithNonExistingSettings_ShouldInitializeAndReturnProfile()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettingsDto(userId);

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetUserProfileAsync(userId))
            .ReturnsAsync((UserProfileDto?)null);
        _mockUserSettingsService.Setup(s => s.InitializeUserSettingsAsync(userId))
            .ReturnsAsync(userSettings);

        // Act
        var result = await _controller.GetUserProfile();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProfile = okResult.Value.Should().BeOfType<UserProfileDto>().Subject;
        returnedProfile.UserId.Should().Be(userId);
        returnedProfile.Settings.Should().NotBeNull();

        _mockUserSettingsService.Verify(s => s.InitializeUserSettingsAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetSettingValue_WithExistingSetting_ShouldReturnOk()
    {
        // Arrange
        var userId = "auth0|123456789";
        var settingName = "Theme";
        var settingValue = "dark";

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetSettingValueAsync<object>(userId, settingName))
            .ReturnsAsync(settingValue);

        // Act
        var result = await _controller.GetSettingValue(settingName);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        // The controller returns an anonymous type { SettingName = settingName, Value = value }
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSettingValue_WithNonExistingSetting_ShouldReturnNotFound()
    {
        // Arrange
        var userId = "auth0|123456789";
        var settingName = "NonExistentSetting";

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetSettingValueAsync<object>(userId, settingName))
            .ReturnsAsync((object?)null);

        // Act
        var result = await _controller.GetSettingValue(settingName);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().Be($"Setting '{settingName}' not found");
    }

    [Fact]
    public async Task UpdateSettingValue_WithValidSetting_ShouldReturnOk()
    {
        // Arrange
        var userId = "auth0|123456789";
        var settingName = "Theme";
        var settingValue = "light";

        SetupAuthenticatedUser(userId);
        // Ensure the mock returns true for a valid setting
        // Use It.IsAny<object>() to match the generic type parameter used by the controller
        _mockUserSettingsService.Setup(s => s.UpdateSettingValueAsync(userId, settingName, It.IsAny<object>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateSettingValue(settingName, settingValue);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        // The controller returns an anonymous type { Message = "Setting updated successfully" }
        okResult.Value.Should().NotBeNull();

        _mockLoggingService.Verify(l => l.LogUserAction("Setting value updated", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingValue_WithInvalidSetting_ShouldReturnBadRequest()
    {
        // Arrange
        var userId = "auth0|123456789";
        var settingName = "InvalidSetting";
        var settingValue = "value";

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.UpdateSettingValueAsync(userId, settingName, settingValue))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateSettingValue(settingName, settingValue);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be($"Invalid setting name: {settingName}");
    }

    [Fact]
    public async Task ResetUserSettings_ShouldDeleteAndInitializeNewSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        var newSettings = CreateTestUserSettingsDto(userId);

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.DeleteUserSettingsAsync(userId))
            .ReturnsAsync(true);
        _mockUserSettingsService.Setup(s => s.InitializeUserSettingsAsync(userId))
            .ReturnsAsync(newSettings);

        // Act
        var result = await _controller.ResetUserSettings();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSettings = okResult.Value.Should().BeOfType<UserSettingsDto>().Subject;
        returnedSettings.UserId.Should().Be(userId);

        _mockUserSettingsService.Verify(s => s.DeleteUserSettingsAsync(userId), Times.Once);
        _mockUserSettingsService.Verify(s => s.InitializeUserSettingsAsync(userId), Times.Once);
        _mockLoggingService.Verify(l => l.LogUserAction("User settings reset to defaults", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ResetUserSettings_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var userId = "auth0|123456789";
        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.DeleteUserSettingsAsync(userId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ResetUserSettings();

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Error resetting user settings");

        _mockLoggingService.Verify(l => l.LogException(It.IsAny<Exception>(), "ResetUserSettings"), Times.Once);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, "user@example.com"),
            new(ClaimTypes.Name, "Test User")
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    private void SetupUnauthenticatedUser()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    private static UserSettingsDto CreateTestUserSettingsDto(string userId)
    {
        return new UserSettingsDto
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

    private static UserProfileDto CreateTestUserProfileDto(string userId)
    {
        return new UserProfileDto
        {
            UserId = userId,
            Email = "user@example.com",
            Name = "Test User",
            Picture = "https://example.com/avatar.jpg",
            EmailVerified = true,
            Settings = CreateTestUserSettingsDto(userId)
        };
    }
}