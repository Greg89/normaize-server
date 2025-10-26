using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.Application.Commands.Statistics;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Common.DTOs;
using Normaize.DataNormalization.Application.Queries.Statistics;
using Normaize.DataNormalization.Application.Statistics.Commands.GenerateDataSummary;
using Normaize.DataNormalization.Application.Statistics.Commands.GenerateStatisticalSummary;
using Xunit;

namespace Normaize.DataNormalization.API.Tests.Controllers;

/// <summary>
/// Unit tests for StatisticsController
/// </summary>
public class StatisticsControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<StatisticsController>> _mockLogger;
    private readonly StatisticsController _controller;

    public StatisticsControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<StatisticsController>>();
        _controller = new StatisticsController(_mockMediator.Object, _mockLogger.Object);

        // Set up HttpContext for ProblemDetails
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.HttpContext.Request.Path = "/api/v1/statistics";
    }

    [Fact]
    public async Task GenerateDataSummary_ShouldReturnOk_WhenValidDataSetId()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var expectedDto = CreateTestDataSummaryDto();

        _mockMediator.Setup(x => x.Send(It.IsAny<GenerateDataSummaryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GenerateDataSummary(dataSetId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDto);

        _mockMediator.Verify(x => x.Send(
            It.Is<GenerateDataSummaryCommand>(cmd => cmd.DataSetId == dataSetId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateDataSummary_ShouldReturnBadRequest_WhenArgumentException()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var expectedException = new ArgumentException("Invalid dataset ID");

        _mockMediator.Setup(x => x.Send(It.IsAny<GenerateDataSummaryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.GenerateDataSummary(dataSetId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<ProblemDetails>();
        
        var problemDetails = badRequestResult.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Invalid Dataset ID");
        problemDetails.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task GenerateDataSummary_ShouldReturnNotFound_WhenInvalidOperationException()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var expectedException = new InvalidOperationException("Dataset not found");

        _mockMediator.Setup(x => x.Send(It.IsAny<GenerateDataSummaryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.GenerateDataSummary(dataSetId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeOfType<ProblemDetails>();
        
        var problemDetails = notFoundResult.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Dataset Not Found");
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task GenerateDataSummary_ShouldReturnInternalServerError_WhenUnexpectedException()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var expectedException = new Exception("Unexpected error");

        _mockMediator.Setup(x => x.Send(It.IsAny<GenerateDataSummaryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _controller.GenerateDataSummary(dataSetId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<ObjectResult>();
        
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().BeOfType<ProblemDetails>();
        
        var problemDetails = objectResult.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Internal Server Error");
        problemDetails.Status.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task GenerateStatisticalSummary_ShouldReturnOk_WhenValidDataSetId()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var expectedDto = CreateTestStatisticalSummaryDto();

        _mockMediator.Setup(x => x.Send(It.IsAny<GenerateStatisticalSummaryCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GenerateStatisticalSummary(dataSetId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDto);

        _mockMediator.Verify(x => x.Send(
            It.Is<GenerateStatisticalSummaryCommand>(cmd => cmd.DataSetId == dataSetId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnOk_WhenStatisticsExist()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var expectedDto = CreateTestStatisticsDto();

        _mockMediator.Setup(x => x.Send(It.IsAny<GetStatisticsByDataSetIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetStatistics(dataSetId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDto);

        _mockMediator.Verify(x => x.Send(
            It.Is<GetStatisticsByDataSetIdQuery>(query => query.DataSetId == dataSetId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStatistics_ShouldReturnNotFound_WhenStatisticsDoNotExist()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();

        _mockMediator.Setup(x => x.Send(It.IsAny<GetStatisticsByDataSetIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StatisticsDto?)null);

        // Act
        var result = await _controller.GetStatistics(dataSetId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeOfType<ProblemDetails>();
        
        var problemDetails = notFoundResult.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Statistics Not Found");
        problemDetails.Status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task DeleteStatistics_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();

        _mockMediator.Setup(x => x.Send(It.IsAny<DeleteStatisticsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteStatistics(dataSetId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _mockMediator.Verify(x => x.Send(
            It.Is<DeleteStatisticsCommand>(cmd => cmd.DataSetId == dataSetId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCorrelationMatrix_ShouldReturnOk_WhenValidDataSetId()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var expectedDto = CreateTestCorrelationMatrixDto();

        _mockMediator.Setup(x => x.Send(It.IsAny<GetCorrelationMatrixQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.GetCorrelationMatrix(dataSetId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDto);

        _mockMediator.Verify(x => x.Send(
            It.Is<GetCorrelationMatrixQuery>(query => query.DataSetId == dataSetId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateConfiguration_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var request = new ConfigurationValidationRequest
        {
            DataSetId = Guid.NewGuid(),
            NumericColumns = new List<string> { "age", "salary" },
            CategoryColumns = new List<string> { "department" },
            IgnoreColumns = new List<string> { "id" }
        };

        var expectedDto = CreateTestValidationResultDto();

        _mockMediator.Setup(x => x.Send(It.IsAny<ValidateStatisticalConfigurationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.ValidateConfiguration(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<OkObjectResult>();
        
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDto);

        _mockMediator.Verify(x => x.Send(
            It.Is<ValidateStatisticalConfigurationCommand>(cmd => 
                cmd.DataSetId == request.DataSetId &&
                cmd.NumericColumns.SequenceEqual(request.NumericColumns) &&
                cmd.CategoryColumns.SequenceEqual(request.CategoryColumns) &&
                cmd.IgnoreColumns.SequenceEqual(request.IgnoreColumns)), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static DataSummaryDto CreateTestDataSummaryDto()
    {
        return new DataSummaryDto
        {
            DataSetId = 1,
            TotalRows = 100,
            TotalColumns = 3,
            MissingValues = 5,
            DuplicateRows = 2,
            ColumnSummaries = new Dictionary<string, BasicColumnSummaryDto>(),
            GeneratedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromSeconds(5),
            QualityScore = new DataQualityScoreDto()
        };
    }

    private static StatisticalSummaryDto CreateTestStatisticalSummaryDto()
    {
        return new StatisticalSummaryDto
        {
            DataSetId = 1,
            ColumnStatistics = new Dictionary<string, ColumnStatisticsDto>(),
            CorrelationMatrix = new Dictionary<string, double>(),
            OutlierColumns = new List<string>(),
            OutlierIndices = new List<int>(),
            GeneratedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromSeconds(3),
            Insights = new StatisticalInsightsDto()
        };
    }

    private static StatisticsDto CreateTestStatisticsDto()
    {
        return new StatisticsDto
        {
            Id = 1,
            DataSetId = Guid.NewGuid(),
            DataSetName = "Test Dataset",
            TotalRows = 100,
            TotalColumns = 3,
            MissingValues = 5,
            DuplicateRows = 2,
            CalculatedAt = DateTime.UtcNow,
            ProcessingTime = TimeSpan.FromSeconds(5),
            ColumnSummaries = new Dictionary<string, DetailedColumnSummaryDto>(),
            ColumnStatistics = new Dictionary<string, StatisticalMeasureDto>()
        };
    }

    private static CorrelationMatrixDto CreateTestCorrelationMatrixDto()
    {
        return new CorrelationMatrixDto
        {
            DataSetId = Guid.NewGuid(),
            DataSetName = "Test Dataset",
            ColumnNames = new List<string> { "age", "salary" },
            Matrix = new List<List<double>>
            {
                new() { 1.0, 0.8 },
                new() { 0.8, 1.0 }
            },
            GeneratedAt = DateTime.UtcNow,
            ObservationCount = 100,
            StrongCorrelations = new List<CorrelationPairDto>()
        };
    }

    private static ValidationResultDto CreateTestValidationResultDto()
    {
        return new ValidationResultDto
        {
            IsValid = true,
            Errors = new List<string>(),
            Warnings = new List<string>(),
            ValidatedColumns = new Dictionary<string, string>
            {
                ["age"] = "Numeric",
                ["salary"] = "Numeric",
                ["department"] = "Category"
            },
            Recommendations = new List<string>()
        };
    }
}