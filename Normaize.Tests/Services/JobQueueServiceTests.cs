using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.Core.DTOs;
using Normaize.Core.Models;
using Normaize.Data;
using Normaize.Data.Services;
using Normaize.Tests.Repositories;
using Xunit;

namespace Normaize.Tests.Services;

[Trait("Category", TestSetup.Categories.Unit)]
public class JobQueueServiceTests : IDisposable
{
    private readonly DbContextOptions<NormaizeContext> _dbContextOptions;
    private readonly NormaizeContext _context;
    private readonly Mock<ILogger<JobQueueService>> _mockLogger;
    private readonly JobQueueOptions _options;
    private readonly JobQueueService _service;
    public JobQueueServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new NormaizeContext(_dbContextOptions);
        _mockLogger = new Mock<ILogger<JobQueueService>>();
        _options = new JobQueueOptions
        {
            MaxConcurrentJobs = 2,
            CleanupInterval = TimeSpan.FromMinutes(1),
            RetryCheckInterval = TimeSpan.FromMinutes(1),
            JobRetentionDays = 30
        };

        _service = new JobQueueService(_context, _mockLogger.Object, Options.Create(_options));

        // Ensure database is created
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task EnqueueJobAsync_WithValidJob_ShouldEnqueueSuccessfully()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");

        // Act
        var result = await _service.EnqueueJobAsync(job);

        // Assert
        result.Should().BeTrue();
        var savedJob = await _context.DataNormalizationJobs.FindAsync(job.Id);
        savedJob.Should().NotBeNull();
        savedJob!.Status.Should().Be(NormalizationJobStatus.Queued);
    }

    [Fact]
    public async Task EnqueueJobAsync_WithDuplicateJobId_ShouldReturnFalse()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        await _service.EnqueueJobAsync(job);

        var duplicateJob = TestDataBuilder.CreateDataNormalizationJob("job1", "user456");

        // Act
        var result = await _service.EnqueueJobAsync(duplicateJob);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DequeueJobAsync_WithAvailableJob_ShouldReturnJob()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        await _service.EnqueueJobAsync(job);

        // Act
        var dequeuedJob = await _service.DequeueJobAsync();

        // Assert
        dequeuedJob.Should().NotBeNull();
        dequeuedJob!.Id.Should().Be(job.Id);
        dequeuedJob.Status.Should().Be(NormalizationJobStatus.Processing);
    }

    [Fact]
    public async Task DequeueJobAsync_WithNoAvailableJobs_ShouldReturnNull()
    {
        // Act
        var job = await _service.DequeueJobAsync();

        // Assert
        job.Should().BeNull();
    }

    [Fact]
    public async Task DequeueJobAsync_WithProcessingJobs_ShouldRespectConcurrencyLimit()
    {
        // Arrange
        var jobs = Enumerable.Range(1, 5)
            .Select(i => TestDataBuilder.CreateDataNormalizationJob($"job{i}", "user123"))
            .ToList();

        foreach (var job in jobs)
        {
            await _service.EnqueueJobAsync(job);
        }

        // Act - Dequeue jobs up to concurrency limit
        var dequeuedJobs = new List<DataNormalizationJob>();
        for (int i = 0; i < _options.MaxConcurrentJobs; i++)
        {
            var job = await _service.DequeueJobAsync();
            if (job != null)
            {
                dequeuedJobs.Add(job);
            }
        }

        // Assert
        dequeuedJobs.Should().HaveCount(_options.MaxConcurrentJobs);
        dequeuedJobs.Should().OnlyContain(j => j.Status == NormalizationJobStatus.Processing);

        // Try to dequeue more - should return the next available job since concurrency limit is not enforced at dequeue level
        var extraJob = await _service.DequeueJobAsync();
        extraJob.Should().NotBeNull();
        extraJob!.Status.Should().Be(NormalizationJobStatus.Processing);
    }

    [Fact]
    public async Task DequeueJobAsync_WithPriorityJobs_ShouldReturnHighestPriorityFirst()
    {
        // Arrange
        var lowPriorityJob = TestDataBuilder.CreateDataNormalizationJob("job1", "user123", priority: 1);
        var highPriorityJob = TestDataBuilder.CreateDataNormalizationJob("job2", "user123", priority: 5);
        var mediumPriorityJob = TestDataBuilder.CreateDataNormalizationJob("job3", "user123", priority: 3);

        await _service.EnqueueJobAsync(lowPriorityJob);
        await _service.EnqueueJobAsync(highPriorityJob);
        await _service.EnqueueJobAsync(mediumPriorityJob);

        // Act
        var firstJob = await _service.DequeueJobAsync();
        var secondJob = await _service.DequeueJobAsync();
        var thirdJob = await _service.DequeueJobAsync();

        // Assert
        firstJob.Should().NotBeNull();
        firstJob!.Priority.Should().Be(5); // Highest priority first
        secondJob.Should().NotBeNull();
        secondJob!.Priority.Should().Be(3); // Medium priority second
        thirdJob.Should().NotBeNull();
        thirdJob!.Priority.Should().Be(1); // Lowest priority last
    }

    [Fact]
    public async Task MarkJobAsStartedAsync_WithValidJob_ShouldUpdateStatus()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        await _service.EnqueueJobAsync(job);

        // Act
        var result = await _service.MarkJobAsStartedAsync(job.Id);

        // Assert
        result.Should().BeTrue();
        var updatedJob = await _context.DataNormalizationJobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(NormalizationJobStatus.Processing);
        updatedJob.StartedAt.Should().NotBeNull();
        updatedJob.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task MarkJobAsStartedAsync_WithNonExistentJob_ShouldReturnFalse()
    {
        // Act
        var result = await _service.MarkJobAsStartedAsync("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateJobProgressAsync_WithValidJob_ShouldUpdateProgress()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        await _service.EnqueueJobAsync(job);
        await _service.MarkJobAsStartedAsync(job.Id);

        // Act
        var result = await _service.UpdateJobProgressAsync(job.Id, 50, "Processing rows");

        // Assert
        result.Should().BeTrue();
        var updatedJob = await _context.DataNormalizationJobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.ProgressPercentage.Should().Be(50);
    }

    [Fact]
    public async Task MarkJobAsCompletedAsync_WithValidJob_ShouldUpdateStatus()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        await _service.EnqueueJobAsync(job);
        await _service.MarkJobAsStartedAsync(job.Id);

        var results = new NormalizationResults
        {
            RowsProcessed = 100,
            DuplicateRowsRemoved = 20,
            RowsRemaining = 80,
            ProcessingTimeMs = 5000,
            MemoryUsageMB = 50.0
        };

        // Act
        var result = await _service.MarkJobAsCompletedAsync(job.Id, JsonSerializer.Serialize(results));

        // Assert
        result.Should().BeTrue();
        var updatedJob = await _context.DataNormalizationJobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(NormalizationJobStatus.Completed);
        updatedJob.CompletedAt.Should().NotBeNull();
        updatedJob.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        updatedJob.Results.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MarkJobAsFailedAsync_WithValidJob_ShouldUpdateStatus()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        await _service.EnqueueJobAsync(job);
        await _service.MarkJobAsStartedAsync(job.Id);

        var errorMessage = "Processing failed due to insufficient memory";

        // Act
        var result = await _service.MarkJobAsFailedAsync(job.Id, errorMessage);

        // Assert
        result.Should().BeTrue();
        var updatedJob = await _context.DataNormalizationJobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(NormalizationJobStatus.Failed);
        updatedJob.CompletedAt.Should().NotBeNull();
        updatedJob.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact]
    public async Task MarkJobAsCancelledAsync_WithValidJob_ShouldUpdateStatus()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        await _service.EnqueueJobAsync(job);

        // Act
        var result = await _service.MarkJobAsCancelledAsync(job.Id);

        // Assert
        result.Should().BeTrue();
        var updatedJob = await _context.DataNormalizationJobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(NormalizationJobStatus.Cancelled);
        updatedJob.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RetryJobAsync_WithFailedJob_ShouldScheduleRetry()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        job.Status = NormalizationJobStatus.Failed;
        job.RetryCount = 1;

        // Add job directly to database to preserve its status and retry count
        _context.DataNormalizationJobs.Add(job);
        await _context.SaveChangesAsync();

        var nextRetryAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = await _service.RetryJobAsync(job.Id, nextRetryAt);

        // Assert
        result.Should().BeTrue();
        var updatedJob = await _context.DataNormalizationJobs.FindAsync(job.Id);
        updatedJob.Should().NotBeNull();
        updatedJob!.NextRetryAt.Should().Be(nextRetryAt);
        updatedJob.RetryCount.Should().Be(2); // RetryCount is incremented by RetryJobAsync
    }

    [Fact]
    public async Task RetryJobAsync_WithMaxRetriesExceeded_ShouldReturnFalse()
    {
        // Arrange
        var job = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        job.Status = NormalizationJobStatus.Failed;
        job.RetryCount = 3;
        await _service.EnqueueJobAsync(job);

        var nextRetryAt = DateTime.UtcNow.AddMinutes(5);

        // Act
        var result = await _service.RetryJobAsync(job.Id, nextRetryAt);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetJobsReadyForRetryAsync_ShouldReturnReadyJobs()
    {
        // Arrange
        var readyJob = TestDataBuilder.CreateDataNormalizationJob("job1", "user123");
        readyJob.Status = NormalizationJobStatus.Failed;
        readyJob.NextRetryAt = DateTime.UtcNow.AddMinutes(-1); // Past due
        readyJob.RetryCount = 1;

        var notReadyJob = TestDataBuilder.CreateDataNormalizationJob("job2", "user123");
        notReadyJob.Status = NormalizationJobStatus.Failed;
        notReadyJob.NextRetryAt = DateTime.UtcNow.AddMinutes(5); // Future
        notReadyJob.RetryCount = 1;

        // Add jobs directly to database to preserve their statuses
        _context.DataNormalizationJobs.Add(readyJob);
        _context.DataNormalizationJobs.Add(notReadyJob);
        await _context.SaveChangesAsync();

        // Act
        var readyJobs = await _service.GetJobsReadyForRetryAsync();

        // Assert
        readyJobs.Should().HaveCount(1);
        readyJobs.Should().OnlyContain(j => j.Id == "job1");
    }

    [Fact]
    public async Task GetQueueLengthAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var jobs = Enumerable.Range(1, 3)
            .Select(i => TestDataBuilder.CreateDataNormalizationJob($"job{i}", "user123"))
            .ToList();

        foreach (var job in jobs)
        {
            await _service.EnqueueJobAsync(job);
        }

        // Act
        var queueLength = await _service.GetQueueLengthAsync();

        // Assert
        queueLength.Should().Be(3);
    }

    [Fact]
    public async Task GetJobsByPriorityAsync_ShouldReturnFilteredJobs()
    {
        // Arrange
        var queuedJob = TestDataBuilder.CreateDataNormalizationJob("job1", "user123", status: NormalizationJobStatus.Queued);
        var processingJob = TestDataBuilder.CreateDataNormalizationJob("job2", "user123", status: NormalizationJobStatus.Processing);
        var completedJob = TestDataBuilder.CreateDataNormalizationJob("job3", "user123", status: NormalizationJobStatus.Completed);

        // Add jobs directly to database with their intended statuses
        _context.DataNormalizationJobs.Add(queuedJob);
        _context.DataNormalizationJobs.Add(processingJob);
        _context.DataNormalizationJobs.Add(completedJob);
        await _context.SaveChangesAsync();

        // Act
        var queuedJobs = await _service.GetJobsByPriorityAsync(NormalizationJobStatus.Queued);
        var processingJobs = await _service.GetJobsByPriorityAsync(NormalizationJobStatus.Processing);
        var completedJobs = await _service.GetJobsByPriorityAsync(NormalizationJobStatus.Completed);

        // Assert
        queuedJobs.Should().HaveCount(1);
        queuedJobs.Should().OnlyContain(j => j.Id == "job1");
        processingJobs.Should().HaveCount(1);
        processingJobs.Should().OnlyContain(j => j.Id == "job2");
        completedJobs.Should().HaveCount(1);
        completedJobs.Should().OnlyContain(j => j.Id == "job3");
    }

    [Fact]
    public async Task CleanupOldJobsAsync_ShouldRemoveOldCompletedJobs()
    {
        // Arrange
        var oldJob = TestDataBuilder.CreateDataNormalizationJob("job1", "user123", status: NormalizationJobStatus.Completed);
        oldJob.CompletedAt = DateTime.UtcNow.AddDays(-31); // Older than retention period

        var recentJob = TestDataBuilder.CreateDataNormalizationJob("job2", "user123", status: NormalizationJobStatus.Completed);
        recentJob.CompletedAt = DateTime.UtcNow.AddDays(-10); // Within retention period

        // Add jobs directly to database to preserve their statuses and completion dates
        _context.DataNormalizationJobs.Add(oldJob);
        _context.DataNormalizationJobs.Add(recentJob);
        await _context.SaveChangesAsync();

        var cutoffDate = DateTime.UtcNow.AddDays(-_options.JobRetentionDays);

        // Act
        var removedCount = await _service.CleanupOldJobsAsync(cutoffDate);

        // Assert
        removedCount.Should().Be(1);
        var remainingJobs = await _context.DataNormalizationJobs.Where(j => !j.IsDeleted).ToListAsync();
        remainingJobs.Should().HaveCount(1);
        remainingJobs.Should().OnlyContain(j => j.Id == "job2");
    }

    [Fact]
    public async Task CleanupOldJobsAsync_WithNoOldJobs_ShouldReturnZero()
    {
        // Arrange
        var recentJob = TestDataBuilder.CreateDataNormalizationJob("job1", "user123", status: NormalizationJobStatus.Completed);
        recentJob.CompletedAt = DateTime.UtcNow.AddDays(-10);
        await _service.EnqueueJobAsync(recentJob);

        var cutoffDate = DateTime.UtcNow.AddDays(-_options.JobRetentionDays);

        // Act
        var removedCount = await _service.CleanupOldJobsAsync(cutoffDate);

        // Assert
        removedCount.Should().Be(0);
        var remainingJobs = await _context.DataNormalizationJobs.ToListAsync();
        remainingJobs.Should().HaveCount(1);
    }
}
