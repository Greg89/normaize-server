using FluentAssertions;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.ValueObjects;

/// <summary>
/// Unit tests for StatisticalMeasure value object
/// </summary>
public class StatisticalMeasureTests
{
    [Fact]
    public void Constructor_ShouldCreateValidStatisticalMeasure_WithValidParameters()
    {
        // Arrange & Act
        var measure = new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: -0.5,
            outlierCount: 3);

        // Assert
        measure.Mean.Should().Be(50.0);
        measure.Median.Should().Be(48.0);
        measure.StandardDeviation.Should().Be(15.2);
        measure.Min.Should().Be(10.0);
        measure.Max.Should().Be(90.0);
        measure.Q1.Should().Be(35.0);
        measure.Q2.Should().Be(48.0);
        measure.Q3.Should().Be(65.0);
        measure.Skewness.Should().Be(0.3);
        measure.Kurtosis.Should().Be(-0.5);
        measure.OutlierCount.Should().Be(3);
    }

    [Fact]
    public void Range_ShouldCalculateCorrectValue()
    {
        // Arrange
        var measure = new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: -0.5,
            outlierCount: 3);

        // Act & Assert
        measure.Range.Should().Be(80.0); // 90 - 10
    }

    [Fact]
    public void InterquartileRange_ShouldCalculateCorrectValue()
    {
        // Arrange
        var measure = new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: -0.5,
            outlierCount: 3);

        // Act & Assert
        measure.InterquartileRange.Should().Be(30.0); // 65 - 35
    }

    [Fact]
    public void IsSignificantlySkewed_ShouldReturnTrue_WhenSkewnessAbsoluteValueAboveThreshold()
    {
        // Arrange
        var measure = new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 1.5, // Above threshold of 1.0
            kurtosis: -0.5,
            outlierCount: 3);

        // Act & Assert
        measure.IsSignificantlySkewed.Should().BeTrue();
    }

    [Fact]
    public void IsSignificantlySkewed_ShouldReturnFalse_WhenSkewnessBelowThreshold()
    {
        // Arrange
        var measure = new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3, // Below threshold
            kurtosis: -0.5,
            outlierCount: 3);

        // Act & Assert
        measure.IsSignificantlySkewed.Should().BeFalse();
    }

    [Fact]
    public void IsHighKurtosis_ShouldReturnTrue_WhenKurtosisAboveThreshold()
    {
        // Arrange
        var measure = new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: 4.0, // Above threshold of 3.0
            outlierCount: 3);

        // Act & Assert
        measure.IsHighKurtosis.Should().BeTrue();
    }

    [Fact]
    public void IsHighKurtosis_ShouldReturnFalse_WhenKurtosisBelowThreshold()
    {
        // Arrange
        var measure = new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: 1.5, // Below threshold
            outlierCount: 3);

        // Act & Assert
        measure.IsHighKurtosis.Should().BeFalse();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_ShouldThrowArgumentException_WhenMeanIsInvalid(double invalidMean)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new StatisticalMeasure(
            mean: invalidMean,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: -0.5,
            outlierCount: 3));

        exception.Message.Should().Contain("Mean must be a valid number");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenStandardDeviationIsNegative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: -5.0, // Negative
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: -0.5,
            outlierCount: 3));

        exception.Message.Should().Contain("Standard deviation cannot be negative");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenMinGreaterThanMax()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 90.0, // Greater than max
            max: 10.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: -0.5,
            outlierCount: 3));

        exception.Message.Should().Contain("Minimum value cannot be greater than maximum value");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenOutlierCountIsNegative()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new StatisticalMeasure(
            mean: 50.0,
            median: 48.0,
            standardDeviation: 15.2,
            min: 10.0,
            max: 90.0,
            q1: 35.0,
            q2: 48.0,
            q3: 65.0,
            skewness: 0.3,
            kurtosis: -0.5,
            outlierCount: -1)); // Negative

        exception.Message.Should().Contain("Outlier count cannot be negative");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenAllPropertiesAreEqual()
    {
        // Arrange
        var measure1 = new StatisticalMeasure(50.0, 48.0, 15.2, 10.0, 90.0, 35.0, 48.0, 65.0, 0.3, -0.5, 3);
        var measure2 = new StatisticalMeasure(50.0, 48.0, 15.2, 10.0, 90.0, 35.0, 48.0, 65.0, 0.3, -0.5, 3);

        // Act & Assert
        measure1.Equals(measure2).Should().BeTrue();
        (measure1 == measure2).Should().BeTrue();
        (measure1 != measure2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenPropertiesAreDifferent()
    {
        // Arrange
        var measure1 = new StatisticalMeasure(50.0, 48.0, 15.2, 10.0, 90.0, 35.0, 48.0, 65.0, 0.3, -0.5, 3);
        var measure2 = new StatisticalMeasure(51.0, 48.0, 15.2, 10.0, 90.0, 35.0, 48.0, 65.0, 0.3, -0.5, 3);

        // Act & Assert
        measure1.Equals(measure2).Should().BeFalse();
        (measure1 == measure2).Should().BeFalse();
        (measure1 != measure2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameValue_ForEqualObjects()
    {
        // Arrange
        var measure1 = new StatisticalMeasure(50.0, 48.0, 15.2, 10.0, 90.0, 35.0, 48.0, 65.0, 0.3, -0.5, 3);
        var measure2 = new StatisticalMeasure(50.0, 48.0, 15.2, 10.0, 90.0, 35.0, 48.0, 65.0, 0.3, -0.5, 3);

        // Act & Assert
        measure1.GetHashCode().Should().Be(measure2.GetHashCode());
    }
}