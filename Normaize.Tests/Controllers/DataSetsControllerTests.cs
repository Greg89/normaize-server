using Microsoft.AspNetCore.Mvc;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Normaize.Core.Models;

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

    [Fact]
    public async Task UploadDataSet_WithValidFile_ShouldReturnOkResult()
    {
        // Arrange
        var uploadDto = new FileUploadDto
        {
            Name = "Test Dataset",
            Description = "Test Description",
            File = CreateMockFile("test.csv", "text/csv", "test,data")
        };

        var expectedResponse = new DataSetUploadResponse
        {
            Success = true,
            Message = "Dataset uploaded successfully",
            DataSetId = 1
        };

        _mockDataProcessingService
            .Setup(x => x.UploadDataSetAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CreateDataSetDto>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.UploadDataSet(uploadDto);

        // Assert
        result.Should().BeOfType<ActionResult<DataSetUploadResponse>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedResponse = okResult.Value.Should().BeOfType<DataSetUploadResponse>().Subject!;
        returnedResponse.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task UploadDataSet_WithNullFile_ShouldReturnBadRequest()
    {
        // Arrange
        var uploadDto = new FileUploadDto
        {
            Name = "Test Dataset",
            Description = "Test Description",
            File = null
        };

        // Act
        var result = await _controller.UploadDataSet(uploadDto);

        // Assert
        result.Should().BeOfType<ActionResult<DataSetUploadResponse>>();
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadDataSet_WithEmptyFile_ShouldReturnBadRequest()
    {
        // Arrange
        var uploadDto = new FileUploadDto
        {
            Name = "Test Dataset",
            Description = "Test Description",
            File = CreateMockFile("test.csv", "text/csv", "")
        };

        // Act
        var result = await _controller.UploadDataSet(uploadDto);

        // Assert
        result.Should().BeOfType<ActionResult<DataSetUploadResponse>>();
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadDataSet_WhenServiceReturnsFailure_ShouldReturnBadRequest()
    {
        // Arrange
        var uploadDto = new FileUploadDto
        {
            Name = "Test Dataset",
            Description = "Test Description",
            File = CreateMockFile("test.csv", "text/csv", "test,data")
        };

        var failureResponse = new DataSetUploadResponse
        {
            Success = false,
            Message = "Upload failed"
        };

        _mockDataProcessingService
            .Setup(x => x.UploadDataSetAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CreateDataSetDto>()))
            .ReturnsAsync(failureResponse);

        // Act
        var result = await _controller.UploadDataSet(uploadDto);

        // Assert
        result.Should().BeOfType<ActionResult<DataSetUploadResponse>>();
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadDataSet_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var uploadDto = new FileUploadDto
        {
            Name = "Test Dataset",
            Description = "Test Description",
            File = CreateMockFile("test.csv", "text/csv", "test,data")
        };

        var exception = new InvalidOperationException("Upload failed");
        _mockDataProcessingService
            .Setup(x => x.UploadDataSetAsync(It.IsAny<FileUploadRequest>(), It.IsAny<CreateDataSetDto>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.UploadDataSet(uploadDto);

        // Assert
        result.Should().BeOfType<ActionResult<DataSetUploadResponse>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, "UploadDataSet"),
            Times.Once);
    }

    [Fact]
    public async Task GetDeletedDataSets_WithValidData_ShouldReturnOkResult()
    {
        // Arrange
        var expectedDataSets = new List<DataSetDto>
        {
            new() { Id = 1, Name = "Deleted Dataset 1", Description = "Deleted Description 1" },
            new() { Id = 2, Name = "Deleted Dataset 2", Description = "Deleted Description 2" }
        };

        _mockDataProcessingService
            .Setup(x => x.GetDeletedDataSetsAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.GetDeletedDataSets();

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedDataSets = okResult.Value.Should().BeOfType<List<DataSetDto>>().Subject!;
        returnedDataSets.Should().HaveCount(2);
        returnedDataSets.Should().BeEquivalentTo(expectedDataSets);
    }

    [Fact]
    public async Task GetDeletedDataSets_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var exception = new InvalidOperationException("Database connection failed");
        _mockDataProcessingService
            .Setup(x => x.GetDeletedDataSetsAsync(It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDeletedDataSets();

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, "GetDeletedDataSets"),
            Times.Once);
    }

    [Fact]
    public async Task SearchDataSets_WithValidQuery_ShouldReturnOkResult()
    {
        // Arrange
        var searchQuery = "test";
        var expectedDataSets = new List<DataSetDto>
        {
            new() { Id = 1, Name = "Test Dataset 1", Description = "Test Description 1" },
            new() { Id = 2, Name = "Test Dataset 2", Description = "Test Description 2" }
        };

        _mockDataProcessingService
            .Setup(x => x.SearchDataSetsAsync(searchQuery, It.IsAny<string>()))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.SearchDataSets(searchQuery);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedDataSets = okResult.Value.Should().BeOfType<List<DataSetDto>>().Subject!;
        returnedDataSets.Should().HaveCount(2);
        returnedDataSets.Should().BeEquivalentTo(expectedDataSets);
    }

    [Fact]
    public async Task SearchDataSets_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var searchQuery = "test";
        var exception = new InvalidOperationException("Search failed");
        _mockDataProcessingService
            .Setup(x => x.SearchDataSetsAsync(searchQuery, It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.SearchDataSets(searchQuery);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, $"SearchDataSets({searchQuery})"),
            Times.Once);
    }

    [Fact]
    public async Task GetDataSetsByFileType_WithValidFileType_ShouldReturnOkResult()
    {
        // Arrange
        var fileType = "csv";
        var expectedDataSets = new List<DataSetDto>
        {
            new() { Id = 1, Name = "CSV Dataset 1", Description = "CSV Description 1" },
            new() { Id = 2, Name = "CSV Dataset 2", Description = "CSV Description 2" }
        };

        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByFileTypeAsync(fileType, It.IsAny<string>()))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.GetDataSetsByFileType(fileType);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedDataSets = okResult.Value.Should().BeOfType<List<DataSetDto>>().Subject!;
        returnedDataSets.Should().HaveCount(2);
        returnedDataSets.Should().BeEquivalentTo(expectedDataSets);
    }

    [Fact]
    public async Task GetDataSetsByFileType_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var fileType = "csv";
        var exception = new InvalidOperationException("File type filter failed");
        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByFileTypeAsync(fileType, It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSetsByFileType(fileType);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, $"GetDataSetsByFileType({fileType})"),
            Times.Once);
    }

    [Fact]
    public async Task GetDataSetsByDateRange_WithValidDates_ShouldReturnOkResult()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;
        var expectedDataSets = new List<DataSetDto>
        {
            new() { Id = 1, Name = "Recent Dataset 1", Description = "Recent Description 1" },
            new() { Id = 2, Name = "Recent Dataset 2", Description = "Recent Description 2" }
        };

        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByDateRangeAsync(startDate, endDate, It.IsAny<string>()))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.GetDataSetsByDateRange(startDate, endDate);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedDataSets = okResult.Value.Should().BeOfType<List<DataSetDto>>().Subject!;
        returnedDataSets.Should().HaveCount(2);
        returnedDataSets.Should().BeEquivalentTo(expectedDataSets);
    }

    [Fact]
    public async Task GetDataSetsByDateRange_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var startDate = DateTime.Now.AddDays(-7);
        var endDate = DateTime.Now;
        var exception = new InvalidOperationException("Date range filter failed");
        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByDateRangeAsync(startDate, endDate, It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSetsByDateRange(startDate, endDate);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<DataSetDto>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, $"GetDataSetsByDateRange({startDate}, {endDate})"),
            Times.Once);
    }

    [Fact]
    public async Task GetDataSetStatistics_WithValidData_ShouldReturnOkResult()
    {
        // Arrange
        var expectedStatistics = new DataSetStatisticsDto
        {
            TotalCount = 10,
            TotalSize = 1000,
            RecentlyModified = new List<DataSetDto>
            {
                new() { Id = 1, Name = "Recent 1", Description = "Desc 1" },
                new() { Id = 2, Name = "Recent 2", Description = "Desc 2" }
            }
        };

        _mockDataProcessingService
            .Setup(x => x.GetDataSetStatisticsAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedStatistics);

        // Act
        var result = await _controller.GetDataSetStatistics();

        // Assert
        result.Should().BeOfType<ActionResult<DataSetStatisticsDto>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var returnedStatistics = okResult.Value.Should().BeOfType<DataSetStatisticsDto>().Subject!;
        returnedStatistics.Should().BeEquivalentTo(expectedStatistics);
    }

    [Fact]
    public async Task GetDataSetStatistics_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var exception = new InvalidOperationException("Statistics calculation failed");
        _mockDataProcessingService
            .Setup(x => x.GetDataSetStatisticsAsync(It.IsAny<string>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSetStatistics();

        // Assert
        result.Should().BeOfType<ActionResult<DataSetStatisticsDto>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(500);
        
        _mockLoggingService.Verify(
            x => x.LogException(exception, "GetDataSetStatistics"),
            Times.Once);
    }

    private static IFormFile CreateMockFile(string fileName, string contentType, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
} 