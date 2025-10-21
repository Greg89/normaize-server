using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Infrastructure.Services;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

/// <summary>
/// Tests for EfCoreJobQueue - demonstrates infrastructure testing with mocks
/// </summary>
public class EfCoreJobQueueTests
{
    private readonly Mock<INormalizationJobRepository> _mockRepository;
    private readonly EfCoreJobQueue _jobQueue;

    public EfCoreJobQueueTests()
    {
        _mockRepository = new Mock<INormalizationJobRepository>();
        _jobQueue = new EfCoreJobQueue(_mockRepository.Object);
    }

    [Fact]
    public async Task EnqueueAsync_WithValidJob_ShouldSaveJob()
    {
        // Arrange
        var job = NormalizationJob.Create(
            Guid.NewGuid(),
            "REMOVE_DUPLICATE_ROWS",
            "{\"columns\":[\"name\",\"email\"]}");

        // Act
        await _jobQueue.EnqueueAsync(job);

        // Assert
        _mockRepository.Verify(r => r.SaveAsync(job), Times.Once);
    }

    [Fact]
    public async Task DequeueAsync_ShouldReturnNextQueuedJob()
    {
        // Arrange
        var expectedJob = NormalizationJob.Create(
            Guid.NewGuid(),
            "REMOVE_DUPLICATE_ROWS",
            "{\"columns\":[\"name\",\"email\"]}");

        _mockRepository.Setup(r => r.GetNextQueuedJobAsync())
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _jobQueue.DequeueAsync();

        // Assert
        result.Should().Be(expectedJob);
        _mockRepository.Verify(r => r.GetNextQueuedJobAsync(), Times.Once);
    }

    [Fact]
    public async Task DequeueAsync_WhenNoJobsAvailable_ShouldReturnNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetNextQueuedJobAsync())
            .ReturnsAsync((NormalizationJob?)null);

        // Act
        var result = await _jobQueue.DequeueAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AckAsync_WithExistingJob_ShouldUpdateJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = NormalizationJob.Create(
            Guid.NewGuid(),
            "REMOVE_DUPLICATE_ROWS",
            "{\"columns\":[\"name\",\"email\"]}");

        _mockRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        // Act
        await _jobQueue.AckAsync(jobId);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(jobId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(job), Times.Once);
    }

    [Fact]
    public async Task AckAsync_WithNonExistentJob_ShouldNotUpdate()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync((NormalizationJob?)null);

        // Act
        await _jobQueue.AckAsync(jobId);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(jobId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<NormalizationJob>()), Times.Never);
    }

    [Fact]
    public async Task NackAsync_WithExistingJob_ShouldFailJobAndUpdate()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = NormalizationJob.Create(
            Guid.NewGuid(),
            "REMOVE_DUPLICATE_ROWS",
            "{\"columns\":[\"name\",\"email\"]}");
        job.Start();

        _mockRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        var reason = "Processing failed";

        // Act
        await _jobQueue.NackAsync(jobId, reason);

        // Assert
        job.Status.Should().Be(JobStatus.Failed);
        job.ErrorMessage.Should().Be(reason);
        _mockRepository.Verify(r => r.UpdateAsync(job), Times.Once);
    }

    [Fact]
    public async Task NackAsync_WithNonExistentJob_ShouldNotUpdate()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync((NormalizationJob?)null);

        // Act
        await _jobQueue.NackAsync(jobId, "test reason");

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(jobId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<NormalizationJob>()), Times.Never);
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new EfCoreJobQueue(null!);
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }
}
