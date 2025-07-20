using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.Core.Constants;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Services;
using Xunit;
using FluentAssertions;
using System.Diagnostics;

namespace Normaize.Tests.Services;

public class DataProcessingServiceTests
{
    private readonly Mock<IDataSetRepository> _mockRepository;
    private readonly Mock<IFileUploadService> _mockFileUploadService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<DataProcessingService>> _mockLogger;
    private readonly IMemoryCache _cache;
    private readonly Mock<IStructuredLoggingService> _mockStructuredLogging;
    private readonly Mock<IChaosEngineeringService> _mockChaosEngineering;
    private readonly DataProcessingService _service;

    public DataProcessingServiceTests()
    {
        _mockRepository = new Mock<IDataSetRepository>();
        _mockFileUploadService = new Mock<IFileUploadService>();
        _mockAuditService = new Mock<IAuditService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<DataProcessingService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockStructuredLogging = new Mock<IStructuredLoggingService>();
        _mockChaosEngineering = new Mock<IChaosEngineeringService>();
        
        _service = new DataProcessingService(
            _mockRepository.Object, 
            _mockFileUploadService.Object, 
            _mockAuditService.Object, 
            _mockMapper.Object, 
            _mockLogger.Object, 
            _cache,
            _mockStructuredLogging.Object,
            _mockChaosEngineering.Object);
        
        // Setup default structured logging mocks
        SetupStructuredLoggingMocks();
    }

    private void SetupStructuredLoggingMocks()
    {
        var mockContext = new Mock<IOperationContext>();
        mockContext.Setup(c => c.OperationName).Returns("TestOperation");
        mockContext.Setup(c => c.CorrelationId).Returns("test-correlation-id");
        mockContext.Setup(c => c.UserId).Returns("test-user");
        mockContext.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        mockContext.Setup(c => c.Steps).Returns(new List<string>());
        mockContext.Setup(c => c.Stopwatch).Returns(Stopwatch.StartNew());
        mockContext.Setup(c => c.SetMetadata(It.IsAny<string>(), It.IsAny<object>()));
        
        _mockStructuredLogging.Setup(s => s.CreateContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(mockContext.Object);
        _mockStructuredLogging.Setup(s => s.LogStep(It.IsAny<IOperationContext>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()));
        _mockStructuredLogging.Setup(s => s.LogSummary(It.IsAny<IOperationContext>(), It.IsAny<bool>(), It.IsAny<string>()));
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataProcessingService(
                null!, 
                _mockFileUploadService.Object, 
                _mockAuditService.Object, 
                _mockMapper.Object, 
                _mockLogger.Object, 
                _cache,
                _mockStructuredLogging.Object,
                _mockChaosEngineering.Object));
        
        exception.ParamName.Should().Be("dataSetRepository");
    }

    [Fact]
    public void Constructor_WithNullFileUploadService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataProcessingService(
                _mockRepository.Object, 
                null!, 
                _mockAuditService.Object, 
                _mockMapper.Object, 
                _mockLogger.Object, 
                _cache,
                _mockStructuredLogging.Object,
                _mockChaosEngineering.Object));
        
        exception.ParamName.Should().Be("fileUploadService");
    }

    [Fact]
    public void Constructor_WithNullAuditService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataProcessingService(
                _mockRepository.Object, 
                _mockFileUploadService.Object, 
                null!, 
                _mockMapper.Object, 
                _mockLogger.Object, 
                _cache,
                _mockStructuredLogging.Object,
                _mockChaosEngineering.Object));
        
        exception.ParamName.Should().Be("auditService");
    }

    [Fact]
    public void Constructor_WithNullMapper_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataProcessingService(
                _mockRepository.Object, 
                _mockFileUploadService.Object, 
                _mockAuditService.Object, 
                null!, 
                _mockLogger.Object, 
                _cache,
                _mockStructuredLogging.Object,
                _mockChaosEngineering.Object));
        
        exception.ParamName.Should().Be("mapper");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataProcessingService(
                _mockRepository.Object, 
                _mockFileUploadService.Object, 
                _mockAuditService.Object, 
                _mockMapper.Object, 
                null!, 
                _cache,
                _mockStructuredLogging.Object,
                _mockChaosEngineering.Object));
        
        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new DataProcessingService(
                _mockRepository.Object, 
                _mockFileUploadService.Object, 
                _mockAuditService.Object, 
                _mockMapper.Object, 
                _mockLogger.Object, 
                null!,
                _mockStructuredLogging.Object,
                _mockChaosEngineering.Object));
        
        exception.ParamName.Should().Be("cache");
    }

    [Fact]
    public async Task UploadDataSetAsync_WithValidInputs_ShouldUploadSuccessfully()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            ContentType = "text/csv",
            FileSize = 1024,
            FileStream = new MemoryStream([1, 2, 3])
        };

        var createDto = new CreateDataSetDto
        {
            Name = "Test Dataset",
            Description = "Test Description",
            UserId = "user123"
        };

        var dataSet = new DataSet
        {
            Id = 1,
            Name = "Test Dataset",
            RowCount = 100,
            ColumnCount = 5,
            FileSize = 1024
        };

        _mockFileUploadService.Setup(f => f.ValidateFileAsync(fileRequest)).ReturnsAsync(true);
        _mockFileUploadService.Setup(f => f.SaveFileAsync(fileRequest)).ReturnsAsync("/uploads/test.csv");
        _mockFileUploadService.Setup(f => f.ProcessFileAsync("/uploads/test.csv", ".csv")).ReturnsAsync(dataSet);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<DataSet>())).ReturnsAsync(dataSet);
        _mockAuditService.Setup(a => a.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadDataSetAsync(fileRequest, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.DataSetId.Should().Be(1);
        result.Message.Should().Be("Dataset uploaded successfully");
        
        _mockFileUploadService.Verify(f => f.ValidateFileAsync(fileRequest), Times.Once);
        _mockFileUploadService.Verify(f => f.SaveFileAsync(fileRequest), Times.Once);
        _mockFileUploadService.Verify(f => f.ProcessFileAsync("/uploads/test.csv", ".csv"), Times.Once);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<DataSet>()), Times.Once);
        _mockAuditService.Verify(a => a.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task UploadDataSetAsync_WithNullFileRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var createDto = new CreateDataSetDto
        {
            Name = "Test Dataset",
            UserId = "user123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.UploadDataSetAsync(null!, createDto));
        
        exception.ParamName.Should().Be("fileRequest");
    }

    [Fact]
    public async Task UploadDataSetAsync_WithNullCreateDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            ContentType = "text/csv",
            FileSize = 1024,
            FileStream = new MemoryStream([1, 2, 3])
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.UploadDataSetAsync(fileRequest, null!));
        
        exception.ParamName.Should().Be("createDto");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UploadDataSetAsync_WithInvalidFileName_ShouldThrowArgumentException(string? fileName)
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName!,
            ContentType = "text/csv",
            FileSize = 1024,
            FileStream = new MemoryStream([1, 2, 3])
        };

        var createDto = new CreateDataSetDto
        {
            Name = "Test Dataset",
            UserId = "user123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UploadDataSetAsync(fileRequest, createDto));
        
        exception.Message.Should().Contain("File name is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UploadDataSetAsync_WithInvalidDatasetName_ShouldThrowArgumentException(string? datasetName)
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            ContentType = "text/csv",
            FileSize = 1024,
            FileStream = new MemoryStream([1, 2, 3])
        };

        var createDto = new CreateDataSetDto
        {
            Name = datasetName!,
            UserId = "user123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UploadDataSetAsync(fileRequest, createDto));
        
        exception.Message.Should().Contain("Dataset name is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UploadDataSetAsync_WithInvalidUserId_ShouldThrowArgumentException(string? userId)
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            ContentType = "text/csv",
            FileSize = 1024,
            FileStream = new MemoryStream([1, 2, 3])
        };

        var createDto = new CreateDataSetDto
        {
            Name = "Test Dataset",
            UserId = userId!
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UploadDataSetAsync(fileRequest, createDto));
        
        exception.Message.Should().Contain("User ID is required");
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("file/with/path")]
    [InlineData("file\\with\\path")]
    public async Task UploadDataSetAsync_WithInvalidFilePath_ShouldThrowArgumentException(string fileName)
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            ContentType = "text/csv",
            FileSize = 1024,
            FileStream = new MemoryStream([1, 2, 3])
        };

        var createDto = new CreateDataSetDto
        {
            Name = "Test Dataset",
            UserId = "user123"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UploadDataSetAsync(fileRequest, createDto));
        
        exception.Message.Should().Contain("Invalid file name");
    }

    [Fact]
    public async Task UploadDataSetAsync_WhenFileValidationFails_ShouldReturnFailureResponse()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            ContentType = "text/csv",
            FileSize = 1024,
            FileStream = new MemoryStream([1, 2, 3])
        };

        var createDto = new CreateDataSetDto
        {
            Name = "Test Dataset",
            UserId = "user123"
        };

        _mockFileUploadService.Setup(f => f.ValidateFileAsync(fileRequest)).ReturnsAsync(false);

        // Act
        var result = await _service.UploadDataSetAsync(fileRequest, createDto);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid file format or size");
        
        _mockFileUploadService.Verify(f => f.ValidateFileAsync(fileRequest), Times.Once);
        _mockFileUploadService.Verify(f => f.SaveFileAsync(It.IsAny<FileUploadRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetDataSetAsync_WithExistingId_ShouldReturnDataSet()
    {
        // Arrange
        var dataSet = new DataSet
        {
            Id = 1,
            Name = "Test Dataset",
            UserId = "user123"
        };

        var dataSetDto = new DataSetDto
        {
            Id = 1,
            Name = "Test Dataset"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dataSet);
        _mockMapper.Setup(m => m.Map<DataSetDto>(dataSet)).Returns(dataSetDto);
        _mockAuditService.Setup(a => a.LogDataSetActionAsync(1, "user123", "Viewed", null, null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GetDataSetAsync(1, "user123");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Dataset");
        
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        _mockMapper.Verify(m => m.Map<DataSetDto>(dataSet), Times.Once);
        _mockAuditService.Verify(a => a.LogDataSetActionAsync(1, "user123", "Viewed", null, null, null), Times.Once);
    }

    [Fact]
    public async Task GetDataSetAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((DataSet?)null);

        // Act
        var result = await _service.GetDataSetAsync(999, "user123");

        // Assert
        result.Should().BeNull();
        
        _mockRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
        _mockMapper.Verify(m => m.Map<DataSetDto>(It.IsAny<DataSet>()), Times.Never);
    }

    [Fact]
    public async Task GetDataSetAsync_WithWrongUserId_ShouldReturnNull()
    {
        // Arrange
        var dataSet = new DataSet
        {
            Id = 1,
            Name = "Test Dataset",
            UserId = "user123"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetDataSetAsync(1, "differentuser");

        // Assert
        result.Should().BeNull();
        
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        _mockMapper.Verify(m => m.Map<DataSetDto>(It.IsAny<DataSet>()), Times.Never);
        _mockAuditService.Verify(a => a.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null), Times.Never);
    }

    [Fact]
    public async Task GetDataSetsByUserAsync_ShouldReturnPagedDataSets()
    {
        // Arrange
        var dataSets = new List<DataSet>
        {
            new() { Id = 1, Name = "Dataset 1", UserId = "user123" },
            new() { Id = 2, Name = "Dataset 2", UserId = "user123" },
            new() { Id = 3, Name = "Dataset 3", UserId = "user123" }
        };

        var dataSetDtos = new List<DataSetDto>
        {
            new() { Id = 1, Name = "Dataset 1" },
            new() { Id = 2, Name = "Dataset 2" },
            new() { Id = 3, Name = "Dataset 3" }
        };

        _mockRepository.Setup(r => r.GetByUserIdAsync("user123", false)).ReturnsAsync(dataSets);
        _mockMapper.Setup(m => m.Map<IEnumerable<DataSetDto>>(It.IsAny<IEnumerable<DataSet>>()))
            .Returns(dataSetDtos.Take(2)); // Simulate paging

        // Act
        var result = await _service.GetDataSetsByUserAsync("user123", 1, 2);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        _mockRepository.Verify(r => r.GetByUserIdAsync("user123", false), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<DataSetDto>>(It.IsAny<IEnumerable<DataSet>>()), Times.Once);
    }

    [Fact]
    public async Task DeleteDataSetAsync_WithExistingId_ShouldDeleteSuccessfully()
    {
        // Arrange
        var dataSet = new DataSet
        {
            Id = 1,
            Name = "Test Dataset",
            UserId = "user123",
            FilePath = "/uploads/test.csv"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dataSet);
        _mockRepository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);
        _mockFileUploadService.Setup(f => f.DeleteFileAsync("/uploads/test.csv")).Returns(Task.CompletedTask);
        _mockAuditService.Setup(a => a.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.DeleteDataSetAsync(1, "user123");

        // Assert
        result.Should().BeTrue();
        
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        _mockFileUploadService.Verify(f => f.DeleteFileAsync("/uploads/test.csv"), Times.Once);
        _mockAuditService.Verify(a => a.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task DeleteDataSetAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((DataSet?)null);

        // Act
        var result = await _service.DeleteDataSetAsync(999, "user123");

        // Assert
        result.Should().BeFalse();
        
        _mockRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDataSetAsync_WithWrongUserId_ShouldReturnFalse()
    {
        // Arrange
        var dataSet = new DataSet
        {
            Id = 1,
            Name = "Test Dataset",
            UserId = "user123"
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.DeleteDataSetAsync(1, "differentuser");

        // Assert
        result.Should().BeFalse();
        
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetDataSetStatisticsAsync_ShouldReturnCachedStatistics()
    {
        // Arrange
        var statistics = new DataSetStatisticsDto
        {
            TotalCount = 5,
            TotalSize = 1024
        };

        var cacheKey = "stats_user123";
        _cache.Set(cacheKey, statistics, TimeSpan.FromMinutes(5));

        // Act
        var result = await _service.GetDataSetStatisticsAsync("user123");

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(5);
        result.TotalSize.Should().Be(1024);
        
        _mockRepository.Verify(r => r.GetTotalCountAsync(It.IsAny<string>()), Times.Never);
        _mockRepository.Verify(r => r.GetTotalSizeAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetDataSetStatisticsAsync_WhenNotCached_ShouldCalculateAndCache()
    {
        // Arrange
        var statistics = new DataSetStatisticsDto
        {
            TotalCount = 3,
            TotalSize = 512
        };

        var recentlyModified = new List<DataSet>
        {
            new() { Id = 1, Name = "Recent 1" },
            new() { Id = 2, Name = "Recent 2" }
        };

        var dataSetDtos = new List<DataSetDto>
        {
            new() { Id = 1, Name = "Recent 1" },
            new() { Id = 2, Name = "Recent 2" }
        };

        _mockRepository.Setup(r => r.GetTotalCountAsync("user123")).ReturnsAsync(3);
        _mockRepository.Setup(r => r.GetTotalSizeAsync("user123")).ReturnsAsync(512L);
        _mockRepository.Setup(r => r.GetRecentlyModifiedAsync("user123", 5)).ReturnsAsync(recentlyModified);
        _mockMapper.Setup(m => m.Map<IEnumerable<DataSetDto>>(recentlyModified)).Returns(dataSetDtos);

        // Act
        var result = await _service.GetDataSetStatisticsAsync("user123");

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.TotalSize.Should().Be(512);
        result.RecentlyModified.Should().HaveCount(2);
        
        _mockRepository.Verify(r => r.GetTotalCountAsync("user123"), Times.Once);
        _mockRepository.Verify(r => r.GetTotalSizeAsync("user123"), Times.Once);
        _mockRepository.Verify(r => r.GetRecentlyModifiedAsync("user123", 5), Times.Once);
        _mockMapper.Verify(m => m.Map<IEnumerable<DataSetDto>>(recentlyModified), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDataSetAsync_WithInvalidId_ShouldThrowArgumentException(int id)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.GetDataSetAsync(id, "user123"));
        
        exception.Message.Should().Contain("Failed to complete GetDataSetAsync for user user123");
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain(AppConstants.ValidationMessages.DATASET_ID_MUST_BE_POSITIVE);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetDataSetAsync_WithInvalidUserId_ShouldThrowArgumentException(string? userId)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.GetDataSetAsync(1, userId!));
        
        exception.Message.Should().Contain("Failed to complete GetDataSetAsync for user");
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("User ID is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetDataSetsByUserAsync_WithInvalidPage_ShouldThrowArgumentException(int page)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.GetDataSetsByUserAsync("user123", page));
        
        exception.Message.Should().Contain("Failed to complete GetDataSetsByUserAsync for user user123");
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Page must be positive");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task GetDataSetsByUserAsync_WithInvalidPageSize_ShouldThrowArgumentException(int pageSize)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.GetDataSetsByUserAsync("user123", 1, pageSize));
        
        exception.Message.Should().Contain("Failed to complete GetDataSetsByUserAsync for user user123");
        exception.InnerException.Should().BeOfType<ArgumentException>();
        exception.InnerException!.Message.Should().Contain("Page size must be between 1 and 100");
    }
} 