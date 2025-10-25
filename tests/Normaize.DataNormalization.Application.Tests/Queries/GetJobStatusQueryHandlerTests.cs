using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Queries;

/// <summary>
/// Tests for GetJobStatusQueryHandler - demonstrates read operation testing
/// </summary>
public class GetJobStatusQueryHandlerTests
{
    private readonly Mock<INormalizationJobRepository> _mockRepository;
    private readonly GetJobStatusQueryHandler _handler;

    public GetJobStatusQueryHandlerTests()
    {
        _mockRepository = new Mock<INormalizationJobRepository>();
        _handler = new GetJobStatusQueryHandler(_mockRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_WithExistingJob_ShouldReturnJobStatus()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = NormalizationJob.Create(
            Guid.NewGuid(),
            "REMOVE_DUPLICATE_ROWS",
            "{\"columns\":[\"name\",\"email\"]}");

        job.Start();
        job.UpdateProgress(50, "Processing");

        _mockRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        var query = new GetJobStatusQuery(jobId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
        result.DataSetId.Should().Be(job.DataSetId);
        result.OperationType.Should().Be(job.OperationType);
        result.Status.Should().Be(job.Status.ToString());
        result.ProgressPercentage.Should().Be(50);
        result.ProgressMessage.Should().Be("Processing");
        result.CreatedAt.Should().Be(job.CreatedAt);
        result.StartedAt.Should().Be(job.StartedAt);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistentJob_ShouldReturnNull()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync((NormalizationJob?)null);

        var query = new GetJobStatusQuery(jobId);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        result.Should().BeNull();
    }
}
