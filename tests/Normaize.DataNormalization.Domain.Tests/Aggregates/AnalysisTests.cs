using System;
using System.Linq;
using FluentAssertions;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Events;
using Normaize.DataNormalization.Domain.ValueObjects;
using Xunit;

namespace Normaize.DataNormalization.Domain.Tests.Aggregates;

/// <summary>
/// Unit tests for Analysis aggregate
/// </summary>
public class AnalysisTests
{
    private readonly Guid _testDataSetId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidParameters_ShouldCreateAnalysis()
    {
        // Arrange
        var name = "Test Analysis";
        var description = "Test Description";
        var type = AnalysisType.Statistical;
        var configuration = new AnalysisConfiguration("{\"param1\": \"value1\"}");

        // Act
        var analysis = Analysis.Create(name, description, type, _testDataSetId, null, configuration);

        // Assert
        analysis.Should().NotBeNull();
        analysis.Name.Should().Be(name);
        analysis.Description.Should().Be(description);
        analysis.Type.Should().Be(type);
        analysis.Status.Should().Be(AnalysisStatus.Pending);
        analysis.DataSetId.Should().Be(_testDataSetId);
        analysis.ComparisonDataSetId.Should().BeNull();
        analysis.Configuration.Should().Be(configuration);
        analysis.Result.Should().BeNull();
        analysis.ErrorMessage.Should().BeNull();
        analysis.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        analysis.StartedAt.Should().BeNull();
        analysis.CompletedAt.Should().BeNull();
        analysis.IsDeleted.Should().BeFalse();
        analysis.DomainEvents.Should().HaveCount(1);
        analysis.DomainEvents.First().Should().BeOfType<AnalysisCreated>();
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Analysis.Create(null!, "Description", AnalysisType.Statistical, _testDataSetId);
        act.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Analysis.Create("", "Description", AnalysisType.Statistical, _testDataSetId);
        act.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Create_WithEmptyDataSetId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Analysis.Create("Test", "Description", AnalysisType.Statistical, Guid.Empty);
        act.Should().Throw<ArgumentException>().WithMessage("*DataSet*");
    }

    [Fact]
    public void Start_WhenPending_ShouldChangeStatusToProcessing()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.ClearDomainEvents(); // Clear creation event

        // Act
        analysis.Start();

        // Assert
        analysis.Status.Should().Be(AnalysisStatus.Processing);
        analysis.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        analysis.DomainEvents.Should().HaveCount(1);
        analysis.DomainEvents.First().Should().BeOfType<AnalysisStarted>();
    }

    [Fact]
    public void Start_WhenNotPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.Start(); // Move to Processing

        // Act & Assert
        var act = () => analysis.Start();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot start analysis in * status*");
    }

    [Fact]
    public void Complete_WhenProcessing_ShouldChangeStatusToCompleted()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.Start();
        analysis.ClearDomainEvents(); // Clear previous events
        var result = AnalysisResult.FromObject(new { test = "data" });

        // Act
        analysis.Complete(result);

        // Assert
        analysis.Status.Should().Be(AnalysisStatus.Completed);
        analysis.Result.Should().Be(result);
        analysis.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        analysis.ErrorMessage.Should().BeNull();
        analysis.DomainEvents.Should().HaveCount(1);
        analysis.DomainEvents.First().Should().BeOfType<AnalysisCompleted>();
    }

    [Fact]
    public void Complete_WhenNotProcessing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        var result = AnalysisResult.FromObject(new { test = "data" });

        // Act & Assert
        var act = () => analysis.Complete(result);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot complete analysis in * status*");
    }

    [Fact]
    public void Fail_WhenProcessing_ShouldChangeStatusToFailed()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.Start();
        analysis.ClearDomainEvents(); // Clear previous events
        var errorMessage = "Test error";

        // Act
        analysis.Fail(errorMessage);

        // Assert
        analysis.Status.Should().Be(AnalysisStatus.Failed);
        analysis.ErrorMessage.Should().Be(errorMessage);
        analysis.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        analysis.Result.Should().BeNull();
        analysis.DomainEvents.Should().HaveCount(1);
        analysis.DomainEvents.First().Should().BeOfType<AnalysisFailed>();
    }

    [Fact]
    public void Fail_WhenNotProcessing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);

        // Act & Assert
        var act = () => analysis.Fail("Error");
        act.Should().Throw<InvalidOperationException>().WithMessage("*Cannot fail analysis in * status*");
    }

    [Fact]
    public void Delete_WhenNotDeleted_ShouldMarkAsDeleted()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.ClearDomainEvents(); // Clear creation event
        var deletedBy = "test-user";

        // Act
        analysis.Delete(deletedBy);

        // Assert
        analysis.IsDeleted.Should().BeTrue();
        analysis.DeletedBy.Should().Be(deletedBy);
        analysis.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        analysis.DomainEvents.Should().HaveCount(1);
        analysis.DomainEvents.First().Should().BeOfType<AnalysisDeleted>();
    }

    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.Delete("user1");

        // Act & Assert
        var act = () => analysis.Delete("user2");
        act.Should().Throw<InvalidOperationException>().WithMessage("*already deleted*");
    }

    [Fact]
    public void Reset_WhenCompleted_ShouldResetToPending()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.Start();
        var result = AnalysisResult.FromObject(new { test = "data" });
        analysis.Complete(result);
        analysis.ClearDomainEvents(); // Clear previous events

        // Act
        analysis.Reset();

        // Assert
        analysis.Status.Should().Be(AnalysisStatus.Pending);
        analysis.Result.Should().BeNull();
        analysis.ErrorMessage.Should().BeNull();
        analysis.StartedAt.Should().BeNull();
        analysis.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Reset_WhenFailed_ShouldResetToPending()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.Start();
        analysis.Fail("Error");
        analysis.ClearDomainEvents(); // Clear previous events

        // Act
        analysis.Reset();

        // Assert
        analysis.Status.Should().Be(AnalysisStatus.Pending);
        analysis.Result.Should().BeNull();
        analysis.ErrorMessage.Should().BeNull();
        analysis.StartedAt.Should().BeNull();
        analysis.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Reset_WhenPending_ShouldNotChangeStatus()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        var originalCreatedAt = analysis.CreatedAt;
        analysis.ClearDomainEvents(); // Clear creation event

        // Act
        analysis.Reset();

        // Assert
        analysis.Status.Should().Be(AnalysisStatus.Pending);
        analysis.CreatedAt.Should().Be(originalCreatedAt); // Should not change
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateNameAndDescription()
    {
        // Arrange
        var analysis = Analysis.Create("Old Name", "Old Description", AnalysisType.Statistical, _testDataSetId);
        var newName = "New Name";
        var newDescription = "New Description";

        // Act
        analysis.UpdateDetails(newName, newDescription);

        // Assert
        analysis.Name.Should().Be(newName);
        analysis.Description.Should().Be(newDescription);
    }

    [Fact]
    public void UpdateConfiguration_WithValidConfiguration_ShouldUpdateConfiguration()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        var newConfiguration = new AnalysisConfiguration("{\"newParam\": \"newValue\"}");

        // Act
        analysis.UpdateConfiguration(newConfiguration);

        // Assert
        analysis.Configuration.Should().Be(newConfiguration);
    }

    [Fact]
    public void ExecutionDuration_WhenStartedAndCompleted_ShouldReturnDuration()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);
        analysis.Start();
        
        // Simulate some processing time
        System.Threading.Thread.Sleep(10);
        
        var result = AnalysisResult.FromObject(new { test = "data" });
        analysis.Complete(result);

        // Act
        var duration = analysis.ExecutionDuration;

        // Assert
        duration.Should().NotBeNull();
        duration.Value.Should().BeGreaterThan(TimeSpan.Zero);
        duration.Value.Should().BeLessThan(TimeSpan.FromSeconds(1)); // Should be very quick
    }

    [Fact]
    public void ExecutionDuration_WhenNotStarted_ShouldReturnNull()
    {
        // Arrange
        var analysis = Analysis.Create("Test", null, AnalysisType.Statistical, _testDataSetId);

        // Act
        var duration = analysis.ExecutionDuration;

        // Assert
        duration.Should().BeNull();
    }
}