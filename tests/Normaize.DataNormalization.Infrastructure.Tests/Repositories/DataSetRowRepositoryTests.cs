using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Infrastructure.Repositories;

namespace Normaize.DataNormalization.Infrastructure.Tests.Repositories;

public class DataSetRowRepositoryTests : IDisposable
{
    private readonly DataNormalizationDbContext _context;
    private readonly DataSetRowRepository _repository;
    private readonly Mock<ILogger<DataSetRowRepository>> _loggerMock;
    private readonly Guid _testDataSetId;

    public DataSetRowRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataNormalizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataNormalizationDbContext(options);
        _loggerMock = new Mock<ILogger<DataSetRowRepository>>();
        _repository = new DataSetRowRepository(_context, _loggerMock.Object);
        _testDataSetId = Guid.NewGuid();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private DataSetRow CreateTestRow(int rowIndex = 0, string? customData = null)
    {
        var data = customData ?? $"{{\"col1\": \"value{rowIndex}\", \"col2\": \"data{rowIndex}\"}}";
        return DataSetRow.Create(_testDataSetId, rowIndex, data);
    }

    [Fact]
    public async Task SaveAsync_WithValidRow_ShouldSaveAndReturnRow()
    {
        // Arrange
        var row = CreateTestRow();

        // Act
        var result = await _repository.SaveAsync(row);

        // Assert
        Assert.Equal(row.Id, result.Id);
        Assert.Equal(row.RowIndex, result.RowIndex);
        Assert.Equal(row.Data, result.Data);

        // Verify it was actually saved to database
        var savedRow = await _context.DataSetRows.FindAsync(row.Id);
        Assert.NotNull(savedRow);
        Assert.Equal(row.Data, savedRow.Data);
    }

    [Fact]
    public async Task SaveRangeAsync_WithMultipleRows_ShouldSaveAllRows()
    {
        // Arrange
        var rows = new List<DataSetRow>
        {
            CreateTestRow(0),
            CreateTestRow(1),
            CreateTestRow(2)
        };

        // Act
        var result = await _repository.SaveRangeAsync(rows);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);

        // Verify all rows were saved to database
        var savedRows = await _context.DataSetRows
            .Where(r => r.DataSetId == _testDataSetId)
            .ToListAsync();
        Assert.Equal(3, savedRows.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnRow()
    {
        // Arrange
        var row = CreateTestRow();
        await _repository.SaveAsync(row);

        // Act
        var result = await _repository.GetByIdAsync(row.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(row.Id, result.Id);
        Assert.Equal(row.DataSetId, result.DataSetId);
        Assert.Equal(row.RowIndex, result.RowIndex);
        Assert.Equal(row.Data, result.Data);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByDataSetIdAsync_WithExistingDataSet_ShouldReturnAllRowsOrderedByIndex()
    {
        // Arrange
        var rows = new List<DataSetRow>
        {
            CreateTestRow(2), // Save out of order
            CreateTestRow(0),
            CreateTestRow(1)
        };

        foreach (var row in rows)
        {
            await _repository.SaveAsync(row);
        }

        // Act
        var result = await _repository.GetByDataSetIdAsync(_testDataSetId);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);

        // Verify ordering by RowIndex
        Assert.Equal(0, resultList[0].RowIndex);
        Assert.Equal(1, resultList[1].RowIndex);
        Assert.Equal(2, resultList[2].RowIndex);
    }

    [Fact]
    public async Task GetByDataSetIdAsync_WithPagination_ShouldReturnCorrectSubset()
    {
        // Arrange
        var rows = new List<DataSetRow>();
        for (int i = 0; i < 10; i++)
        {
            rows.Add(CreateTestRow(i));
        }

        foreach (var row in rows)
        {
            await _repository.SaveAsync(row);
        }

        // Act
        var result = await _repository.GetByDataSetIdAsync(_testDataSetId, skip: 3, take: 4);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(4, resultList.Count);
        Assert.Equal(3, resultList[0].RowIndex);
        Assert.Equal(4, resultList[1].RowIndex);
        Assert.Equal(5, resultList[2].RowIndex);
        Assert.Equal(6, resultList[3].RowIndex);
    }

    [Fact]
    public async Task GetByDataSetIdAsync_WithNonExistingDataSet_ShouldReturnEmpty()
    {
        // Arrange
        var nonExistingDataSetId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByDataSetIdAsync(nonExistingDataSetId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldRemoveRowAndReturnTrue()
    {
        // Arrange
        var row = CreateTestRow();
        await _repository.SaveAsync(row);

        // Act
        var result = await _repository.DeleteAsync(row.Id);

        // Assert
        Assert.True(result);

        // Verify it was actually removed from database
        var deletedRow = await _context.DataSetRows.FindAsync(row.Id);
        Assert.Null(deletedRow);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteByDataSetIdAsync_WithExistingRows_ShouldRemoveAllRowsAndReturnTrue()
    {
        // Arrange
        var rows = new List<DataSetRow>
        {
            CreateTestRow(0),
            CreateTestRow(1),
            CreateTestRow(2)
        };

        foreach (var row in rows)
        {
            await _repository.SaveAsync(row);
        }

        // Act
        var result = await _repository.DeleteByDataSetIdAsync(_testDataSetId);

        // Assert
        Assert.True(result);

        // Verify all rows were removed
        var remainingRows = await _context.DataSetRows
            .Where(r => r.DataSetId == _testDataSetId)
            .ToListAsync();
        Assert.Empty(remainingRows);
    }

    [Fact]
    public async Task DeleteByDataSetIdAsync_WithNoRows_ShouldReturnTrue()
    {
        // Arrange
        var emptyDataSetId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteByDataSetIdAsync(emptyDataSetId);

        // Assert
        Assert.True(result); // Should succeed even if no rows to delete
    }

    [Fact]
    public async Task GetCountByDataSetIdAsync_WithExistingRows_ShouldReturnCorrectCount()
    {
        // Arrange
        var rows = new List<DataSetRow>
        {
            CreateTestRow(0),
            CreateTestRow(1),
            CreateTestRow(2),
            CreateTestRow(3),
            CreateTestRow(4)
        };

        foreach (var row in rows)
        {
            await _repository.SaveAsync(row);
        }

        // Act
        var result = await _repository.GetCountByDataSetIdAsync(_testDataSetId);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public async Task GetCountByDataSetIdAsync_WithNoRows_ShouldReturnZero()
    {
        // Arrange
        var emptyDataSetId = Guid.NewGuid();

        // Act
        var result = await _repository.GetCountByDataSetIdAsync(emptyDataSetId);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Repository_ShouldHandleJsonDataProperly()
    {
        // Arrange
        var complexData = @"{
            ""name"": ""John Doe"",
            ""age"": 30,
            ""address"": {
                ""street"": ""123 Main St"",
                ""city"": ""Anytown""
            },
            ""tags"": [""developer"", ""manager""],
            ""isActive"": true,
            ""salary"": 75000.50
        }";
        var row = CreateTestRow(0, complexData);

        // Act
        await _repository.SaveAsync(row);
        var retrieved = await _repository.GetByIdAsync(row.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(complexData, retrieved.Data);

        // Verify we can still parse the JSON
        var values = retrieved.GetAllValues();
        Assert.NotEmpty(values);
        Assert.Contains("name", values.Keys);
    }

    [Fact]
    public async Task Repository_ShouldLogDebugMessages()
    {
        // Arrange
        var row = CreateTestRow();

        // Act
        await _repository.SaveAsync(row);
        await _repository.GetByIdAsync(row.Id);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting row by ID")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Saving row")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Repository_ShouldHandleConcurrentOperations()
    {
        // Arrange
        var tasks = new List<Task<DataSetRow>>();
        for (int i = 0; i < 10; i++)
        {
            var row = CreateTestRow(i);
            tasks.Add(_repository.SaveAsync(row));
        }

        // Act
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(10, results.Length);
        Assert.All(results, r => Assert.NotEqual(Guid.Empty, r.Id));

        // Verify all were saved
        var count = await _repository.GetCountByDataSetIdAsync(_testDataSetId);
        Assert.Equal(10, count);
    }

    [Fact]
    public async Task Repository_ShouldMaintainRowIndexOrdering()
    {
        // Arrange - Save rows in random order
        var random = new Random();
        var indices = Enumerable.Range(0, 100).OrderBy(x => random.Next()).ToList();

        foreach (var index in indices)
        {
            var row = CreateTestRow(index);
            await _repository.SaveAsync(row);
        }

        // Act
        var allRows = await _repository.GetByDataSetIdAsync(_testDataSetId);

        // Assert
        var rowsList = allRows.ToList();
        Assert.Equal(100, rowsList.Count);

        // Verify strict ordering
        for (int i = 0; i < rowsList.Count; i++)
        {
            Assert.Equal(i, rowsList[i].RowIndex);
        }
    }
}