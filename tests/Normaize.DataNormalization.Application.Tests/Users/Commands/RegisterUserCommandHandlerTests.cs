using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Users.Commands.RegisterUser;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Users.Commands;

/// <summary>
/// Unit tests for RegisterUserCommandHandler
/// </summary>
public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<RegisterUserCommandHandler>> _mockLogger;
    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<RegisterUserCommandHandler>>();
        _handler = new RegisterUserCommandHandler(_mockUserRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldRegisterUser_WhenValidCommand()
    {
        // Arrange
        var command = new RegisterUserCommand("auth0|12345", "Test User");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken ct) => user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Auth0UserId.Should().Be("auth0|12345");
        result.DisplayName.Should().Be("Test User");
        result.Preferences.Should().NotBeNull();
        result.NotificationSettings.Should().NotBeNull();
        result.ProcessingDefaults.Should().NotBeNull();
        result.PrivacySettings.Should().NotBeNull();

        _mockUserRepository.Verify(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_ShouldThrowArgumentException_WhenAuth0UserIdIsInvalid(string invalidAuth0UserId)
    {
        // Arrange
        var command = new RegisterUserCommand(invalidAuth0UserId, "Test User");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Handle_ShouldThrowArgumentException_WhenDisplayNameIsInvalid(string invalidDisplayName)
    {
        // Arrange
        var command = new RegisterUserCommand("auth0|12345", invalidDisplayName);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenUserAlreadyExists()
    {
        // Arrange
        var command = new RegisterUserCommand("auth0|12345", "Test User");
        var existingUser = User.Register("auth0|12345", "Existing User");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("already exists");

        _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSetDefaultPreferences()
    {
        // Arrange
        var command = new RegisterUserCommand("auth0|12345", "Test User");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _mockUserRepository
            .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken ct) => user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Preferences.Theme.Should().Be("light");
        result.Preferences.Language.Should().Be("en");
        result.Preferences.DefaultPageSize.Should().Be(20); // Actual default
        result.NotificationSettings.EmailNotificationsEnabled.Should().BeTrue();
        result.ProcessingDefaults.AutoProcessUploads.Should().BeTrue(); // Default is true
        result.PrivacySettings.ShareAnalytics.Should().BeTrue(); // Default is true
    }
}
