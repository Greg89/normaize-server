using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Application.DTOs;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Application.Queries;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Commands;

/// <summary>
/// Example command handler tests - demonstrates application layer testing with mocks
/// </summary>
public class SubmitJobCommandHandlerTests
{
    private readonly Mock<INormalizationJobRepository> _mockRepository;
    private readonly Mock<IJobQueue> _mockJobQueue;
    private readonly SubmitJobCommandHandler _handler;

    public SubmitJobCommandHandlerTests()
    {
        _mockRepository = new Mock<INormalizationJobRepository>();
        _mockJobQueue = new Mock<IJobQueue>();
        _handler = new SubmitJobCommandHandler(_mockRepository.Object, _mockJobQueue.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_ShouldCreateAndEnqueueJob()
    {
        // Arrange
        var command = new SubmitJobCommand(
            Guid.NewGuid(),
            "REMOVE_DUPLICATE_ROWS",
            "{\"columns\":[\"name\",\"email\"]}");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        result.Should().NotBeEmpty();
        
        _mockRepository.Verify(r => r.SaveAsync(It.Is<NormalizationJob>(j =>
            j.DataSetId == command.DataSetId &&
            j.OperationType == command.OperationType &&
            j.OperationParameters == command.OperationParameters &&
            j.Status == JobStatus.Queued
        )), Times.Once);

        _mockJobQueue.Verify(q => q.EnqueueAsync(It.IsAny<NormalizationJob>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidDataSetId_ShouldThrowArgumentException()
    {
        // Arrange
        var command = new SubmitJobCommand(
            Guid.Empty,
            "REMOVE_DUPLICATE_ROWS",
            "{\"columns\":[\"name\",\"email\"]}");

        // Act & Assert
        var action = async () => await _handler.HandleAsync(command);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*DataSetId*");
    }
}

/// <summary>
/// Example implementation of SubmitJobCommandHandler for testing
/// </summary>
public class SubmitJobCommandHandler : ICommandHandler<SubmitJobCommand, Guid>
{
    private readonly INormalizationJobRepository _repository;
    private readonly IJobQueue _jobQueue;

    public SubmitJobCommandHandler(INormalizationJobRepository repository, IJobQueue jobQueue)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _jobQueue = jobQueue ?? throw new ArgumentNullException(nameof(jobQueue));
    }

    public async Task<Guid> HandleAsync(SubmitJobCommand command)
    {
        if (command.DataSetId == Guid.Empty)
            throw new ArgumentException("DataSetId cannot be empty", nameof(command));

        var job = NormalizationJob.Create(
            command.DataSetId,
            command.OperationType,
            command.OperationParameters);

        await _repository.SaveAsync(job);
        await _jobQueue.EnqueueAsync(job);

        return job.Id;
    }
}
