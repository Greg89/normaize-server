using FluentAssertions;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.Aggregates;

/// <summary>
/// Unit tests for Statistics aggregate
/// </summary>
public class StatisticsTests
{
    [Fact]
    public void Constructor_ShouldCreateValidStatistics_WithValidParameters()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var dataSetName = "Test Dataset";
        var totalRows = 100;
        var totalColumns = 5;
        var missingValues = 10;
        var duplicateRows = 2;
        var columnSummaries = new Dictionary<string, ColumnSummary>();
        var columnStatistics = new Dictionary<string, StatisticalMeasure>();
        var processingTime = TimeSpan.FromSeconds(5);

        // Act
        var statistics = Statistics.CreateDataSummary(
            dataSetId,
            dataSetName,
            totalRows,
            totalColumns,
            missingValues,
            duplicateRows,
            columnSummaries,
            processingTime);

        // Assert
        statistics.DataSetId.Should().Be(dataSetId);
        statistics.DataSetName.Should().Be(dataSetName);
        statistics.TotalRows.Should().Be(totalRows);
        statistics.TotalColumns.Should().Be(totalColumns);
        statistics.MissingValues.Should().Be(missingValues);
        statistics.DuplicateRows.Should().Be(duplicateRows);
        statistics.ColumnSummaries.Should().BeEquivalentTo(columnSummaries);
        statistics.ColumnStatistics.Should().BeEquivalentTo(columnStatistics);
        statistics.ProcessingTime.Should().Be(processingTime);
        statistics.CalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        statistics.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenDataSetNameIsEmpty()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var dataSetName = "";
        var columnSummaries = new Dictionary<string, ColumnSummary>();
        var columnStatistics = new Dictionary<string, StatisticalMeasure>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Statistics.CreateDataSummary(
            dataSetId,
            dataSetName,
            100,
            5,
            10,
            2,
            columnSummaries,
            TimeSpan.FromSeconds(5)));

        exception.Message.Should().Contain("DataSet name cannot be null or empty");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenTotalRowsIsNegative()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var dataSetName = "Test Dataset";
        var columnSummaries = new Dictionary<string, ColumnSummary>();
        var columnStatistics = new Dictionary<string, StatisticalMeasure>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Statistics.CreateDataSummary(
            dataSetId,
            dataSetName,
            -1,
            5,
            10,
            2,
            columnSummaries,
            TimeSpan.FromSeconds(5)));

        exception.Message.Should().Contain("Total rows cannot be negative");
    }

    [Fact]
    public void CreateDataSummary_ShouldCreateStatisticsWithCorrectProperties()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var dataSetName = "Test Dataset";
        var totalRows = 100;
        var totalColumns = 3;
        var columnSummaries = CreateTestColumnSummaries();

        // Act
        var statistics = Statistics.CreateDataSummary(
            dataSetId,
            dataSetName,
            totalRows,
            totalColumns,
            5, // missingValues
            0, // duplicateRows
            columnSummaries,
            TimeSpan.FromSeconds(2));

        // Assert
        statistics.DataSetId.Should().Be(dataSetId);
        statistics.DataSetName.Should().Be(dataSetName);
        statistics.TotalRows.Should().Be(totalRows);
        statistics.TotalColumns.Should().Be(totalColumns);
        statistics.MissingValues.Should().Be(5); // Sum of null counts
        statistics.DuplicateRows.Should().Be(0); // Not calculated in data summary
        statistics.ColumnSummaries.Should().HaveCount(3);
        statistics.ProcessingTime.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void CreateStatisticalSummary_ShouldCreateStatisticsWithNumericStatistics()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var dataSetName = "Test Dataset";
        var totalRows = 100;
        var totalColumns = 2;
        var columnStatistics = CreateTestColumnStatistics();

        // Act
        var statistics = Statistics.CreateStatisticalSummary(
            dataSetId,
            dataSetName,
            totalRows,
            totalColumns,
            5, // missingValues
            0, // duplicateRows
            CreateTestColumnSummaries(), // columnSummaries
            columnStatistics,
            TimeSpan.FromSeconds(3));

        // Assert
        statistics.DataSetId.Should().Be(dataSetId);
        statistics.DataSetName.Should().Be(dataSetName);
        statistics.TotalRows.Should().Be(totalRows);
        statistics.TotalColumns.Should().Be(totalColumns);
        statistics.ColumnStatistics.Should().HaveCount(2);
        statistics.ColumnStatistics.Should().ContainKey("age");
        statistics.ColumnStatistics.Should().ContainKey("salary");
        statistics.ProcessingTime.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void GetDataQualitySummary_ShouldCalculateCorrectQualityScore()
    {
        // Arrange
        var columnSummaries = CreateTestColumnSummaries();
        var statistics = Statistics.CreateDataSummary(
            Guid.NewGuid(),
            "Test Dataset",
            100,
            3,
            5, // missingValues
            0, // duplicateRows
            columnSummaries,
            TimeSpan.FromSeconds(1));

        // Act
        var qualitySummary = statistics.GetDataQualitySummary();

        // Assert
        qualitySummary.Should().NotBeNull();
        qualitySummary.QualityScore.Should().BeInRange(0, 100);
        qualitySummary.MissingDataPercentage.Should().BeApproximately(1.67, 0.01); // 5 missing out of 300 total cells (100 rows * 3 columns)
        qualitySummary.DuplicateRowsPercentage.Should().Be(0.0f);
    }

    [Fact]
    public void GetNumericColumnStatistics_ShouldReturnOnlyNumericColumns()
    {
        // Arrange
        var columnSummaries = CreateTestColumnSummaries();
        var columnStatistics = CreateTestColumnStatistics();
        var statistics = Statistics.CreateStatisticalSummary(
            Guid.NewGuid(),
            "Test Dataset",
            100,
            3,
            5,
            0,
            columnSummaries,
            columnStatistics,
            TimeSpan.FromSeconds(1));

        // Act
        var numericStats = statistics.GetNumericColumnStatistics();

        // Assert
        numericStats.Should().HaveCount(2);
        numericStats.Should().ContainKey("age");
        numericStats.Should().ContainKey("salary");
        numericStats.Should().NotContainKey("name"); // String column
    }

    [Fact]
    public void SoftDelete_ShouldMarkStatisticsAsDeleted()
    {
        // Arrange
        var statistics = CreateTestStatistics();

        // Act
        statistics.SoftDelete();

        // Assert
        statistics.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldUpdateStatisticsProperties()
    {
        // Arrange
        var statistics = CreateTestStatistics();
        var newColumnSummaries = new Dictionary<string, ColumnSummary>();
        var newColumnStatistics = new Dictionary<string, StatisticalMeasure>();
        var newProcessingTime = TimeSpan.FromSeconds(10);

        // Act
        statistics.Update(
            150,
            4,
            20,
            5,
            newColumnSummaries,
            newColumnStatistics,
            newProcessingTime);

        // Assert
        statistics.TotalRows.Should().Be(150);
        statistics.TotalColumns.Should().Be(4);
        statistics.MissingValues.Should().Be(20);
        statistics.DuplicateRows.Should().Be(5);
        statistics.ColumnSummaries.Should().BeEquivalentTo(newColumnSummaries);
        statistics.ColumnStatistics.Should().BeEquivalentTo(newColumnStatistics);
        statistics.ProcessingTime.Should().Be(newProcessingTime);
        statistics.CalculatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    private static Dictionary<string, ColumnSummary> CreateTestColumnSummaries()
    {
        return new Dictionary<string, ColumnSummary>
        {
            ["name"] = new ColumnSummary(
                "name",
                new DataTypeClassification("String", false, false, false),
                100,
                0,
                100,
                new List<object> { "John", "Jane", "Bob" },
                "Alice",
                "Zoe",
                null),
            ["age"] = new ColumnSummary(
                "age",
                new DataTypeClassification("Numeric", true, false, false),
                98,
                2,
                50,
                new List<object> { 25, 30, 35 },
                18,
                75,
                new StatisticalMeasure(42.5, 40, 15.2, 18, 75, 30, 40, 55, 0.2, -0.5, 3)),
            ["salary"] = new ColumnSummary(
                "salary",
                new DataTypeClassification("Numeric", true, false, false),
                97,
                3,
                80,
                new List<object> { 50000, 60000, 70000 },
                30000,
                120000,
                new StatisticalMeasure(65000, 62000, 18000, 30000, 120000, 50000, 62000, 80000, 0.3, -0.2, 5))
        };
    }

    private static Dictionary<string, StatisticalMeasure> CreateTestColumnStatistics()
    {
        return new Dictionary<string, StatisticalMeasure>
        {
            ["age"] = new StatisticalMeasure(42.5, 40, 15.2, 18, 75, 30, 40, 55, 0.2, -0.5, 3),
            ["salary"] = new StatisticalMeasure(65000, 62000, 18000, 30000, 120000, 50000, 62000, 80000, 0.3, -0.2, 5)
        };
    }

    private static Statistics CreateTestStatistics()
    {
        return Statistics.CreateStatisticalSummary(
            Guid.NewGuid(),
            "Test Dataset",
            100,
            3,
            5,
            2,
            CreateTestColumnSummaries(),
            CreateTestColumnStatistics(),
            TimeSpan.FromSeconds(5));
    }
}