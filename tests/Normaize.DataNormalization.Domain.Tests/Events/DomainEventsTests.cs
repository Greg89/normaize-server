using FluentAssertions;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Events;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.Events;

/// <summary>
/// Tests for domain events - demonstrates event testing patterns
/// </summary>
public class DomainEventsTests
{
    [Fact]
    public void JobCreated_ShouldHaveCorrectProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var dataSetId = Guid.NewGuid();
        var operationType = "REMOVE_DUPLICATE_ROWS";

        // Act
        var @event = new JobCreated(jobId, dataSetId, operationType);

        // Assert
        @event.JobId.Should().Be(jobId);
        @event.DataSetId.Should().Be(dataSetId);
        @event.OperationType.Should().Be(operationType);
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void JobStarted_ShouldHaveCorrectProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var dataSetId = Guid.NewGuid();
        var operationType = "REMOVE_DUPLICATE_ROWS";

        // Act
        var @event = new JobStarted(jobId, dataSetId, operationType);

        // Assert
        @event.JobId.Should().Be(jobId);
        @event.DataSetId.Should().Be(dataSetId);
        @event.OperationType.Should().Be(operationType);
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void JobProgressUpdated_ShouldHaveCorrectProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var percentage = 75;
        var message = "Processing row 1000";

        // Act
        var @event = new JobProgressUpdated(jobId, percentage, message);

        // Assert
        @event.JobId.Should().Be(jobId);
        @event.Percentage.Should().Be(percentage);
        @event.Message.Should().Be(message);
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void JobCompleted_ShouldHaveCorrectProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var result = "{\"rowsRemoved\": 10}";

        // Act
        var @event = new JobCompleted(jobId, result);

        // Assert
        @event.JobId.Should().Be(jobId);
        @event.Result.Should().Be(result);
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void JobFailed_ShouldHaveCorrectProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var error = "Processing failed";
        var retryCount = 2;

        // Act
        var @event = new JobFailed(jobId, error, retryCount);

        // Assert
        @event.JobId.Should().Be(jobId);
        @event.Error.Should().Be(error);
        @event.RetryCount.Should().Be(retryCount);
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void JobMovedToDeadLetter_ShouldHaveCorrectProperties()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var reason = "Too many failures";

        // Act
        var @event = new JobMovedToDeadLetter(jobId, reason);

        // Assert
        @event.JobId.Should().Be(jobId);
        @event.Reason.Should().Be(reason);
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AllEvents_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var jobId = Guid.NewGuid();
        var dataSetId = Guid.NewGuid();

        var events = new IDomainEvent[]
        {
            new JobCreated(jobId, dataSetId, "test"),
            new JobStarted(jobId, dataSetId, "test"),
            new JobProgressUpdated(jobId, 50, "test"),
            new JobCompleted(jobId, "result"),
            new JobFailed(jobId, "error", 1),
            new JobMovedToDeadLetter(jobId, "reason")
        };

        // Assert
        events.Should().AllSatisfy(e => e.Should().BeAssignableTo<IDomainEvent>());
        events.Should().AllSatisfy(e => e.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1)));
    }
}
