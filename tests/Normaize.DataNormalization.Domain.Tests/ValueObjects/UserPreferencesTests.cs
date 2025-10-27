using FluentAssertions;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for UserPreferences value object
/// </summary>
public class UserPreferencesTests
{
    [Fact]
    public void Default_ShouldCreateValidUserPreferences()
    {
        // Act
        var preferences = UserPreferences.Default();

        // Assert
        preferences.Theme.Should().Be("light");
        preferences.Language.Should().Be("en");
        preferences.TimeZone.Should().Be("UTC");
        preferences.DateFormat.Should().Be("MM/dd/yyyy");
        preferences.TimeFormat.Should().Be("12h");
        preferences.DefaultPageSize.Should().Be(20); // Actual default
        preferences.ShowTutorials.Should().BeTrue();
        preferences.CompactMode.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldCreateValidUserPreferences_WithValidParameters()
    {
        // Act
        var preferences = UserPreferences.Create(
            theme: "dark",
            language: "es",
            timeZone: "America/New_York",
            dateFormat: "dd/MM/yyyy",
            timeFormat: "24h",
            defaultPageSize: 50,
            showTutorials: false,
            compactMode: true);

        // Assert
        preferences.Theme.Should().Be("dark");
        preferences.Language.Should().Be("es");
        preferences.TimeZone.Should().Be("America/New_York");
        preferences.DateFormat.Should().Be("dd/MM/yyyy");
        preferences.TimeFormat.Should().Be("24h");
        preferences.DefaultPageSize.Should().Be(50);
        preferences.ShowTutorials.Should().BeFalse();
        preferences.CompactMode.Should().BeTrue();
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    [InlineData("auto")]
    public void Create_ShouldAcceptValidTheme(string theme)
    {
        // Act
        var preferences = UserPreferences.Create(
            theme: theme,
            language: "en",
            timeZone: "UTC",
            dateFormat: "MM/dd/yyyy",
            timeFormat: "12h",
            defaultPageSize: 25,
            showTutorials: true,
            compactMode: false);

        // Assert
        preferences.Theme.Should().Be(theme);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    public void Create_ShouldThrowArgumentException_WhenThemeIsInvalid(string invalidTheme)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserPreferences.Create(
            theme: invalidTheme,
            language: "en",
            timeZone: "UTC",
            dateFormat: "MM/dd/yyyy",
            timeFormat: "12h",
            defaultPageSize: 25,
            showTutorials: true,
            compactMode: false));

        exception.Message.Should().Contain("Theme must be one of:");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("de")]
    public void Create_ShouldAcceptValidLanguage(string language)
    {
        // Act
        var preferences = UserPreferences.Create(
            theme: "light",
            language: language,
            timeZone: "UTC",
            dateFormat: "MM/dd/yyyy",
            timeFormat: "12h",
            defaultPageSize: 25,
            showTutorials: true,
            compactMode: false);

        // Assert
        preferences.Language.Should().Be(language);
    }

    [Theory]
    [InlineData("")]
    [InlineData("e")]
    [InlineData("eng")]
    [InlineData("english")]
    public void Create_ShouldThrowArgumentException_WhenLanguageIsInvalid(string invalidLanguage)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserPreferences.Create(
            theme: "light",
            language: invalidLanguage,
            timeZone: "UTC",
            dateFormat: "MM/dd/yyyy",
            timeFormat: "12h",
            defaultPageSize: 25,
            showTutorials: true,
            compactMode: false));

        exception.Message.Should().Contain("Language must be a valid ISO 639-1 code");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void Create_ShouldAcceptValidPageSize(int pageSize)
    {
        // Act
        var preferences = UserPreferences.Create(
            theme: "light",
            language: "en",
            timeZone: "UTC",
            dateFormat: "MM/dd/yyyy",
            timeFormat: "12h",
            defaultPageSize: pageSize,
            showTutorials: true,
            compactMode: false);

        // Assert
        preferences.DefaultPageSize.Should().Be(pageSize);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(101)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrowArgumentException_WhenPageSizeIsInvalid(int invalidPageSize)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserPreferences.Create(
            theme: "light",
            language: "en",
            timeZone: "UTC",
            dateFormat: "MM/dd/yyyy",
            timeFormat: "12h",
            defaultPageSize: invalidPageSize,
            showTutorials: true,
            compactMode: false));

        exception.Message.Should().Contain("Default page size must be between 10 and 100");
    }

    [Theory]
    [InlineData("12h")]
    [InlineData("24h")]
    public void Create_ShouldAcceptValidTimeFormat(string timeFormat)
    {
        // Act
        var preferences = UserPreferences.Create(
            theme: "light",
            language: "en",
            timeZone: "UTC",
            dateFormat: "MM/dd/yyyy",
            timeFormat: timeFormat,
            defaultPageSize: 25,
            showTutorials: true,
            compactMode: false);

        // Assert
        preferences.TimeFormat.Should().Be(timeFormat);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("24")]
    [InlineData("invalid")]
    public void Create_ShouldThrowArgumentException_WhenTimeFormatIsInvalid(string invalidTimeFormat)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => UserPreferences.Create(
            theme: "light",
            language: "en",
            timeZone: "UTC",
            dateFormat: "MM/dd/yyyy",
            timeFormat: invalidTimeFormat,
            defaultPageSize: 25,
            showTutorials: true,
            compactMode: false));

        exception.Message.Should().Contain("Time format must be one of:");
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithUpdatedValues()
    {
        // Arrange
        var original = UserPreferences.Default();

        // Act
        var updated = original.With(
            theme: "dark",
            language: "fr",
            timeZone: "Europe/Paris",
            dateFormat: "dd/MM/yyyy",
            timeFormat: "24h",
            defaultPageSize: 50,
            showTutorials: false,
            compactMode: true);

        // Assert
        updated.Should().NotBeSameAs(original); // Records are immutable
        updated.Theme.Should().Be("dark");
        updated.Language.Should().Be("fr");
        updated.TimeZone.Should().Be("Europe/Paris");
        updated.DateFormat.Should().Be("dd/MM/yyyy");
        updated.TimeFormat.Should().Be("24h");
        updated.DefaultPageSize.Should().Be(50);
        updated.ShowTutorials.Should().BeFalse();
        updated.CompactMode.Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenAllPropertiesAreEqual()
    {
        // Arrange
        var preferences1 = UserPreferences.Create("light", "en", "UTC", "MM/dd/yyyy", "12h", 25, true, false);
        var preferences2 = UserPreferences.Create("light", "en", "UTC", "MM/dd/yyyy", "12h", 25, true, false);

        // Act & Assert
        preferences1.Should().Be(preferences2);
        (preferences1 == preferences2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenPropertiesAreDifferent()
    {
        // Arrange
        var preferences1 = UserPreferences.Create("light", "en", "UTC", "MM/dd/yyyy", "12h", 25, true, false);
        var preferences2 = UserPreferences.Create("dark", "en", "UTC", "MM/dd/yyyy", "12h", 25, true, false);

        // Act & Assert
        preferences1.Should().NotBe(preferences2);
        (preferences1 == preferences2).Should().BeFalse();
    }
}
