using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Handlers;
using Normaize.DataNormalization.Infrastructure.Services;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Infrastructure.Tests.Handlers;

public class RemoveDuplicatesHandlerTests
{
    private readonly Mock<IDataSetDataLoader> _dataLoaderMock;
    private readonly Mock<IDataSetDataPersister> _dataPersisterMock;
    private readonly Mock<IDuplicateRemovalProcessor> _duplicateProcessorMock;
    private readonly Mock<ILogger<RemoveDuplicatesHandler>> _loggerMock;
    private readonly Mock<IJobProgress> _progressMock;
    private readonly RemoveDuplicatesHandler _handler;
    private readonly Guid _testJobId = Guid.NewGuid();
    private readonly Guid _testDataSetId = Guid.NewGuid();

    public RemoveDuplicatesHandlerTests()
    {
        _dataLoaderMock = new Mock<IDataSetDataLoader>();
        _dataPersisterMock = new Mock<IDataSetDataPersister>();
        _duplicateProcessorMock = new Mock<IDuplicateRemovalProcessor>();
        _loggerMock = new Mock<ILogger<RemoveDuplicatesHandler>>();
        _progressMock = new Mock<IJobProgress>();
        
        _handler = new RemoveDuplicatesHandler(
            _dataLoaderMock.Object,
            _dataPersisterMock.Object,
            _duplicateProcessorMock.Object,
            _loggerMock.Object);
    }

    private NormalizationJob CreateTestJob(string? operationParameters = null)
    {
        operationParameters ??= "{\"KeyColumns\":[\"email\"],\"RetentionStrategy\":\"KeepFirst\",\"CaseSensitive\":false}";
        
        return NormalizationJob.Create(
            _testDataSetId,
            "RemoveDuplicates",
            operationParameters);
    }

    private static DataSetData CreateTestDataSetData()
    {
        var columns = new List<DataSetColumn>
        {
            new("id", "int", 0, false),
            new("email", "string", 1, false),
            new("name", "string", 2, true)
        };

        var rows = new List<DataSetRowData>
        {
            new(0, new Dictionary<string, object?> { ["id"] = 1, ["email"] = "john@test.com", ["name"] = "John" }),
            new(1, new Dictionary<string, object?> { ["id"] = 2, ["email"] = "jane@test.com", ["name"] = "Jane" }),
            new(2, new Dictionary<string, object?> { ["id"] = 3, ["email"] = "john@test.com", ["name"] = "John Doe" }),
            new(3, new Dictionary<string, object?> { ["id"] = 4, ["email"] = "bob@test.com", ["name"] = "Bob" })
        };

        return new DataSetData(columns, rows);
    }

    private static DuplicateRemovalResult CreateTestDuplicateRemovalResult()
    {
        var processedData = CreateTestDataSetData();
        // Remove one duplicate row for the result
        var deduplicatedRows = processedData.Rows.Take(3).ToList();
        var deduplicatedData = new DataSetData(processedData.Columns, deduplicatedRows);

        return new DuplicateRemovalResult(
            deduplicatedData,
            4, // originalRowCount
            1, // duplicatesRemoved
            TimeSpan.FromMilliseconds(100), // processingTime
            new[] { "email" }, // processedColumns
            CaseSensitivity.Insensitive,
            RetentionStrategy.First
        );
    }

    [Fact]
    public async Task HandleAsync_WithValidJob_ShouldCompleteSuccessfully()
    {
        // Arrange
        var job = CreateTestJob();
        var testData = CreateTestDataSetData();
        var duplicateResult = CreateTestDuplicateRemovalResult();
        var backupId = "backup_test_123";

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ReturnsAsync(testData);
        _dataPersisterMock.Setup(x => x.CreateBackupAsync(_testDataSetId))
            .ReturnsAsync(backupId);
        _duplicateProcessorMock.Setup(x => x.RemoveDuplicatesAsync(
                It.IsAny<DataSetData>(), 
                It.IsAny<DuplicateRemovalOptions>(), 
                It.IsAny<IProgress<DuplicateRemovalProgress>>()))
            .ReturnsAsync(duplicateResult);
        _dataPersisterMock.Setup(x => x.SaveProcessedDataAsync(_testDataSetId, duplicateResult.ProcessedData, "RemoveDuplicates"))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(job, _progressMock.Object);

        // Assert
        _progressMock.Verify(x => x.StartedAsync(job.Id), Times.Once);
        _progressMock.Verify(x => x.SucceededAsync(job.Id, It.IsAny<object>()), Times.Once);
        _dataPersisterMock.Verify(x => x.SaveProcessedDataAsync(_testDataSetId, duplicateResult.ProcessedData, "RemoveDuplicates"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldReportProgressAtCorrectStages()
    {
        // Arrange
        var job = CreateTestJob();
        var testData = CreateTestDataSetData();
        var duplicateResult = CreateTestDuplicateRemovalResult();

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ReturnsAsync(testData);
        _dataPersisterMock.Setup(x => x.CreateBackupAsync(_testDataSetId))
            .ReturnsAsync("backup_test");
        _duplicateProcessorMock.Setup(x => x.RemoveDuplicatesAsync(
                It.IsAny<DataSetData>(), 
                It.IsAny<DuplicateRemovalOptions>(), 
                It.IsAny<IProgress<DuplicateRemovalProgress>>()))
            .ReturnsAsync(duplicateResult);
        _dataPersisterMock.Setup(x => x.SaveProcessedDataAsync(It.IsAny<Guid>(), It.IsAny<DataSetData>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(job, _progressMock.Object);

        // Assert
        _progressMock.Verify(x => x.ReportAsync(job.Id, 5, "Loading dataset data"), Times.Once);
        _progressMock.Verify(x => x.ReportAsync(job.Id, 10, "Validating duplicate removal options"), Times.Once);
        _progressMock.Verify(x => x.ReportAsync(job.Id, 15, "Creating backup of original data"), Times.Once);
        _progressMock.Verify(x => x.ReportAsync(job.Id, 15, "Starting duplicate detection and removal"), Times.Once);
        _progressMock.Verify(x => x.ReportAsync(job.Id, 90, "Saving processed data"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldValidateKeyColumns()
    {
        // Arrange
        var invalidParams = "{\"KeyColumns\":[\"nonexistent\"],\"RetentionStrategy\":\"KeepFirst\",\"CaseSensitive\":false}";
        var job = CreateTestJob(invalidParams);
        var testData = CreateTestDataSetData();

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ReturnsAsync(testData);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.HandleAsync(job, _progressMock.Object));
        
        Assert.Contains("key columns were not found", exception.Message);
        Assert.Contains("nonexistent", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_WhenDataLoadingFails_ShouldFailGracefully()
    {
        // Arrange
        var job = CreateTestJob();
        var expectedException = new InvalidOperationException("Data loading failed");

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(job, _progressMock.Object));

        Assert.Equal(expectedException, exception);
        _progressMock.Verify(x => x.FailedAsync(job.Id, "Data loading failed"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenSavingFails_ShouldThrowException()
    {
        // Arrange
        var job = CreateTestJob();
        var testData = CreateTestDataSetData();
        var duplicateResult = CreateTestDuplicateRemovalResult();

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ReturnsAsync(testData);
        _dataPersisterMock.Setup(x => x.CreateBackupAsync(_testDataSetId))
            .ReturnsAsync("backup_test");
        _duplicateProcessorMock.Setup(x => x.RemoveDuplicatesAsync(
                It.IsAny<DataSetData>(), 
                It.IsAny<DuplicateRemovalOptions>(), 
                It.IsAny<IProgress<DuplicateRemovalProgress>>()))
            .ReturnsAsync(duplicateResult);
        _dataPersisterMock.Setup(x => x.SaveProcessedDataAsync(It.IsAny<Guid>(), It.IsAny<DataSetData>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.HandleAsync(job, _progressMock.Object));
        
        Assert.Contains("Failed to save processed data", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateBackupBeforeProcessing()
    {
        // Arrange
        var job = CreateTestJob();
        var testData = CreateTestDataSetData();
        var duplicateResult = CreateTestDuplicateRemovalResult();
        var backupId = "backup_test_123";

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ReturnsAsync(testData);
        _dataPersisterMock.Setup(x => x.CreateBackupAsync(_testDataSetId))
            .ReturnsAsync(backupId);
        _duplicateProcessorMock.Setup(x => x.RemoveDuplicatesAsync(
                It.IsAny<DataSetData>(), 
                It.IsAny<DuplicateRemovalOptions>(), 
                It.IsAny<IProgress<DuplicateRemovalProgress>>()))
            .ReturnsAsync(duplicateResult);
        _dataPersisterMock.Setup(x => x.SaveProcessedDataAsync(It.IsAny<Guid>(), It.IsAny<DataSetData>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(job, _progressMock.Object);

        // Assert
        _dataPersisterMock.Verify(x => x.CreateBackupAsync(_testDataSetId), Times.Once);
        
        // Verify backup is included in success result
        _progressMock.Verify(x => x.SucceededAsync(job.Id, 
            It.Is<object>(result => result.ToString()!.Contains(backupId))), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldLogAppropriateMessages()
    {
        // Arrange
        var job = CreateTestJob();
        var testData = CreateTestDataSetData();
        var duplicateResult = CreateTestDuplicateRemovalResult();

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ReturnsAsync(testData);
        _dataPersisterMock.Setup(x => x.CreateBackupAsync(_testDataSetId))
            .ReturnsAsync("backup_test");
        _duplicateProcessorMock.Setup(x => x.RemoveDuplicatesAsync(
                It.IsAny<DataSetData>(), 
                It.IsAny<DuplicateRemovalOptions>(), 
                It.IsAny<IProgress<DuplicateRemovalProgress>>()))
            .ReturnsAsync(duplicateResult);
        _dataPersisterMock.Setup(x => x.SaveProcessedDataAsync(It.IsAny<Guid>(), It.IsAny<DataSetData>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(job, _progressMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting duplicate removal")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loaded dataset with")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Duplicate removal completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeCompleteResultSummary()
    {
        // Arrange
        var job = CreateTestJob();
        var testData = CreateTestDataSetData();
        var duplicateResult = CreateTestDuplicateRemovalResult();
        var backupId = "backup_test_20231025_123456";

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ReturnsAsync(testData);
        _dataPersisterMock.Setup(x => x.CreateBackupAsync(_testDataSetId))
            .ReturnsAsync(backupId);
        _duplicateProcessorMock.Setup(x => x.RemoveDuplicatesAsync(
                It.IsAny<DataSetData>(), 
                It.IsAny<DuplicateRemovalOptions>(), 
                It.IsAny<IProgress<DuplicateRemovalProgress>>()))
            .ReturnsAsync(duplicateResult);
        _dataPersisterMock.Setup(x => x.SaveProcessedDataAsync(It.IsAny<Guid>(), It.IsAny<DataSetData>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        await _handler.HandleAsync(job, _progressMock.Object);

        // Assert
        _progressMock.Verify(x => x.SucceededAsync(job.Id, It.Is<object>(result => 
            result.ToString()!.Contains("OriginalRowCount") &&
            result.ToString()!.Contains("DuplicatesRemoved") &&
            result.ToString()!.Contains("FinalRowCount") &&
            result.ToString()!.Contains("KeyColumns") &&
            result.ToString()!.Contains("RetentionStrategy") &&
            result.ToString()!.Contains("CaseSensitivity") &&
            result.ToString()!.Contains("ProcessingTimeMs") &&
            result.ToString()!.Contains(backupId))), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new RemoveDuplicatesHandler(null!, _dataPersisterMock.Object, _duplicateProcessorMock.Object, _loggerMock.Object));
        
        Assert.Throws<ArgumentNullException>(() => 
            new RemoveDuplicatesHandler(_dataLoaderMock.Object, null!, _duplicateProcessorMock.Object, _loggerMock.Object));
        
        Assert.Throws<ArgumentNullException>(() => 
            new RemoveDuplicatesHandler(_dataLoaderMock.Object, _dataPersisterMock.Object, null!, _loggerMock.Object));
        
        Assert.Throws<ArgumentNullException>(() => 
            new RemoveDuplicatesHandler(_dataLoaderMock.Object, _dataPersisterMock.Object, _duplicateProcessorMock.Object, null!));
    }

    [Fact]
    public async Task HandleAsync_WithCaseInsensitiveColumns_ShouldValidateCorrectly()
    {
        // Arrange
        var params1 = "{\"KeyColumns\":[\"EMAIL\"],\"RetentionStrategy\":\"KeepFirst\",\"CaseSensitive\":false}";
        var job = CreateTestJob(params1);
        var testData = CreateTestDataSetData(); // Has "email" column
        var duplicateResult = CreateTestDuplicateRemovalResult();

        _dataLoaderMock.Setup(x => x.LoadDataSetDataAsync(_testDataSetId))
            .ReturnsAsync(testData);
        _dataPersisterMock.Setup(x => x.CreateBackupAsync(_testDataSetId))
            .ReturnsAsync("backup_test");
        _duplicateProcessorMock.Setup(x => x.RemoveDuplicatesAsync(
                It.IsAny<DataSetData>(), 
                It.IsAny<DuplicateRemovalOptions>(), 
                It.IsAny<IProgress<DuplicateRemovalProgress>>()))
            .ReturnsAsync(duplicateResult);
        _dataPersisterMock.Setup(x => x.SaveProcessedDataAsync(It.IsAny<Guid>(), It.IsAny<DataSetData>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act & Assert - Should not throw (case insensitive validation)
        await _handler.HandleAsync(job, _progressMock.Object);
        
        _progressMock.Verify(x => x.SucceededAsync(job.Id, It.IsAny<object>()), Times.Once);
    }
}