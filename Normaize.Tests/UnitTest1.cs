using Xunit;
using FluentAssertions;

namespace Normaize.Tests;

public class BasicTests
{
    [Fact]
    public void BasicTest_ShouldPass()
    {
        // Arrange
        var expected = true;

        // Act
        var actual = true;

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void StringTest_ShouldContainExpectedValue()
    {
        // Arrange
        var testString = "Hello, World!";

        // Act & Assert
        testString.Should().Contain("Hello");
        testString.Should().HaveLength(13);
    }

    [Fact]
    public void NumberTest_ShouldBeInRange()
    {
        // Arrange
        var number = 42;

        // Act & Assert
        number.Should().BeGreaterThan(0);
        number.Should().BeLessThan(100);
        number.Should().Be(42);
    }
}