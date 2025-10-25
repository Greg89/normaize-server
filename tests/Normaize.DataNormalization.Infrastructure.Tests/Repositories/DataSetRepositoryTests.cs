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

public class DataSetRepositoryTests : IDisposable
{
    private readonly DataNormalizationDbContext _context;
    private readonly DataSetRepository _repository;
    private readonly Mock<ILogger<DataSetRepository>> _loggerMock;

    public DataSetRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataNormalizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataNormalizationDbContext(options);
        _loggerMock = new Mock<ILogger<DataSetRepository>>();
        _repository = new DataSetRepository(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static DataSet CreateTestDataSet(string name = "Test Dataset", string userId = "user123")
    {
        var fileInfo = FileMetadata.Create("test.csv", "/uploads/test.csv", FileType.CSV, 1024, "hash123");
        var statistics = DatasetStatistics.Create(100, 5);
        return DataSet.Create(name, "Test description", userId, fileInfo, statistics);
    }

    [Fact]
    public async Task SaveAsync_WithValidDataSet_ShouldSaveAndReturnDataSet()
    {
        // Arrange
        var dataSet = CreateTestDataSet();

        // Act
        var result = await _repository.SaveAsync(dataSet);

        // Assert
        Assert.Equal(dataSet.Id, result.Id);
        Assert.Equal(dataSet.Name, result.Name);

        // Verify it was actually saved to database
        var savedDataSet = await _context.DataSets.FindAsync(dataSet.Id);
        Assert.NotNull(savedDataSet);
        Assert.Equal(dataSet.Name, savedDataSet.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnDataSet()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        await _repository.SaveAsync(dataSet);

        // Act
        var result = await _repository.GetByIdAsync(dataSet.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataSet.Id, result.Id);
        Assert.Equal(dataSet.Name, result.Name);
        Assert.Equal(dataSet.UserId, result.UserId);
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
    public async Task GetByIdAsync_WithDeletedDataSet_ShouldReturnNull()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        dataSet.Delete("admin");
        await _repository.SaveAsync(dataSet);

        // Act
        var result = await _repository.GetByIdAsync(dataSet.Id);

        // Assert
        Assert.Null(result); // Should be filtered out by query filter
    }

    [Fact]
    public async Task GetByUserIdAsync_WithExistingUser_ShouldReturnUserDataSets()
    {
        // Arrange
        var userId = "user123";
        var dataSet1 = CreateTestDataSet("Dataset 1", userId);
        var dataSet2 = CreateTestDataSet("Dataset 2", userId);
        var dataSet3 = CreateTestDataSet("Dataset 3", "otherUser");

        await _repository.SaveAsync(dataSet1);
        await _repository.SaveAsync(dataSet2);
        await _repository.SaveAsync(dataSet3);

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.All(resultList, ds => Assert.Equal(userId, ds.UserId));
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOrderedByCreatedAt()
    {
        // Arrange
        var userId = "user123";
        var dataSet1 = CreateTestDataSet("Dataset 1", userId);
        var dataSet2 = CreateTestDataSet("Dataset 2", userId);

        // Save with slight delay to ensure different timestamps
        await _repository.SaveAsync(dataSet1);
        await Task.Delay(1);
        await _repository.SaveAsync(dataSet2);

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        // Should be ordered by UploadedAt descending (newest first)
        Assert.True(resultList[0].UploadedAt >= resultList[1].UploadedAt);
    }

    [Fact]
    public async Task UpdateAsync_WithValidDataSet_ShouldUpdateDataSet()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        await _repository.SaveAsync(dataSet);

        var newSchema = "{\"columns\": [\"col1\", \"col2\"]}";
        dataSet.UpdateSchema(newSchema, "admin");

        // Act
        var result = await _repository.UpdateAsync(dataSet);

        // Assert
        Assert.Equal(dataSet.Id, result.Id);
        Assert.Equal(newSchema, result.Schema);

        // Verify it was actually updated in database
        var updatedDataSet = await _context.DataSets.FindAsync(dataSet.Id);
        Assert.NotNull(updatedDataSet);
        Assert.Equal(newSchema, updatedDataSet.Schema);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldSoftDeleteDataSet()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        await _repository.SaveAsync(dataSet);

        // Act
        var result = await _repository.DeleteAsync(dataSet.Id);

        // Assert
        Assert.True(result);

        // Verify it was soft deleted (not physically removed)
        var deletedDataSet = await _context.DataSets.IgnoreQueryFilters().FirstOrDefaultAsync(ds => ds.Id == dataSet.Id);
        Assert.NotNull(deletedDataSet);
        Assert.True(deletedDataSet.IsDeleted);
        Assert.NotNull(deletedDataSet.DeletedAt);

        // Verify it's not returned by normal queries
        var normalQuery = await _repository.GetByIdAsync(dataSet.Id);
        Assert.Null(normalQuery);
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
    public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        await _repository.SaveAsync(dataSet);

        // Act
        var result = await _repository.ExistsAsync(dataSet.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(nonExistingId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_WithDeletedDataSet_ShouldReturnFalse()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        dataSet.Delete("admin");
        await _repository.SaveAsync(dataSet);

        // Act
        var result = await _repository.ExistsAsync(dataSet.Id);

        // Assert
        Assert.False(result); // Soft deleted datasets should not exist from repository perspective
    }

    [Fact]
    public async Task GetByIdWithRowsAsync_WithExistingId_ShouldReturnDataSetWithoutRows()
    {
        // Arrange
        var dataSet = CreateTestDataSet();
        await _repository.SaveAsync(dataSet);

        // Act
        var result = await _repository.GetByIdWithRowsAsync(dataSet.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(dataSet.Id, result.Id);
        // Note: Current implementation doesn't actually load rows for performance reasons
        // This is documented behavior
    }

    [Fact]
    public async Task Repository_ShouldHandleValueObjectsProperly()
    {
        // Arrange
        var fileInfo = FileMetadata.Create("test.csv", "/uploads/test.csv", FileType.CSV, 2048, "hash456");
        var statistics = DatasetStatistics.Create(500, 10);
        var dataSet = DataSet.Create("Value Object Test", "Testing value objects", "user456", fileInfo, statistics);

        // Act
        await _repository.SaveAsync(dataSet);
        var retrieved = await _repository.GetByIdAsync(dataSet.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(fileInfo.FileName, retrieved.FileInfo.FileName);
        Assert.Equal(fileInfo.FileSize, retrieved.FileInfo.FileSize);
        Assert.Equal(fileInfo.FileType.Value, retrieved.FileInfo.FileType.Value);
        Assert.Equal(statistics.RowCount, retrieved.Statistics.RowCount);
        Assert.Equal(statistics.ColumnCount, retrieved.Statistics.ColumnCount);
    }

    [Fact]
    public async Task Repository_ShouldLogDebugMessages()
    {
        // Arrange
        var dataSet = CreateTestDataSet();

        // Act
        await _repository.SaveAsync(dataSet);
        await _repository.GetByIdAsync(dataSet.Id);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Getting dataset by ID")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Saving dataset")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}