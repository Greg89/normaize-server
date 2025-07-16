using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Normaize.Core.Services;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Normaize.Core.Models;

namespace Normaize.Tests.Services;

public class DataVisualizationServiceTests
{
    private readonly Mock<ILogger<DataVisualizationService>> _mockLogger = new();
    private readonly Mock<IDataSetRepository> _mockRepo = new();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly DataVisualizationService _service;

    public DataVisualizationServiceTests()
    {
        _service = new DataVisualizationService(_mockLogger.Object, _mockRepo.Object, _memoryCache);
    }

    [Fact]
    public async Task GenerateChartAsync_ReturnsChartData_WhenValidInputAndCacheMiss()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user1";
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto { Title = "Test Chart" };
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, ProcessedData = "[{\"value\": 10}, {\"value\": 20}]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GenerateChartAsync(dataSetId, chartType, config, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataSetId, result.DataSetId);
        Assert.Equal(chartType, result.ChartType);
        Assert.NotEmpty(result.Series);
        Assert.NotEmpty(result.Labels);
    }

    [Fact]
    public async Task GenerateChartAsync_ReturnsChartData_WhenCacheHit()
    {
        // Arrange
        var dataSetId = 2;
        var userId = "user2";
        var chartType = ChartType.Pie;
        var config = new ChartConfigurationDto { Title = "Pie Chart" };
        var expected = new ChartDataDto { DataSetId = dataSetId, ChartType = chartType, Labels = new List<string> { "A" }, Series = new List<ChartSeriesDto> { new ChartSeriesDto { Name = "S", Data = new List<object> { 1 } } } };
        var cacheKey = $"chart_{dataSetId}_{chartType}_{System.Text.Json.JsonSerializer.Serialize(config)}";
        _memoryCache.Set(cacheKey, expected);
        
        // Act
        var result = await _service.GenerateChartAsync(dataSetId, chartType, config, userId);
        
        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GenerateChartAsync_Throws_WhenInvalidDatasetId()
    {
        // Arrange
        var dataSetId = 0;
        var userId = "user";
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateChartAsync(dataSetId, chartType, config, userId));
    }

    [Fact]
    public async Task GenerateChartAsync_Throws_WhenUnauthorizedUser()
    {
        // Arrange
        var dataSetId = 3;
        var userId = "user3";
        var dataSet = new DataSet { Id = dataSetId, UserId = "other", ProcessedData = "[]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GenerateChartAsync(dataSetId, ChartType.Bar, null, userId));
    }

    [Fact]
    public async Task GenerateChartAsync_Throws_WhenDeletedDataset()
    {
        // Arrange
        var dataSetId = 4;
        var userId = "user4";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, IsDeleted = true, ProcessedData = "[]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateChartAsync(dataSetId, ChartType.Bar, null, userId));
    }

    [Fact]
    public async Task GenerateChartAsync_Throws_WhenInvalidChartConfig()
    {
        // Arrange
        var dataSetId = 5;
        var userId = "user5";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, ProcessedData = "[]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        var config = new ChartConfigurationDto { MaxDataPoints = 0 };
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateChartAsync(dataSetId, ChartType.Bar, config, userId));
    }

    [Fact]
    public async Task GenerateComparisonChartAsync_ReturnsComparison_WhenValidInput()
    {
        // Arrange
        var id1 = 10; var id2 = 11; var userId = "user10";
        var ds1 = new DataSet { Id = id1, UserId = userId, ProcessedData = "[{\"value\": 1}]", UseSeparateTable = false };
        var ds2 = new DataSet { Id = id2, UserId = userId, ProcessedData = "[{\"value\": 2}]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(id1)).ReturnsAsync(ds1);
        _mockRepo.Setup(r => r.GetByIdAsync(id2)).ReturnsAsync(ds2);
        
        // Act
        var result = await _service.GenerateComparisonChartAsync(id1, id2, ChartType.Bar, null, userId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(id1, result.DataSetId1);
        Assert.Equal(id2, result.DataSetId2);
        Assert.NotEmpty(result.Series);
    }

    [Fact]
    public async Task GenerateComparisonChartAsync_Throws_WhenSameDatasetIds()
    {
        // Arrange
        var id = 12; var userId = "user12";
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateComparisonChartAsync(id, id, ChartType.Bar, null, userId));
    }

    [Fact]
    public async Task GenerateComparisonChartAsync_Throws_WhenUnauthorizedUser()
    {
        // Arrange
        var id1 = 13; var id2 = 14; var userId = "user13";
        var ds1 = new DataSet { Id = id1, UserId = "other", ProcessedData = "[]", UseSeparateTable = false };
        var ds2 = new DataSet { Id = id2, UserId = userId, ProcessedData = "[]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(id1)).ReturnsAsync(ds1);
        _mockRepo.Setup(r => r.GetByIdAsync(id2)).ReturnsAsync(ds2);
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GenerateComparisonChartAsync(id1, id2, ChartType.Bar, null, userId));
    }

    [Fact]
    public async Task GetDataSummaryAsync_ReturnsSummary_WhenValidInput()
    {
        // Arrange
        var dataSetId = 20;
        var userId = "user20";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, ProcessedData = "[{\"value\": 1}]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        
        // Act
        var result = await _service.GetDataSummaryAsync(dataSetId, userId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataSetId, result.DataSetId);
    }

    [Fact]
    public async Task GetDataSummaryAsync_Throws_WhenInvalidDatasetId()
    {
        // Arrange
        var dataSetId = 0;
        var userId = "user";
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetDataSummaryAsync(dataSetId, userId));
    }

    [Fact]
    public async Task GetDataSummaryAsync_Throws_WhenUnauthorizedUser()
    {
        // Arrange
        var dataSetId = 21;
        var userId = "user21";
        var dataSet = new DataSet { Id = dataSetId, UserId = "other", ProcessedData = "[]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetDataSummaryAsync(dataSetId, userId));
    }

    [Fact]
    public async Task GetStatisticalSummaryAsync_ReturnsStats_WhenValidInput()
    {
        // Arrange
        var dataSetId = 30;
        var userId = "user30";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, ProcessedData = "[{\"value\": 1}]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        
        // Act
        var result = await _service.GetStatisticalSummaryAsync(dataSetId, userId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataSetId, result.DataSetId);
    }

    [Fact]
    public async Task GetStatisticalSummaryAsync_Throws_WhenInvalidDatasetId()
    {
        // Arrange
        var dataSetId = 0;
        var userId = "user";
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetStatisticalSummaryAsync(dataSetId, userId));
    }

    [Fact]
    public async Task GetStatisticalSummaryAsync_Throws_WhenUnauthorizedUser()
    {
        // Arrange
        var dataSetId = 31;
        var userId = "user31";
        var dataSet = new DataSet { Id = dataSetId, UserId = "other", ProcessedData = "[]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.GetStatisticalSummaryAsync(dataSetId, userId));
    }

    [Fact]
    public async Task ValidateChartConfigurationAsync_ReturnsTrue_WhenValid()
    {
        // Arrange
        var config = new ChartConfigurationDto { MaxDataPoints = 5 };
        
        // Act
        var result = await _service.ValidateChartConfigurationAsync(ChartType.Bar, config);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateChartConfigurationAsync_Throws_WhenInvalid()
    {
        // Arrange
        var config = new ChartConfigurationDto { MaxDataPoints = 0 };
        
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.ValidateChartConfigurationAsync(ChartType.Bar, config));
    }

    [Fact]
    public async Task GetSupportedChartTypesAsync_ReturnsAllTypes()
    {
        // Act
        var result = await _service.GetSupportedChartTypesAsync();
        
        // Assert
        Assert.NotNull(result);
        Assert.Contains(ChartType.Bar, result);
        Assert.Contains(ChartType.Pie, result);
    }
} 