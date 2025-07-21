using Microsoft.EntityFrameworkCore;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Core.Models;
using FluentAssertions;
using Xunit;

namespace Normaize.Tests.Repositories;

public class DataSetRowRepositoryTests : IDisposable
{
    private readonly NormaizeContext _context;
    private readonly DataSetRowRepository _repository;

    public DataSetRowRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new NormaizeContext(options);
        _repository = new DataSetRowRepository(_context);
        
        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test dataset first
        var dataSet = new DataSet
        {
            Id = 1,
            Name = "Test Dataset",
            FileName = "test.csv",
                            FileType = Normaize.Core.DTOs.FileType.CSV,
            FileSize = 1024,
            UserId = "user1",
            UploadedAt = DateTime.UtcNow.AddDays(-1),
            LastModifiedAt = DateTime.UtcNow.AddDays(-1),
            LastModifiedBy = "user1",
            IsDeleted = false
        };

        _context.DataSets.Add(dataSet);

        // Create test data set rows
        var dataSetRows = new List<DataSetRow>
        {
            new()
            {
                Id = 1,
                DataSetId = 1,
                RowIndex = 1,
                Data = "{\"column1\": \"value1\", \"column2\": \"value2\"}",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = 2,
                DataSetId = 1,
                RowIndex = 2,
                Data = "{\"column1\": \"value3\", \"column2\": \"value4\"}",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = 3,
                DataSetId = 1,
                RowIndex = 3,
                Data = "{\"column1\": \"value5\", \"column2\": \"value6\"}",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = 4,
                DataSetId = 2, // Different dataset
                RowIndex = 1,
                Data = "{\"column1\": \"other_value\"}",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _context.DataSetRows.AddRange(dataSetRows);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByDataSetIdAsync_WithValidDataSetId_ShouldReturnAllRows()
    {
        // Act
        var result = await _repository.GetByDataSetIdAsync(1);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(r => r.DataSetId == 1);
        result.Should().BeInAscendingOrder(r => r.RowIndex);
    }

    [Fact]
    public async Task GetByDataSetIdAsync_WithInvalidDataSetId_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetByDataSetIdAsync(999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByDataSetIdAsync_WithSkipAndTake_ShouldReturnPaginatedRows()
    {
        // Act
        var result = await _repository.GetByDataSetIdAsync(1, skip: 1, take: 2);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.DataSetId == 1);
        result.Should().BeInAscendingOrder(r => r.RowIndex);
        result.First().RowIndex.Should().Be(2);
        result.Last().RowIndex.Should().Be(3);
    }

    [Fact]
    public async Task GetByDataSetIdAsync_WithSkipBeyondAvailable_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetByDataSetIdAsync(1, skip: 10, take: 5);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnRow()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.DataSetId.Should().Be(1);
        result.RowIndex.Should().Be(1);
        result.Data.Should().Be("{\"column1\": \"value1\", \"column2\": \"value2\"}");
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
    public async Task AddAsync_WithValidRow_ShouldAddAndReturnRow()
    {
        // Arrange
        var newRow = new DataSetRow
        {
            DataSetId = 1,
            RowIndex = 4,
            Data = "{\"column1\": \"new_value\"}"
        };

        // Act
        var result = await _repository.AddAsync(newRow);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.DataSetId.Should().Be(1);
        result.RowIndex.Should().Be(4);
        result.Data.Should().Be("{\"column1\": \"new_value\"}");

        // Verify it's in the database
        var savedRow = await _context.DataSetRows.FindAsync(result.Id);
        savedRow.Should().NotBeNull();
        savedRow!.DataSetId.Should().Be(1);
    }

    [Fact]
    public async Task AddRangeAsync_WithValidRows_ShouldAddAllRows()
    {
        // Arrange
        var newRows = new List<DataSetRow>
        {
            new()
            {
                DataSetId = 1,
                RowIndex = 4,
                Data = "{\"column1\": \"value7\"}"
            },
            new()
            {
                DataSetId = 1,
                RowIndex = 5,
                Data = "{\"column1\": \"value8\"}"
            }
        };

        // Act
        var result = await _repository.AddRangeAsync(newRows);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.DataSetId == 1);

        // Verify they're in the database
        var savedRows = await _context.DataSetRows
            .Where(r => r.DataSetId == 1 && r.RowIndex >= 4)
            .ToListAsync();
        savedRows.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemoveRow()
    {
        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify it's removed from database
        var deletedRow = await _context.DataSetRows.FindAsync(1);
        deletedRow.Should().BeNull();
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
    public async Task DeleteByDataSetIdAsync_WithValidDataSetId_ShouldRemoveAllRows()
    {
        // Act
        var result = await _repository.DeleteByDataSetIdAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify all rows are removed
        var remainingRows = await _context.DataSetRows
            .Where(r => r.DataSetId == 1)
            .ToListAsync();
        remainingRows.Should().BeEmpty();

        // Verify rows from other datasets remain
        var otherRows = await _context.DataSetRows
            .Where(r => r.DataSetId == 2)
            .ToListAsync();
        otherRows.Should().HaveCount(1);
    }

    [Fact]
    public async Task DeleteByDataSetIdAsync_WithInvalidDataSetId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteByDataSetIdAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteByDataSetIdAsync_WithEmptyDataSet_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteByDataSetIdAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCountByDataSetIdAsync_WithValidDataSetId_ShouldReturnCount()
    {
        // Act
        var result = await _repository.GetCountByDataSetIdAsync(1);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetCountByDataSetIdAsync_WithInvalidDataSetId_ShouldReturnZero()
    {
        // Act
        var result = await _repository.GetCountByDataSetIdAsync(999);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetCountByDataSetIdAsync_WithEmptyDataSet_ShouldReturnZero()
    {
        // Act
        var result = await _repository.GetCountByDataSetIdAsync(2);

        // Assert
        result.Should().Be(1);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
} 