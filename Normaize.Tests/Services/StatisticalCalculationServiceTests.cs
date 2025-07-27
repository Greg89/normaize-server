using Xunit;
using FluentAssertions;
using Normaize.Core.Services.Visualization;
using Normaize.Core.DTOs;
using Normaize.Core.Models;
using Normaize.Core.Constants;

namespace Normaize.Tests.Services;

public class StatisticalCalculationServiceTests
{
    private readonly StatisticalCalculationService _service;

    public StatisticalCalculationServiceTests()
    {
        _service = new StatisticalCalculationService();
    }

    [Fact]
    public void GenerateDataSummary_ReturnsEmptySummary_WhenEmptyData()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1" };
        var data = new List<Dictionary<string, object?>>();

        // Act
        var result = _service.GenerateDataSummary(dataSet, data);

        // Assert
        result.Should().NotBeNull();
        result.DataSetId.Should().Be(1);
        result.TotalRows.Should().Be(0);
        result.TotalColumns.Should().Be(0);
        result.MissingValues.Should().Be(0);
        result.DuplicateRows.Should().Be(0);
        result.ColumnSummaries.Should().BeEmpty();
        result.ProcessingTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GenerateDataSummary_ReturnsCorrectSummary_WhenValidData()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1" };
        var data = new List<Dictionary<string, object?>>
        {
            new() { ["name"] = "John", ["age"] = 25, ["city"] = "NYC" },
            new() { ["name"] = "Jane", ["age"] = 30, ["city"] = "LA" },
            new() { ["name"] = "Bob", ["age"] = null, ["city"] = "Chicago" }
        };

        // Act
        var result = _service.GenerateDataSummary(dataSet, data);

        // Assert
        result.Should().NotBeNull();
        result.DataSetId.Should().Be(1);
        result.TotalRows.Should().Be(3);
        result.TotalColumns.Should().Be(3);
        result.MissingValues.Should().Be(1); // age column has 1 null value
        result.DuplicateRows.Should().Be(0);
        result.ColumnSummaries.Should().HaveCount(3);

        // Verify column summaries
        result.ColumnSummaries.Should().ContainKey("name");
        result.ColumnSummaries["name"].DataType.Should().Be("String");
        result.ColumnSummaries["name"].NonNullCount.Should().Be(3);
        result.ColumnSummaries["name"].NullCount.Should().Be(0);
        result.ColumnSummaries["name"].UniqueCount.Should().Be(3);

        result.ColumnSummaries.Should().ContainKey("age");
        result.ColumnSummaries["age"].DataType.Should().Be("Numeric");
        result.ColumnSummaries["age"].NonNullCount.Should().Be(2);
        result.ColumnSummaries["age"].NullCount.Should().Be(1);
        result.ColumnSummaries["age"].UniqueCount.Should().Be(2);
    }

    [Fact]
    public void GenerateDataSummary_DetectsDuplicateRows()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1" };
        var data = new List<Dictionary<string, object?>>
        {
            new() { ["name"] = "John", ["age"] = 25 },
            new() { ["name"] = "Jane", ["age"] = 30 },
            new() { ["name"] = "John", ["age"] = 25 } // Duplicate
        };

        // Act
        var result = _service.GenerateDataSummary(dataSet, data);

        // Assert
        result.DuplicateRows.Should().Be(1);
    }

    [Fact]
    public void GenerateStatisticalSummary_ReturnsEmptySummary_WhenEmptyData()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1" };
        var data = new List<Dictionary<string, object?>>();

        // Act
        var result = _service.GenerateStatisticalSummary(dataSet, data);

        // Assert
        result.Should().NotBeNull();
        result.DataSetId.Should().Be(1);
        result.ColumnStatistics.Should().BeEmpty();
        result.ProcessingTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GenerateStatisticalSummary_ReturnsStatistics_WhenNumericData()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1" };
        var data = new List<Dictionary<string, object?>>
        {
            new() { ["name"] = "John", ["age"] = 25, ["salary"] = 50000 },
            new() { ["name"] = "Jane", ["age"] = 30, ["salary"] = 60000 },
            new() { ["name"] = "Bob", ["age"] = 35, ["salary"] = 70000 }
        };

        // Act
        var result = _service.GenerateStatisticalSummary(dataSet, data);

        // Assert
        result.Should().NotBeNull();
        result.DataSetId.Should().Be(1);
        result.ColumnStatistics.Should().HaveCount(2); // age and salary are numeric

        // Verify age statistics
        result.ColumnStatistics.Should().ContainKey("age");
        var ageStats = result.ColumnStatistics["age"];
        ageStats.ColumnName.Should().Be("age");
        ageStats.Mean.Should().Be(30.0);
        ageStats.Median.Should().Be(30.0);
        ageStats.Min.Should().Be(25.0);
        ageStats.Max.Should().Be(35.0);

        // Verify salary statistics
        result.ColumnStatistics.Should().ContainKey("salary");
        var salaryStats = result.ColumnStatistics["salary"];
        salaryStats.ColumnName.Should().Be("salary");
        salaryStats.Mean.Should().Be(60000.0);
        salaryStats.Median.Should().Be(60000.0);
        salaryStats.Min.Should().Be(50000.0);
        salaryStats.Max.Should().Be(70000.0);
    }

    [Fact]
    public void CalculateMedian_ReturnsCorrectValue_WhenOddNumberOfElements()
    {
        // Arrange
        var data = new List<double> { 1, 3, 5, 7, 9 };

        // Act
        var result = _service.CalculateMedian(data);

        // Assert
        result.Should().Be(5.0);
    }

    [Fact]
    public void CalculateMedian_ReturnsCorrectValue_WhenEvenNumberOfElements()
    {
        // Arrange
        var data = new List<double> { 1, 3, 5, 7 };

        // Act
        var result = _service.CalculateMedian(data);

        // Assert
        result.Should().Be(4.0); // (3 + 5) / 2
    }

    [Fact]
    public void CalculateMedian_ReturnsZero_WhenEmptyList()
    {
        // Arrange
        var data = new List<double>();

        // Act
        var result = _service.CalculateMedian(data);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateStandardDeviation_ReturnsCorrectValue()
    {
        // Arrange
        var data = new List<double> { 2, 4, 4, 4, 5, 5, 7, 9 };

        // Act
        var result = _service.CalculateStandardDeviation(data);

        // Assert
        result.Should().BeApproximately(2.0, 0.01);
    }

    [Fact]
    public void CalculateStandardDeviation_ReturnsZero_WhenSingleElement()
    {
        // Arrange
        var data = new List<double> { 5.0 };

        // Act
        var result = _service.CalculateStandardDeviation(data);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateQuartile_ReturnsCorrectValue()
    {
        // Arrange
        var data = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Act
        var q1 = _service.CalculateQuartile(data, 0.25);
        var q2 = _service.CalculateQuartile(data, 0.50);
        var q3 = _service.CalculateQuartile(data, 0.75);

        // Assert
        q1.Should().BeApproximately(3.25, 0.01);
        q2.Should().BeApproximately(5.5, 0.01);
        q3.Should().BeApproximately(7.75, 0.01);
    }

    [Fact]
    public void CalculateQuartile_ReturnsZero_WhenEmptyList()
    {
        // Arrange
        var data = new List<double>();

        // Act
        var result = _service.CalculateQuartile(data, 0.5);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateSkewness_ReturnsCorrectValue()
    {
        // Arrange
        var data = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Act
        var result = _service.CalculateSkewness(data);

        // Assert
        result.Should().BeApproximately(0.0, 0.1); // Approximately normal distribution
    }

    [Fact]
    public void CalculateSkewness_ReturnsZero_WhenInsufficientData()
    {
        // Arrange
        var data = new List<double> { 1, 2 };

        // Act
        var result = _service.CalculateSkewness(data);

        // Assert
        result.Should().Be(0.0);
    }

    [Fact]
    public void CalculateKurtosis_ReturnsCorrectValue()
    {
        // Arrange
        var data = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Act
        var result = _service.CalculateKurtosis(data);

        // Assert
        result.Should().BeApproximately(-0.21, 0.01); // Actual calculated kurtosis for uniform distribution from 1 to 10
    }

    [Fact]
    public void CalculateKurtosis_ReturnsZero_WhenInsufficientData()
    {
        // Arrange
        var data = new List<double> { 1, 2, 3 };

        // Act
        var result = _service.CalculateKurtosis(data);

        // Assert
        result.Should().Be(0.0);
    }

    [Theory]
    [InlineData("10", "Numeric")]
    [InlineData("20.5", "Numeric")]
    [InlineData("2023-01-01", "DateTime")]
    [InlineData("true", "Boolean")]
    [InlineData("false", "Boolean")]
    [InlineData("hello", "String")]
    [InlineData("", "String")]
    public void DetermineDataType_ReturnsCorrectType_WhenSingleValue(string value, string expectedType)
    {
        // Arrange
        var data = new List<object?> { value };

        // Act
        var result = _service.DetermineDataType(data);

        // Assert
        result.Should().Be(expectedType);
    }

    [Fact]
    public void DetermineDataType_ReturnsUnknown_WhenAllNullValues()
    {
        // Arrange
        var data = new List<object?> { null, null, null };

        // Act
        var result = _service.DetermineDataType(data);

        // Assert
        result.Should().Be("Unknown");
    }

    [Fact]
    public void DetermineDataType_ReturnsString_WhenMixedTypes()
    {
        // Arrange
        var data = new List<object?> { "10", "hello", "20.5" };

        // Act
        var result = _service.DetermineDataType(data);

        // Assert
        result.Should().Be("String");
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(20.5, true)]
    [InlineData(20.5f, true)]
    [InlineData("10", true)]
    [InlineData("20.5", true)]
    [InlineData("hello", false)]
    [InlineData("", false)]
    public void IsNumeric_ReturnsCorrectValue(object? value, bool expected)
    {
        // Act
        var result = _service.IsNumeric(value);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsNumeric_ReturnsFalse_WhenNullValue()
    {
        // Act
        var result = _service.IsNumeric(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNumericColumn_ReturnsTrue_WhenAllNumericValues()
    {
        // Arrange
        var data = new List<object?> { "10", "20", "30", null };

        // Act
        var result = _service.IsNumericColumn(data);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsNumericColumn_ReturnsFalse_WhenMixedTypes()
    {
        // Arrange
        var data = new List<object?> { "10", "hello", "30" };

        // Act
        var result = _service.IsNumericColumn(data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNumericColumn_ReturnsFalse_WhenAllNullValues()
    {
        // Arrange
        var data = new List<object?> { null, null, null };

        // Act
        var result = _service.IsNumericColumn(data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNumericColumn_ReturnsFalse_WhenEmptyList()
    {
        // Arrange
        var data = new List<object?>();

        // Act
        var result = _service.IsNumericColumn(data);

        // Assert
        result.Should().BeFalse();
    }
}