using FluentAssertions;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for ProcessingDefaults value object
/// </summary>
public class ProcessingDefaultsTests
{
    [Fact]
    public void Default_ShouldCreateValidProcessingDefaults()
    {
        // Act
        var defaults = ProcessingDefaults.Default();

        // Assert
        defaults.AutoProcessUploads.Should().BeFalse();
        defaults.MaxPreviewRows.Should().Be(100);
        defaults.DefaultFileType.Should().Be("CSV");
        defaults.EnableDataValidation.Should().BeTrue();
        defaults.EnableSchemaInference.Should().BeTrue();
        defaults.RetentionDays.Should().Be(365);
    }

    [Fact]
    public void Create_ShouldCreateValidProcessingDefaults_WithValidParameters()
    {
        // Act
        var defaults = ProcessingDefaults.Create(
            autoProcessUploads: true,
            maxPreviewRows: 500,
            defaultFileType: "JSON",
            enableDataValidation: false,
            enableSchemaInference: false,
            retentionDays: 730);

        // Assert
        defaults.AutoProcessUploads.Should().BeTrue();
        defaults.MaxPreviewRows.Should().Be(500);
        defaults.DefaultFileType.Should().Be("JSON");
        defaults.EnableDataValidation.Should().BeFalse();
        defaults.EnableSchemaInference.Should().BeFalse();
        defaults.RetentionDays.Should().Be(730);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Create_ShouldAcceptValidMaxPreviewRows(int rows)
    {
        // Act
        var defaults = ProcessingDefaults.Create(
            autoProcessUploads: false,
            maxPreviewRows: rows,
            defaultFileType: "CSV",
            enableDataValidation: true,
            enableSchemaInference: true,
            retentionDays: 365);

        // Assert
        defaults.MaxPreviewRows.Should().Be(rows);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10001)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldThrowArgumentException_WhenMaxPreviewRowsIsInvalid(int invalidRows)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ProcessingDefaults.Create(
            autoProcessUploads: false,
            maxPreviewRows: invalidRows,
            defaultFileType: "CSV",
            enableDataValidation: true,
            enableSchemaInference: true,
            retentionDays: 365));

        exception.Message.Should().Contain("MaxPreviewRows must be between 10 and 10000");
    }

    [Theory]
    [InlineData("CSV")]
    [InlineData("JSON")]
    [InlineData("XML")]
    [InlineData("EXCEL")]
    [InlineData("PARQUET")]
    [InlineData("TXT")]
    public void Create_ShouldAcceptValidFileType(string fileType)
    {
        // Act
        var defaults = ProcessingDefaults.Create(
            autoProcessUploads: false,
            maxPreviewRows: 100,
            defaultFileType: fileType,
            enableDataValidation: true,
            enableSchemaInference: true,
            retentionDays: 365);

        // Assert
        defaults.DefaultFileType.Should().Be(fileType);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("pdf")]
    [InlineData("")]
    public void Create_ShouldThrowArgumentException_WhenFileTypeIsInvalid(string invalidFileType)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ProcessingDefaults.Create(
            autoProcessUploads: false,
            maxPreviewRows: 100,
            defaultFileType: invalidFileType,
            enableDataValidation: true,
            enableSchemaInference: true,
            retentionDays: 365));

        exception.Message.Should().Contain("DefaultFileType must be one of: CSV, JSON, XML, EXCEL, PARQUET, TXT");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    [InlineData(730)]
    [InlineData(3650)]
    public void Create_ShouldAcceptValidRetentionDays(int days)
    {
        // Act
        var defaults = ProcessingDefaults.Create(
            autoProcessUploads: false,
            maxPreviewRows: 100,
            defaultFileType: "CSV",
            enableDataValidation: true,
            enableSchemaInference: true,
            retentionDays: days);

        // Assert
        defaults.RetentionDays.Should().Be(days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3651)]
    public void Create_ShouldThrowArgumentException_WhenRetentionDaysIsInvalid(int invalidDays)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => ProcessingDefaults.Create(
            autoProcessUploads: false,
            maxPreviewRows: 100,
            defaultFileType: "CSV",
            enableDataValidation: true,
            enableSchemaInference: true,
            retentionDays: invalidDays));

        exception.Message.Should().Contain("RetentionDays must be between 1 and 3650");
    }

    [Fact]
    public void Conservative_ShouldCreateConservativeSettings()
    {
        // Act
        var conservative = ProcessingDefaults.Conservative();

        // Assert
        conservative.AutoProcessUploads.Should().BeFalse();
        conservative.MaxPreviewRows.Should().Be(50);
        conservative.EnableDataValidation.Should().BeTrue();
        conservative.EnableSchemaInference.Should().BeFalse();
        conservative.RetentionDays.Should().Be(180);
    }

    [Fact]
    public void Aggressive_ShouldCreateAggressiveSettings()
    {
        // Act
        var aggressive = ProcessingDefaults.Aggressive();

        // Assert
        aggressive.AutoProcessUploads.Should().BeTrue();
        aggressive.MaxPreviewRows.Should().Be(1000);
        aggressive.EnableDataValidation.Should().BeTrue();
        aggressive.EnableSchemaInference.Should().BeTrue();
        aggressive.RetentionDays.Should().Be(730);
    }

    [Fact]
    public void With_ShouldCreateNewInstanceWithUpdatedValues()
    {
        // Arrange
        var original = ProcessingDefaults.Default();

        // Act
        var updated = original.With(
            autoProcessUploads: true,
            maxPreviewRows: 200,
            defaultFileType: "JSON",
            enableDataValidation: false,
            enableSchemaInference: false,
            retentionDays: 180);

        // Assert
        updated.Should().NotBeSameAs(original);
        updated.AutoProcessUploads.Should().BeTrue();
        updated.MaxPreviewRows.Should().Be(200);
        updated.DefaultFileType.Should().Be("JSON");
        updated.EnableDataValidation.Should().BeFalse();
        updated.EnableSchemaInference.Should().BeFalse();
        updated.RetentionDays.Should().Be(180);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenAllPropertiesAreEqual()
    {
        // Arrange
        var defaults1 = ProcessingDefaults.Create(false, 100, "CSV", true, true, 365);
        var defaults2 = ProcessingDefaults.Create(false, 100, "CSV", true, true, 365);

        // Act & Assert
        defaults1.Should().Be(defaults2);
        (defaults1 == defaults2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenPropertiesAreDifferent()
    {
        // Arrange
        var defaults1 = ProcessingDefaults.Create(false, 100, "CSV", true, true, 365);
        var defaults2 = ProcessingDefaults.Create(true, 100, "CSV", true, true, 365);

        // Act & Assert
        defaults1.Should().NotBe(defaults2);
        (defaults1 == defaults2).Should().BeFalse();
    }
}
