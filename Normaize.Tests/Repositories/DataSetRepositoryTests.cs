using Microsoft.EntityFrameworkCore;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using FluentAssertions;
using Xunit;

namespace Normaize.Tests.Repositories;

public class DataSetRepositoryTests : IDisposable
{
    private readonly NormaizeContext _context;
    private readonly DataSetRepository _repository;

    public DataSetRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new NormaizeContext(options);
        _repository = new DataSetRepository(_context);
        
        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var dataSets = new List<DataSet>
        {
            new()
            {
                Id = 1,
                Name = "Test Dataset 1",
                FileName = "test1.csv",
                FileType = FileType.CSV,
                FileSize = 1024,
                UserId = "user1",
                UploadedAt = DateTime.UtcNow.AddDays(-1),
                LastModifiedAt = DateTime.UtcNow.AddDays(-1),
                LastModifiedBy = "user1",
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                Name = "Test Dataset 2",
                FileName = "test2.json",
                FileType = FileType.JSON,
                FileSize = 2048,
                UserId = "user1",
                UploadedAt = DateTime.UtcNow.AddDays(-2),
                LastModifiedAt = DateTime.UtcNow.AddDays(-2),
                LastModifiedBy = "user1",
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                Name = "Deleted Dataset",
                FileName = "deleted.csv",
                FileType = FileType.CSV,
                FileSize = 512,
                UserId = "user2",
                UploadedAt = DateTime.UtcNow.AddDays(-3),
                LastModifiedAt = DateTime.UtcNow.AddDays(-1),
                LastModifiedBy = "user2",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddDays(-1),
                DeletedBy = "user2"
            }
        };

        _context.DataSets.AddRange(dataSets);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnDataSet()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Dataset 1");
        result.UserId.Should().Be("user1");
        result.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedDataSet_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(3);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnNonDeletedDataSets()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(ds => !ds.IsDeleted);
        result.Should().BeInDescendingOrder(ds => ds.UploadedAt);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnUserDataSets()
    {
        // Act
        var result = await _repository.GetByUserIdAsync("user1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(ds => ds.UserId == "user1" && !ds.IsDeleted);
        result.Should().BeInDescendingOrder(ds => ds.UploadedAt);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithInvalidUserId_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetByUserIdAsync("nonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_WithValidDataSet_ShouldAddAndReturnDataSet()
    {
        // Arrange
        var newDataSet = new DataSet
        {
            Name = "New Dataset",
            FileName = "new.csv",
            FileType = FileType.CSV,
            FileSize = 1024,
            UserId = "user3"
        };

        // Act
        var result = await _repository.AddAsync(newDataSet);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Dataset");
        result.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.LastModifiedBy.Should().Be("user3");

        // Verify it's in the database
        var savedDataSet = await _context.DataSets.FindAsync(result.Id);
        savedDataSet.Should().NotBeNull();
        savedDataSet!.Name.Should().Be("New Dataset");
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldSoftDeleteDataSet()
    {
        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify soft delete
        var deletedDataSet = await _context.DataSets.FindAsync(1);
        deletedDataSet.Should().NotBeNull();
        deletedDataSet!.IsDeleted.Should().BeTrue();
        deletedDataSet.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        deletedDataSet.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithAlreadyDeletedDataSet_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(3);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteAsync_WithValidId_ShouldRemoveDataSet()
    {
        // Act
        var result = await _repository.HardDeleteAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify hard delete
        var deletedDataSet = await _context.DataSets.FindAsync(1);
        deletedDataSet.Should().BeNull();
    }

    [Fact]
    public async Task HardDeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.HardDeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreAsync_WithDeletedDataSet_ShouldRestoreDataSet()
    {
        // Act
        var result = await _repository.RestoreAsync(3);

        // Assert
        result.Should().BeTrue();

        // Verify restore
        var restoredDataSet = await _context.DataSets.FindAsync(3);
        restoredDataSet.Should().NotBeNull();
        restoredDataSet!.IsDeleted.Should().BeFalse();
        restoredDataSet.DeletedAt.Should().BeNull();
        restoredDataSet.DeletedBy.Should().BeNull();
        restoredDataSet.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RestoreAsync_WithNonDeletedDataSet_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.RestoreAsync(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RestoreAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.RestoreAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithValidDataSet_ShouldUpdateDataSet()
    {
        // Arrange
        var dataSet = await _repository.GetByIdAsync(1);
        dataSet!.Name = "Updated Dataset Name";

        // Act
        var result = await _repository.UpdateAsync(dataSet);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Dataset Name");
        result.LastModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify in database
        var updatedDataSet = await _context.DataSets.FindAsync(1);
        updatedDataSet.Should().NotBeNull();
        updatedDataSet!.Name.Should().Be("Updated Dataset Name");
    }

    [Fact]
    public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Act
        var result = await _repository.ExistsAsync(1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetDeletedAsync_ShouldReturnDeletedDataSets()
    {
        // Act
        var result = await _repository.GetDeletedAsync();

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(ds => ds.IsDeleted);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithIncludeDeleted_ShouldReturnAllUserDataSets()
    {
        // Act
        var result = await _repository.GetByUserIdAsync("user2", includeDeleted: true);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(ds => ds.UserId == "user2");
    }

    [Fact]
    public async Task SearchAsync_WithValidTerm_ShouldReturnMatchingDataSets()
    {
        // Act
        var result = await _repository.SearchAsync("Dataset", "user1");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(ds => ds.Name.Contains("Dataset") && ds.UserId == "user1");
    }

    [Fact]
    public async Task GetByFileTypeAsync_WithValidType_ShouldReturnMatchingDataSets()
    {
        // Act
        var result = await _repository.GetByFileTypeAsync(FileType.CSV, "user1");

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(ds => ds.FileType == FileType.CSV && ds.UserId == "user1");
    }

    [Fact]
    public async Task GetTotalSizeAsync_WithValidUserId_ShouldReturnTotalSize()
    {
        // Act
        var result = await _repository.GetTotalSizeAsync("user1");

        // Assert
        result.Should().Be(3072); // 1024 + 2048
    }

    [Fact]
    public async Task GetTotalCountAsync_WithValidUserId_ShouldReturnTotalCount()
    {
        // Act
        var result = await _repository.GetTotalCountAsync("user1");

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetRecentlyModifiedAsync_WithValidUserId_ShouldReturnRecentDataSets()
    {
        // Act
        var result = await _repository.GetRecentlyModifiedAsync("user1", 5);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeInDescendingOrder(ds => ds.LastModifiedAt);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
} 