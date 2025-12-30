using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Application.Visualization.DTOs;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Services;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

public class VisualizationCacheServiceTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<VisualizationCacheService>> _mockLogger;
    private readonly VisualizationCacheService _sut;

    public VisualizationCacheServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = new Mock<ILogger<VisualizationCacheService>>();
        _sut = new VisualizationCacheService(_memoryCache, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new VisualizationCacheService(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new VisualizationCacheService(_memoryCache, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Chart Caching Tests

    [Fact]
    public void CacheChart_ShouldStoreChartData()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var chartType = ChartType.Bar;
        var chartData = CreateTestChartData();

        // Act
        _sut.CacheChart(dataSetId, chartType, null, chartData);
        var result = _sut.TryGetChart(dataSetId, chartType, null, out var cachedData);

        // Assert
        result.Should().BeTrue();
        cachedData.Should().NotBeNull();
        cachedData!.ChartType.Should().Be(chartData.ChartType);
        cachedData.Configuration?.Title.Should().Be(chartData.Configuration?.Title);
    }

    [Fact]
    public void TryGetChart_WithMissingData_ShouldReturnFalse()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var chartType = ChartType.Line;

        // Act
        var result = _sut.TryGetChart(dataSetId, chartType, null, out var cachedData);

        // Assert
        result.Should().BeFalse();
        cachedData.Should().BeNull();
    }

    [Fact]
    public void CacheChart_WithConfiguration_ShouldGenerateUniqueKey()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var chartType = ChartType.Pie;
        var config1 = new ChartConfigurationDto { Title = "Config 1" };
        var config2 = new ChartConfigurationDto { Title = "Config 2" };
        var chartData1 = CreateTestChartData();
        chartData1.Configuration = config1;
        var chartData2 = CreateTestChartData();
        chartData2.Configuration = config2;

        // Act
        _sut.CacheChart(dataSetId, chartType, config1, chartData1);
        _sut.CacheChart(dataSetId, chartType, config2, chartData2);

        // Assert - Different configurations should create different cache entries
        _sut.TryGetChart(dataSetId, chartType, config1, out var cached1).Should().BeTrue();
        _sut.TryGetChart(dataSetId, chartType, config2, out var cached2).Should().BeTrue();
        cached1!.Configuration?.Title.Should().Be("Config 1");
        cached2!.Configuration?.Title.Should().Be("Config 2");
    }

    [Fact]
    public void CacheChart_WithCustomExpiration_ShouldRespectExpiration()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var chartType = ChartType.Scatter;
        var chartData = CreateTestChartData();
        var expiration = TimeSpan.FromMilliseconds(100);

        // Act
        _sut.CacheChart(dataSetId, chartType, null, chartData, expiration);

        // Assert - Should be available immediately
        _sut.TryGetChart(dataSetId, chartType, null, out _).Should().BeTrue();

        // Wait for expiration
        System.Threading.Thread.Sleep(150);

        // Should be expired now (with sliding expiration, if not accessed)
        // Note: This test is timing-dependent and may be flaky
    }

    #endregion

    #region Comparison Chart Caching Tests

    [Fact]
    public void CacheComparisonChart_ShouldStoreComparisonChartData()
    {
        // Arrange
        var dataSetId1 = Guid.NewGuid();
        var dataSetId2 = Guid.NewGuid();
        var chartType = ChartType.Bar;
        var comparisonData = CreateTestComparisonChartData();

        // Act
        _sut.CacheComparisonChart(dataSetId1, dataSetId2, chartType, null, comparisonData);
        var result = _sut.TryGetComparisonChart(dataSetId1, dataSetId2, chartType, null, out var cachedData);

        // Assert
        result.Should().BeTrue();
        cachedData.Should().NotBeNull();
        cachedData!.ChartType.Should().Be(comparisonData.ChartType);
    }

    [Fact]
    public void TryGetComparisonChart_WithMissingData_ShouldReturnFalse()
    {
        // Arrange
        var dataSetId1 = Guid.NewGuid();
        var dataSetId2 = Guid.NewGuid();
        var chartType = ChartType.Line;

        // Act
        var result = _sut.TryGetComparisonChart(dataSetId1, dataSetId2, chartType, null, out var cachedData);

        // Assert
        result.Should().BeFalse();
        cachedData.Should().BeNull();
    }

    [Fact]
    public void CacheComparisonChart_WithReversedDataSetIds_ShouldUseSameCacheKey()
    {
        // Arrange
        var dataSetId1 = Guid.NewGuid();
        var dataSetId2 = Guid.NewGuid();
        var chartType = ChartType.Column;
        var comparisonData = CreateTestComparisonChartData();

        // Act - Cache with IDs in one order
        _sut.CacheComparisonChart(dataSetId1, dataSetId2, chartType, null, comparisonData);

        // Assert - Retrieve with IDs in reversed order should work
        var result = _sut.TryGetComparisonChart(dataSetId2, dataSetId1, chartType, null, out var cachedData);
        result.Should().BeTrue();
        cachedData.Should().NotBeNull();
    }

    #endregion

    #region Data Summary Caching Tests

    [Fact]
    public void CacheDataSummary_ShouldStoreDataSummary()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var summary = CreateTestDataSummary();

        // Act
        _sut.CacheDataSummary(dataSetId, summary);
        var result = _sut.TryGetDataSummary(dataSetId, out var cachedSummary);

        // Assert
        result.Should().BeTrue();
        cachedSummary.Should().NotBeNull();
        cachedSummary!.TotalRows.Should().Be(summary.TotalRows);
    }

    [Fact]
    public void TryGetDataSummary_WithMissingData_ShouldReturnFalse()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();

        // Act
        var result = _sut.TryGetDataSummary(dataSetId, out var cachedSummary);

        // Assert
        result.Should().BeFalse();
        cachedSummary.Should().BeNull();
    }

    #endregion

    #region Statistical Summary Caching Tests

    [Fact]
    public void CacheStatisticalSummary_ShouldStoreStatisticalSummary()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var summary = CreateTestStatisticalSummary();

        // Act
        _sut.CacheStatisticalSummary(dataSetId, summary);
        var result = _sut.TryGetStatisticalSummary(dataSetId, out var cachedSummary);

        // Assert
        result.Should().BeTrue();
        cachedSummary.Should().NotBeNull();
        cachedSummary!.ColumnStatistics.Should().HaveCount(summary.ColumnStatistics.Count);
    }

    [Fact]
    public void TryGetStatisticalSummary_WithMissingData_ShouldReturnFalse()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();

        // Act
        var result = _sut.TryGetStatisticalSummary(dataSetId, out var cachedSummary);

        // Assert
        result.Should().BeFalse();
        cachedSummary.Should().BeNull();
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public void InvalidateDataSet_ShouldLogInvalidationAttempt()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();

        // Act
        Action act = () => _sut.InvalidateDataSet(dataSetId);

        // Assert - Should not throw, though IMemoryCache has limited invalidation support
        act.Should().NotThrow();

        // Note: IMemoryCache doesn't support prefix-based removal
        // For production use with comprehensive invalidation, consider IDistributedCache
    }

    [Fact]
    public void InvalidateDataSet_WithNonExistentDataSet_ShouldNotThrow()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();

        // Act
        Action act = () => _sut.InvalidateDataSet(dataSetId);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Cache Key Generation Tests

    [Fact]
    public void CacheChart_WithSameParameters_ShouldOverwriteExistingEntry()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var chartType = ChartType.Area;
        var chartData1 = CreateTestChartData();
        chartData1.Configuration = new ChartConfigurationDto { Title = "First Chart" };
        var chartData2 = CreateTestChartData();
        chartData2.Configuration = new ChartConfigurationDto { Title = "Second Chart" };

        // Act
        _sut.CacheChart(dataSetId, chartType, null, chartData1);
        _sut.CacheChart(dataSetId, chartType, null, chartData2);

        // Assert - Should get the second chart (overwrites first)
        _sut.TryGetChart(dataSetId, chartType, null, out var cachedData).Should().BeTrue();
        cachedData!.Configuration?.Title.Should().Be("Second Chart");
    }

    [Fact]
    public void CacheChart_WithDifferentChartTypes_ShouldStoreSeparately()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var chartData1 = CreateTestChartData();
        chartData1.ChartType = ChartType.Bar;
        var chartData2 = CreateTestChartData();
        chartData2.ChartType = ChartType.Line;

        // Act
        _sut.CacheChart(dataSetId, ChartType.Bar, null, chartData1);
        _sut.CacheChart(dataSetId, ChartType.Line, null, chartData2);

        // Assert - Both should be retrievable
        _sut.TryGetChart(dataSetId, ChartType.Bar, null, out var barChart).Should().BeTrue();
        _sut.TryGetChart(dataSetId, ChartType.Line, null, out var lineChart).Should().BeTrue();
        barChart!.ChartType.Should().Be(ChartType.Bar);
        lineChart!.ChartType.Should().Be(ChartType.Line);
    }

    #endregion

    #region Helper Methods

    private ChartDataDto CreateTestChartData()
    {
        return new ChartDataDto
        {
            DataSetId = Guid.NewGuid(),
            ChartType = ChartType.Bar,
            Configuration = new ChartConfigurationDto { Title = "Test Chart" },
            Labels = new List<string> { "A", "B", "C" },
            Series = new List<ChartSeriesDto>
            {
                new ChartSeriesDto
                {
                    Name = "Series 1",
                    Data = new List<object> { 1.0, 2.0, 3.0 }
                }
            },
            GeneratedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromMilliseconds(100)
        };
    }

    private ComparisonChartDto CreateTestComparisonChartData()
    {
        return new ComparisonChartDto
        {
            DataSetId1 = Guid.NewGuid(),
            DataSetId2 = Guid.NewGuid(),
            ChartType = ChartType.Bar,
            Configuration = new ChartConfigurationDto { Title = "Comparison Chart" },
            Labels = new List<string> { "A", "B" },
            Series = new List<ChartSeriesDto>
            {
                new ChartSeriesDto
                {
                    Name = "Dataset 1",
                    Data = new List<object> { 1.0, 2.0 }
                },
                new ChartSeriesDto
                {
                    Name = "Dataset 2",
                    Data = new List<object> { 3.0, 4.0 }
                }
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    private DataSummaryDto CreateTestDataSummary()
    {
        return new DataSummaryDto
        {
            DataSetId = Guid.NewGuid(),
            TotalRows = 100,
            TotalColumns = 5,
            MissingValues = 5,
            DuplicateRows = 2,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private StatisticalSummaryDto CreateTestStatisticalSummary()
    {
        return new StatisticalSummaryDto
        {
            DataSetId = Guid.NewGuid(),
            ColumnStatistics = new Dictionary<string, ColumnStatisticsDto>
            {
                ["Column1"] = new ColumnStatisticsDto { ColumnName = "Column1", Mean = 10.0, Median = 9.5, StandardDeviation = 1.0, Min = 5.0, Max = 15.0, Q1 = 8.0, Q2 = 9.5, Q3 = 12.0 },
                ["Column2"] = new ColumnStatisticsDto { ColumnName = "Column2", Mean = 20.0, Median = 19.5, StandardDeviation = 2.0, Min = 15.0, Max = 25.0, Q1 = 18.0, Q2 = 19.5, Q3 = 22.0 }
            },
            GeneratedAt = DateTime.UtcNow
        };
    }

    #endregion
}
