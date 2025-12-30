using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Users.Queries.GetUserProfile;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Users.Queries;

/// <summary>
/// Unit tests for GetUserProfileQueryHandler
/// </summary>
public class GetUserProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<GetUserProfileQueryHandler>> _mockLogger;
    private readonly GetUserProfileQueryHandler _handler;

    public GetUserProfileQueryHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<GetUserProfileQueryHandler>>();
        _handler = new GetUserProfileQueryHandler(_mockUserRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUserProfile_WhenUserExists()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        var query = new GetUserProfileQuery("auth0|12345");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(query.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Auth0UserId.Should().Be("auth0|12345");
        result.DisplayName.Should().Be("Test User");
        result.Preferences.Should().NotBeNull();
        result.NotificationSettings.Should().NotBeNull();
        result.ProcessingDefaults.Should().NotBeNull();
        result.PrivacySettings.Should().NotBeNull();

        _mockUserRepository.Verify(x => x.GetByAuth0UserIdAsync(query.Auth0UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenUserNotFound()
    {
        // Arrange
        var query = new GetUserProfileQuery("auth0|nonexistent");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(query.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _mockUserRepository.Verify(x => x.GetByAuth0UserIdAsync(query.Auth0UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapAllProperties()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        var query = new GetUserProfileQuery("auth0|12345");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(query.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.UpdatedAt.Should().Be(user.UpdatedAt);

        // Verify Preferences
        result.Preferences.Theme.Should().Be(user.Preferences.Theme);
        result.Preferences.Language.Should().Be(user.Preferences.Language);
        result.Preferences.TimeZone.Should().Be(user.Preferences.TimeZone);
        result.Preferences.DefaultPageSize.Should().Be(user.Preferences.DefaultPageSize);

        // Verify Notification Settings
        result.NotificationSettings.EmailNotificationsEnabled.Should().Be(user.NotificationSettings.EmailNotificationsEnabled);
        result.NotificationSettings.PushNotificationsEnabled.Should().Be(user.NotificationSettings.PushNotificationsEnabled);

        // Verify Processing Defaults
        result.ProcessingDefaults.AutoProcessUploads.Should().Be(user.ProcessingDefaults.AutoProcessUploads);
        result.ProcessingDefaults.MaxPreviewRows.Should().Be(user.ProcessingDefaults.MaxPreviewRows);

        // Verify Privacy Settings
        result.PrivacySettings.ShareAnalytics.Should().Be(user.PrivacySettings.ShareAnalytics);
        result.PrivacySettings.AllowDataUsageForImprovement.Should().Be(user.PrivacySettings.AllowDataUsageForImprovement);
    }

    [Fact]
    public async Task Handle_ShouldEnforceAccessControl()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        var query = new GetUserProfileQuery("auth0|different");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(query.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
