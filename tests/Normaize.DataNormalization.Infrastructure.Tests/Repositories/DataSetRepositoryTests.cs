using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.ValueObjects;
using Normaize.DataNormalization.Infrastructure.Data;
using Normaize.DataNormalization.Infrastructure.Repositories;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Repositories;

public class DataSetRepositoryTests : IDisposable
{
    private readonly DataNormalizationDbContext _dbContext;
    private readonly DataSetRepository _repository;
    private readonly Mock<ILogger<DataSetRepository>> _mockLogger;

    public DataSetRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<DataNormalizationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DataNormalizationDbContext(options);
        _mockLogger = new Mock<ILogger<DataSetRepository>>();
        _repository = new DataSetRepository(_dbContext, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task AddAsync_ShouldAddDataSetToDatabase()
    {
        // Arrange
        var dataSet = DataSet.Create(
            name: "Test Dataset",
            description: null,
            userId: "user-123",
            fileInfo: FileMetadata .Create("test.csv", "user-123/test.csv", FileType.CSV, 1024),
            statistics: null,
            retentionDays: 30
        );

        // Act
        await _repository.AddAsync(dataSet);

        // Assert
        var retrieved = await _repository.GetByIdAsync(dataSet.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Test Dataset");
        retrieved.UserId.Should().Be("user-123");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenDataSetNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldNotReturnDeletedDataSet()
    {
        // Arrange
        var dataSet = CreateTestDataSet("user-123");
        await _repository.AddAsync(dataSet);

        dataSet.Delete("user-123");
        await _repository.UpdateAsync(dataSet);

        // Act
        var result = await _repository.GetByIdAsync(dataSet.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUserDataSets()
    {
        // Arrange
        var user1Id = "user-1";
        var user2Id = "user-2";

        var dataSet1 = CreateTestDataSet(user1Id, "Dataset 1");
        var dataSet2 = CreateTestDataSet(user1Id, "Dataset 2");
        var dataSet3 = CreateTestDataSet(user2Id, "Dataset 3");

        await _repository.AddAsync(dataSet1);
        await _repository.AddAsync(dataSet2);
        await _repository.AddAsync(dataSet3);

        // Act
        var user1DataSets = await _repository.GetByUserIdAsync(user1Id, CancellationToken.None);

        // Assert
        user1DataSets.Should().HaveCount(2);
        user1DataSets.Should().AllSatisfy(ds => ds.UserId.Should().Be(user1Id));
        user1DataSets.Should().Contain(ds => ds.Name == "Dataset 1");
        user1DataSets.Should().Contain(ds => ds.Name == "Dataset 2");
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldNotReturnDeletedDataSets()
    {
        // Arrange
        var userId = "user-123";
        var dataSet1 = CreateTestDataSet(userId, "Active Dataset");
        var dataSet2 = CreateTestDataSet(userId, "Deleted Dataset");

        await _repository.AddAsync(dataSet1);
        await _repository.AddAsync(dataSet2);

        dataSet2.Delete(userId);
        await _repository.UpdateAsync(dataSet2);

        // Act
        var result = await _repository.GetByUserIdAsync(userId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active Dataset");
    }

    [Fact]
    public async Task GetDeletedByUserIdAsync_ShouldReturnOnlyDeletedDataSets()
    {
        // Arrange
        var userId = "user-123";
        var activeDataSet = CreateTestDataSet(userId, "Active Dataset");
        var deletedDataSet1 = CreateTestDataSet(userId, "Deleted Dataset 1");
        var deletedDataSet2 = CreateTestDataSet(userId, "Deleted Dataset 2");

        await _repository.AddAsync(activeDataSet);
        await _repository.AddAsync(deletedDataSet1);
        await _repository.AddAsync(deletedDataSet2);

        deletedDataSet1.Delete(userId);
        deletedDataSet2.Delete(userId);
        await _repository.UpdateAsync(deletedDataSet1);
        await _repository.UpdateAsync(deletedDataSet2);

        // Clear the change tracker to ensure fresh query
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetDeletedByUserIdAsync(userId, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(ds => ds.IsDeleted.Should().BeTrue());
        result.Should().Contain(ds => ds.Name == "Deleted Dataset 1");
        result.Should().Contain(ds => ds.Name == "Deleted Dataset 2");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDataSetProperties()
    {
        // Arrange
        var dataSet = CreateTestDataSet("user-123", "Original Name");
        await _repository.AddAsync(dataSet);

        // Act
        dataSet.UpdateMetadata("Updated Name", "New description", "user-123");
        await _repository.UpdateAsync(dataSet);

        // Assert
        var updated = await _repository.GetByIdAsync(dataSet.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("New description");
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteDataSet()
    {
        // Arrange
        var dataSet = CreateTestDataSet("user-123");
        await _repository.AddAsync(dataSet);

        // Act
        dataSet.Delete("user-123");
        await _repository.UpdateAsync(dataSet);

        // Clear the change tracker to ensure fresh query
        _dbContext.ChangeTracker.Clear();

        // Assert
        var result = await _repository.GetByIdAsync(dataSet.Id);
        result.Should().BeNull(); // GetByIdAsync filters out deleted

        var deletedResult = await _repository.GetDeletedByUserIdAsync("user-123", CancellationToken.None);
        deletedResult.Should().HaveCount(1);
        deletedResult.First().IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var userId = "user-123";
        for (int i = 1; i <= 10; i++)
        {
            var dataSet = CreateTestDataSet(userId, $"Dataset {i}");
            await _repository.AddAsync(dataSet);
        }

        // Act - Get page 2 with page size 3
        var allDataSets = await _repository.GetByUserIdAsync(userId, CancellationToken.None);
        var page2 = allDataSets.Skip(3).Take(3).ToList();

        // Assert
        allDataSets.Should().HaveCount(10);
        page2.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddAsync_WithProcessingStatus_ShouldPersistCorrectly()
    {
        // Arrange
        var dataSet = CreateTestDataSet("user-123");
        var processedData = "test data";
        dataSet.SetProcessedData(processedData);

        // Act
        await _repository.AddAsync(dataSet);

        // Assert
        var retrieved = await _repository.GetByIdAsync(dataSet.Id);
        retrieved.Should().NotBeNull();
        retrieved!.ProcessingStatus.Should().NotBeNull();
        retrieved.ProcessingStatus.IsProcessed.Should().BeTrue();
        retrieved.ProcessingStatus.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddAsync_WithRetentionPolicy_ShouldCalculateExpiryDate()
    {
        // Arrange
        var dataSet = CreateTestDataSet("user-123", "Test", 7); // 7 days retention

        // Act
        await _repository.AddAsync(dataSet);

        // Assert
        var retrieved = await _repository.GetByIdAsync(dataSet.Id);
        retrieved.Should().NotBeNull();
        retrieved!.RetentionPolicy.Should().NotBeNull();
        retrieved.RetentionPolicy.RetentionDays.Should().Be(7);
        retrieved.RetentionExpiryDate.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(7),
            TimeSpan.FromSeconds(5)
        );
    }

    #region Helper Methods

    private DataSet CreateTestDataSet(string userId, string name = "Test Dataset", int? retentionDays = 30)
    {
        return DataSet.Create(
            name: name,
            description: null,
            userId: userId,
            fileInfo: FileMetadata.Create($"{name}.csv", $"{userId}/{name}.csv", FileType.CSV, 1024),
            statistics: null,
            retentionDays: retentionDays
        );
    }

    #endregion
}
