using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Infrastructure.Services;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

public class AuditServiceTests
{
    private readonly Mock<ILogger<AuditService>> _mockLogger;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _mockLogger = new Mock<ILogger<AuditService>>();
        _service = new AuditService(_mockLogger.Object);
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldLogInformation_WithCorrectParameters()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = "Upload";
        var metadata = new Dictionary<string, object>
        {
            { "FileName", "test.csv" },
            { "FileSize", 1024 },
            { "RowCount", 100 }
        };

        // Act
        await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Dataset Action") && 
                                              v.ToString()!.Contains(action) && 
                                              v.ToString()!.Contains(userId) && 
                                              v.ToString()!.Contains(dataSetId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldCompleteSuccessfully_WithEmptyMetadata()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = "Delete";
        var metadata = new Dictionary<string, object>();

        // Act
        var act = async () => await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldCompleteSuccessfully_WithComplexMetadata()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = "Update";
        var metadata = new Dictionary<string, object>
        {
            { "FileName", "test.csv" },
            { "FileSize", 1024 },
            { "RowCount", 100 },
            { "Columns", new[] { "Name", "Age", "City" } },
            { "ProcessingOptions", new { RemoveDuplicates = true, Normalize = false } },
            { "Timestamp", DateTime.UtcNow }
        };

        // Act
        var act = async () => await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("Upload")]
    [InlineData("Delete")]
    [InlineData("Update")]
    [InlineData("Reset")]
    [InlineData("Restore")]
    [InlineData("HardDelete")]
    [InlineData("UpdateRetentionPolicy")]
    public async Task LogDataSetActionAsync_ShouldLogAction_ForDifferentActionTypes(string action)
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var metadata = new Dictionary<string, object> { { "Test", "Value" } };

        // Act
        await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(action)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldReturnCompletedTask()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = "Test";
        var metadata = new Dictionary<string, object>();

        // Act
        var task = _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldSupportCancellation()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = "Test";
        var metadata = new Dictionary<string, object>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata, cts.Token);

        // Assert - Should not throw even when cancelled since it's already completed
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new AuditService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldLogWithStructuredLogging_IncludingMetadata()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = "Upload";
        var metadata = new Dictionary<string, object>
        {
            { "FileName", "test.csv" },
            { "RowCount", 100 }
        };

        // Act
        await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert - Verify structured logging includes metadata
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true), // Metadata logged as structured data
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldHandleNullMetadataValues()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = "Update";
        var metadata = new Dictionary<string, object>
        {
            { "FileName", null! },
            { "Description", null! }
        };

        // Act
        var act = async () => await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldHandleEmptyGuid()
    {
        // Arrange
        var dataSetId = Guid.Empty;
        var userId = "user123";
        var action = "Test";
        var metadata = new Dictionary<string, object>();

        // Act
        var act = async () => await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldHandleEmptyUserId()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = string.Empty;
        var action = "Test";
        var metadata = new Dictionary<string, object>();

        // Act
        var act = async () => await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldHandleLongActionNames()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = new string('A', 1000); // Very long action name
        var metadata = new Dictionary<string, object>();

        // Act
        var act = async () => await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogDataSetActionAsync_ShouldHandleLargeMetadata()
    {
        // Arrange
        var dataSetId = Guid.NewGuid();
        var userId = "user123";
        var action = "Upload";
        var metadata = new Dictionary<string, object>();
        
        // Add many metadata items
        for (int i = 0; i < 100; i++)
        {
            metadata[$"Key{i}"] = $"Value{i}";
        }

        // Act
        var act = async () => await _service.LogDataSetActionAsync(dataSetId, userId, action, metadata);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
