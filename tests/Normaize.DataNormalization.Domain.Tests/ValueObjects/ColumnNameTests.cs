using FluentAssertions;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for ColumnName value object
/// </summary>
public class ColumnNameTests
{
    [Theory]
    [InlineData("name")]
    [InlineData("first_name")]
    [InlineData("FirstName")]
    [InlineData("column123")]
    [InlineData("_privateColumn")]
    [InlineData("A")]
    [InlineData("a123_B456")]
    public void Constructor_WithValidColumnName_ShouldCreateColumnName(string validName)
    {
        // Act
        var columnName = new ColumnName(validName);

        // Assert
        columnName.Value.Should().Be(validName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithNullOrEmptyName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act & Assert
        var action = () => new ColumnName(invalidName!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Column name cannot be null or empty*");
    }

    [Fact]
    public void Constructor_WithTooLongName_ShouldThrowArgumentException()
    {
        // Arrange
        var tooLongName = new string('a', 129); // 129 characters

        // Act & Assert
        var action = () => new ColumnName(tooLongName);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Column name cannot exceed 128 characters*");
    }

    [Theory]
    [InlineData("123column")] // Starts with number
    [InlineData("column-name")] // Contains dash
    [InlineData("column name")] // Contains space
    [InlineData("column@name")] // Contains special character
    [InlineData("column.name")] // Contains dot
    [InlineData("column+name")] // Contains plus
    public void Constructor_WithInvalidFormat_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var action = () => new ColumnName(invalidName);
        action.Should().Throw<ArgumentException>()
            .WithMessage($"*Invalid column name format: {invalidName}*");
    }

    [Fact]
    public void Constructor_WithWhitespaceAroundValidName_ShouldTrimWhitespace()
    {
        // Arrange
        var nameWithWhitespace = "  valid_column_name  ";
        var expectedName = "valid_column_name";

        // Act
        var columnName = new ColumnName(nameWithWhitespace);

        // Assert
        columnName.Value.Should().Be(expectedName);
    }

    [Fact]
    public void ImplicitOperator_ToString_ShouldReturnValue()
    {
        // Arrange
        var columnName = new ColumnName("test_column");

        // Act
        string result = columnName;

        // Assert
        result.Should().Be("test_column");
    }

    [Fact]
    public void ImplicitOperator_FromString_ShouldCreateColumnName()
    {
        // Arrange
        string name = "test_column";

        // Act
        ColumnName columnName = name;

        // Assert
        columnName.Value.Should().Be(name);
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var columnName = new ColumnName("test_column");

        // Act
        var result = columnName.ToString();

        // Assert
        result.Should().Be("test_column");
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var columnName1 = new ColumnName("test_column");
        var columnName2 = new ColumnName("test_column");

        // Act & Assert
        columnName1.Should().Be(columnName2);
        (columnName1 == columnName2).Should().BeTrue();
        (columnName1 != columnName2).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var columnName1 = new ColumnName("column1");
        var columnName2 = new ColumnName("column2");

        // Act & Assert
        columnName1.Should().NotBe(columnName2);
        (columnName1 == columnName2).Should().BeFalse();
        (columnName1 != columnName2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldReturnSameHashCode()
    {
        // Arrange
        var columnName1 = new ColumnName("test_column");
        var columnName2 = new ColumnName("test_column");

        // Act & Assert
        columnName1.GetHashCode().Should().Be(columnName2.GetHashCode());
    }
}