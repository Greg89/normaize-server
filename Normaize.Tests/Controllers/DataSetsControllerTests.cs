using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.API.Services;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Normaize.Tests.Controllers;

public class DataSetsControllerTests
{
    private readonly Mock<IDataProcessingService> _mockDataProcessingService;
    private readonly Mock<IStructuredLoggingService> _mockLoggingService;
    private readonly DataSetsController _controller;

    public DataSetsControllerTests()
    {
        _mockDataProcessingService = new Mock<IDataProcessingService>();
        _mockLoggingService = new Mock<IStructuredLoggingService>();
        _controller = new DataSetsController(_mockDataProcessingService.Object, _mockLoggingService.Object);
        // Set up mock user for controller context
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, "test-user-id")
                }, "mock"))
            }
        };
    }

    [Fact]
    public async Task GetDataSets_WithValidData_ShouldReturnOkResult()
    {
        // Arrange
        var expectedDataSets = new List<DataSetDto>
        {
            new() { Id = 1, Name = "Test Dataset 1", Description = "Test Description 1" },
            new() { Id = 2, Name = "Test Dataset 2", Description = "Test Description 2" }
        };

        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByUserAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.GetDataSets();

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedDataSets = okResult.Value.Should().BeOfType<List<DataSetDto>>().Subject!;
        returnedDataSets.Should().HaveCount(2);
        returnedDataSets.Should().BeEquivalentTo(expectedDataSets);
    }

    [Fact]
    public async Task GetDataSets_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var exception = new InvalidOperationException("Database connection failed");
        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByUserAsync(It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSets();

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, "GetDataSets"),
            Times.Once);
    }

    [Fact]
    public async Task GetDataSet_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var datasetId = 1;
        var expectedDataSet = new DataSetDto 
        { 
            Id = datasetId, 
            Name = "Test Dataset", 
            Description = "Test Description" 
        };

        _mockDataProcessingService
            .Setup(x => x.GetDataSetAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync(expectedDataSet);

        // Act
        var result = await _controller.GetDataSet(datasetId);

        // Assert
        result.Should().BeOfType<ActionResult<DataSetDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedDataSet = okResult.Value.Should().BeOfType<DataSetDto>().Subject!;
        returnedDataSet.Should().BeEquivalentTo(expectedDataSet);
    }

    [Fact]
    public async Task GetDataSet_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var datasetId = 999;
        _mockDataProcessingService
            .Setup(x => x.GetDataSetAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync((DataSetDto?)null);

        // Act
        var result = await _controller.GetDataSet(datasetId);

        // Assert
        result.Should().BeOfType<ActionResult<DataSetDto>>();
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetDataSet_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var datasetId = 1;
        var exception = new InvalidOperationException("Database error");
        _mockDataProcessingService
            .Setup(x => x.GetDataSetAsync(datasetId, It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSet(datasetId);

        // Assert
        result.Should().BeOfType<ActionResult<DataSetDto>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, $"GetDataSet({datasetId})"),
            Times.Once);
    }

    [Fact]
    public async Task DeleteDataSet_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var datasetId = 1;
        _mockDataProcessingService
            .Setup(x => x.DeleteDataSetAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteDataSet(datasetId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteDataSet_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var datasetId = 999;
        _mockDataProcessingService
            .Setup(x => x.DeleteDataSetAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteDataSet(datasetId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteDataSet_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var datasetId = 1;
        var exception = new InvalidOperationException("Delete failed");
        _mockDataProcessingService
            .Setup(x => x.DeleteDataSetAsync(datasetId, It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.DeleteDataSet(datasetId);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, $"DeleteDataSet({datasetId})"),
            Times.Once);
    }

    [Fact]
    public async Task GetDataSetPreview_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var datasetId = 1;
        var expectedPreview = "Sample preview data";
        _mockDataProcessingService
            .Setup(x => x.GetDataSetPreviewAsync(datasetId, It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(expectedPreview);

        // Act
        var result = await _controller.GetDataSetPreview(datasetId);

        // Assert
        result.Should().BeOfType<ActionResult<string>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedPreview = okResult.Value.Should().BeOfType<string>().Subject!;
        returnedPreview.Should().Be(expectedPreview);
    }

    [Fact]
    public async Task GetDataSetPreview_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var datasetId = 999;
        _mockDataProcessingService
            .Setup(x => x.GetDataSetPreviewAsync(datasetId, It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _controller.GetDataSetPreview(datasetId);

        // Assert
        result.Should().BeOfType<ActionResult<string>>();
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetDataSetSchema_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var datasetId = 1;
        var expectedSchema = new { columns = 5, rows = 100 };
        _mockDataProcessingService
            .Setup(x => x.GetDataSetSchemaAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync(expectedSchema);

        // Act
        var result = await _controller.GetDataSetSchema(datasetId);

        // Assert
        result.Should().BeOfType<ActionResult<object>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        okResult.Value.Should().Be(expectedSchema);
    }

    [Fact]
    public async Task GetDataSetSchema_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var datasetId = 999;
        _mockDataProcessingService
            .Setup(x => x.GetDataSetSchemaAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync((object?)null);

        // Act
        var result = await _controller.GetDataSetSchema(datasetId);

        // Assert
        result.Should().BeOfType<ActionResult<object>>();
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RestoreDataSet_WithValidId_ShouldReturnOkResult()
    {
        // Arrange
        var datasetId = 1;
        _mockDataProcessingService
            .Setup(x => x.RestoreDataSetAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RestoreDataSet(datasetId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { message = "Dataset restored successfully" });
    }

    [Fact]
    public async Task RestoreDataSet_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var datasetId = 999;
        _mockDataProcessingService
            .Setup(x => x.RestoreDataSetAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RestoreDataSet(datasetId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task HardDeleteDataSet_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var datasetId = 1;
        _mockDataProcessingService
            .Setup(x => x.HardDeleteDataSetAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.HardDeleteDataSet(datasetId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task HardDeleteDataSet_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var datasetId = 999;
        _mockDataProcessingService
            .Setup(x => x.HardDeleteDataSetAsync(datasetId, It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.HardDeleteDataSet(datasetId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
} 