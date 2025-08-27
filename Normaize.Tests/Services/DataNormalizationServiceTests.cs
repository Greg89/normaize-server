using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Services.DataNormalization;
using Normaize.Tests.Repositories;
using Xunit;

namespace Normaize.Tests.Services;

[Trait("Category", TestSetup.Categories.Unit)]
public class DataNormalizationServiceTests
{
    private readonly Mock<IJobQueueService> _mockJobQueueService;
    private readonly Mock<IDataSetRepository> _mockDataSetRepository;
    private readonly Mock<IDuplicateRowRemovalProcessor> _mockProcessor;
    private readonly Mock<ILogger<DataNormalizationService>> _mockLogger;
    private readonly DataNormalizationService _service;
    public DataNormalizationServiceTests()
    {
        _mockJobQueueService = new Mock<IJobQueueService>();
        _mockDataSetRepository = new Mock<IDataSetRepository>();
        _mockProcessor = new Mock<IDuplicateRowRemovalProcessor>();
        _mockLogger = new Mock<ILogger<DataNormalizationService>>();
        _service = new DataNormalizationService(
            _mockJobQueueService.Object,
            _mockDataSetRepository.Object,
            _mockProcessor.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SubmitDuplicateRowRemovalJobAsync_WithValidRequest_ShouldSubmitJob()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user123";
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name", "Age"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var dataSet = TestDataBuilder.CreateDataSet(id: dataSetId, userId: userId, processed: true);

        _mockDataSetRepository.Setup(r => r.GetByIdAsync(dataSetId))
            .ReturnsAsync(dataSet);
        _mockProcessor.Setup(p => p.ValidateRequestAsync(dataSet, request))
            .ReturnsAsync(NormalizationValidationResult.Success());
        _mockJobQueueService.Setup(q => q.EnqueueJobAsync(It.IsAny<DataNormalizationJob>()))
            .ReturnsAsync(true);
        _mockProcessor.Setup(p => p.EstimateProcessingTimeAsync(dataSet, request))
            .ReturnsAsync(5000);
        _mockProcessor.Setup(p => p.EstimateMemoryUsageAsync(dataSet, request))
            .ReturnsAsync(100.0);

        // Act
        var result = await _service.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.JobId.Should().NotBeNullOrEmpty();
        result.Status.Should().Be(NormalizationJobStatus.Queued);
        result.Success.Should().BeTrue();
        result.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.EstimatedCompletionAt.Should().BeAfter(DateTime.UtcNow);

        _mockJobQueueService.Verify(q => q.EnqueueJobAsync(It.IsAny<DataNormalizationJob>()), Times.Once);
    }

    [Fact]
    public async Task SubmitDuplicateRowRemovalJobAsync_WithNonExistentDataset_ShouldThrowException()
    {
        // Arrange
        var dataSetId = 999;
        var userId = "user123";
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };

        _mockDataSetRepository.Setup(r => r.GetByIdAsync(dataSetId))
            .ReturnsAsync((DataSet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, userId));
    }

    [Fact]
    public async Task SubmitDuplicateRowRemovalJobAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user123";
        var unauthorizedUserId = "user456";
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var dataSet = TestDataBuilder.CreateDataSet(id: dataSetId, userId: userId, processed: true);

        _mockDataSetRepository.Setup(r => r.GetByIdAsync(dataSetId))
            .ReturnsAsync(dataSet);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, unauthorizedUserId));
    }

    [Fact]
    public async Task SubmitDuplicateRowRemovalJobAsync_WithInvalidRequest_ShouldThrowException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user123";
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = [],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var dataSet = TestDataBuilder.CreateDataSet(id: dataSetId, userId: userId, processed: true);

        _mockDataSetRepository.Setup(r => r.GetByIdAsync(dataSetId))
            .ReturnsAsync(dataSet);
        _mockProcessor.Setup(p => p.ValidateRequestAsync(dataSet, request))
            .ReturnsAsync(NormalizationValidationResult.Failure("Invalid request"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, userId));
    }

    [Fact]
    public async Task SubmitDuplicateRowRemovalJobAsync_WhenEnqueueFails_ShouldThrowException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user123";
        var request = new RemoveDuplicateRowsRequest
        {
            ColumnNames = ["Name"],
            KeepFirstOccurrence = true,
            CaseSensitive = false
        };
        var dataSet = TestDataBuilder.CreateDataSet(id: dataSetId, userId: userId, processed: true);

        _mockDataSetRepository.Setup(r => r.GetByIdAsync(dataSetId))
            .ReturnsAsync(dataSet);
        _mockProcessor.Setup(p => p.ValidateRequestAsync(dataSet, request))
            .ReturnsAsync(NormalizationValidationResult.Success());
        _mockJobQueueService.Setup(q => q.EnqueueJobAsync(It.IsAny<DataNormalizationJob>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SubmitDuplicateRowRemovalJobAsync(dataSetId, request, userId));
    }

    [Fact]
    public async Task GetJobStatusAsync_WithValidJob_ShouldReturnStatus()
    {
        // Arrange
        var jobId = "job123";
        var userId = "user123";
        var job = TestDataBuilder.CreateDataNormalizationJob(jobId, userId);

        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Queued, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { job });

        // Act
        var result = await _service.GetJobStatusAsync(jobId, userId);

        // Assert
        result.Should().NotBeNull();
        result.JobId.Should().Be(jobId);
        result.Status.Should().Be(job.Status);
        result.SubmittedAt.Should().Be(job.SubmittedAt);
    }

    [Fact]
    public async Task GetJobStatusAsync_WithNonExistentJob_ShouldThrowException()
    {
        // Arrange
        var jobId = "nonexistent";
        var userId = "user123";

        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(It.IsAny<NormalizationJobStatus>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(Enumerable.Empty<DataNormalizationJob>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.GetJobStatusAsync(jobId, userId));
    }

    [Fact]
    public async Task GetJobStatusAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        // Arrange
        var jobId = "job123";
        var userId = "user123";
        var unauthorizedUserId = "user456";
        var job = TestDataBuilder.CreateDataNormalizationJob(jobId, userId);

        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Queued, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { job });

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetJobStatusAsync(jobId, unauthorizedUserId));
    }

    [Fact]
    public async Task CancelJobAsync_WithValidJob_ShouldCancelJob()
    {
        // Arrange
        var jobId = "job123";
        var userId = "user123";
        var job = TestDataBuilder.CreateDataNormalizationJob(jobId, userId, status: NormalizationJobStatus.Queued);

        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Queued, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { job });
        _mockJobQueueService.Setup(q => q.MarkJobAsCancelledAsync(jobId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CancelJobAsync(jobId, userId);

        // Assert
        result.Should().BeTrue();
        _mockJobQueueService.Verify(q => q.MarkJobAsCancelledAsync(jobId), Times.Once);
    }

    [Fact]
    public async Task CancelJobAsync_WithCompletedJob_ShouldThrowException()
    {
        // Arrange
        var jobId = "job123";
        var userId = "user123";
        var job = TestDataBuilder.CreateDataNormalizationJob(jobId, userId, status: NormalizationJobStatus.Completed);

        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(It.IsAny<NormalizationJobStatus>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { job });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CancelJobAsync(jobId, userId));
    }

    [Fact]
    public async Task GetUserJobsAsync_ShouldReturnUserJobs()
    {
        // Arrange
        var userId = "user123";
        var jobs = new[]
        {
            TestDataBuilder.CreateDataNormalizationJob("job1", userId, status: NormalizationJobStatus.Queued),
            TestDataBuilder.CreateDataNormalizationJob("job2", userId, status: NormalizationJobStatus.Processing)
        };

        // The service calls GetJobsByPriorityAsync for each status, so we need to mock all calls
        // Each status should return only the jobs that belong to that status
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Queued, int.MaxValue, 1000))
            .ReturnsAsync(new[] { jobs[0] });
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Processing, int.MaxValue, 1000))
            .ReturnsAsync(new[] { jobs[1] });
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Completed, int.MaxValue, 1000))
            .ReturnsAsync(new DataNormalizationJob[0]);
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Failed, int.MaxValue, 1000))
            .ReturnsAsync(new DataNormalizationJob[0]);
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Cancelled, int.MaxValue, 1000))
            .ReturnsAsync(new DataNormalizationJob[0]);

        // Act
        var result = await _service.GetUserJobsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(j => j.JobId == "job1" || j.JobId == "job2");
    }

    [Fact]
    public async Task GetUserJobsAsync_WithPagination_ShouldReturnPaginatedResults()
    {
        // Arrange
        var userId = "user123";
        var allJobs = Enumerable.Range(1, 25)
            .Select(i => TestDataBuilder.CreateDataNormalizationJob($"job{i}", userId))
            .ToList();

        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(It.IsAny<NormalizationJobStatus>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(allJobs);

        // Act
        var result = await _service.GetUserJobsAsync(userId, page: 2, pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetDataSetJobsAsync_WithValidDataset_ShouldReturnDatasetJobs()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user123";
        var dataSet = TestDataBuilder.CreateDataSet(id: dataSetId, userId: userId);
        var jobs = new[]
        {
            TestDataBuilder.CreateDataNormalizationJob("job1", userId, dataSetId: dataSetId, status: NormalizationJobStatus.Queued),
            TestDataBuilder.CreateDataNormalizationJob("job2", userId, dataSetId: dataSetId, status: NormalizationJobStatus.Processing)
        };

        _mockDataSetRepository.Setup(r => r.GetByIdAsync(dataSetId))
            .ReturnsAsync(dataSet);
        // The service calls GetJobsByPriorityAsync for each status, so we need to mock all calls
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Queued, int.MaxValue, 1000))
            .ReturnsAsync(new[] { jobs[0] }); // Only job1 is queued
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Processing, int.MaxValue, 1000))
            .ReturnsAsync(new[] { jobs[1] }); // Only job2 is processing
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Completed, int.MaxValue, 1000))
            .ReturnsAsync(new DataNormalizationJob[0]);
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Failed, int.MaxValue, 1000))
            .ReturnsAsync(new DataNormalizationJob[0]);
        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(NormalizationJobStatus.Cancelled, int.MaxValue, 1000))
            .ReturnsAsync(new DataNormalizationJob[0]);

        // Act
        var result = await _service.GetDataSetJobsAsync(dataSetId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(j => j.JobId == "job1" || j.JobId == "job2");
    }

    [Fact]
    public async Task GetDataSetJobsAsync_WithUnauthorizedUser_ShouldThrowException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "user123";
        var unauthorizedUserId = "user456";
        var dataSet = TestDataBuilder.CreateDataSet(id: dataSetId, userId: userId);

        _mockDataSetRepository.Setup(r => r.GetByIdAsync(dataSetId))
            .ReturnsAsync(dataSet);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetDataSetJobsAsync(dataSetId, unauthorizedUserId));
    }

    [Fact]
    public async Task GetJobStatusAsync_WithCompletedJob_ShouldIncludeResults()
    {
        // Arrange
        var jobId = "job123";
        var userId = "user123";
        var results = new NormalizationResults
        {
            RowsProcessed = 100,
            DuplicateRowsRemoved = 20,
            RowsRemaining = 80,
            ProcessingTimeMs = 5000,
            MemoryUsageMB = 50.0
        };
        var job = TestDataBuilder.CreateDataNormalizationJob(jobId, userId, status: NormalizationJobStatus.Completed);
        job.Results = JsonSerializer.Serialize(results);

        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(It.IsAny<NormalizationJobStatus>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { job });

        // Act
        var result = await _service.GetJobStatusAsync(jobId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().NotBeNull();
        result.Results!.RowsProcessed.Should().Be(100);
        result.Results.DuplicateRowsRemoved.Should().Be(20);
        result.Results.RowsRemaining.Should().Be(80);
    }

    [Fact]
    public async Task GetJobStatusAsync_WithFailedJob_ShouldIncludeErrorMessage()
    {
        // Arrange
        var jobId = "job123";
        var userId = "user123";
        var errorMessage = "Processing failed due to insufficient memory";
        var job = TestDataBuilder.CreateDataNormalizationJob(jobId, userId, status: NormalizationJobStatus.Failed);
        job.ErrorMessage = errorMessage;

        _mockJobQueueService.Setup(q => q.GetJobsByPriorityAsync(It.IsAny<NormalizationJobStatus>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new[] { job });

        // Act
        var result = await _service.GetJobStatusAsync(jobId, userId);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessage.Should().Be(errorMessage);
    }
}
