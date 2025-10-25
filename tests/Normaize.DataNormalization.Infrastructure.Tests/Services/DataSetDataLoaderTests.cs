using System;
using System.Collections.Generic;
using System.Linq;
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

public class DataSetDataLoaderTests
{
    private readonly Mock<IDataSetRepository> _dataSetRepositoryMock;
    private readonly Mock<IDataSetRowRepository> _rowRepositoryMock;
    private readonly Mock<ILogger<DataSetDataLoader>> _loggerMock;
    private readonly DataSetDataLoader _dataLoader;
    private readonly Guid _testDataSetId = Guid.NewGuid();

    public DataSetDataLoaderTests()
    {
        _dataSetRepositoryMock = new Mock<IDataSetRepository>();
        _rowRepositoryMock = new Mock<IDataSetRowRepository>();
        _loggerMock = new Mock<ILogger<DataSetDataLoader>>();
        _dataLoader = new DataSetDataLoader(_dataSetRepositoryMock.Object, _rowRepositoryMock.Object, _loggerMock.Object);
    }

    private static DataSet CreateTestDataSet(int rowCount = 100, int columnCount = 5)
    {
        var fileInfo = FileMetadata.Create("test.csv", "/uploads/test.csv", FileType.CSV, 1024, "hash123");
        var statistics = DatasetStatistics.Create(rowCount, columnCount);
        return DataSet.Create("Test Dataset", "Test description", "user123", fileInfo, statistics);
    }

    private static List<DataSetRow> CreateTestRows(Guid dataSetId, int count)
    {
        var rows = new List<DataSetRow>();
        for (int i = 0; i < count; i++)
        {
            var data = $"{{\"col1\": \"value{i}\", \"col2\": \"data{i}\", \"col3\": \"item{i}\"}}";
            rows.Add(DataSetRow.Create(dataSetId, i, data));
        }
        return rows;
    }

    [Fact]
    public async Task LoadDataSetDataAsync_WithValidDataSet_ShouldReturnCompleteData()
    {
        // Arrange
        var dataSet = CreateTestDataSet(50, 3);
        var rows = CreateTestRows(_testDataSetId, 50);

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.GetByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(rows);

        // Act
        var result = await _dataLoader.LoadDataSetDataAsync(_testDataSetId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalColumns);
        Assert.Equal(50, result.TotalRows);
        Assert.Equal(50, result.Rows.Count);

        // Verify columns were created
        Assert.Equal(3, result.Columns.Count);
        Assert.Equal("Column1", result.Columns[0].Name);
        Assert.Equal("Column2", result.Columns[1].Name);
        Assert.Equal("Column3", result.Columns[2].Name);

        // Verify row data
        Assert.Equal(0, result.Rows[0].RowIndex);
        Assert.Equal(49, result.Rows[49].RowIndex);
    }

    [Fact]
    public async Task LoadDataSetDataAsync_WithNonExistentDataSet_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync((DataSet?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dataLoader.LoadDataSetDataAsync(_testDataSetId));
        
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task LoadDataSetSampleAsync_WithValidDataSet_ShouldReturnLimitedData()
    {
        // Arrange
        var dataSet = CreateTestDataSet(1000, 5);
        var allRows = CreateTestRows(_testDataSetId, 1000);
        var sampleRows = allRows.Take(100).ToList(); // First 100 rows

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.GetByDataSetIdAsync(_testDataSetId, 0, 1000))
            .ReturnsAsync(sampleRows);

        // Act
        var result = await _dataLoader.LoadDataSetSampleAsync(_testDataSetId, maxRows: 1000);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalColumns);
        Assert.Equal(100, result.TotalRows);
        Assert.Equal(100, result.Rows.Count);
    }

    [Fact]
    public async Task LoadDataSetSampleAsync_WithCustomMaxRows_ShouldRespectLimit()
    {
        // Arrange
        var dataSet = CreateTestDataSet(1000, 3);
        var sampleRows = CreateTestRows(_testDataSetId, 50); // Return 50 rows

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.GetByDataSetIdAsync(_testDataSetId, 0, 50))
            .ReturnsAsync(sampleRows);

        // Act
        var result = await _dataLoader.LoadDataSetSampleAsync(_testDataSetId, maxRows: 50);

        // Assert
        Assert.Equal(50, result.TotalRows);
        Assert.Equal(50, result.Rows.Count);
        
        // Verify the correct repository method was called with correct parameters
        _rowRepositoryMock.Verify(x => x.GetByDataSetIdAsync(_testDataSetId, 0, 50), Times.Once);
    }

    [Fact]
    public async Task GetDataSetColumnsAsync_WithValidDataSet_ShouldReturnColumnMetadata()
    {
        // Arrange
        var dataSet = CreateTestDataSet(100, 7);

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);

        // Act
        var result = await _dataLoader.GetDataSetColumnsAsync(_testDataSetId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(7, result.Count);
        
        for (int i = 0; i < 7; i++)
        {
            Assert.Equal($"Column{i + 1}", result[i].Name);
            Assert.Equal("string", result[i].DataType);
            Assert.Equal(i, result[i].Index);
            Assert.True(result[i].AllowNull);
        }
    }

    [Fact]
    public async Task GetDataSetColumnsAsync_WithNonExistentDataSet_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync((DataSet?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dataLoader.GetDataSetColumnsAsync(_testDataSetId));
        
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task LoadDataSetDataAsync_ShouldLogAppropriateMessages()
    {
        // Arrange
        var dataSet = CreateTestDataSet(10, 2);
        var rows = CreateTestRows(_testDataSetId, 10);

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.GetByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(rows);

        // Act
        await _dataLoader.LoadDataSetDataAsync(_testDataSetId);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Loading complete dataset data for")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadDataSetDataAsync_WhenRepositoryThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database error");
        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dataLoader.LoadDataSetDataAsync(_testDataSetId));

        Assert.Equal(expectedException, exception);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to load dataset data")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DataLoader_ShouldHandleEmptyDataSet()
    {
        // Arrange
        var dataSet = CreateTestDataSet(0, 0);
        var emptyRows = new List<DataSetRow>();

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.GetByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(emptyRows);

        // Act
        var result = await _dataLoader.LoadDataSetDataAsync(_testDataSetId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalColumns);
        Assert.Equal(0, result.TotalRows);
        Assert.Empty(result.Rows);
        Assert.Empty(result.Columns);
    }

    [Fact]
    public void Constructor_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new DataSetDataLoader(null!, _rowRepositoryMock.Object, _loggerMock.Object));
        
        Assert.Throws<ArgumentNullException>(() => 
            new DataSetDataLoader(_dataSetRepositoryMock.Object, null!, _loggerMock.Object));
        
        Assert.Throws<ArgumentNullException>(() => 
            new DataSetDataLoader(_dataSetRepositoryMock.Object, _rowRepositoryMock.Object, null!));
    }

    [Fact]
    public async Task LoadDataSetDataAsync_ShouldConvertRowDataProperly()
    {
        // Arrange
        var dataSet = CreateTestDataSet(2, 2);
        var rows = new List<DataSetRow>
        {
            DataSetRow.Create(_testDataSetId, 0, "{\"name\": \"John\", \"age\": 30}"),
            DataSetRow.Create(_testDataSetId, 1, "{\"name\": \"Jane\", \"age\": 25}")
        };

        _dataSetRepositoryMock.Setup(x => x.GetByIdAsync(_testDataSetId))
            .ReturnsAsync(dataSet);
        _rowRepositoryMock.Setup(x => x.GetByDataSetIdAsync(_testDataSetId))
            .ReturnsAsync(rows);

        // Act
        var result = await _dataLoader.LoadDataSetDataAsync(_testDataSetId);

        // Assert
        Assert.Equal(2, result.Rows.Count);
        
        var firstRow = result.Rows[0];
        Assert.Equal(0, firstRow.RowIndex);
        Assert.Contains("name", firstRow.Values.Keys);
        Assert.Contains("age", firstRow.Values.Keys);
        Assert.Equal("John", firstRow.Values["name"]?.ToString());
    }
}