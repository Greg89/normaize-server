using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Users.Commands.UpdateUserPreferences;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Users.Commands;

/// <summary>
/// Unit tests for UpdateUserPreferencesCommandHandler
/// </summary>
public class UpdateUserPreferencesCommandHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UpdateUserPreferencesCommandHandler>> _mockLogger;
    private readonly UpdateUserPreferencesCommandHandler _handler;

    public UpdateUserPreferencesCommandHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UpdateUserPreferencesCommandHandler>>();
        _handler = new UpdateUserPreferencesCommandHandler(_mockUserRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdatePreferences_WhenValidCommand()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        var command = new UpdateUserPreferencesCommand(
            Auth0UserId: "auth0|12345",
            Theme: "dark",
            Language: "es",
            TimeZone: "Europe/Madrid",
            DateFormat: "dd/MM/yyyy",
            TimeFormat: "24h",
            DefaultPageSize: 50,
            ShowTutorials: false,
            CompactMode: true);

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        user.Preferences.Theme.Should().Be("dark");
        user.Preferences.Language.Should().Be("es");
        user.Preferences.TimeZone.Should().Be("Europe/Madrid");
        user.Preferences.DefaultPageSize.Should().Be(50);
        user.Preferences.ShowTutorials.Should().BeFalse();
        user.Preferences.CompactMode.Should().BeTrue();

        _mockUserRepository.Verify(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateOnlyProvidedValues()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        var command = new UpdateUserPreferencesCommand(
            Auth0UserId: "auth0|12345",
            Theme: "dark",
            Language: null, // Not updating
            TimeZone: null,
            DateFormat: null,
            TimeFormat: null,
            DefaultPageSize: null,
            ShowTutorials: null,
            CompactMode: null);

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Preferences.Theme.Should().Be("dark"); // Updated
        user.Preferences.Language.Should().Be("en"); // Not changed (default)
        user.Preferences.DefaultPageSize.Should().Be(20); // Not changed (actual default)
    }

    [Fact]
    public async Task Handle_ShouldThrowInvalidOperationException_WhenUserNotFound()
    {
        // Arrange
        var command = new UpdateUserPreferencesCommand(
            Auth0UserId: "auth0|nonexistent",
            Theme: "dark");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        exception.Message.Should().Contain("not found");

        _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRaiseDomainEvent()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.ClearDomainEvents(); // Clear registration event

        var command = new UpdateUserPreferencesCommand(
            Auth0UserId: "auth0|12345",
            Theme: "dark");

        _mockUserRepository
            .Setup(x => x.GetByAuth0UserIdAsync(command.Auth0UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRepository
            .Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken ct) => u);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().Should().BeOfType<Domain.Events.UserPreferencesUpdatedEvent>();
    }
}
