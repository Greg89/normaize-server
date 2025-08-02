using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using System.Security.Claims;
using Xunit;
using FluentAssertions;
using Normaize.Tests.Repositories;

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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserSettingsDto>>>().Subject;
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserSettingsDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(userId);
        apiResponse.Data.Theme.Should().Be("dark");
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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserSettingsDto>>>().Subject;
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserSettingsDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(userId);

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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserSettingsDto>>>().Subject;
        var statusResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(401);

        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<UserSettingsDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("not authorized");
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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserSettingsDto>>>().Subject;
        var statusResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);

        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<UserSettingsDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("unexpected error");

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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserSettingsDto>>>().Subject;
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserSettingsDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(userId);
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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserSettingsDto>>>().Subject;
        var statusResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(401);

        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<UserSettingsDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("not authorized");
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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserProfileDto>>>().Subject;
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserProfileDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(userId);
        apiResponse.Data.Settings.Should().NotBeNull();
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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserProfileDto>>>().Subject;
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserProfileDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(userId);
        apiResponse.Data.Settings.Should().NotBeNull();

        _mockUserSettingsService.Verify(s => s.InitializeUserSettingsAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetSettingValue_WithExistingSetting_ShouldReturnOk()
    {
        // Arrange
        var userId = "auth0|123456789";
        var settingValue = "dark";

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetSettingValueAsync<object>(userId, "Theme"))
            .ReturnsAsync(settingValue);

        // Act
        var result = await _controller.GetSettingValue("Theme");

        // Assert
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<object>>>().Subject;
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().Be(settingValue);
    }

    [Fact]
    public async Task GetSettingValue_WithNonExistingSetting_ShouldReturnNotFound()
    {
        // Arrange
        var userId = "auth0|123456789";

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.GetSettingValueAsync<object>(userId, "NonExistent"))
            .ReturnsAsync((object?)null);

        // Act
        var result = await _controller.GetSettingValue("NonExistent");

        // Assert
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<object>>>().Subject;
        var statusResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(404);

        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task UpdateSettingValue_WithValidSetting_ShouldReturnOk()
    {
        // Arrange
        var userId = "auth0|123456789";
        var settingValue = "light";

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.UpdateSettingValueAsync(userId, "Theme", settingValue))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateSettingValue("Theme", settingValue);

        // Assert
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<object?>>>().Subject;
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object?>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().BeNull();
        apiResponse.Message.Should().Contain("updated successfully");
    }

    [Fact]
    public async Task UpdateSettingValue_WithInvalidSetting_ShouldReturnBadRequest()
    {
        // Arrange
        var userId = "auth0|123456789";

        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.UpdateSettingValueAsync(userId, "InvalidSetting", "value"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateSettingValue("InvalidSetting", "value");

        // Assert
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<object?>>>().Subject;
        var statusResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(400);

        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<object?>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("Invalid setting name");
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
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserSettingsDto>>>().Subject;
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserSettingsDto>>().Subject;

        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(userId);

        _mockUserSettingsService.Verify(s => s.DeleteUserSettingsAsync(userId), Times.Once);
        _mockUserSettingsService.Verify(s => s.InitializeUserSettingsAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ResetUserSettings_WhenServiceThrowsException_ShouldReturnInternalServerError()
    {
        // Arrange
        var userId = "auth0|123456789";
        SetupAuthenticatedUser(userId);
        _mockUserSettingsService.Setup(s => s.DeleteUserSettingsAsync(userId))
            .ThrowsAsync(new Exception("Reset failed"));

        // Act
        var result = await _controller.ResetUserSettings();

        // Assert
        var actionResult = result.Should().BeOfType<ActionResult<ApiResponse<UserSettingsDto>>>().Subject;
        var statusResult = actionResult.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);

        var apiResponse = statusResult.Value.Should().BeOfType<ApiResponse<UserSettingsDto>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("unexpected error");

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