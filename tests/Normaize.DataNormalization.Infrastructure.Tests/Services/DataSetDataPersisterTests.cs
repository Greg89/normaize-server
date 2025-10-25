using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Services;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

public class DataSetDataPersisterTests
{
    private readonly Mock<IDataSetRepository> _dataSetRepositoryMock;
    private readonly Mock<IDataSetRowRepository> _rowRepositoryMock;
    private readonly Mock<ILogger<DataSetDataPersister>> _loggerMock;
    private readonly DataSetDataPersister _dataPersister;
    private readonly Guid _testDataSetId = Guid.NewGuid();

    public DataSetDataPersisterTests()
    {
        _dataSetRepositoryMock = new Mock<IDataSetRepository>();
        _rowRepositoryMock = new Mock<IDataSetRowRepository>();
        _loggerMock = new Mock<ILogger<DataSetDataPersister>>();
        _dataPersister = new DataSetDataPersister(_dataSetRepositoryMock.Object, _rowRepositoryMock.Object, _loggerMock.Object);
    }

    private static DataSet CreateTestDataSet()
    {
        var fileInfo = FileMetadata.Create("test.csv", "/uploads/test.csv", FileType.CSV, 1024, "hash123");
        var statistics = DatasetStatistics.Create(100, 5);
        return DataSet.Create("Test Dataset", "Test description", "user123", fileInfo, statistics);
    }

    private static DataSetData CreateTestDataSetData(int rowCount = 3)
    {
        var columns = new List<DataSetColumn>
        {
            new("Name", "string", 0, true),
            new("Age", "int", 1, false),
            new("Email", "string", 2, true)
        };

        var rows = new List<DataSetRowData>();
        for (int i = 0; i < rowCount; i++)
        {
            var values = new Dictionary<string, object?>
            {
                ["Name"] = $"Person {i}",
                ["Age"] = 20 + i,
                ["Email"] = $"person{i}@example.com"
            };
            rows.Add(new DataSetRowData(i, values));
        }

        return new DataSetData(columns, rows);
    }

    [Fact]
    public async Task SaveProcessedDataAsync_WithValidData_ShouldSaveSuccessfully()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        var processedData = CreateTestDataSetData(5);
        var operation = "RemoveDuplicates";

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _dataSetRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<DataSet>()))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.DeleteByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(true);
        _rowRepositoryMock.Setup(x => x.SaveRangeAsync(It.IsAny<IEnumerable<DataSetRow>>()))
            .ReturnsAsync(new List<DataSetRow>());

        // Act
        var result = await _dataPersister.SaveProcessedDataAsync(_testDataSetId, processedData, operation);

        // Assert
        Assert.True(result);

        // Verify the correct sequence of operations
        _rowRepositoryMock.Verify(x => x.DeleteByDataSetIdAsync(_testDataSetId), Times.Once);
        _rowRepositoryMock.Verify(x => x.SaveRangeAsync(It.Is<IEnumerable<DataSetRow>>(rows =>
            rows.Count() == 5)), Times.Once);
        _dataSetRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<DataSet>()), Times.Once);
    }

    [Fact]
    public async Task SaveProcessedDataAsync_WithNonExistentDataSet_ShouldReturnFalse()
    {
        // Arrange
        var processedData = CreateTestDataSetData();
        var operation = "TestOperation";

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync((DataSet?)null);

        // Act
        var result = await _dataPersister.SaveProcessedDataAsync(_testDataSetId, processedData, operation);

        // Assert
        Assert.False(result);

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to save processed data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveProcessedDataAsync_WhenDeleteFails_ShouldReturnFalse()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        var processedData = CreateTestDataSetData();
        var operation = "TestOperation";

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.DeleteByDataSetIdAsync(_testDataSetId))
            .ThrowsAsync(new InvalidOperationException("Delete failed"));

        // Act
        var result = await _dataPersister.SaveProcessedDataAsync(_testDataSetId, processedData, operation);

        // Assert
        Assert.False(result);

        // Verify error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to save processed data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveProcessedDataAsync_ShouldMarkDataSetAsProcessed()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        var processedData = CreateTestDataSetData();
        var operation = "TestOperation";

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _dataSetRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<DataSet>()))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.DeleteByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(true);
        _rowRepositoryMock.Setup(x => x.SaveRangeAsync(It.IsAny<IEnumerable<DataSetRow>>()))
            .ReturnsAsync(new List<DataSetRow>());

        // Act
        await _dataPersister.SaveProcessedDataAsync(_testDataSetId, processedData, operation);

        // Assert
        _dataSetRepositoryMock.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => ds.IsProcessed)), Times.Once);
    }

    [Fact]
    public async Task SaveProcessedDataAsync_ShouldConvertRowsToEntityRows()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        var processedData = CreateTestDataSetData(2);
        var operation = "TestOperation";

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _dataSetRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<DataSet>()))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.DeleteByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(true);
        _rowRepositoryMock.Setup(x => x.SaveRangeAsync(It.IsAny<IEnumerable<DataSetRow>>()))
            .ReturnsAsync(new List<DataSetRow>());

        // Act
        await _dataPersister.SaveProcessedDataAsync(_testDataSetId, processedData, operation);

        // Assert
        _rowRepositoryMock.Verify(x => x.SaveRangeAsync(It.Is<IEnumerable<DataSetRow>>(rows =>
            rows.All(r => r.DataSetId == _testDataSetId) &&
            rows.Count() == 2)), Times.Once);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldReturnValidBackupId()
    {
        // Arrange
        var originalTime = DateTime.UtcNow;

        // Act
        var backupId = await _dataPersister.CreateBackupAsync(_testDataSetId);

        // Assert
        Assert.NotNull(backupId);
        Assert.StartsWith("backup_", backupId);
        Assert.Contains(_testDataSetId.ToString(), backupId);

        // Should contain a timestamp
        Assert.Matches(@"backup_[a-f0-9\-]+_\d{8}_\d{6}", backupId);
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldLogInformation()
    {
        // Act
        await _dataPersister.CreateBackupAsync(_testDataSetId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Creating backup")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Created backup")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_ShouldReturnTrue()
    {
        // Arrange
        var backupId = "backup_test_20231025_123456";

        // Act
        var result = await _dataPersister.RestoreFromBackupAsync(_testDataSetId, backupId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RestoreFromBackupAsync_ShouldLogInformation()
    {
        // Arrange
        var backupId = "backup_test_20231025_123456";

        // Act
        await _dataPersister.RestoreFromBackupAsync(_testDataSetId, backupId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Restoring dataset")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully restored")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveProcessedDataAsync_ShouldLogProgressInformation()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        var processedData = CreateTestDataSetData(10);
        var operation = "TestOperation";

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _dataSetRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<DataSet>()))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.DeleteByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(true);
        _rowRepositoryMock.Setup(x => x.SaveRangeAsync(It.IsAny<IEnumerable<DataSetRow>>()))
            .ReturnsAsync(new List<DataSetRow>());

        // Act
        await _dataPersister.SaveProcessedDataAsync(_testDataSetId, processedData, operation);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Saving processed data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully saved") && v.ToString()!.Contains("10")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DataSetDataPersister(null!, _rowRepositoryMock.Object, _loggerMock.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new DataSetDataPersister(_dataSetRepositoryMock.Object, null!, _loggerMock.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new DataSetDataPersister(_dataSetRepositoryMock.Object, _rowRepositoryMock.Object, null!));
    }

    [Fact]
    public async Task SaveProcessedDataAsync_WithEmptyData_ShouldHandleGracefully()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        var emptyData = new DataSetData(new List<DataSetColumn>(), new List<DataSetRowData>());
        var operation = "TestOperation";

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _dataSetRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<DataSet>()))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.DeleteByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(true);
        _rowRepositoryMock.Setup(x => x.SaveRangeAsync(It.IsAny<IEnumerable<DataSetRow>>()))
            .ReturnsAsync(new List<DataSetRow>());

        // Act
        var result = await _dataPersister.SaveProcessedDataAsync(_testDataSetId, emptyData, operation);

        // Assert
        Assert.True(result);
        _rowRepositoryMock.Verify(x => x.SaveRangeAsync(It.Is<IEnumerable<DataSetRow>>(rows =>
            !rows.Any())), Times.Once);
    }

    [Fact]
    public async Task SaveProcessedDataAsync_ShouldConvertComplexRowData()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        var complexData = new DataSetData(
            new List<DataSetColumn>
            {
                new("Data", "object", 0, true)
            },
            new List<DataSetRowData>
            {
                new(0, new Dictionary<string, object?>
                {
                    ["nested"] = new { prop = "value", array = new[] { 1, 2, 3 } }
                })
            });

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _dataSetRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<DataSet>()))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.DeleteByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(true);
        _rowRepositoryMock.Setup(x => x.SaveRangeAsync(It.IsAny<IEnumerable<DataSetRow>>()))
            .ReturnsAsync(new List<DataSetRow>());

        // Act
        var result = await _dataPersister.SaveProcessedDataAsync(_testDataSetId, complexData, "TestOperation");

        // Assert
        Assert.True(result);
        _rowRepositoryMock.Verify(x => x.SaveRangeAsync(It.Is<IEnumerable<DataSetRow>>(rows =>
            rows.Count() == 1 && !string.IsNullOrEmpty(rows.First().Data))), Times.Once);
    }
}