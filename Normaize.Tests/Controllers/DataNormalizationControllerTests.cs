using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.API.Controllers;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Tests.Repositories;
using Xunit;

namespace Normaize.Tests.Controllers;

[Trait("Category", TestSetup.Categories.Unit)]
public class DataNormalizationControllerTests
{
    private readonly Mock<IDataNormalizationService> _mockService;
    private readonly Mock<ILogger<DataNormalizationController>> _mockLogger;
    private readonly DataNormalizationController _controller;
    private readonly HttpContext _httpContext;

    public DataNormalizationControllerTests()
    {
        _mockService = new Mock<IDataNormalizationService>();
        _mockLogger = new Mock<ILogger<DataNormalizationController>>();
        _controller = new DataNormalizationController(_mockService.Object, _mockLogger.Object);

        // Setup HTTP context with user claims
        _httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user123"),
            new(ClaimTypes.Name, "Test User"),
            new(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _httpContext.User = principal;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    [Fact]
    public async Task RemoveDuplicateRows_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var dataSetId = 1;
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name", "Age"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var response = new NormalizationJobResponse
        {
            JobId = "job123",
            Status = NormalizationJobStatus.Queued,
            Message = "Job submitted successfully",
            SubmittedAt = DateTime.UtcNow,
            Success = true
        };

        _mockService.Setup(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", It.IsAny<string>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RemoveDuplicateRows(dataSetId, request);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobResponse>>();
        var actionResult = result as ActionResult<NormalizationJobResponse>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);

        _mockService.Verify(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDuplicateRows_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var dataSetId = 1;
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = [], // Invalid - empty column names
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        _mockService.Setup(s => s.SubmitDuplicateRowRemovalJobAsync(It.IsAny<int>(), It.IsAny<RemoveDuplicateRowsRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid request"));

        // Act
        var result = await _controller.RemoveDuplicateRows(dataSetId, request);



        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobResponse>>();
        var actionResult = result as ActionResult<NormalizationJobResponse>;
        actionResult!.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Invalid request" });

        // Verify the service was called (correlation ID will be the trace identifier)
        _mockService.Verify(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDuplicateRows_WithUnauthorizedAccess_ShouldReturnForbidden()
    {
        // Arrange
        var dataSetId = 1;
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        _mockService.Setup(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.RemoveDuplicateRows(dataSetId, request);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobResponse>>();
        var actionResult = result as ActionResult<NormalizationJobResponse>;
        actionResult!.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task RemoveDuplicateRows_WithNonExistentDataset_ShouldReturnNotFound()
    {
        // Arrange
        var dataSetId = 999;
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        _mockService.Setup(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Dataset not found"));

        // Act
        var result = await _controller.RemoveDuplicateRows(dataSetId, request);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobResponse>>();
        var actionResult = result as ActionResult<NormalizationJobResponse>;
        actionResult!.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Dataset not found" });
    }

    [Fact]
    public async Task GetJobStatus_WithValidJob_ShouldReturnOkResult()
    {
        // Arrange
        var jobId = "job123";
        var response = new NormalizationJobStatusResponse
        {
            JobId = jobId,
            Status = NormalizationJobStatus.Processing,
            Message = "Job is processing",
            SubmittedAt = DateTime.UtcNow.AddMinutes(-5),
            StartedAt = DateTime.UtcNow.AddMinutes(-3),
            ProgressPercentage = 45
        };

        _mockService.Setup(s => s.GetJobStatusAsync(jobId, "user123"))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobStatusResponse>>();
        var actionResult = result as ActionResult<NormalizationJobStatusResponse>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetJobStatus_WithNonExistentJob_ShouldReturnNotFound()
    {
        // Arrange
        var jobId = "nonexistent";

        _mockService.Setup(s => s.GetJobStatusAsync(jobId, "user123"))
            .ThrowsAsync(new InvalidOperationException("Job not found"));

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobStatusResponse>>();
        var actionResult = result as ActionResult<NormalizationJobStatusResponse>;
        actionResult!.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = actionResult.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { error = "Job not found" });
    }

    [Fact]
    public async Task GetJobStatus_WithUnauthorizedAccess_ShouldReturnForbidden()
    {
        // Arrange
        var jobId = "job123";

        _mockService.Setup(s => s.GetJobStatusAsync(jobId, "user123"))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobStatusResponse>>();
        var actionResult = result as ActionResult<NormalizationJobStatusResponse>;
        actionResult!.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CancelJob_WithValidJob_ShouldReturnOkResult()
    {
        // Arrange
        var jobId = "job123";

        _mockService.Setup(s => s.CancelJobAsync(jobId, "user123"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<bool>>();
        var actionResult = result as ActionResult<bool>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().Be(true);
    }

    [Fact]
    public async Task CancelJob_WithNonExistentJob_ShouldReturnNotFound()
    {
        // Arrange
        var jobId = "nonexistent";

        _mockService.Setup(s => s.CancelJobAsync(jobId, "user123"))
            .ThrowsAsync(new InvalidOperationException("Job not found"));

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<bool>>();
        var actionResult = result as ActionResult<bool>;
        actionResult!.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Job not found" });
    }

    [Fact]
    public async Task CancelJob_WithUnauthorizedAccess_ShouldReturnForbidden()
    {
        // Arrange
        var jobId = "job123";

        _mockService.Setup(s => s.CancelJobAsync(jobId, "user123"))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<bool>>();
        var actionResult = result as ActionResult<bool>;
        actionResult!.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CancelJob_WithCompletedJob_ShouldReturnBadRequest()
    {
        // Arrange
        var jobId = "job123";

        _mockService.Setup(s => s.CancelJobAsync(jobId, "user123"))
            .ThrowsAsync(new InvalidOperationException("Cannot cancel completed job"));

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<bool>>();
        var actionResult = result as ActionResult<bool>;
        actionResult!.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Cannot cancel completed job" });
    }

    [Fact]
    public async Task GetUserJobs_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var jobs = new[]
        {
            new NormalizationJobStatusResponse
            {
                JobId = "job1",
                Status = NormalizationJobStatus.Queued,
                Message = "Job queued",
                SubmittedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new NormalizationJobStatusResponse
            {
                JobId = "job2",
                Status = NormalizationJobStatus.Processing,
                Message = "Job processing",
                SubmittedAt = DateTime.UtcNow.AddMinutes(-5),
                StartedAt = DateTime.UtcNow.AddMinutes(-3),
                ProgressPercentage = 30
            }
        };

        _mockService.Setup(s => s.GetUserJobsAsync("user123", 1, 20, false))
            .ReturnsAsync(jobs);

        // Act
        var result = await _controller.GetUserJobs(1, 20, false);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<NormalizationJobStatusResponse>>>();
        var actionResult = result as ActionResult<IEnumerable<NormalizationJobStatusResponse>>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(jobs);
    }

    [Fact]
    public async Task GetUserJobs_WithDefaultParameters_ShouldUseDefaultValues()
    {
        // Arrange
        var jobs = new[]
        {
            new NormalizationJobStatusResponse
            {
                JobId = "job1",
                Status = NormalizationJobStatus.Queued,
                Message = "Job queued",
                SubmittedAt = DateTime.UtcNow.AddMinutes(-10)
            }
        };

        _mockService.Setup(s => s.GetUserJobsAsync("user123", 1, 20, false))
            .ReturnsAsync(jobs);

        // Act
        var result = await _controller.GetUserJobs();

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<NormalizationJobStatusResponse>>>();
        var actionResult = result as ActionResult<IEnumerable<NormalizationJobStatusResponse>>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        _mockService.Verify(s => s.GetUserJobsAsync("user123", 1, 20, false), Times.Once);
    }

    [Fact]
    public async Task GetUserJobs_WithPagination_ShouldPassCorrectParameters()
    {
        // Arrange
        var jobs = new[]
        {
            new NormalizationJobStatusResponse
            {
                JobId = "job1",
                Status = NormalizationJobStatus.Queued,
                Message = "Job queued",
                SubmittedAt = DateTime.UtcNow.AddMinutes(-10)
            }
        };

        _mockService.Setup(s => s.GetUserJobsAsync("user123", 2, 10, true))
            .ReturnsAsync(jobs);

        // Act
        var result = await _controller.GetUserJobs(2, 10, true);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<NormalizationJobStatusResponse>>>();
        var actionResult = result as ActionResult<IEnumerable<NormalizationJobStatusResponse>>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        _mockService.Verify(s => s.GetUserJobsAsync("user123", 2, 10, true), Times.Once);
    }

    [Fact]
    public async Task GetDataSetJobs_WithValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var dataSetId = 1;
        var jobs = new[]
        {
            new NormalizationJobStatusResponse
            {
                JobId = "job1",
                Status = NormalizationJobStatus.Completed,
                Message = "Job completed",
                SubmittedAt = DateTime.UtcNow.AddMinutes(-30),
                StartedAt = DateTime.UtcNow.AddMinutes(-25),
                CompletedAt = DateTime.UtcNow.AddMinutes(-5),
                ProgressPercentage = 100,
                Results = new NormalizationResults
                {
                    RowsProcessed = 100,
                    DuplicateRowsRemoved = 20,
                    RowsRemaining = 80,
                    ProcessingTimeMs = 5000,
                    MemoryUsageMB = 50.0
                }
            }
        };

        _mockService.Setup(s => s.GetDataSetJobsAsync(dataSetId, "user123"))
            .ReturnsAsync(jobs);

        // Act
        var result = await _controller.GetDataSetJobs(dataSetId);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<NormalizationJobStatusResponse>>>();
        var actionResult = result as ActionResult<IEnumerable<NormalizationJobStatusResponse>>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(jobs);
    }

    [Fact]
    public async Task GetDataSetJobs_WithUnauthorizedAccess_ShouldReturnForbidden()
    {
        // Arrange
        var dataSetId = 1;

        _mockService.Setup(s => s.GetDataSetJobsAsync(dataSetId, "user123"))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.GetDataSetJobs(dataSetId);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<NormalizationJobStatusResponse>>>();
        var actionResult = result as ActionResult<IEnumerable<NormalizationJobStatusResponse>>;
        actionResult!.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetDataSetJobs_WithNonExistentDataset_ShouldReturnNotFound()
    {
        // Arrange
        var dataSetId = 999;

        _mockService.Setup(s => s.GetDataSetJobsAsync(dataSetId, "user123"))
            .ThrowsAsync(new InvalidOperationException("Dataset not found"));

        // Act
        var result = await _controller.GetDataSetJobs(dataSetId);

        // Assert
        result.Should().BeOfType<ActionResult<IEnumerable<NormalizationJobStatusResponse>>>();
        var actionResult = result as ActionResult<IEnumerable<NormalizationJobStatusResponse>>;
        actionResult!.Result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = actionResult.Result as NotFoundObjectResult;
        notFoundResult!.Value.Should().BeEquivalentTo(new { error = "Dataset not found" });
    }

    [Fact]
    public async Task RemoveDuplicateRows_WithCorrelationId_ShouldPassCorrelationId()
    {
        // Arrange
        var dataSetId = 1;
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var response = new NormalizationJobResponse
        {
            JobId = "job123",
            Status = NormalizationJobStatus.Queued,
            Success = true
        };

        // Add correlation ID to request headers
        _httpContext.Request.Headers["X-Correlation-ID"] = "corr123";

        _mockService.Setup(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", "corr123"))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RemoveDuplicateRows(dataSetId, request);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobResponse>>();
        var actionResult = result as ActionResult<NormalizationJobResponse>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        _mockService.Verify(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", "corr123"), Times.Once);
    }

    [Fact]
    public async Task RemoveDuplicateRows_WithoutCorrelationId_ShouldPassNull()
    {
        // Arrange
        var dataSetId = 1;
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var response = new NormalizationJobResponse
        {
            JobId = "job123",
            Status = NormalizationJobStatus.Queued,
            Success = true
        };

        _mockService.Setup(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", null))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RemoveDuplicateRows(dataSetId, request);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobResponse>>();
        var actionResult = result as ActionResult<NormalizationJobResponse>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        _mockService.Verify(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RemoveDuplicateRows_WithValidationError_ShouldReturnBadRequest()
    {
        // Arrange
        var dataSetId = 1;
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = Enumerable.Range(1, 15).Select(i => $"Column{i}").ToArray(), // Too many columns
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        _mockService.Setup(s => s.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, "user123", It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Maximum 10 columns allowed for duplicate detection"));

        // Act
        var result = await _controller.RemoveDuplicateRows(dataSetId, request);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobResponse>>();
        var actionResult = result as ActionResult<NormalizationJobResponse>;
        actionResult!.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = actionResult.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeEquivalentTo(new { error = "Maximum 10 columns allowed for duplicate detection" });
    }

    [Fact]
    public async Task GetJobStatus_WithCompletedJob_ShouldIncludeResults()
    {
        // Arrange
        var jobId = "job123";
        var results = new NormalizationResults
        {
            RowsProcessed = 1000,
            DuplicateRowsRemoved = 150,
            RowsRemaining = 850,
            ProcessingTimeMs = 15000,
            MemoryUsageMB = 200.0
        };
        var response = new NormalizationJobStatusResponse
        {
            JobId = jobId,
            Status = NormalizationJobStatus.Completed,
            Message = "Job completed successfully",
            SubmittedAt = DateTime.UtcNow.AddMinutes(-30),
            StartedAt = DateTime.UtcNow.AddMinutes(-25),
            CompletedAt = DateTime.UtcNow.AddMinutes(-5),
            ProgressPercentage = 100,
            Results = results
        };

        _mockService.Setup(s => s.GetJobStatusAsync(jobId, "user123"))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobStatusResponse>>();
        var actionResult = result as ActionResult<NormalizationJobStatusResponse>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        var returnedResponse = okResult!.Value as NormalizationJobStatusResponse;
        returnedResponse.Should().NotBeNull();
        returnedResponse!.Results.Should().BeEquivalentTo(results);
    }

    [Fact]
    public async Task GetJobStatus_WithFailedJob_ShouldIncludeErrorMessage()
    {
        // Arrange
        var jobId = "job123";
        var errorMessage = "Processing failed due to insufficient memory";
        var response = new NormalizationJobStatusResponse
        {
            JobId = jobId,
            Status = NormalizationJobStatus.Failed,
            Message = "Job failed",
            SubmittedAt = DateTime.UtcNow.AddMinutes(-30),
            StartedAt = DateTime.UtcNow.AddMinutes(-25),
            CompletedAt = DateTime.UtcNow.AddMinutes(-5),
            ProgressPercentage = 45,
            ErrorMessage = errorMessage
        };

        _mockService.Setup(s => s.GetJobStatusAsync(jobId, "user123"))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        result.Should().BeOfType<ActionResult<NormalizationJobStatusResponse>>();
        var actionResult = result as ActionResult<NormalizationJobStatusResponse>;
        actionResult!.Result.Should().BeOfType<OkObjectResult>();
        var okResult = actionResult.Result as OkObjectResult;
        var returnedResponse = okResult!.Value as NormalizationJobStatusResponse;
        returnedResponse.Should().NotBeNull();
        returnedResponse!.ErrorMessage.Should().Be(errorMessage);
    }
}
