using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.Application.Users.Commands.RegisterUser;
using Normaize.DataNormalization.Application.Users.Commands.UpdateAllSettings;
using Normaize.DataNormalization.Application.Users.Queries.GetUserProfile;
using Normaize.DataNormalization.Application.Users.DTOs;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using System.Security.Claims;
using Xunit;

namespace Normaize.DataNormalization.API.Tests.Controllers;

/// <summary>
/// Unit tests for UserSettingsController
/// </summary>
public class UserSettingsControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<UserSettingsController>> _loggerMock;
    private readonly UserSettingsController _controller;
    private const string TestAuth0UserId = "auth0|123456789";
    private const string TestEmail = "test@example.com";
    private const string TestName = "Test User";

    public UserSettingsControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserSettingsController>>();
        
        _controller = new UserSettingsController(
            _mediatorMock.Object,
            _userRepositoryMock.Object,
            _loggerMock.Object);

        // Set up HttpContext with authenticated user claims
        SetupAuthenticatedUser(TestAuth0UserId, TestEmail, TestName);
    }

    [Fact]
    public async Task GetUserProfile_WithExistingUser_ShouldReturnOk()
    {
        // Arrange
        var expectedProfile = CreateTestUserProfileDto();

        _mediatorMock.Setup(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProfile);

        // Act
        var result = await _controller.GetUserProfile();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserProfileResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(TestAuth0UserId);
        apiResponse.Data.Email.Should().Be(TestEmail);
        apiResponse.Data.Name.Should().Be(TestName);
        apiResponse.Data.Settings.Should().NotBeNull();
        apiResponse.Data.Settings.UserId.Should().Be(TestAuth0UserId);

        _mediatorMock.Verify(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserProfile_WithNonExistingUser_ShouldAutoRegisterAndReturnOk()
    {
        // Arrange
        _mediatorMock.Setup(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null);

        var newProfile = CreateTestUserProfileDto();
        _mediatorMock.Setup(x => x.Send(
            It.Is<RegisterUserCommand>(c => c.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(newProfile);

        // Act
        var result = await _controller.GetUserProfile();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserProfileResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.UserId.Should().Be(TestAuth0UserId);

        _mediatorMock.Verify(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _mediatorMock.Verify(x => x.Send(
            It.Is<RegisterUserCommand>(c => c.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserProfile_ShouldHandleException()
    {
        // Arrange
        _mediatorMock.Setup(x => x.Send(
            It.IsAny<GetUserProfileQuery>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetUserProfile();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdateUserProfile_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var currentProfile = CreateTestUserProfileDto();
        var updateRequest = new UpdateUserSettingsRequest
        {
            Theme = "dark",
            EmailNotificationsEnabled = true
        };

        _mediatorMock.Setup(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile);

        _mediatorMock.Setup(x => x.Send(
            It.IsAny<UpdateAllSettingsCommand>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var updatedProfile = CreateTestUserProfileDto();
        updatedProfile.Preferences.Theme = "dark";
        _mediatorMock.SetupSequence(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile)
            .ReturnsAsync(updatedProfile);

        // Act
        var result = await _controller.UpdateUserProfile(updateRequest);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserProfileResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Settings.Theme.Should().Be("dark");

        _mediatorMock.Verify(x => x.Send(
            It.IsAny<UpdateAllSettingsCommand>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfile_WithNonExistingUser_ShouldAutoRegisterThenUpdate()
    {
        // Arrange
        var updateRequest = new UpdateUserSettingsRequest
        {
            Theme = "dark"
        };

        var newProfile = CreateTestUserProfileDto();
        
        _mediatorMock.SetupSequence(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfileDto?)null)
            .ReturnsAsync(newProfile);

        _mediatorMock.Setup(x => x.Send(
            It.Is<RegisterUserCommand>(c => c.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(newProfile);

        _mediatorMock.Setup(x => x.Send(
            It.IsAny<UpdateAllSettingsCommand>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        var result = await _controller.UpdateUserProfile(updateRequest);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserProfileResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();

        _mediatorMock.Verify(x => x.Send(
            It.Is<RegisterUserCommand>(c => c.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _mediatorMock.Verify(x => x.Send(
            It.IsAny<UpdateAllSettingsCommand>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfile_WithDisplayName_ShouldUpdateDisplayName()
    {
        // Arrange
        var currentProfile = CreateTestUserProfileDto();
        var updateRequest = new UpdateUserSettingsRequest
        {
            DisplayName = "Updated Name"
        };

        var user = User.Register(TestAuth0UserId, "Original Name");

        _mediatorMock.Setup(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile);

        _mediatorMock.Setup(x => x.Send(
            It.IsAny<UpdateAllSettingsCommand>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        _userRepositoryMock.Setup(x => x.GetByAuth0UserIdAsync(TestAuth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var updatedProfile = CreateTestUserProfileDto();
        updatedProfile.DisplayName = "Updated Name";
        _mediatorMock.SetupSequence(x => x.Send(
            It.Is<GetUserProfileQuery>(q => q.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentProfile)
            .ReturnsAsync(updatedProfile);

        // Act
        var result = await _controller.UpdateUserProfile(updateRequest);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<UserProfileResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();

        _userRepositoryMock.Verify(x => x.GetByAuth0UserIdAsync(
            TestAuth0UserId, It.IsAny<CancellationToken>()), Times.Once);
        
        _userRepositoryMock.Verify(x => x.UpdateAsync(
            It.Is<User>(u => u.Auth0UserId == TestAuth0UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserProfile_ShouldHandleException()
    {
        // Arrange
        var updateRequest = new UpdateUserSettingsRequest();

        _mediatorMock.Setup(x => x.Send(
            It.IsAny<GetUserProfileQuery>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateUserProfile(updateRequest);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    private void SetupAuthenticatedUser(string auth0UserId, string email, string name)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, auth0UserId),
            new Claim("sub", auth0UserId),
            new Claim(ClaimTypes.Email, email),
            new Claim("email", email),
            new Claim(ClaimTypes.Name, name),
            new Claim("name", name),
            new Claim("picture", "https://example.com/avatar.jpg"),
            new Claim("email_verified", "true")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    private static UserProfileDto CreateTestUserProfileDto()
    {
        return new UserProfileDto
        {
            Id = Guid.NewGuid(),
            Auth0UserId = TestAuth0UserId,
            DisplayName = TestName,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow,
            Preferences = new UserPreferencesDto
            {
                Theme = "light",
                Language = "en",
                TimeZone = "UTC",
                DateFormat = "MM/dd/yyyy",
                TimeFormat = "12h",
                DefaultPageSize = 25,
                ShowTutorials = true,
                CompactMode = false
            },
            NotificationSettings = new NotificationSettingsDto
            {
                EmailNotificationsEnabled = true,
                PushNotificationsEnabled = true,
                ProcessingCompleteNotifications = true,
                ErrorNotifications = true,
                WeeklyDigestEnabled = false
            },
            ProcessingDefaults = new ProcessingDefaultsDto
            {
                AutoProcessUploads = false,
                MaxPreviewRows = 100,
                DefaultFileType = "CSV",
                EnableDataValidation = true,
                EnableSchemaInference = true,
                RetentionDays = 365
            },
            PrivacySettings = new PrivacySettingsDto
            {
                ShareAnalytics = false,
                AllowDataUsageForImprovement = false,
                ShowProcessingTime = true
            }
        };
    }
}

