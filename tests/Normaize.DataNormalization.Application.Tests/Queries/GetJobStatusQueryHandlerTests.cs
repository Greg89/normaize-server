using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Queries;

/// <summary>
/// Example query handler tests - demonstrates read operation testing
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
        result.Id.Should().Be(job.Id);
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

    [Fact]
    public async Task HandleAsync_WithEmptyJobId_ShouldThrowArgumentException()
    {
        // Arrange
        var query = new GetJobStatusQuery(Guid.Empty);

        // Act & Assert
        var action = async () => await _handler.HandleAsync(query);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*JobId*");
    }
}

/// <summary>
/// Example implementation of GetJobStatusQueryHandler for testing
/// </summary>
public class GetJobStatusQueryHandler : IQueryHandler<GetJobStatusQuery, JobStatusDto?>
{
    private readonly INormalizationJobRepository _repository;

    public GetJobStatusQueryHandler(INormalizationJobRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<JobStatusDto?> HandleAsync(GetJobStatusQuery query)
    {
        if (query.JobId == Guid.Empty)
            throw new ArgumentException("JobId cannot be empty", nameof(query));

        var job = await _repository.GetByIdAsync(query.JobId);
        
        if (job == null)
            return null;

        return new JobStatusDto
        {
            Id = job.Id,
            DataSetId = job.DataSetId,
            OperationType = job.OperationType,
            Status = job.Status.ToString(),
            ProgressPercentage = job.ProgressPercentage,
            ProgressMessage = job.ProgressMessage,
            ErrorMessage = job.ErrorMessage,
            Result = job.Result,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            RetryCount = job.RetryCount,
            MaxRetries = job.MaxRetries
        };
    }
}
