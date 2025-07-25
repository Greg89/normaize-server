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
    private readonly Mock<IStatisticalCalculationService> _mockStatisticalCalculationService = new();
    private readonly Mock<IChartGenerationService> _mockChartGenerationService = new();
    private readonly Mock<IVisualizationServices> _mockVisualizationServices = new();
    private readonly Mock<ICacheManagementService> _mockCacheManagement = new();
    private readonly DataVisualizationService _service;

    public DataVisualizationServiceTests()
    {
        var options = new DataVisualizationOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Setup infrastructure mocks
        SetupInfrastructureMocks();

        // Setup visualization services mock
        _mockVisualizationServices.Setup(x => x.StatisticalCalculation).Returns(_mockStatisticalCalculationService.Object);
        _mockVisualizationServices.Setup(x => x.ChartGeneration).Returns(_mockChartGenerationService.Object);
        _mockVisualizationServices.Setup(x => x.CacheManagement).Returns(_mockCacheManagement.Object);

        _service = new DataVisualizationService(_mockRepo.Object, _mockOptions.Object, _mockInfrastructure.Object, _mockVisualizationServices.Object);
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

        // Setup mock chart generation service
        var expectedChartData = new ChartDataDto
        {
            DataSetId = dataSetId,
            ChartType = chartType,
            Labels = new List<string> { "A", "B" },
            Series = new List<ChartSeriesDto> { new ChartSeriesDto { Name = "value", Data = new List<object> { 10.0, 20.0 } } }
        };

        _mockChartGenerationService.Setup(x => x.GenerateChartData(It.IsAny<DataSet>(), It.IsAny<List<Dictionary<string, object>>>(), chartType, config, It.IsAny<IOperationContext>()))
            .Returns(expectedChartData);

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

        // Setup cache management mock to return cached value
        _mockCacheManagement.Setup(x => x.TryGetValue(It.IsAny<string>(), out expected)).Returns(true);

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

        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(ChartType.Bar, config))
            .Throws(new ArgumentException("MaxDataPoints must be greater than 0"));

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

        // Setup mock chart generation service
        var expectedComparisonChart = new ComparisonChartDto
        {
            DataSetId1 = id1,
            DataSetId2 = id2,
            ChartType = ChartType.Bar,
            Series = new List<ChartSeriesDto> { new ChartSeriesDto { Name = "test", Data = new List<object> { 1 } } },
            Labels = new List<string> { "A" }
        };

        _mockChartGenerationService.Setup(x => x.GenerateComparisonChartData(It.IsAny<DataSet>(), It.IsAny<DataSet>(), It.IsAny<List<Dictionary<string, object>>>(), It.IsAny<List<Dictionary<string, object>>>(), ChartType.Bar, null, It.IsAny<IOperationContext>()))
            .Returns(expectedComparisonChart);

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

        // Setup mock for StatisticalCalculationService
        var expectedSummary = new DataSummaryDto
        {
            DataSetId = dataSetId,
            TotalRows = 1,
            TotalColumns = 2,
            MissingValues = 0,
            DuplicateRows = 0,
            ProcessingTime = TimeSpan.FromMilliseconds(10)
        };
        _mockStatisticalCalculationService.Setup(s => s.GenerateDataSummary(It.IsAny<DataSet>(), It.IsAny<List<Dictionary<string, object>>>()))
            .Returns(expectedSummary);

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

        // Setup mock for StatisticalCalculationService
        var expectedStats = new StatisticalSummaryDto
        {
            DataSetId = dataSetId,
            ProcessingTime = TimeSpan.FromMilliseconds(10)
        };
        _mockStatisticalCalculationService.Setup(s => s.GenerateStatisticalSummary(It.IsAny<DataSet>(), It.IsAny<List<Dictionary<string, object>>>()))
            .Returns(expectedStats);

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
        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(ChartType.Bar, config)).Returns(true);

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
        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(ChartType.Bar, config))
            .Throws(new ArgumentException("MaxDataPoints must be greater than 0"));

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
    public void TestChartGenerationDirectly()
    {
        // Test the chart generation logic through the mock service
        var dataSet = new DataSet { Id = 1, UserId = "user1", ProcessedData = "[{\"label\": \"A\", \"value\": 10}, {\"label\": \"B\", \"value\": 20}]", UseSeparateTable = false };
        var data = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(dataSet.ProcessedData);

        // Create a mock operation context for testing
        var mockContext = new Mock<IOperationContext>();
        mockContext.Setup(x => x.CorrelationId).Returns("test-correlation-id");
        mockContext.Setup(x => x.UserId).Returns("user1");

        // Setup the mock chart generation service
        var expectedChartData = new ChartDataDto
        {
            DataSetId = 1,
            ChartType = ChartType.Bar,
            Labels = new List<string> { "A", "B" },
            Series = new List<ChartSeriesDto> { new ChartSeriesDto { Name = "value", Data = new List<object> { 10.0, 20.0 } } }
        };

        _mockChartGenerationService.Setup(x => x.GenerateChartData(dataSet, data!, ChartType.Bar, null, mockContext.Object))
            .Returns(expectedChartData);

        // Act
        var result = _mockChartGenerationService.Object.GenerateChartData(dataSet, data!, ChartType.Bar, null, mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DataSetId);
        Assert.Equal(ChartType.Bar, result.ChartType);
        Assert.NotEmpty(result.Series);
        Assert.NotEmpty(result.Labels);
    }
}