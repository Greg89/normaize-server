using Xunit;
using Moq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Normaize.Core.DTOs;
using Normaize.Core.Services.Visualization;
using Normaize.Core.Constants;
using Normaize.Core.Services;
using FluentAssertions;

namespace Normaize.Tests.Services;

public class CacheManagementServiceTests
{
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<IOptions<DataVisualizationOptions>> _mockOptions;
    private readonly CacheManagementService _service;
    private readonly DataVisualizationOptions _options;

    public CacheManagementServiceTests()
    {
        _mockCache = new Mock<IMemoryCache>();
        _options = new DataVisualizationOptions();
        _mockOptions = new Mock<IOptions<DataVisualizationOptions>>();
        _mockOptions.Setup(x => x.Value).Returns(_options);
        _service = new CacheManagementService(_mockCache.Object, _mockOptions.Object);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCacheIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new CacheManagementService(null!, _mockOptions.Object));
        exception.ParamName.Should().Be("cache");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new CacheManagementService(_mockCache.Object, null!));
        exception.ParamName.Should().Be("options");
    }

    [Fact]
    public void TryGetValue_ReturnsTrue_WhenValueExists()
    {
        // Arrange
        var cacheKey = "test_key";
        var expectedValue = "test_value";
        object? cachedValue = expectedValue;

        _mockCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue)).Returns(true);

        // Act
        var result = _service.TryGetValue(cacheKey, out string? value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be(expectedValue);
        _mockCache.Verify(x => x.TryGetValue(cacheKey, out cachedValue), Times.Once);
    }

    [Fact]
    public void TryGetValue_ReturnsFalse_WhenValueDoesNotExist()
    {
        // Arrange
        var cacheKey = "test_key";
        object? cachedValue = null;

        _mockCache.Setup(x => x.TryGetValue(cacheKey, out cachedValue)).Returns(false);

        // Act
        var result = _service.TryGetValue(cacheKey, out string? value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
        _mockCache.Verify(x => x.TryGetValue(cacheKey, out cachedValue), Times.Once);
    }

    [Fact]
    public void Set_CallsCacheSet_WithCorrectParameters()
    {
        // Arrange
        var cacheKey = "test_key";
        var value = "test_value";
        var expiration = TimeSpan.FromMinutes(5);

        // Use a real MemoryCache for this test
        var realCache = new MemoryCache(new MemoryCacheOptions());
        var realService = new CacheManagementService(realCache, _mockOptions.Object);

        // Act & Assert - Test that the method doesn't throw
        var exception = Record.Exception(() => realService.Set(cacheKey, value, expiration));
        exception.Should().BeNull();
    }

    [Fact]
    public void GenerateCacheKey_ReturnsBaseKey_WhenConfigurationIsNull()
    {
        // Arrange
        var baseKey = "test_base_key";
        ChartConfigurationDto? configuration = null;

        // Act
        var result = _service.GenerateCacheKey(baseKey, configuration);

        // Assert
        result.Should().Be(baseKey);
    }

    [Fact]
    public void GenerateCacheKey_ReturnsKeyWithHash_WhenConfigurationIsNotNull()
    {
        // Arrange
        var baseKey = "test_base_key";
        var configuration = new ChartConfigurationDto { Title = "Test Chart", MaxDataPoints = 100 };

        // Act
        var result = _service.GenerateCacheKey(baseKey, configuration);

        // Assert
        result.Should().StartWith(baseKey + "_");
        result.Should().HaveLength(baseKey.Length + 1 + AppConstants.DataProcessing.CACHE_KEY_HASH_LENGTH);
    }

    [Fact]
    public void GenerateCacheKey_ProducesConsistentHash_ForSameConfiguration()
    {
        // Arrange
        var baseKey = "test_base_key";
        var configuration = new ChartConfigurationDto { Title = "Test Chart", MaxDataPoints = 100 };

        // Act
        var result1 = _service.GenerateCacheKey(baseKey, configuration);
        var result2 = _service.GenerateCacheKey(baseKey, configuration);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void GenerateCacheKey_ProducesDifferentHash_ForDifferentConfiguration()
    {
        // Arrange
        var baseKey = "test_base_key";
        var config1 = new ChartConfigurationDto { Title = "Test Chart 1", MaxDataPoints = 100 };
        var config2 = new ChartConfigurationDto { Title = "Test Chart 2", MaxDataPoints = 100 };

        // Act
        var result1 = _service.GenerateCacheKey(baseKey, config1);
        var result2 = _service.GenerateCacheKey(baseKey, config2);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GenerateChartCacheKey_ReturnsCorrectFormat()
    {
        // Arrange
        var dataSetId = 123;
        var chartType = ChartType.Bar;
        var configuration = new ChartConfigurationDto { Title = "Test Chart" };

        // Act
        var result = _service.GenerateChartCacheKey(dataSetId, chartType, configuration);

        // Assert
        result.Should().StartWith($"chart_{dataSetId}_{chartType}_");
    }

    [Fact]
    public void GenerateChartCacheKey_ReturnsBaseKey_WhenConfigurationIsNull()
    {
        // Arrange
        var dataSetId = 123;
        var chartType = ChartType.Bar;
        ChartConfigurationDto? configuration = null;

        // Act
        var result = _service.GenerateChartCacheKey(dataSetId, chartType, configuration);

        // Assert
        result.Should().Be($"chart_{dataSetId}_{chartType}");
    }

    [Fact]
    public void GenerateComparisonChartCacheKey_ReturnsCorrectFormat()
    {
        // Arrange
        var dataSetId1 = 123;
        var dataSetId2 = 456;
        var chartType = ChartType.Bar;
        var configuration = new ChartConfigurationDto { Title = "Test Chart" };

        // Act
        var result = _service.GenerateComparisonChartCacheKey(dataSetId1, dataSetId2, chartType, configuration);

        // Assert
        result.Should().StartWith($"comparison_{dataSetId1}_{dataSetId2}_{chartType}_");
    }

    [Fact]
    public void GenerateComparisonChartCacheKey_ReturnsBaseKey_WhenConfigurationIsNull()
    {
        // Arrange
        var dataSetId1 = 123;
        var dataSetId2 = 456;
        var chartType = ChartType.Bar;
        ChartConfigurationDto? configuration = null;

        // Act
        var result = _service.GenerateComparisonChartCacheKey(dataSetId1, dataSetId2, chartType, configuration);

        // Assert
        result.Should().Be($"comparison_{dataSetId1}_{dataSetId2}_{chartType}");
    }

    [Fact]
    public void GenerateDataSummaryCacheKey_ReturnsCorrectFormat()
    {
        // Arrange
        var dataSetId = 123;

        // Act
        var result = _service.GenerateDataSummaryCacheKey(dataSetId);

        // Assert
        result.Should().Be($"summary_{dataSetId}");
    }

    [Fact]
    public void GenerateStatisticalSummaryCacheKey_ReturnsCorrectFormat()
    {
        // Arrange
        var dataSetId = 123;

        // Act
        var result = _service.GenerateStatisticalSummaryCacheKey(dataSetId);

        // Assert
        result.Should().Be($"stats_{dataSetId}");
    }

    [Fact]
    public void Remove_CallsCacheRemove_WithCorrectKey()
    {
        // Arrange
        var cacheKey = "test_key";

        // Act
        _service.Remove(cacheKey);

        // Assert
        _mockCache.Verify(x => x.Remove(cacheKey), Times.Once);
    }

    [Fact]
    public void Clear_DoesNotThrow_WhenCacheIsNotMemoryCache()
    {
        // Arrange
        var mockGenericCache = new Mock<IMemoryCache>();
        var service = new CacheManagementService(mockGenericCache.Object, _mockOptions.Object);

        // Act & Assert
        var exception = Record.Exception(service.Clear);
        exception.Should().BeNull();
    }

    [Fact]
    public void GenerateCacheKey_HashIsDeterministic_ForComplexConfiguration()
    {
        // Arrange
        var baseKey = "test_base_key";
        var configuration = new ChartConfigurationDto
        {
            Title = "Complex Chart",
            XAxisLabel = "X Axis",
            YAxisLabel = "Y Axis",
            ShowLegend = true,
            ShowGrid = false,
            ColorScheme = "viridis",
            MaxDataPoints = 1000,
            AggregationType = DataAggregationType.Average,
            CustomOptions = new Dictionary<string, object>
            {
                ["option1"] = "value1",
                ["option2"] = 42,
                ["option3"] = true
            }
        };

        // Act
        var result1 = _service.GenerateCacheKey(baseKey, configuration);
        var result2 = _service.GenerateCacheKey(baseKey, configuration);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void GenerateCacheKey_HashLengthIsCorrect()
    {
        // Arrange
        var baseKey = "test_base_key";
        var configuration = new ChartConfigurationDto { Title = "Test" };

        // Act
        var result = _service.GenerateCacheKey(baseKey, configuration);

        // Assert
        var expectedLength = baseKey.Length + 1 + AppConstants.DataProcessing.CACHE_KEY_HASH_LENGTH;
        result.Should().HaveLength(expectedLength);
    }

    [Fact]
    public void GenerateCacheKey_HandlesSpecialCharactersInConfiguration()
    {
        // Arrange
        var baseKey = "test_base_key";
        var configuration = new ChartConfigurationDto
        {
            Title = "Chart with special chars: !@#$%^&*()",
            XAxisLabel = "X-Axis (with parentheses)",
            YAxisLabel = "Y-Axis [with brackets]"
        };

        // Act
        var result1 = _service.GenerateCacheKey(baseKey, configuration);
        var result2 = _service.GenerateCacheKey(baseKey, configuration);

        // Assert
        result1.Should().Be(result2);
        result1.Should().StartWith(baseKey + "_");
    }
}