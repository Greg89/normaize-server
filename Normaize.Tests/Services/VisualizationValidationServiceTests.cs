using Xunit;
using Moq;
using Normaize.Core.Interfaces;
using Normaize.Core.DTOs;
using Normaize.Core.Services.Visualization;
using Normaize.Core.Constants;
using FluentAssertions;

namespace Normaize.Tests.Services;

public class VisualizationValidationServiceTests
{
    private readonly Mock<IChartGenerationService> _mockChartGenerationService;
    private readonly VisualizationValidationService _service;

    public VisualizationValidationServiceTests()
    {
        _mockChartGenerationService = new Mock<IChartGenerationService>();
        _service = new VisualizationValidationService(_mockChartGenerationService.Object);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenChartGenerationServiceIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new VisualizationValidationService(null!));
        exception.ParamName.Should().Be("chartGenerationService");
    }

    [Fact]
    public void ValidateGenerateChartInputs_ThrowsArgumentException_WhenDataSetIdIsZero()
    {
        // Arrange
        var dataSetId = 0;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();
        var userId = "user1";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateGenerateChartInputs(dataSetId, chartType, config, userId));
        exception.ParamName.Should().Be("dataSetId");
        exception.Message.Should().Contain(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE);
    }

    [Fact]
    public void ValidateGenerateChartInputs_ThrowsArgumentException_WhenDataSetIdIsNegative()
    {
        // Arrange
        var dataSetId = -1;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();
        var userId = "user1";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateGenerateChartInputs(dataSetId, chartType, config, userId));
        exception.ParamName.Should().Be("dataSetId");
        exception.Message.Should().Contain(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateGenerateChartInputs_ThrowsArgumentException_WhenUserIdIsInvalid(string? userId)
    {
        // Arrange
        var dataSetId = 1;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateGenerateChartInputs(dataSetId, chartType, config, userId));
        exception.ParamName.Should().Be("userId");
        exception.Message.Should().Contain(AppConstants.VisualizationMessages.INVALID_USER_ID);
    }

    [Fact]
    public void ValidateGenerateChartInputs_CallsChartGenerationService_WhenInputsAreValid()
    {
        // Arrange
        var dataSetId = 1;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();
        var userId = "user1";

        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(chartType, config)).Returns(true);

        // Act
        _service.ValidateGenerateChartInputs(dataSetId, chartType, config, userId);

        // Assert
        _mockChartGenerationService.Verify(x => x.ValidateChartConfiguration(chartType, config), Times.Once);
    }

    [Fact]
    public void ValidateGenerateChartInputs_ThrowsArgumentException_WhenChartGenerationServiceThrows()
    {
        // Arrange
        var dataSetId = 1;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto { MaxDataPoints = 0 };
        var userId = "user1";

        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(chartType, config))
            .Throws(new ArgumentException("MaxDataPoints must be greater than 0"));

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateGenerateChartInputs(dataSetId, chartType, config, userId));
        exception.Message.Should().Contain("MaxDataPoints must be greater than 0");
    }

    [Fact]
    public void ValidateComparisonChartInputs_ThrowsArgumentException_WhenDataSetId1IsZero()
    {
        // Arrange
        var dataSetId1 = 0;
        var dataSetId2 = 2;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();
        var userId = "user1";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateComparisonChartInputs(dataSetId1, dataSetId2, chartType, config, userId));
        exception.ParamName.Should().Be("dataSetId1");
        exception.Message.Should().Contain(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE);
    }

    [Fact]
    public void ValidateComparisonChartInputs_ThrowsArgumentException_WhenDataSetId2IsZero()
    {
        // Arrange
        var dataSetId1 = 1;
        var dataSetId2 = 0;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();
        var userId = "user1";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateComparisonChartInputs(dataSetId1, dataSetId2, chartType, config, userId));
        exception.ParamName.Should().Be("dataSetId2");
        exception.Message.Should().Contain(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE);
    }

    [Fact]
    public void ValidateComparisonChartInputs_ThrowsArgumentException_WhenDataSetIdsAreSame()
    {
        // Arrange
        var dataSetId = 1;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();
        var userId = "user1";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateComparisonChartInputs(dataSetId, dataSetId, chartType, config, userId));
        exception.ParamName.Should().Be("dataSetId2");
        exception.Message.Should().Contain("Dataset IDs must be different for comparison");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateComparisonChartInputs_ThrowsArgumentException_WhenUserIdIsInvalid(string? userId)
    {
        // Arrange
        var dataSetId1 = 1;
        var dataSetId2 = 2;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateComparisonChartInputs(dataSetId1, dataSetId2, chartType, config, userId));
        exception.ParamName.Should().Be("userId");
        exception.Message.Should().Contain(AppConstants.VisualizationMessages.INVALID_USER_ID);
    }

    [Fact]
    public void ValidateComparisonChartInputs_CallsChartGenerationService_WhenInputsAreValid()
    {
        // Arrange
        var dataSetId1 = 1;
        var dataSetId2 = 2;
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto();
        var userId = "user1";

        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(chartType, config)).Returns(true);

        // Act
        _service.ValidateComparisonChartInputs(dataSetId1, dataSetId2, chartType, config, userId);

        // Assert
        _mockChartGenerationService.Verify(x => x.ValidateChartConfiguration(chartType, config), Times.Once);
    }

    [Fact]
    public void ValidateDataSummaryInputs_ThrowsArgumentException_WhenDataSetIdIsZero()
    {
        // Arrange
        var dataSetId = 0;
        var userId = "user1";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateDataSummaryInputs(dataSetId, userId));
        exception.ParamName.Should().Be("dataSetId");
        exception.Message.Should().Contain(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE);
    }

    [Fact]
    public void ValidateDataSummaryInputs_ThrowsArgumentException_WhenDataSetIdIsNegative()
    {
        // Arrange
        var dataSetId = -1;
        var userId = "user1";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateDataSummaryInputs(dataSetId, userId));
        exception.ParamName.Should().Be("dataSetId");
        exception.Message.Should().Contain(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateDataSummaryInputs_ThrowsArgumentException_WhenUserIdIsInvalid(string? userId)
    {
        // Arrange
        var dataSetId = 1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateDataSummaryInputs(dataSetId, userId));
        exception.ParamName.Should().Be("userId");
        exception.Message.Should().Contain(AppConstants.VisualizationMessages.INVALID_USER_ID);
    }

    [Fact]
    public void ValidateDataSummaryInputs_DoesNotThrow_WhenInputsAreValid()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user1";

        // Act & Assert
        var exception = Record.Exception(() => _service.ValidateDataSummaryInputs(dataSetId, userId));
        exception.Should().BeNull();
    }

    [Fact]
    public void ValidateStatisticalSummaryInputs_ThrowsArgumentException_WhenDataSetIdIsZero()
    {
        // Arrange
        var dataSetId = 0;
        var userId = "user1";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateStatisticalSummaryInputs(dataSetId, userId));
        exception.ParamName.Should().Be("dataSetId");
        exception.Message.Should().Contain(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateStatisticalSummaryInputs_ThrowsArgumentException_WhenUserIdIsInvalid(string? userId)
    {
        // Arrange
        var dataSetId = 1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateStatisticalSummaryInputs(dataSetId, userId));
        exception.ParamName.Should().Be("userId");
        exception.Message.Should().Contain(AppConstants.VisualizationMessages.INVALID_USER_ID);
    }

    [Fact]
    public void ValidateStatisticalSummaryInputs_DoesNotThrow_WhenInputsAreValid()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user1";

        // Act & Assert
        var exception = Record.Exception(() => _service.ValidateStatisticalSummaryInputs(dataSetId, userId));
        exception.Should().BeNull();
    }

    [Fact]
    public void ValidateChartConfiguration_ReturnsTrue_WhenChartGenerationServiceReturnsTrue()
    {
        // Arrange
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto { MaxDataPoints = 5 };

        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(chartType, config)).Returns(true);

        // Act
        var result = _service.ValidateChartConfiguration(chartType, config);

        // Assert
        result.Should().BeTrue();
        _mockChartGenerationService.Verify(x => x.ValidateChartConfiguration(chartType, config), Times.Once);
    }

    [Fact]
    public void ValidateChartConfiguration_ReturnsFalse_WhenChartGenerationServiceReturnsFalse()
    {
        // Arrange
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto { MaxDataPoints = 0 };

        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(chartType, config)).Returns(false);

        // Act
        var result = _service.ValidateChartConfiguration(chartType, config);

        // Assert
        result.Should().BeFalse();
        _mockChartGenerationService.Verify(x => x.ValidateChartConfiguration(chartType, config), Times.Once);
    }

    [Fact]
    public void ValidateChartConfiguration_ThrowsException_WhenChartGenerationServiceThrows()
    {
        // Arrange
        var chartType = ChartType.Bar;
        var config = new ChartConfigurationDto { MaxDataPoints = 0 };

        _mockChartGenerationService.Setup(x => x.ValidateChartConfiguration(chartType, config))
            .Throws(new ArgumentException("MaxDataPoints must be greater than 0"));

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _service.ValidateChartConfiguration(chartType, config));
        exception.Message.Should().Contain("MaxDataPoints must be greater than 0");
    }
}