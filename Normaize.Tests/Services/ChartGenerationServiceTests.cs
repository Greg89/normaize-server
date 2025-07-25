using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using Normaize.Core.Services.Visualization;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Normaize.Core.Models;
using Normaize.Core.Services;
using FluentAssertions;

namespace Normaize.Tests.Services;

public class ChartGenerationServiceTests
{
    private readonly Mock<IStatisticalCalculationService> _mockStatisticalCalculationService = new();
    private readonly Mock<IOptions<DataVisualizationOptions>> _mockOptions = new();
    private readonly Mock<IDataProcessingInfrastructure> _mockInfrastructure = new();
    private readonly ChartGenerationService _service;

    public ChartGenerationServiceTests()
    {
        var options = new DataVisualizationOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);

        // Setup infrastructure mocks
        SetupInfrastructureMocks();

        _service = new ChartGenerationService(_mockStatisticalCalculationService.Object, _mockOptions.Object, _mockInfrastructure.Object);
    }

    private void SetupInfrastructureMocks()
    {
        // Setup structured logging mock
        var mockStructuredLogging = new Mock<IStructuredLoggingService>();
        var mockContext = new Mock<IOperationContext>();
        mockContext.Setup(x => x.OperationName).Returns("TestOperation");
        mockStructuredLogging.Setup(x => x.LogStep(It.IsAny<IOperationContext>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()));
        _mockInfrastructure.Setup(x => x.StructuredLogging).Returns(mockStructuredLogging.Object);
    }

    [Fact]
    public void GenerateChartData_ReturnsEmptyChart_WhenNoData()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1", ProcessedData = "[]", UseSeparateTable = false };
        var data = new List<Dictionary<string, object>>();
        var mockContext = new Mock<IOperationContext>();

        // Act
        var result = _service.GenerateChartData(dataSet, data, ChartType.Bar, null, mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DataSetId);
        Assert.Equal(ChartType.Bar, result.ChartType);
        Assert.Empty(result.Labels);
        Assert.Empty(result.Series);
    }

    [Fact]
    public void GenerateChartData_GeneratesBarChart_WhenValidData()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1", ProcessedData = "[{\"label\": \"A\", \"value\": 10}, {\"label\": \"B\", \"value\": 20}]", UseSeparateTable = false };
        var data = new List<Dictionary<string, object>>
        {
            new() { ["label"] = "A", ["value"] = 10 },
            new() { ["label"] = "B", ["value"] = 20 }
        };
        var mockContext = new Mock<IOperationContext>();

        _mockStatisticalCalculationService.Setup(x => x.IsNumericColumn(It.IsAny<List<object?>>()))
            .Returns((List<object?> columnData) =>
            {
                if (columnData.Count == 0) return false;
                var firstValue = columnData[0];
                return firstValue?.ToString() == "value";
            });

        // Act
        var result = _service.GenerateChartData(dataSet, data, ChartType.Bar, null, mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DataSetId);
        Assert.Equal(ChartType.Bar, result.ChartType);
        Assert.NotEmpty(result.Labels);
        Assert.NotEmpty(result.Series);
    }

    [Fact]
    public void GenerateChartData_GeneratesPieChart_WhenValidData()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1", ProcessedData = "[{\"category\": \"A\", \"value\": 10}, {\"category\": \"B\", \"value\": 20}]", UseSeparateTable = false };
        var data = new List<Dictionary<string, object>>
        {
            new() { ["category"] = "A", ["value"] = 10 },
            new() { ["category"] = "B", ["value"] = 20 }
        };
        var mockContext = new Mock<IOperationContext>();

        _mockStatisticalCalculationService.Setup(x => x.IsNumericColumn(It.IsAny<List<object?>>()))
            .Returns((List<object?> columnData) =>
            {
                if (columnData.Count == 0) return false;
                var firstValue = columnData[0];
                return firstValue?.ToString() == "value";
            });

        // Act
        var result = _service.GenerateChartData(dataSet, data, ChartType.Pie, null, mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DataSetId);
        Assert.Equal(ChartType.Pie, result.ChartType);
        Assert.NotEmpty(result.Labels);
        Assert.NotEmpty(result.Series);
    }

    [Fact]
    public void GenerateChartData_GeneratesScatterChart_WhenValidData()
    {
        // Arrange
        var dataSet = new DataSet { Id = 1, UserId = "user1", ProcessedData = "[{\"point\": \"A\", \"x\": 10, \"y\": 20}, {\"point\": \"B\", \"x\": 30, \"y\": 40}]", UseSeparateTable = false };
        var data = new List<Dictionary<string, object>>
        {
            new() { ["point"] = "A", ["x"] = 10, ["y"] = 20 },
            new() { ["point"] = "B", ["x"] = 30, ["y"] = 40 }
        };
        var mockContext = new Mock<IOperationContext>();

        _mockStatisticalCalculationService.Setup(x => x.IsNumericColumn(It.IsAny<List<object?>>()))
            .Returns((List<object?> columnData) =>
            {
                if (columnData.Count == 0) return false;
                var firstValue = columnData[0];
                return firstValue?.ToString() == "x" || firstValue?.ToString() == "y";
            });

        // Act
        var result = _service.GenerateChartData(dataSet, data, ChartType.Scatter, null, mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DataSetId);
        Assert.Equal(ChartType.Scatter, result.ChartType);
        Assert.NotEmpty(result.Labels);
        Assert.NotEmpty(result.Series);
    }

    [Fact]
    public void GenerateComparisonChartData_ReturnsCombinedChart_WhenValidData()
    {
        // Arrange
        var dataSet1 = new DataSet { Id = 1, UserId = "user1", ProcessedData = "[{\"label\": \"A\", \"value\": 10}]", UseSeparateTable = false };
        var dataSet2 = new DataSet { Id = 2, UserId = "user1", ProcessedData = "[{\"label\": \"A\", \"value\": 20}]", UseSeparateTable = false };
        var data1 = new List<Dictionary<string, object>> { new() { ["label"] = "A", ["value"] = 10 } };
        var data2 = new List<Dictionary<string, object>> { new() { ["label"] = "A", ["value"] = 20 } };
        var mockContext = new Mock<IOperationContext>();

        _mockStatisticalCalculationService.Setup(x => x.IsNumericColumn(It.IsAny<List<object?>>()))
            .Returns((List<object?> columnData) =>
            {
                if (columnData.Count == 0) return false;
                var firstValue = columnData[0];
                return firstValue?.ToString() == "value";
            });

        // Act
        var result = _service.GenerateComparisonChartData(dataSet1, dataSet2, data1, data2, ChartType.Bar, null, mockContext.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.DataSetId1);
        Assert.Equal(2, result.DataSetId2);
        Assert.Equal(ChartType.Bar, result.ChartType);
        Assert.NotEmpty(result.Series);
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
    public void ValidateChartConfiguration_Throws_WhenInvalidMaxDataPoints()
    {
        // Arrange
        var config = new ChartConfigurationDto { MaxDataPoints = 0 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateChartConfiguration(ChartType.Bar, config));
        Assert.Contains("MaxDataPoints must be greater than 0", exception.Message);
    }

    [Fact]
    public void ValidateChartConfiguration_ReturnsTrue_WhenNullConfiguration()
    {
        // Act
        var result = _service.ValidateChartConfiguration(ChartType.Bar, null);

        // Assert
        Assert.True(result);
    }
}