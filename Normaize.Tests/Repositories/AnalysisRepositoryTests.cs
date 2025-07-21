using Microsoft.EntityFrameworkCore;
using Normaize.Data;
using Normaize.Data.Repositories;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using FluentAssertions;
using Xunit;

namespace Normaize.Tests.Repositories;

public class AnalysisRepositoryTests : IDisposable
{
    private readonly NormaizeContext _context;
    private readonly AnalysisRepository _repository;

    public AnalysisRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<NormaizeContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new NormaizeContext(options);
        _repository = new AnalysisRepository(_context);
        
        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test datasets first
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
            }
        };

        _context.DataSets.AddRange(dataSets);

        // Create test analyses
        var analyses = new List<Analysis>
        {
            new()
            {
                Id = 1,
                DataSetId = 1,
                Type = AnalysisType.Normalization,
                Status = AnalysisStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-1),

                Results = "{\"result\": \"test\"}",
                IsDeleted = false
            },
            new()
            {
                Id = 2,
                DataSetId = 1,
                Type = AnalysisType.Comparison,
                Status = AnalysisStatus.Processing,
                ComparisonDataSetId = 2,
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                IsDeleted = false
            },
            new()
            {
                Id = 3,
                DataSetId = 2,
                Type = AnalysisType.Statistical,
                Status = AnalysisStatus.Failed,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                ErrorMessage = "Analysis failed",
                IsDeleted = false
            },
            new()
            {
                Id = 4,
                DataSetId = 1,
                Type = AnalysisType.DataCleaning,
                Status = AnalysisStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                Results = "{\"cleaned\": true}",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddDays(-1),
                DeletedBy = "user1"
            }
        };

        _context.Analyses.AddRange(analyses);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnAnalysis()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.DataSetId.Should().Be(1);
        result.Type.Should().Be(AnalysisType.Normalization);
        result.Status.Should().Be(AnalysisStatus.Completed);
        result.IsDeleted.Should().BeFalse();
        result.DataSet.Should().NotBeNull();
        result.DataSet!.Id.Should().Be(1);
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
    public async Task GetByIdAsync_WithDeletedAnalysis_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(4);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithComparisonAnalysis_ShouldIncludeComparisonDataSet()
    {
        // Act
        var result = await _repository.GetByIdAsync(2);

        // Assert
        result.Should().NotBeNull();
        result!.ComparisonDataSet.Should().NotBeNull();
        result.ComparisonDataSet!.Id.Should().Be(2);
    }

    [Fact]
    public async Task GetByDataSetIdAsync_WithValidDataSetId_ShouldReturnAnalyses()
    {
        // Act
        var result = await _repository.GetByDataSetIdAsync(1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.DataSetId == 1 && !a.IsDeleted);
        result.Should().BeInDescendingOrder(a => a.CreatedAt);
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
    public async Task GetAllAsync_ShouldReturnNonDeletedAnalyses()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(a => !a.IsDeleted);
        result.Should().BeInDescendingOrder(a => a.CreatedAt);
    }

    [Fact]
    public async Task AddAsync_WithValidAnalysis_ShouldAddAndReturnAnalysis()
    {
        // Arrange
        var newAnalysis = new Analysis
        {
            DataSetId = 1,
            Type = AnalysisType.OutlierDetection,
            Status = AnalysisStatus.Pending
        };

        // Act
        var result = await _repository.AddAsync(newAnalysis);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.DataSetId.Should().Be(1);
        result.Type.Should().Be(AnalysisType.OutlierDetection);
        result.Status.Should().Be(AnalysisStatus.Pending);

        // Verify it's in the database
        var savedAnalysis = await _context.Analyses.FindAsync(result.Id);
        savedAnalysis.Should().NotBeNull();
        savedAnalysis!.DataSetId.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_WithValidAnalysis_ShouldUpdateAnalysis()
    {
        // Arrange
        var analysis = await _repository.GetByIdAsync(1);
        analysis!.Status = AnalysisStatus.Processing;
        analysis.Results = "{\"updated\": true}";

        // Act
        var result = await _repository.UpdateAsync(analysis);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(AnalysisStatus.Processing);
        result.Results.Should().Be("{\"updated\": true}");

        // Verify in database
        var updatedAnalysis = await _context.Analyses.FindAsync(1);
        updatedAnalysis.Should().NotBeNull();
        updatedAnalysis!.Status.Should().Be(AnalysisStatus.Processing);
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldSoftDeleteAnalysis()
    {
        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify soft delete
        var deletedAnalysis = await _context.Analyses.FindAsync(1);
        deletedAnalysis.Should().NotBeNull();
        deletedAnalysis!.IsDeleted.Should().BeTrue();
        deletedAnalysis.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        deletedAnalysis.DeletedBy.Should().Be("System");
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
    public async Task DeleteAsync_WithAlreadyDeletedAnalysis_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(4);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HardDeleteAsync_WithValidId_ShouldRemoveAnalysis()
    {
        // Act
        var result = await _repository.HardDeleteAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify hard delete
        var deletedAnalysis = await _context.Analyses.FindAsync(1);
        deletedAnalysis.Should().BeNull();
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
    public async Task RestoreAsync_WithDeletedAnalysis_ShouldRestoreAnalysis()
    {
        // Act
        var result = await _repository.RestoreAsync(4);

        // Assert
        result.Should().BeTrue();

        // Verify restore
        var restoredAnalysis = await _context.Analyses.FindAsync(4);
        restoredAnalysis.Should().NotBeNull();
        restoredAnalysis!.IsDeleted.Should().BeFalse();
        restoredAnalysis.DeletedAt.Should().BeNull();
        restoredAnalysis.DeletedBy.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_WithNonDeletedAnalysis_ShouldReturnFalse()
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
    public async Task GetByStatusAsync_WithValidStatus_ShouldReturnMatchingAnalyses()
    {
        // Act
        var result = await _repository.GetByStatusAsync(AnalysisStatus.Completed);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(a => a.Status == AnalysisStatus.Completed);
    }

    [Fact]
    public async Task GetByStatusAsync_WithInvalidStatus_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetByStatusAsync(AnalysisStatus.Pending);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByTypeAsync_WithValidType_ShouldReturnMatchingAnalyses()
    {
        // Act
        var result = await _repository.GetByTypeAsync(AnalysisType.Normalization);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(a => a.Type == AnalysisType.Normalization);
    }

    [Fact]
    public async Task GetByTypeAsync_WithInvalidType_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetByTypeAsync(AnalysisType.Custom);

        // Assert
        result.Should().BeEmpty();
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
    public async Task PermanentlyDeleteOldSoftDeletedAsync_WithValidDays_ShouldDeleteOldSoftDeleted()
    {
        // Arrange - Create an old soft-deleted analysis
        var oldDeletedAnalysis = new Analysis
        {
            DataSetId = 1,
            Type = AnalysisType.Statistical,
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-30),
            DeletedBy = "user1"
        };
        _context.Analyses.Add(oldDeletedAnalysis);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.PermanentlyDeleteOldSoftDeletedAsync(10);

        // Assert
        result.Should().Be(1); // Should delete 1 old soft-deleted analysis

        // Verify it's permanently deleted
        var deletedAnalysis = await _context.Analyses.FindAsync(oldDeletedAnalysis.Id);
        deletedAnalysis.Should().BeNull();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
} 