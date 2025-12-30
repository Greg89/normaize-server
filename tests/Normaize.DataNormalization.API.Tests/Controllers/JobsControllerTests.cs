using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.API.Controllers;
using Normaize.DataNormalization.API.DTOs;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Application.DTOs;
using System.Security.Claims;
using Xunit;

namespace Normaize.DataNormalization.API.Tests.Controllers;

/// <summary>
/// Unit tests for JobsController
/// </summary>
public class JobsControllerTests
{
    private readonly Mock<IQueryHandler<GetJobStatusQuery, JobStatusDto?>> _getJobStatusHandlerMock;
    private readonly Mock<ILogger<JobsController>> _loggerMock;
    private readonly JobsController _controller;
    private const string TestUserId = "test-user-id";
    private readonly Guid TestJobId = Guid.NewGuid();
    private readonly Guid TestDataSetId = Guid.NewGuid();

    public JobsControllerTests()
    {
        _getJobStatusHandlerMock = new Mock<IQueryHandler<GetJobStatusQuery, JobStatusDto?>>();
        _loggerMock = new Mock<ILogger<JobsController>>();
        
        _controller = new JobsController(
            _getJobStatusHandlerMock.Object,
            _loggerMock.Object);

        // Set up HttpContext with authenticated user
        SetupAuthenticatedUser(TestUserId);
    }

    [Fact]
    public async Task GetJobStatus_WithValidGuidString_ShouldReturnOk()
    {
        // Arrange
        var jobIdString = TestJobId.ToString();
        var expectedJobStatus = CreateTestJobStatusDto();

        _getJobStatusHandlerMock.Setup(x => x.HandleAsync(
            It.Is<GetJobStatusQuery>(q => q.JobId == TestJobId)))
            .ReturnsAsync(expectedJobStatus);

        // Act
        var result = await _controller.GetJobStatus(jobIdString);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<JobStatusResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.JobId.Should().Be(TestJobId);
        apiResponse.Data.DataSetId.Should().Be(TestDataSetId);
        apiResponse.Data.Status.Should().Be("Completed");
        apiResponse.Data.ProgressPercentage.Should().Be(100);

        _getJobStatusHandlerMock.Verify(x => x.HandleAsync(
            It.Is<GetJobStatusQuery>(q => q.JobId == TestJobId)), Times.Once);
    }

    [Fact]
    public async Task GetJobStatus_WithInvalidGuidString_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidJobId = "not-a-valid-guid";

        // Act
        var result = await _controller.GetJobStatus(invalidJobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(400);
        
        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<object?>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.ErrorCode.Should().Be("INVALID_JOB_ID");

        _getJobStatusHandlerMock.Verify(x => x.HandleAsync(
            It.IsAny<GetJobStatusQuery>()), Times.Never);
    }

    [Fact]
    public async Task GetJobStatus_WithNonExistentJob_ShouldReturnNotFound()
    {
        // Arrange
        var jobIdString = TestJobId.ToString();

        _getJobStatusHandlerMock.Setup(x => x.HandleAsync(
            It.Is<GetJobStatusQuery>(q => q.JobId == TestJobId)))
            .ReturnsAsync((JobStatusDto?)null);

        // Act
        var result = await _controller.GetJobStatus(jobIdString);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(404);
        
        var apiResponse = objectResult.Value.Should().BeOfType<ApiResponse<object?>>().Subject;
        apiResponse.Success.Should().BeFalse();
        apiResponse.ErrorCode.Should().Be("JOB_NOT_FOUND");

        _getJobStatusHandlerMock.Verify(x => x.HandleAsync(
            It.Is<GetJobStatusQuery>(q => q.JobId == TestJobId)), Times.Once);
    }

    [Fact]
    public async Task GetJobStatus_WithQueuedJob_ShouldReturnCorrectStatus()
    {
        // Arrange
        var jobIdString = TestJobId.ToString();
        var jobStatus = CreateQueuedJobStatusDto();

        _getJobStatusHandlerMock.Setup(x => x.HandleAsync(
            It.Is<GetJobStatusQuery>(q => q.JobId == TestJobId)))
            .ReturnsAsync(jobStatus);

        // Act
        var result = await _controller.GetJobStatus(jobIdString);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<JobStatusResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Status.Should().Be("Queued");
        apiResponse.Data.ProgressPercentage.Should().Be(0);
        apiResponse.Data.StartedAt.Should().BeNull();
        apiResponse.Data.CompletedAt.Should().BeNull();
        apiResponse.Data.Results.Should().BeNull(); // Results only available for completed jobs
    }

    [Fact]
    public async Task GetJobStatus_WithProcessingJob_ShouldReturnCorrectStatus()
    {
        // Arrange
        var jobIdString = TestJobId.ToString();
        var jobStatus = CreateProcessingJobStatusDto();

        _getJobStatusHandlerMock.Setup(x => x.HandleAsync(
            It.Is<GetJobStatusQuery>(q => q.JobId == TestJobId)))
            .ReturnsAsync(jobStatus);

        // Act
        var result = await _controller.GetJobStatus(jobIdString);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<JobStatusResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Status.Should().Be("Processing");
        apiResponse.Data.ProgressPercentage.Should().Be(50);
        apiResponse.Data.StatusMessage.Should().Be("Processing row 500 of 1000");
    }

    [Fact]
    public async Task GetJobStatus_WithFailedJob_ShouldReturnErrorStatus()
    {
        // Arrange
        var jobIdString = TestJobId.ToString();
        var jobStatus = CreateFailedJobStatusDto();

        _getJobStatusHandlerMock.Setup(x => x.HandleAsync(
            It.Is<GetJobStatusQuery>(q => q.JobId == TestJobId)))
            .ReturnsAsync(jobStatus);

        // Act
        var result = await _controller.GetJobStatus(jobIdString);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<JobStatusResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Status.Should().Be("Failed");
        apiResponse.Data.StatusMessage.Should().Be("Processing failed due to invalid data");
    }

    [Fact]
    public async Task GetJobStatus_WithCompletedJob_ShouldIncludeResults()
    {
        // Arrange
        var jobIdString = TestJobId.ToString();
        var jobStatus = CreateCompletedJobStatusDto();

        _getJobStatusHandlerMock.Setup(x => x.HandleAsync(
            It.Is<GetJobStatusQuery>(q => q.JobId == TestJobId)))
            .ReturnsAsync(jobStatus);

        // Act
        var result = await _controller.GetJobStatus(jobIdString);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<JobStatusResponse>>().Subject;
        
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Should().NotBeNull();
        apiResponse.Data!.Status.Should().Be("Completed");
        apiResponse.Data.Results.Should().NotBeNull();
        apiResponse.Data.Results!.ProcessedRows.Should().Be(1000);
        apiResponse.Data.Results.RowsRemoved.Should().Be(50);
    }

    [Fact]
    public async Task GetJobStatus_ShouldHandleException()
    {
        // Arrange
        var jobIdString = TestJobId.ToString();

        _getJobStatusHandlerMock.Setup(x => x.HandleAsync(
            It.IsAny<GetJobStatusQuery>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetJobStatus(jobIdString);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetJobStatus_WithEmptyString_ShouldReturnBadRequest()
    {
        // Arrange
        var emptyJobId = string.Empty;

        // Act
        var result = await _controller.GetJobStatus(emptyJobId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetJobStatus_WithNullString_ShouldReturnBadRequest()
    {
        // Arrange
        string? nullJobId = null;

        // Act
        var result = await _controller.GetJobStatus(nullJobId!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(400);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    private JobStatusDto CreateTestJobStatusDto()
    {
        return new JobStatusDto(
            Id: TestJobId,
            DataSetId: TestDataSetId,
            OperationType: "RemoveDuplicates",
            OperationParameters: "{\"strategy\":\"KeepFirst\",\"columns\":[\"email\"]}",
            Status: "Completed",
            RetryCount: 0,
            MaxRetries: 3,
            CreatedAt: DateTime.UtcNow.AddMinutes(-10),
            StartedAt: DateTime.UtcNow.AddMinutes(-9),
            CompletedAt: DateTime.UtcNow,
            ErrorMessage: null,
            Result: "{\"processedRows\":1000,\"rowsRemoved\":50,\"processingTimeMs\":5000}",
            ProgressPercentage: 100,
            ProgressMessage: "Job completed successfully"
        );
    }

    private JobStatusDto CreateQueuedJobStatusDto()
    {
        return new JobStatusDto(
            Id: TestJobId,
            DataSetId: TestDataSetId,
            OperationType: "RemoveDuplicates",
            OperationParameters: "{\"strategy\":\"KeepFirst\",\"columns\":[\"email\"]}",
            Status: "Queued",
            RetryCount: 0,
            MaxRetries: 3,
            CreatedAt: DateTime.UtcNow,
            StartedAt: null,
            CompletedAt: null,
            ErrorMessage: null,
            Result: null,
            ProgressPercentage: 0,
            ProgressMessage: null
        );
    }

    private JobStatusDto CreateProcessingJobStatusDto()
    {
        return new JobStatusDto(
            Id: TestJobId,
            DataSetId: TestDataSetId,
            OperationType: "RemoveDuplicates",
            OperationParameters: "{\"strategy\":\"KeepFirst\",\"columns\":[\"email\"]}",
            Status: "Processing",
            RetryCount: 0,
            MaxRetries: 3,
            CreatedAt: DateTime.UtcNow.AddMinutes(-5),
            StartedAt: DateTime.UtcNow.AddMinutes(-4),
            CompletedAt: null,
            ErrorMessage: null,
            Result: null,
            ProgressPercentage: 50,
            ProgressMessage: "Processing row 500 of 1000"
        );
    }

    private JobStatusDto CreateFailedJobStatusDto()
    {
        return new JobStatusDto(
            Id: TestJobId,
            DataSetId: TestDataSetId,
            OperationType: "RemoveDuplicates",
            OperationParameters: "{\"strategy\":\"KeepFirst\",\"columns\":[\"email\"]}",
            Status: "Failed",
            RetryCount: 1,
            MaxRetries: 3,
            CreatedAt: DateTime.UtcNow.AddMinutes(-10),
            StartedAt: DateTime.UtcNow.AddMinutes(-9),
            CompletedAt: DateTime.UtcNow,
            ErrorMessage: "Processing failed due to invalid data",
            Result: null,
            ProgressPercentage: 75,
            ProgressMessage: null
        );
    }

    private JobStatusDto CreateCompletedJobStatusDto()
    {
        return new JobStatusDto(
            Id: TestJobId,
            DataSetId: TestDataSetId,
            OperationType: "RemoveDuplicates",
            OperationParameters: "{\"strategy\":\"KeepFirst\",\"columns\":[\"email\"]}",
            Status: "Completed",
            RetryCount: 0,
            MaxRetries: 3,
            CreatedAt: DateTime.UtcNow.AddMinutes(-10),
            StartedAt: DateTime.UtcNow.AddMinutes(-9),
            CompletedAt: DateTime.UtcNow,
            ErrorMessage: null,
            Result: "{\"processedRows\":1000,\"rowsRemoved\":50,\"rowsModified\":0,\"processingTimeMs\":5000,\"warnings\":[]}",
            ProgressPercentage: 100,
            ProgressMessage: "Job completed successfully"
        );
    }
}

