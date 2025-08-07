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

public class DataSetLifecycleServiceTests
{
    private readonly Mock<IDataSetRepository> _mockRepository;
    private readonly Mock<IFileUploadService> _mockFileUploadService;
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IDataProcessingInfrastructure> _mockInfrastructure;
    private readonly Mock<IStructuredLoggingService> _mockStructuredLogging;
    private readonly Mock<IChaosEngineeringService> _mockChaosEngineering;
    private readonly Mock<IOperationContext> _mockOperationContext;
    private readonly DataSetLifecycleService _service;

    public DataSetLifecycleServiceTests()
    {
        _mockRepository = new Mock<IDataSetRepository>();
        _mockFileUploadService = new Mock<IFileUploadService>();
        _mockAuditService = new Mock<IAuditService>();
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

        _service = new DataSetLifecycleService(
            _mockRepository.Object,
            _mockFileUploadService.Object,
            _mockAuditService.Object,
            _mockInfrastructure.Object);
    }

    #region RestoreDataSetEnhancedAsync Tests

    [Fact]
    public async Task RestoreDataSetEnhancedAsync_WithValidInputsAndSimpleRestore_ShouldRestoreSuccessfully()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var restoreDto = new DataSetRestoreDto { RestoreType = RestoreType.Simple };
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, IsDeleted = true, DeletedAt = DateTime.UtcNow };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>())).ReturnsAsync(dataSet);
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RestoreDataSetEnhancedAsync(dataSetId, restoreDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("restored successfully");
        result.Data.Should().NotBeNull();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => !ds.IsDeleted && ds.DeletedAt == null)), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(dataSetId, userId, AppConstants.DataSetLifecycle.RESTORE_DATA_SET_SIMPLE, It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task RestoreDataSetEnhancedAsync_WithValidInputsAndFullRestore_ShouldRestoreAndResetProcessing()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var restoreDto = new DataSetRestoreDto { RestoreType = RestoreType.WithReset };
        var dataSet = new DataSet 
        { 
            Id = dataSetId, 
            UserId = userId, 
            IsDeleted = true, 
            DeletedAt = DateTime.UtcNow,
            IsProcessed = true,
            ProcessedAt = DateTime.UtcNow,
            PreviewData = "test",
            Schema = "test",
            RowCount = 10,
            ColumnCount = 5
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>())).ReturnsAsync(dataSet);
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RestoreDataSetEnhancedAsync(dataSetId, restoreDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("restored successfully");
        result.Data.Should().NotBeNull();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => 
            !ds.IsDeleted && 
            ds.DeletedAt == null &&
            !ds.IsProcessed &&
            ds.ProcessedAt == null &&
            ds.PreviewData == null &&
            ds.Schema == null &&
            ds.RowCount == 0 &&
            ds.ColumnCount == 0)), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(dataSetId, userId, AppConstants.DataSetLifecycle.RESTORE_DATA_SET_FULL, It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task RestoreDataSetEnhancedAsync_WithNonDeletedDataset_ShouldReturnSuccessWithoutAction()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var restoreDto = new DataSetRestoreDto { RestoreType = RestoreType.Simple };
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, IsDeleted = false };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.RestoreDataSetEnhancedAsync(dataSetId, restoreDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("not deleted");
        result.Data.Should().NotBeNull();

        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<DataSet>()), Times.Never);
    }

    [Fact]
    public async Task RestoreDataSetEnhancedAsync_WithInvalidDataSetId_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = 0;
        var userId = "test-user";
        var restoreDto = new DataSetRestoreDto { RestoreType = RestoreType.Simple };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.RestoreDataSetEnhancedAsync(dataSetId, restoreDto, userId));
    }

    [Fact]
    public async Task RestoreDataSetEnhancedAsync_WithNullUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = 1;
        string? userId = null;
        var restoreDto = new DataSetRestoreDto { RestoreType = RestoreType.Simple };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.RestoreDataSetEnhancedAsync(dataSetId, restoreDto, userId));
    }

    [Fact]
    public async Task RestoreDataSetEnhancedAsync_WithNullRestoreDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        DataSetRestoreDto? restoreDto = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.RestoreDataSetEnhancedAsync(dataSetId, restoreDto, userId));
    }

    [Fact]
    public async Task RestoreDataSetEnhancedAsync_WithDatasetNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var restoreDto = new DataSetRestoreDto { RestoreType = RestoreType.Simple };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync((DataSet?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.RestoreDataSetEnhancedAsync(dataSetId, restoreDto, userId));
    }

    [Fact]
    public async Task RestoreDataSetEnhancedAsync_WithUnauthorizedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var differentUserId = "different-user";
        var restoreDto = new DataSetRestoreDto { RestoreType = RestoreType.Simple };
        var dataSet = new DataSet { Id = dataSetId, UserId = differentUserId };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _service.RestoreDataSetEnhancedAsync(dataSetId, restoreDto, userId));
    }

    #endregion

    #region ResetDataSetAsync Tests

    [Fact]
    public async Task ResetDataSetAsync_WithValidInputsAndOriginalFileReset_ShouldResetSuccessfully()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var resetDto = new DataSetResetDto { ResetType = ResetType.OriginalFile };
        var dataSet = new DataSet 
        { 
            Id = dataSetId, 
            UserId = userId, 
            FilePath = "test/path.csv",
            FileName = "test.csv",
            IsProcessed = true,
            ProcessedAt = DateTime.UtcNow,
            PreviewData = "test",
            Schema = "test",
            RowCount = 10,
            ColumnCount = 5
        };

        var processedDataSet = new DataSet
        {
            PreviewData = "new-preview",
            Schema = "new-schema",
            RowCount = 20,
            ColumnCount = 8,
            DataHash = "new-hash"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>())).ReturnsAsync(dataSet);
        _mockFileUploadService.Setup(x => x.FileExistsAsync(dataSet.FilePath)).ReturnsAsync(true);
        _mockFileUploadService.Setup(x => x.ProcessFileAsync(dataSet.FilePath, ".csv")).ReturnsAsync(processedDataSet);
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResetDataSetAsync(dataSetId, resetDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("reset successfully");
        result.Data.Should().NotBeNull();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => 
            ds.IsProcessed &&
            ds.ProcessedAt != null &&
            ds.PreviewData == "new-preview" &&
            ds.Schema == "new-schema" &&
            ds.RowCount == 20 &&
            ds.ColumnCount == 8 &&
            ds.DataHash == "new-hash")), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(dataSetId, userId, AppConstants.DataSetLifecycle.RESET_DATA_SET_FILE_BASED, It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task ResetDataSetAsync_WithValidInputsAndDatabaseReset_ShouldResetProcessingOnly()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var resetDto = new DataSetResetDto { ResetType = ResetType.Database };
        var dataSet = new DataSet 
        { 
            Id = dataSetId, 
            UserId = userId, 
            IsProcessed = true,
            ProcessedAt = DateTime.UtcNow,
            PreviewData = "test",
            Schema = "test",
            RowCount = 10,
            ColumnCount = 5
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>())).ReturnsAsync(dataSet);
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResetDataSetAsync(dataSetId, resetDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("reset successfully");
        result.Data.Should().NotBeNull();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => 
            !ds.IsProcessed &&
            ds.ProcessedAt == null &&
            ds.PreviewData == null &&
            ds.Schema == null &&
            ds.RowCount == 0 &&
            ds.ColumnCount == 0)), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(dataSetId, userId, AppConstants.DataSetLifecycle.RESET_DATA_SET_DATABASE_ONLY, It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task ResetDataSetAsync_WithFileNotAvailable_ShouldReturnFailure()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var resetDto = new DataSetResetDto { ResetType = ResetType.OriginalFile };
        var dataSet = new DataSet 
        { 
            Id = dataSetId, 
            UserId = userId, 
            FilePath = "test/path.csv",
            FileName = "test.csv"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockFileUploadService.Setup(x => x.FileExistsAsync(dataSet.FilePath)).ReturnsAsync(false);

        // Act
        var result = await _service.ResetDataSetAsync(dataSetId, resetDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Cannot reset dataset");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetDataSetAsync_WithDeletedDataset_ShouldRestoreAndReset()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var resetDto = new DataSetResetDto { ResetType = ResetType.Database };
        var dataSet = new DataSet 
        { 
            Id = dataSetId, 
            UserId = userId, 
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>())).ReturnsAsync(dataSet);
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ResetDataSetAsync(dataSetId, resetDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => !ds.IsDeleted && ds.DeletedAt == null)), Times.Once);
    }

    #endregion

    #region UpdateRetentionPolicyAsync Tests

    [Fact]
    public async Task UpdateRetentionPolicyAsync_WithValidInputs_ShouldUpdateSuccessfully()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var retentionDto = new DataSetRetentionDto { RetentionDays = 30 };
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, RetentionDays = 7 };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>())).ReturnsAsync(dataSet);
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateRetentionPolicyAsync(dataSetId, retentionDto, userId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("updated successfully");
        result.Data.Should().NotBeNull();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => 
            ds.RetentionDays == 30 &&
            ds.RetentionExpiryDate.HasValue)), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(dataSetId, userId, "UpdateRetentionPolicy", It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task UpdateRetentionPolicyAsync_WithInvalidRetentionDays_ShouldThrowArgumentException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var retentionDto = new DataSetRetentionDto { RetentionDays = 0 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UpdateRetentionPolicyAsync(dataSetId, retentionDto, userId));
    }

    [Fact]
    public async Task UpdateRetentionPolicyAsync_WithNullRetentionDto_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        DataSetRetentionDto? retentionDto = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _service.UpdateRetentionPolicyAsync(dataSetId, retentionDto!, userId));
    }

    #endregion

    #region GetRetentionStatusAsync Tests

    [Fact]
    public async Task GetRetentionStatusAsync_WithValidInputs_ShouldReturnStatus()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet 
        { 
            Id = dataSetId, 
            UserId = userId, 
            RetentionDays = 30,
            RetentionExpiryDate = DateTime.UtcNow.AddDays(15)
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetRetentionStatusAsync(dataSetId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.DataSetId.Should().Be(dataSetId);
        result.RetentionDays.Should().Be(30);
        result.RetentionExpiryDate.Should().Be(dataSet.RetentionExpiryDate);
        result.IsRetentionExpired.Should().BeFalse();
        result.DaysUntilExpiry.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetRetentionStatusAsync_WithExpiredRetention_ShouldReturnExpiredStatus()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet 
        { 
            Id = dataSetId, 
            UserId = userId, 
            RetentionDays = 30,
            RetentionExpiryDate = DateTime.UtcNow.AddDays(-5)
        };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.GetRetentionStatusAsync(dataSetId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.IsRetentionExpired.Should().BeTrue();
        result.DaysUntilExpiry.Should().Be(0);
    }

    #endregion

    #region RestoreDataSetAsync Tests

    [Fact]
    public async Task RestoreDataSetAsync_WithValidInputs_ShouldRestoreSuccessfully()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, IsDeleted = true, DeletedAt = DateTime.UtcNow };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<DataSet>())).ReturnsAsync(dataSet);
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RestoreDataSetAsync(dataSetId, userId);

        // Assert
        result.Should().BeTrue();

        _mockRepository.Verify(x => x.UpdateAsync(It.Is<DataSet>(ds => !ds.IsDeleted && ds.DeletedAt == null)), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(dataSetId, userId, "RestoreDataSet", It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task RestoreDataSetAsync_WithNonDeletedDataset_ShouldReturnTrueWithoutAction()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, IsDeleted = false };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);

        // Act
        var result = await _service.RestoreDataSetAsync(dataSetId, userId);

        // Assert
        result.Should().BeTrue();

        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<DataSet>()), Times.Never);
    }

    #endregion

    #region HardDeleteDataSetAsync Tests

    [Fact]
    public async Task HardDeleteDataSetAsync_WithValidInputs_ShouldDeleteSuccessfully()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, FilePath = "test/path.csv" };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.DeleteAsync(dataSetId)).ReturnsAsync(true);
        _mockFileUploadService.Setup(x => x.DeleteFileAsync(dataSet.FilePath)).Returns(Task.CompletedTask);
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.HardDeleteDataSetAsync(dataSetId, userId);

        // Assert
        result.Should().BeTrue();

        _mockFileUploadService.Verify(x => x.DeleteFileAsync(dataSet.FilePath), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(dataSetId), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(dataSetId, userId, "HardDeleteDataSet", It.IsAny<object>(), null, null), Times.Once);
    }

    [Fact]
    public async Task HardDeleteDataSetAsync_WithFileDeletionFailure_ShouldContinueWithDatabaseDeletion()
    {
        // Arrange
        var dataSetId = 1;
        var userId = "test-user";
        var dataSet = new DataSet { Id = dataSetId, UserId = userId, FilePath = "test/path.csv" };

        _mockRepository.Setup(x => x.GetByIdAsync(dataSetId)).ReturnsAsync(dataSet);
        _mockRepository.Setup(x => x.DeleteAsync(dataSetId)).ReturnsAsync(true);
        _mockFileUploadService.Setup(x => x.DeleteFileAsync(dataSet.FilePath)).ThrowsAsync(new Exception("File deletion failed"));
        _mockAuditService.Setup(x => x.LogDataSetActionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), null, null))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.HardDeleteDataSetAsync(dataSetId, userId);

        // Assert
        result.Should().BeTrue();

        _mockRepository.Verify(x => x.DeleteAsync(dataSetId), Times.Once);
        _mockAuditService.Verify(x => x.LogDataSetActionAsync(dataSetId, userId, "HardDeleteDataSet", It.IsAny<object>(), null, null), Times.Once);
    }

    #endregion
} 