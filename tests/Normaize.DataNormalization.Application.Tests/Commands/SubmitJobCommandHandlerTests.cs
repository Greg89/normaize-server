using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.Commands;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Repositories;
using Xunit;

namespace Normaize.DataNormalization.Application.Tests.Commands;

/// <summary>
/// Tests for SubmitJobCommandHandler - demonstrates application layer testing with mocks
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
}
