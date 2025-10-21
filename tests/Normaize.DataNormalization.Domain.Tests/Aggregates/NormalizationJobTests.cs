using FluentAssertions;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Events;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.Aggregates;

/// <summary>
/// Tests for NormalizationJob aggregate - demonstrates pure domain testing
/// </summary>
public class NormalizationJobTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateJobWithCorrectProperties()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var operationType = "REMOVE_DUPLICATE_ROWS";
        var operationParameters = "{\"columns\":[\"name\",\"email\"]}";

        // Act
        var job = NormalizationJob.Create(dataSetId, operationType, operationParameters);

        // Assert
        job.Should().NotBeNull();
        job.Id.Should().NotBeEmpty();
        job.DataSetId.Should().Be(dataSetId);
        job.OperationType.Should().Be(operationType);
        job.OperationParameters.Should().Be(operationParameters);
        job.Status.Should().Be(JobStatus.Queued);
        job.RetryCount.Should().Be(0);
        job.MaxRetries.Should().Be(5);
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.ProgressPercentage.Should().Be(0);
        
        // Domain events
        job.DomainEvents.Should().HaveCount(1);
        job.DomainEvents.Should().ContainSingle(e => e is JobCreated);
    }

    [Fact]
    public void Start_WhenQueued_ShouldTransitionToProcessing()
    {
        // Arrange
        var job = CreateValidJob();

        // Act
        job.Start();

        // Assert
        job.Status.Should().Be(JobStatus.Processing);
        job.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.DomainEvents.Should().HaveCount(2); // JobCreated + JobStarted
        job.DomainEvents.Should().ContainSingle(e => e is JobStarted);
    }

    [Fact]
    public void Start_WhenNotQueued_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var job = CreateValidJob();
        job.Start(); // Already processing

        // Act & Assert
        var action = () => job.Start();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Processing*");
    }

    [Fact]
    public void UpdateProgress_WhenProcessing_ShouldUpdateProgressAndEmitEvent()
    {
        // Arrange
        var job = CreateValidJob();
        job.Start();

        // Act
        job.UpdateProgress(50, "Processing rows");

        // Assert
        job.ProgressPercentage.Should().Be(50);
        job.ProgressMessage.Should().Be("Processing rows");
        job.DomainEvents.Should().ContainSingle(e => e is JobProgressUpdated);
    }

    [Fact]
    public void UpdateProgress_WhenNotProcessing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var job = CreateValidJob(); // Still queued

        // Act & Assert
        var action = () => job.UpdateProgress(50, "Processing");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Queued*");
    }

    [Fact]
    public void UpdateProgress_ShouldClampPercentageToValidRange()
    {
        // Arrange
        var job = CreateValidJob();
        job.Start();

        // Act & Assert
        job.UpdateProgress(-10, "test");
        job.ProgressPercentage.Should().Be(0);

        job.UpdateProgress(150, "test");
        job.ProgressPercentage.Should().Be(100);
    }

    [Fact]
    public void Complete_WhenProcessing_ShouldTransitionToSucceeded()
    {
        // Arrange
        var job = CreateValidJob();
        job.Start();
        var result = "{\"rowsRemoved\": 10}";

        // Act
        job.Complete(result);

        // Assert
        job.Status.Should().Be(JobStatus.Succeeded);
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.ProgressPercentage.Should().Be(100);
        job.Result.Should().Be(result);
        job.DomainEvents.Should().ContainSingle(e => e is JobCompleted);
    }

    [Fact]
    public void Fail_WhenProcessing_ShouldTransitionToFailed()
    {
        // Arrange
        var job = CreateValidJob();
        job.Start();
        var errorMessage = "Processing failed";

        // Act
        job.Fail(errorMessage);

        // Assert
        job.Status.Should().Be(JobStatus.Failed);
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.ErrorMessage.Should().Be(errorMessage);
        job.DomainEvents.Should().ContainSingle(e => e is JobFailed);
    }

    [Fact]
    public void ScheduleRetry_WhenFailed_ShouldResetJobForRetry()
    {
        // Arrange
        var job = CreateValidJob();
        job.Start();
        job.Fail("Test error");

        // Act
        job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5));

        // Assert
        job.Status.Should().Be(JobStatus.Queued);
        job.RetryCount.Should().Be(1);
        job.StartedAt.Should().BeNull();
        job.CompletedAt.Should().BeNull();
        job.ErrorMessage.Should().BeNull();
        job.ProgressPercentage.Should().Be(0);
        job.ProgressMessage.Should().BeNull();
    }

    [Fact]
    public void ScheduleRetry_WhenNotFailed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var job = CreateValidJob(); // Still queued

        // Act & Assert
        var action = () => job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5));
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Queued*");
    }

    [Fact]
    public void ScheduleRetry_WhenMaxRetriesExceeded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var job = CreateValidJob();
        // Note: We can't directly set MaxRetries and RetryCount as they're private setters
        // This test demonstrates the business rule validation
        job.Start();
        job.Fail("Test error");
        job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5)); // Retry 1
        job.Start();
        job.Fail("Test error");
        job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5)); // Retry 2
        job.Start();
        job.Fail("Test error");
        job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5)); // Retry 3
        job.Start();
        job.Fail("Test error");
        job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5)); // Retry 4
        job.Start();
        job.Fail("Test error");
        job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5)); // Retry 5
        job.Start();
        job.Fail("Test error");

        // Act & Assert
        var action = () => job.ScheduleRetry(DateTime.UtcNow.AddMinutes(5));
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Maximum retry count exceeded*");
    }

    [Fact]
    public void MoveToDeadLetter_ShouldTransitionToDeadLettered()
    {
        // Arrange
        var job = CreateValidJob();
        var reason = "Too many failures";

        // Act
        job.MoveToDeadLetter(reason);

        // Assert
        job.Status.Should().Be(JobStatus.DeadLettered);
        job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        job.ErrorMessage.Should().Be(reason);
        job.DomainEvents.Should().ContainSingle(e => e is JobMovedToDeadLetter);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var job = CreateValidJob();
        job.Start();
        job.UpdateProgress(50, "test");

        // Act
        job.ClearDomainEvents();

        // Assert
        job.DomainEvents.Should().BeEmpty();
    }

    private static NormalizationJob CreateValidJob()
    {
        return NormalizationJob.Create(
            Guid.NewGuid(),
            "REMOVE_DUPLICATE_ROWS",
            "{\"columns\":[\"name\",\"email\"]}");
    }
}
