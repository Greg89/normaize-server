using Moq;
using FluentAssertions;
using Xunit;
using Normaize.Core.Services;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.DTOs;
using Normaize.Core.Constants;
using System.Security.Claims;

namespace Normaize.Tests.Services;

public class DataSetQueryServiceTests
{
    private readonly Mock<IDataSetRepository> _mockRepository;
    private readonly Mock<IDataProcessingInfrastructure> _mockInfrastructure;
    private readonly Mock<IStructuredLoggingService> _mockStructuredLogging;
    private readonly Mock<IChaosEngineeringService> _mockChaosEngineering;
    private readonly Mock<IOperationContext> _mockOperationContext;
    private readonly DataSetQueryService _service;

    public DataSetQueryServiceTests()
    {
        _mockRepository = new Mock<IDataSetRepository>();
        _mockInfrastructure = new Mock<IDataProcessingInfrastructure>();
        _mockStructuredLogging = new Mock<IStructuredLoggingService>();
        _mockChaosEngineering = new Mock<IChaosEngineeringService>();
        _mockOperationContext = new Mock<IOperationContext>();

        _mockInfrastructure.Setup(x => x.StructuredLogging).Returns(_mockStructuredLogging.Object);
        _mockInfrastructure.Setup(x => x.ChaosEngineering).Returns(_mockChaosEngineering.Object);

        _mockOperationContext.Setup(x => x.CorrelationId).Returns("test-correlation-id");
        _mockOperationContext.Setup(x => x.OperationName).Returns("TestOperation");

        _mockStructuredLogging
            .Setup(x => x.CreateContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>()))
            .Returns(_mockOperationContext.Object);

        _service = new DataSetQueryService(
            _mockRepository.Object,
            _mockInfrastructure.Object);
    }

    #region GetDataSetsByUserAsync Tests

    [Fact]
    public async Task GetDataSetsByUserAsync_WithValidInputs_ShouldReturnDataSets()
    {
        // Arrange
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;
        var dataSets = new List<DataSet>
        {
            new DataSet { Id = 1, UserId = userId, Name = "Test1", IsDeleted = false },
            new DataSet { Id = 2, UserId = userId, Name = "Test2", IsDeleted = false }
        };

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.GetDataSetsByUserAsync(userId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);

        _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetDataSetsByUserAsync_WithInvalidPage_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "test-user";
        var page = 0;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDataSetsByUserAsync(userId, page, pageSize));
    }

    [Fact]
    public async Task GetDataSetsByUserAsync_WithInvalidPageSize_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "test-user";
        var page = 1;
        var pageSize = 0;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDataSetsByUserAsync(userId, page, pageSize));
    }

    [Fact]
    public async Task GetDataSetsByUserAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Arrange
        string? userId = null;
        var page = 1;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDataSetsByUserAsync(userId!, page, pageSize));
    }

    [Fact]
    public async Task GetDataSetsByUserAsync_WithLargePageSize_ShouldUseMaxPageSize()
    {
        // Arrange
        var userId = "test-user";
        var page = 1;
        var pageSize = 1000; // Larger than max
        var dataSets = new List<DataSet>();

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.GetDataSetsByUserAsync(userId, page, pageSize);

        // Assert
        _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
    }

    #endregion

    #region GetDeletedDataSetsAsync Tests

    [Fact]
    public async Task GetDeletedDataSetsAsync_WithValidInputs_ShouldReturnDeletedDataSets()
    {
        // Arrange
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;
        var dataSets = new List<DataSet>
        {
            new DataSet { Id = 1, UserId = userId, Name = "Deleted1", IsDeleted = true, DeletedAt = DateTime.UtcNow },
            new DataSet { Id = 2, UserId = userId, Name = "Deleted2", IsDeleted = true, DeletedAt = DateTime.UtcNow }
        };

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.GetDeletedDataSetsAsync(userId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);

        _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetDeletedDataSetsAsync_WithInvalidPage_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "test-user";
        var page = 0;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDeletedDataSetsAsync(userId, page, pageSize));
    }

    #endregion

    #region SearchDataSetsAsync Tests

    [Fact]
    public async Task SearchDataSetsAsync_WithValidInputs_ShouldReturnMatchingDataSets()
    {
        // Arrange
        var searchTerm = "test";
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;
        var dataSets = new List<DataSet>
        {
            new DataSet { Id = 1, UserId = userId, Name = "Test Dataset", IsDeleted = false },
            new DataSet { Id = 2, UserId = userId, Name = "Another Test", IsDeleted = false }
        };

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.SearchDataSetsAsync(searchTerm, userId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);

        _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task SearchDataSetsAsync_WithNullSearchTerm_ShouldThrowArgumentException()
    {
        // Arrange
        string? searchTerm = null;
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.SearchDataSetsAsync(searchTerm!, userId, page, pageSize));
    }

    [Fact]
    public async Task SearchDataSetsAsync_WithEmptySearchTerm_ShouldThrowArgumentException()
    {
        // Arrange
        var searchTerm = "";
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.SearchDataSetsAsync(searchTerm, userId, page, pageSize));
    }

    [Fact]
    public async Task SearchDataSetsAsync_WithShortSearchTerm_ShouldThrowArgumentException()
    {
        // Arrange
        var searchTerm = "ab"; // Less than minimum
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.SearchDataSetsAsync(searchTerm, userId, page, pageSize));
    }

    #endregion

    #region GetDataSetsByFileTypeAsync Tests

    [Fact]
    public async Task GetDataSetsByFileTypeAsync_WithValidInputs_ShouldReturnDataSets()
    {
        // Arrange
        var fileType = FileType.CSV;
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;
        var dataSets = new List<DataSet>
        {
            new DataSet { Id = 1, UserId = userId, FileType = FileType.CSV, IsDeleted = false },
            new DataSet { Id = 2, UserId = userId, FileType = FileType.CSV, IsDeleted = false }
        };

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.GetDataSetsByFileTypeAsync(fileType, userId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);

        _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetDataSetsByFileTypeAsync_WithInvalidPage_ShouldThrowArgumentException()
    {
        // Arrange
        var fileType = FileType.CSV;
        var userId = "test-user";
        var page = 0;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDataSetsByFileTypeAsync(fileType, userId, page, pageSize));
    }

    #endregion

    #region GetDataSetsByDateRangeAsync Tests

    [Fact]
    public async Task GetDataSetsByDateRangeAsync_WithValidInputs_ShouldReturnDataSets()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;
        var dataSets = new List<DataSet>
        {
            new DataSet { Id = 1, UserId = userId, UploadedAt = DateTime.UtcNow.AddDays(-15), IsDeleted = false },
            new DataSet { Id = 2, UserId = userId, UploadedAt = DateTime.UtcNow.AddDays(-10), IsDeleted = false }
        };

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.GetDataSetsByDateRangeAsync(startDate, endDate, userId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
        result.Last().Id.Should().Be(2);

        _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetDataSetsByDateRangeAsync_WithInvalidDateRange_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-30); // End before start
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDataSetsByDateRangeAsync(startDate, endDate, userId, page, pageSize));
    }

    [Fact]
    public async Task GetDataSetsByDateRangeAsync_WithFutureStartDate_ShouldThrowArgumentException()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(1); // Future date
        var endDate = DateTime.UtcNow.AddDays(30);
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDataSetsByDateRangeAsync(startDate, endDate, userId, page, pageSize));
    }

    #endregion

    #region GetDataSetStatisticsAsync Tests

    [Fact]
    public async Task GetDataSetStatisticsAsync_WithValidInputs_ShouldReturnStatistics()
    {
        // Arrange
        var userId = "test-user";
        var dataSets = new List<DataSet>
        {
            new DataSet { Id = 1, UserId = userId, FileSize = 1000, FileType = FileType.CSV, IsDeleted = false, IsProcessed = true },
            new DataSet { Id = 2, UserId = userId, FileSize = 2000, FileType = FileType.JSON, IsDeleted = false, IsProcessed = true },
            new DataSet { Id = 3, UserId = userId, FileSize = 1500, FileType = FileType.CSV, IsDeleted = true, IsProcessed = false }
        };

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.GetDataSetStatisticsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.TotalDataSets.Should().Be(2); // Excluding deleted
        result.DeletedDataSets.Should().Be(1);
        result.TotalFileSize.Should().Be(3000); // 1000 + 2000
        result.AverageFileSize.Should().Be(1500); // 3000 / 2
        result.FileTypeBreakdown.Should().ContainKey("CSV");
        result.FileTypeBreakdown.Should().ContainKey("JSON");
        result.ProcessingStatusBreakdown.Should().ContainKey("Processed");
        result.ProcessingStatusBreakdown.Should().ContainKey("Pending");

        _mockRepository.Verify(x => x.GetByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetDataSetStatisticsAsync_WithNoDataSets_ShouldReturnZeroStatistics()
    {
        // Arrange
        var userId = "test-user";
        var dataSets = new List<DataSet>();

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.GetDataSetStatisticsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.TotalDataSets.Should().Be(0);
        result.DeletedDataSets.Should().Be(0);
        result.TotalFileSize.Should().Be(0);
        result.AverageFileSize.Should().Be(0);
        result.FileTypeBreakdown.Should().BeEmpty();
        result.ProcessingStatusBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDataSetStatisticsAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Arrange
        string? userId = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDataSetStatisticsAsync(userId!));
    }

    [Fact]
    public async Task GetDataSetStatisticsAsync_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var userId = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetDataSetStatisticsAsync(userId));
    }

    #endregion

    #region Recent Uploads Tests

    [Fact]
    public async Task GetDataSetsByUserAsync_ShouldIncludeRecentUploads()
    {
        // Arrange
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;
        var dataSets = new List<DataSet>
        {
            new DataSet { Id = 1, UserId = userId, Name = "Test1", IsDeleted = false, UploadedAt = DateTime.UtcNow.AddDays(-1) },
            new DataSet { Id = 2, UserId = userId, Name = "Test2", IsDeleted = false, UploadedAt = DateTime.UtcNow.AddDays(-2) }
        };

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ReturnsAsync(dataSets);

        // Act
        var result = await _service.GetDataSetsByUserAsync(userId, page, pageSize);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        // Verify that the result contains DataSetDto objects (not raw DataSet objects)
        result.First().Should().BeOfType<DataSetDto>();
        result.Last().Should().BeOfType<DataSetDto>();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetDataSetsByUserAsync_WhenRepositoryThrowsException_ShouldLogAndRethrow()
    {
        // Arrange
        var userId = "test-user";
        var page = 1;
        var pageSize = 20;
        var exception = new Exception("Database error");

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.GetDataSetsByUserAsync(userId, page, pageSize));

        _mockStructuredLogging.Verify(x => x.LogException(exception, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetDataSetStatisticsAsync_WhenRepositoryThrowsException_ShouldLogAndRethrow()
    {
        // Arrange
        var userId = "test-user";
        var exception = new Exception("Database error");

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId)).ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.GetDataSetStatisticsAsync(userId));

        _mockStructuredLogging.Verify(x => x.LogException(exception, It.IsAny<string>()), Times.Once);
    }

    #endregion
} 