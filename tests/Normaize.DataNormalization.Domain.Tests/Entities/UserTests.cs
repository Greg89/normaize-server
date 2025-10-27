using FluentAssertions;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Events;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.Entities;

/// <summary>
/// Unit tests for User aggregate root
/// </summary>
public class UserTests
{
    [Fact]
    public void Register_ShouldCreateUserWithDefaultSettings()
    {
        // Arrange
        var auth0UserId = "auth0|12345";
        var displayName = "Test User";

        // Act
        var user = User.Register(auth0UserId, displayName);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.Auth0UserId.Should().Be(auth0UserId);
        user.DisplayName.Should().Be(displayName);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
        
        // Verify default settings
        user.Preferences.Should().NotBeNull();
        user.Preferences.Theme.Should().Be("light");
        user.NotificationSettings.Should().NotBeNull();
        user.ProcessingDefaults.Should().NotBeNull();
        user.PrivacySettings.Should().NotBeNull();
        
        // Verify domain event
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().Should().BeOfType<UserRegisteredEvent>();
        var registeredEvent = (UserRegisteredEvent)user.DomainEvents.First();
        registeredEvent.UserId.Should().Be(user.Id);
        registeredEvent.Auth0UserId.Should().Be(auth0UserId);
        registeredEvent.DisplayName.Should().Be(displayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_ShouldThrowArgumentException_WhenAuth0UserIdIsInvalid(string invalidAuth0UserId)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => User.Register(invalidAuth0UserId, "Test User"));
        exception.Message.Should().Contain("Auth0 user ID cannot be null or empty");
    }

    // DisplayName validation removed - DisplayName is optional and can be null/empty

    [Fact]
    public void RegisterWithSettings_ShouldCreateUserWithCustomSettings()
    {
        // Arrange
        var auth0UserId = "auth0|12345";
        var displayName = "Test User";
        var preferences = UserPreferences.Create("dark", "es", "Europe/Madrid", "dd/MM/yyyy", "24h", 50, false, true);
        var notifications = NotificationSettings.Create(false, false, true, true, true);
        var processing = ProcessingDefaults.Conservative();
        var privacy = PrivacySettings.MostPrivate();

        // Act
        var user = User.RegisterWithSettings(auth0UserId, displayName, preferences, notifications, processing, privacy);

        // Assert
        user.Auth0UserId.Should().Be(auth0UserId);
        user.DisplayName.Should().Be(displayName);
        user.Preferences.Theme.Should().Be("dark");
        user.NotificationSettings.EmailNotificationsEnabled.Should().BeFalse();
        user.ProcessingDefaults.AutoProcessUploads.Should().BeFalse();
        user.PrivacySettings.ShareAnalytics.Should().BeFalse();
    }

    [Fact]
    public void UpdateDisplayName_ShouldUpdateDisplayNameAndTimestamp()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Old Name");
        var originalUpdatedAt = user.UpdatedAt;
        Thread.Sleep(10); // Ensure timestamp difference

        // Act
        user.UpdateDisplayName("New Name");

        // Assert
        user.DisplayName.Should().Be("New Name");
        user.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    // UpdateDisplayName validation removed - DisplayName is optional and can be null/empty

    [Fact]
    public void UpdatePreferences_ShouldUpdatePreferencesAndRaiseDomainEvent()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.ClearDomainEvents(); // Clear registration event
        var newPreferences = UserPreferences.Create("dark", "fr", "Europe/Paris", "dd/MM/yyyy", "24h", 50, false, true);

        // Act
        user.UpdatePreferences(newPreferences, "auth0|12345");

        // Assert
        user.Preferences.Theme.Should().Be("dark");
        user.Preferences.Language.Should().Be("fr");
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().Should().BeOfType<UserPreferencesUpdatedEvent>();
    }

    [Fact]
    public void UpdateNotificationSettings_ShouldUpdateSettingsAndRaiseDomainEvent()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.ClearDomainEvents();
        var newSettings = NotificationSettings.Default().DisableAll();

        // Act
        user.UpdateNotificationSettings(newSettings, "auth0|12345");

        // Assert
        user.NotificationSettings.EmailNotificationsEnabled.Should().BeFalse();
        user.NotificationSettings.PushNotificationsEnabled.Should().BeFalse();
        user.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void UpdateProcessingDefaults_ShouldUpdateDefaultsAndRaiseDomainEvent()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.ClearDomainEvents();
        var newDefaults = ProcessingDefaults.Aggressive();

        // Act
        user.UpdateProcessingDefaults(newDefaults, "auth0|12345");

        // Assert
        user.ProcessingDefaults.AutoProcessUploads.Should().BeTrue();
        user.ProcessingDefaults.MaxPreviewRows.Should().Be(1000);
        user.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void UpdatePrivacySettings_ShouldUpdateSettingsAndRaiseDomainEvent()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.ClearDomainEvents();
        var newSettings = PrivacySettings.MostOpen();

        // Act
        user.UpdatePrivacySettings(newSettings, "auth0|12345");

        // Assert
        user.PrivacySettings.ShareAnalytics.Should().BeTrue();
        user.PrivacySettings.AllowDataUsageForImprovement.Should().BeTrue();
        user.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void UpdateAllSettings_ShouldUpdateAllSettingsAndRaiseDomainEvent()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.ClearDomainEvents();
        var newPreferences = UserPreferences.Create("dark", "es", "Europe/Madrid", "dd/MM/yyyy", "24h", 50, false, true);
        var newNotifications = NotificationSettings.Default().DisableAll();
        var newProcessing = ProcessingDefaults.Conservative();
        var newPrivacy = PrivacySettings.MostPrivate();

        // Act
        user.UpdateAllSettings("New Name", newPreferences, newNotifications, newProcessing, newPrivacy, "auth0|12345");

        // Assert
        user.DisplayName.Should().Be("New Name");
        user.Preferences.Theme.Should().Be("dark");
        user.NotificationSettings.EmailNotificationsEnabled.Should().BeFalse();
        user.ProcessingDefaults.AutoProcessUploads.Should().BeFalse();
        user.PrivacySettings.ShareAnalytics.Should().BeFalse();
        user.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void ResetToDefaults_ShouldResetAllSettingsToDefaults()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        var customPreferences = UserPreferences.Create("dark", "es", "Europe/Madrid", "dd/MM/yyyy", "24h", 50, false, true);
        user.UpdatePreferences(customPreferences, "auth0|12345");
        user.ClearDomainEvents();

        // Act
        user.ResetToDefaults("auth0|12345");

        // Assert
        user.Preferences.Theme.Should().Be("light");
        user.Preferences.Language.Should().Be("en");
        user.NotificationSettings.EmailNotificationsEnabled.Should().BeTrue();
        user.ProcessingDefaults.AutoProcessUploads.Should().BeTrue(); // Default is true
        user.PrivacySettings.ShareAnalytics.Should().BeTrue(); // Default is true
        user.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Delete_ShouldMarkUserAsDeletedAndSetDeletedAt()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");

        // Act
        user.Delete();

        // Assert
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
        user.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Restore_ShouldUnmarkUserAsDeleted()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.Delete();

        // Act
        user.Restore();

        // Assert
        user.IsDeleted.Should().BeFalse();
        user.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void UpdatePreferences_ShouldThrowInvalidOperationException_WhenUserIsDeleted()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.Delete();
        var newPreferences = UserPreferences.Create("dark", "fr", "Europe/Paris", "dd/MM/yyyy", "24h", 50, false, true);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => user.UpdatePreferences(newPreferences, "auth0|12345"));
        exception.Message.Should().Contain("Cannot update a deleted user");
    }

    [Fact]
    public void EnsureUserAccess_ShouldNotThrow_WhenAuth0UserIdMatches()
    {
        // Arrange
        var auth0UserId = "auth0|12345";
        var user = User.Register(auth0UserId, "Test User");

        // Act & Assert
        user.Invoking(u => u.EnsureUserAccess(auth0UserId))
            .Should().NotThrow();
    }

    [Fact]
    public void EnsureUserAccess_ShouldThrowUnauthorizedAccessException_WhenAuth0UserIdDoesNotMatch()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");

        // Act & Assert
        var exception = Assert.Throws<UnauthorizedAccessException>(() => user.EnsureUserAccess("auth0|different"));
        exception.Message.Should().Contain("is not authorized to access this user profile");
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllDomainEvents()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");
        user.DomainEvents.Should().ContainSingle();

        // Act
        user.ClearDomainEvents();

        // Assert
        user.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdatePreferences_ShouldThrowArgumentNullException_WhenPreferencesIsNull()
    {
        // Arrange
        var user = User.Register("auth0|12345", "Test User");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => user.UpdatePreferences(null!, "auth0|12345"));
    }
}
