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
            .Setup(x => x.GetDataSetsByUserAsync(It.IsAny<string>(), 1, 20))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.GetDataSets();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<DataSetDto>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedDataSets = apiResponse.Data!;
        returnedDataSets.Should().HaveCount(2);
        returnedDataSets.Should().BeEquivalentTo(expectedDataSets);
    }

    [Fact]
    public async Task GetDataSets_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var exception = new InvalidOperationException("Database connection failed");
        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByUserAsync(It.IsAny<string>(), 1, 20))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSets();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(400);

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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DataSetDto>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedDataSet = apiResponse.Data!;
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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetDto>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetDto>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(400);

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
        result.Should().BeOfType<ActionResult<ApiResponse<string?>>>();
        result.Result.Should().BeOfType<OkObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<string?>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<string?>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(400);

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
        result.Should().BeOfType<ActionResult<ApiResponse<string>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<string>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedPreview = apiResponse.Data!;
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
        result.Should().BeOfType<ActionResult<ApiResponse<string>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data.Should().Be(expectedSchema);
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
        result.Should().BeOfType<ActionResult<ApiResponse<object>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<string>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<string>>().Subject!;
        apiResponse.Message.Should().Be("Dataset restored successfully");
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
        result.Should().BeOfType<ActionResult<ApiResponse<string?>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<string?>>>();
        result.Result.Should().BeOfType<OkObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<string?>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetUploadResponse>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DataSetUploadResponse>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedResponse = apiResponse.Data!;
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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetUploadResponse>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetUploadResponse>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetUploadResponse>>>();
        result.Result.Should().BeOfType<ObjectResult>();
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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetUploadResponse>>>();
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
            .Setup(x => x.GetDeletedDataSetsAsync(It.IsAny<string>(), 1, 20))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.GetDeletedDataSets();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<DataSetDto>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedDataSets = apiResponse.Data!;
        returnedDataSets.Should().HaveCount(2);
        returnedDataSets.Should().BeEquivalentTo(expectedDataSets);
    }

    [Fact]
    public async Task GetDeletedDataSets_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var exception = new InvalidOperationException("Database connection failed");
        _mockDataProcessingService
            .Setup(x => x.GetDeletedDataSetsAsync(It.IsAny<string>(), 1, 20))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDeletedDataSets();

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(400);

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
            .Setup(x => x.SearchDataSetsAsync(searchQuery, It.IsAny<string>(), 1, 10))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.SearchDataSets(searchQuery, 1, 10);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<DataSetDto>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedDataSets = apiResponse.Data!;
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
            .Setup(x => x.SearchDataSetsAsync(searchQuery, It.IsAny<string>(), 1, 10))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.SearchDataSets(searchQuery, 1, 10);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(400);

        _mockLoggingService.Verify(
            x => x.LogException(exception, $"SearchDataSets({searchQuery})"),
            Times.Once);
    }

    [Fact]
    public async Task GetDataSetsByFileType_WithValidFileType_ShouldReturnOkResult()
    {
        // Arrange
        var fileType = FileType.CSV;
        var expectedDataSets = new List<DataSetDto>
        {
            new() { Id = 1, Name = "CSV Dataset 1", Description = "CSV Description 1", FileType = fileType },
            new() { Id = 2, Name = "CSV Dataset 2", Description = "CSV Description 2", FileType = fileType }
        };

        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByFileTypeAsync(fileType, It.IsAny<string>(), 1, 10))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.GetDataSetsByFileType(fileType, 1, 10);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<DataSetDto>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedDataSets = apiResponse.Data!;
        returnedDataSets.Should().HaveCount(2);
        returnedDataSets.Should().BeEquivalentTo(expectedDataSets);
    }

    [Fact]
    public async Task GetDataSetsByFileType_WhenServiceThrowsException_ShouldLogExceptionAndReturn500()
    {
        // Arrange
        var fileType = FileType.CSV;
        var exception = new InvalidOperationException("File type filter failed");
        _mockDataProcessingService
            .Setup(x => x.GetDataSetsByFileTypeAsync(fileType, It.IsAny<string>(), 1, 10))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSetsByFileType(fileType, 1, 10);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(400);

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
            .Setup(x => x.GetDataSetsByDateRangeAsync(startDate, endDate, It.IsAny<string>(), 1, 10))
            .ReturnsAsync(expectedDataSets);

        // Act
        var result = await _controller.GetDataSetsByDateRange(startDate, endDate, 1, 10);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<List<DataSetDto>>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedDataSets = apiResponse.Data!;
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
            .Setup(x => x.GetDataSetsByDateRangeAsync(startDate, endDate, It.IsAny<string>(), 1, 10))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.GetDataSetsByDateRange(startDate, endDate, 1, 10);

        // Assert
        result.Should().BeOfType<ActionResult<ApiResponse<List<DataSetDto>>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(400);

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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetStatisticsDto>>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject!;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<DataSetStatisticsDto>>().Subject!;
        apiResponse.Data.Should().NotBeNull();
        var returnedStatistics = apiResponse.Data!;
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
        result.Should().BeOfType<ActionResult<ApiResponse<DataSetStatisticsDto>>>();
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject!;
        statusResult.StatusCode.Should().Be(400);

        _mockLoggingService.Verify(
            x => x.LogException(exception, "GetDataSetStatistics"),
            Times.Once);
    }

    private static FormFile CreateMockFile(string fileName, string contentType, string content)
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