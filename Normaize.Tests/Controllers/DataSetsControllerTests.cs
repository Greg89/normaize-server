using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.API.Services;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using FluentAssertions;
using Xunit;

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
            .Setup(x => x.GetAllDataSetsAsync())
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
            .Setup(x => x.GetAllDataSetsAsync())
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
            .Setup(x => x.GetDataSetAsync(datasetId))
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
            .Setup(x => x.GetDataSetAsync(datasetId))
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
            .Setup(x => x.GetDataSetAsync(datasetId))
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
            .Setup(x => x.DeleteDataSetAsync(datasetId))
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
            .Setup(x => x.DeleteDataSetAsync(datasetId))
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
            .Setup(x => x.DeleteDataSetAsync(datasetId))
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
} 