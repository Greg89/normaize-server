using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Normaize.Core.Services;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Normaize.Core.Models;
using FluentAssertions;

namespace Normaize.Tests.Services;

public class DataVisualizationServiceTests
{
    private readonly Mock<IDataSetRepository> _mockRepo = new();
    private readonly Mock<IOptions<DataVisualizationOptions>> _mockOptions = new();
    private readonly Mock<IDataProcessingInfrastructure> _mockInfrastructure = new();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly DataVisualizationService _service;

    public DataVisualizationServiceTests()
    {
        var options = new DataVisualizationOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Setup infrastructure mocks
        SetupInfrastructureMocks();

        _service = new DataVisualizationService(_mockRepo.Object, _memoryCache, _mockOptions.Object, _mockInfrastructure.Object);
    }

    private void SetupInfrastructureMocks()
    {
        // Setup logger mock
        var mockLogger = new Mock<ILogger<DataVisualizationService>>();
        _mockInfrastructure.Setup(x => x.Logger).Returns(mockLogger.Object);

        // Setup structured logging mock
        var mockStructuredLogging = new Mock<IStructuredLoggingService>();
        var mockContext = new Mock<IOperationContext>();
        mockContext.Setup(x => x.OperationName).Returns("TestOperation");
        mockStructuredLogging.Setup(x => x.CreateContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(mockContext.Object);
        mockStructuredLogging.Setup(x => x.LogStep(It.IsAny<IOperationContext>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()));
        mockStructuredLogging.Setup(x => x.LogSummary(It.IsAny<IOperationContext>(), It.IsAny<bool>(), It.IsAny<string>()));
        _mockInfrastructure.Setup(x => x.StructuredLogging).Returns(mockStructuredLogging.Object);

        // Setup chaos engineering mock
        var mockChaosEngineering = new Mock<IChaosEngineeringService>();
        mockChaosEngineering.Setup(x => x.ExecuteChaosAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>(), It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(false);
        _mockInfrastructure.Setup(x => x.ChaosEngineering).Returns(mockChaosEngineering.Object);

        // Setup default timeout values
        _mockInfrastructure.Setup(x => x.DefaultTimeout).Returns(TimeSpan.FromMinutes(5));
        _mockInfrastructure.Setup(x => x.QuickTimeout).Returns(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task GenerateChartAsync_ReturnsChartData_WhenValidInputAndCacheMiss()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user1";
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto { Title = "Test Chart" };
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, ProcessedData = "[{\"label\": \"A\", \"value\": 10}, {\"label\": \"B\", \"value\": 20}]", UseSeparateTable = false };
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

        // Use the same cache key generation logic as the service
        var baseKey = $"chart_{dataSetId}_{chartType}";
        var configHash = System.Text.Json.JsonSerializer.Serialize(config);
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(configHash));
        var cacheKey = $"{baseKey}_{Convert.ToBase64String(hash)[..8]}";

        _memoryCache.Set(cacheKey, expected);

        // Setup dataset for validation
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, ProcessedData = "[{\"label\": \"A\", \"value\": 1}]", UseSeparateTable = false };
        _mockRepo.Setup(r => r.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateChartAsync(dataSetId, chartType, config, userId));
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Dataset ID must be positive");
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateChartAsync(dataSetId, ChartType.Bar, null, userId));
        exception.Message.Should().Contain("Failed to complete GenerateChartAsync for dataset ID");
        exception.InnerException.Should().BeOfType<UnauthorizedAccessException>();
        exception.InnerException!.Message.Should().Contain("User user3 is not authorized to access dataset 3");
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateChartAsync(dataSetId, ChartType.Bar, null, userId));
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Dataset 4 has been deleted");
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateChartAsync(dataSetId, ChartType.Bar, config, userId));
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("MaxDataPoints must be greater than 0");
    }

    [Fact]
    public async Task GenerateComparisonChartAsync_ReturnsComparison_WhenValidInput()
    {
        // Arrange
        var id1 = 10; var id2 = 11; var userId = "user10";
        var ds1 = new DataSet { Id = id1, UserId = userId, ProcessedData = "[{\"label\": \"A\", \"value\": 1}]", UseSeparateTable = false };
        var ds2 = new DataSet { Id = id2, UserId = userId, ProcessedData = "[{\"label\": \"A\", \"value\": 2}]", UseSeparateTable = false };
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateComparisonChartAsync(id, id, ChartType.Bar, null, userId));
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Dataset IDs must be different for comparison");
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateComparisonChartAsync(id1, id2, ChartType.Bar, null, userId));
        exception.Message.Should().Contain("Failed to complete GenerateComparisonChartAsync for dataset IDs");
        exception.InnerException.Should().BeOfType<UnauthorizedAccessException>();
        exception.InnerException!.Message.Should().Contain("User user13 is not authorized to access dataset 13");
    }

    [Fact]
    public async Task GetDataSummaryAsync_ReturnsSummary_WhenValidInput()
    {
        // Arrange
        var dataSetId = 20;
        var userId = "user20";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, ProcessedData = "[{\"label\": \"A\", \"value\": 1}]", UseSeparateTable = false };
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetDataSummaryAsync(dataSetId, userId));
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Dataset ID must be positive");
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetDataSummaryAsync(dataSetId, userId));
        exception.Message.Should().Contain("Failed to complete GetDataSummaryAsync for dataset ID");
        exception.InnerException.Should().BeOfType<UnauthorizedAccessException>();
        exception.InnerException!.Message.Should().Contain("User user21 is not authorized to access dataset 21");
    }

    [Fact]
    public async Task GetStatisticalSummaryAsync_ReturnsStats_WhenValidInput()
    {
        // Arrange
        var dataSetId = 30;
        var userId = "user30";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, ProcessedData = "[{\"label\": \"A\", \"value\": 1}]", UseSeparateTable = false };
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetStatisticalSummaryAsync(dataSetId, userId));
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Dataset ID must be positive");
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
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetStatisticalSummaryAsync(dataSetId, userId));
        exception.Message.Should().Contain("Failed to complete GetStatisticalSummaryAsync for dataset ID");
        exception.InnerException.Should().BeOfType<UnauthorizedAccessException>();
        exception.InnerException!.Message.Should().Contain("User user31 is not authorized to access dataset 31");
    }

    [Fact]
    public void ValidateChartConfiguration_ReturnsTrue_WhenValid()
    {
        // Arrange
        var config = new ChartConfigurationDto { MaxDataPoints = 5 };

        // Act
        var result = _service.ValidateChartConfiguration(ChartType.Bar, config);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateChartConfiguration_Throws_WhenInvalid()
    {
        // Arrange
        var config = new ChartConfigurationDto { MaxDataPoints = 0 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.ValidateChartConfiguration(ChartType.Bar, config));
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

    [Fact]
    public void TestJsonParsing()
    {
        // Test the JSON parsing logic directly
        var jsonData = "[{\"label\": \"A\", \"value\": 10}, {\"label\": \"B\", \"value\": 20}]";
        var data = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonData);

        Assert.NotNull(data);
        Assert.Equal(2, data.Count);
        Assert.Equal("A", data[0]["label"].ToString());
        Assert.Equal("10", data[0]["value"].ToString()); // JSON numbers are parsed as strings by default
        Assert.Equal("B", data[1]["label"].ToString());
        Assert.Equal("20", data[1]["value"].ToString());
    }

    [Fact]
    public void TestIsNumericMethod()
    {
        // Test the IsNumeric method directly
        var service = new DataVisualizationService(_mockRepo.Object, _memoryCache, _mockOptions.Object, _mockInfrastructure.Object);

        // Test with reflection to access the private static method
        var method = typeof(DataVisualizationService).GetMethod("IsNumeric",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);

        // Test various numeric representations
        Assert.True((bool)method.Invoke(null, ["10"])!);
        Assert.True((bool)method.Invoke(null, ["20.5"])!);
        Assert.True((bool)method.Invoke(null, [10])!);
        Assert.True((bool)method.Invoke(null, [20.5])!);
        Assert.False((bool)method.Invoke(null, ["A"])!);
        Assert.False((bool)method.Invoke(null, ["label"])!);
    }

    [Fact]
    public void TestIsNumericColumnMethod()
    {
        // Test with reflection to access the private static method
        var method = typeof(DataVisualizationService).GetMethod("IsNumericColumn",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);

        // Test with numeric data
        var numericData = new List<object?> { "10", "20", "30" };
        Assert.True((bool)method.Invoke(null, [numericData])!);

        // Test with mixed data
        var mixedData = new List<object?> { "10", "A", "30" };
        Assert.False((bool)method.Invoke(null, [mixedData])!);

        // Test with string data
        var stringData = new List<object?> { "A", "B", "C" };
        Assert.False((bool)method.Invoke(null, [stringData])!);
    }

    [Fact]
    public void TestChartGenerationDirectly()
    {
        // Test the chart generation logic directly
        var service = new DataVisualizationService(_mockRepo.Object, _memoryCache, _mockOptions.Object, _mockInfrastructure.Object);

        // Create test data
        var dataSet = new DataSet { Id = 1, UserId = "user1", ProcessedData = "[{\"label\": \"A\", \"value\": 10}, {\"label\": \"B\", \"value\": 20}]", UseSeparateTable = false };
        var data = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);

        // Create a mock operation context for testing
        var mockContext = new Mock<IOperationContext>();
        mockContext.Setup(x => x.CorrelationId).Returns("test-correlation-id");
        mockContext.Setup(x => x.UserId).Returns("user1");

        // Test with reflection to access the private method
        var method = typeof(DataVisualizationService).GetMethod("GenerateChartData",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        var result = (ChartDataDto)method.Invoke(service, [dataSet, data!, ChartType.Bar, null!, mockContext.Object])!;

        Assert.NotNull(result);
        Assert.Equal(1, result.DataSetId);
        Assert.Equal(ChartType.Bar, result.ChartType);
        Assert.NotEmpty(result.Series);
        Assert.NotEmpty(result.Labels);
    }
}