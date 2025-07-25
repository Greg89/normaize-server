using Microsoft.EntityFrameworkCore;
using Moq;
using Normaize.Core.Models;
using Normaize.Data;
using Normaize.Data.Repositories;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Repositories;

public class UserSettingsRepositoryTests : IDisposable
{
    private readonly DbContextOptions<NormaizeContext> _options;
    private readonly NormaizeContext _context;
    private readonly UserSettingsRepository _repository;

    public UserSettingsRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new NormaizeContext(_options);
        _repository = new UserSettingsRepository(_context);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithExistingSettings_ShouldReturnSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        await _context.UserSettings.AddAsync(userSettings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Theme.Should().Be("dark");
        result.Language.Should().Be("es");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNonExistingSettings_ShouldReturnNull()
    {
        // Arrange
        var userId = "auth0|123456789";

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithDeletedSettings_ShouldReturnNull()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        userSettings.IsDeleted = true;
        userSettings.DeletedAt = DateTime.UtcNow;

        await _context.UserSettings.AddAsync(userSettings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateNewSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);

        // Act
        var result = await _repository.CreateAsync(userSettings);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(userId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Verify it was saved to database
        var savedSettings = await _context.UserSettings.FindAsync(result.Id);
        savedSettings.Should().NotBeNull();
        savedSettings!.UserId.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingSettings()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        await _context.UserSettings.AddAsync(userSettings);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = userSettings.UpdatedAt;
        userSettings.Theme = "light";
        userSettings.Language = "en";

        // Act
        var result = await _repository.UpdateAsync(userSettings);

        // Assert
        result.Should().NotBeNull();
        result.Theme.Should().Be("light");
        result.Language.Should().Be("en");
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);

        // Verify it was updated in database
        var updatedSettings = await _context.UserSettings.FindAsync(result.Id);
        updatedSettings.Should().NotBeNull();
        updatedSettings!.Theme.Should().Be("light");
        updatedSettings.Language.Should().Be("en");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingSettings_ShouldSoftDeleteAndReturnTrue()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        await _context.UserSettings.AddAsync(userSettings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(userId);

        // Assert
        result.Should().BeTrue();

        // Verify soft delete
        var deletedSettings = await _context.UserSettings.FindAsync(userSettings.Id);
        deletedSettings.Should().NotBeNull();
        deletedSettings!.IsDeleted.Should().BeTrue();
        deletedSettings.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        deletedSettings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingSettings_ShouldReturnFalse()
    {
        // Arrange
        var userId = "auth0|123456789";

        // Act
        var result = await _repository.DeleteAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithAlreadyDeletedSettings_ShouldReturnFalse()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        userSettings.IsDeleted = true;
        userSettings.DeletedAt = DateTime.UtcNow;

        await _context.UserSettings.AddAsync(userSettings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingSettings_ShouldReturnTrue()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        await _context.UserSettings.AddAsync(userSettings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(userId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingSettings_ShouldReturnFalse()
    {
        // Arrange
        var userId = "auth0|123456789";

        // Act
        var result = await _repository.ExistsAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithDeletedSettings_ShouldReturnFalse()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        userSettings.IsDeleted = true;
        userSettings.DeletedAt = DateTime.UtcNow;

        await _context.UserSettings.AddAsync(userSettings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(userId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_ShouldSetTimestamps()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        userSettings.CreatedAt = DateTime.MinValue;
        userSettings.UpdatedAt = DateTime.MinValue;

        // Act
        var result = await _repository.CreateAsync(userSettings);

        // Assert
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTimestamp()
    {
        // Arrange
        var userId = "auth0|123456789";
        var userSettings = CreateTestUserSettings(userId);
        await _context.UserSettings.AddAsync(userSettings);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = userSettings.UpdatedAt;
        await Task.Delay(100); // Ensure time difference

        // Act
        var result = await _repository.UpdateAsync(userSettings);

        // Assert
        result.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleUsers_ShouldCreateSeparateSettings()
    {
        // Arrange
        var userId1 = "auth0|123456789";
        var userId2 = "auth0|987654321";
        var userSettings1 = CreateTestUserSettings(userId1);
        var userSettings2 = CreateTestUserSettings(userId2);

        // Act
        var result1 = await _repository.CreateAsync(userSettings1);
        var result2 = await _repository.CreateAsync(userSettings2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Id.Should().NotBe(result2.Id);
        result1.UserId.Should().Be(userId1);
        result2.UserId.Should().Be(userId2);

        // Verify both exist in database
        var saved1 = await _repository.GetByUserIdAsync(userId1);
        var saved2 = await _repository.GetByUserIdAsync(userId2);
        saved1.Should().NotBeNull();
        saved2.Should().NotBeNull();
    }

    private static UserSettings CreateTestUserSettings(string userId)
    {
        return new UserSettings
        {
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
            TimeFormat = "24h"
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}