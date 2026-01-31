using FluentAssertions;
using Moq;
using Normaize.DataNormalization.Application.Commands.DataSets;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Domain.Aggregates;
using Normaize.DataNormalization.Domain.Entities;
using Normaize.DataNormalization.Domain.Repositories;
using Normaize.DataNormalization.Domain.ValueObjects;

namespace Normaize.DataNormalization.Application.Tests.Commands.DataSets;

public class UploadDataSetCommandHandlerTests
{
    private const long FiveMB = 5 * 1024 * 1024;

    private readonly Mock<IDataSetRepository> _dataSetRepository;
    private readonly Mock<IFileStorageService> _fileStorageService;
    private readonly Mock<IFileProcessingService> _fileProcessingService;
    private readonly Mock<IAuditService> _auditService;
    private readonly Mock<INormalizationJobRepository> _jobRepository;
    private readonly Mock<IJobQueue> _jobQueue;
    private readonly UploadDataSetCommandHandler _handler;

    public UploadDataSetCommandHandlerTests()
    {
        _dataSetRepository = new Mock<IDataSetRepository>();
        _fileStorageService = new Mock<IFileStorageService>();
        _fileProcessingService = new Mock<IFileProcessingService>();
        _auditService = new Mock<IAuditService>();
        _jobRepository = new Mock<INormalizationJobRepository>();
        _jobQueue = new Mock<IJobQueue>();

        _handler = new UploadDataSetCommandHandler(
            _dataSetRepository.Object,
            _fileStorageService.Object,
            _fileProcessingService.Object,
            _auditService.Object,
            _jobRepository.Object,
            _jobQueue.Object);
    }

    [Fact]
    public async Task Handle_SmallFile_ShouldProcessSynchronously()
    {
        // Arrange
        var fileSize = 1 * 1024 * 1024; // 1MB - below 5MB threshold
        var command = CreateCommand(fileSize);
        var savedFilePath = "s3://bucket/file.csv";

        SetupSuccessfulValidation();
        SetupFileStorage(savedFilePath);
        SetupSynchronousProcessing();
        SetupRepository();
        SetupAuditService();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.DataSetId.Should().NotBeNull();
        result.ProcessingJobId.Should().BeNull(); // No async job for small files
        result.Message.Should().Contain("processed successfully");

        // Verify synchronous processing was called
        _fileProcessingService.Verify(
            x => x.ProcessFileAsync(
                savedFilePath,
                It.Is<FileType>(ft => ft != null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify no job was created
        _jobRepository.Verify(
            x => x.SaveAsync(It.IsAny<NormalizationJob>()),
            Times.Never);

        _jobQueue.Verify(
            x => x.EnqueueAsync(It.IsAny<NormalizationJob>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_LargeFile_ShouldProcessAsynchronously()
    {
        // Arrange
        var fileSize = 10 * 1024 * 1024; // 10MB - above 5MB threshold
        var command = CreateCommand(fileSize);
        var savedFilePath = "s3://bucket/large-file.csv";
        var dataSetId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        SetupSuccessfulValidation();
        SetupFileStorage(savedFilePath);
        SetupRepositoryForAsyncProcessing(dataSetId);
        SetupJobRepositoryAndQueue(jobId);
        SetupAuditService();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.DataSetId.Should().Be(dataSetId);
        result.ProcessingJobId.Should().Be(jobId);
        result.Message.Should().Contain("Processing in background");

        // Verify synchronous processing was NOT called
        _fileProcessingService.Verify(
            x => x.ProcessFileAsync(
                It.IsAny<string>(),
                It.Is<FileType>(ft => ft != null),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Verify job was created and queued
        _jobRepository.Verify(
            x => x.SaveAsync(It.IsAny<NormalizationJob>()),
            Times.Once);

        _jobQueue.Verify(
            x => x.EnqueueAsync(It.IsAny<NormalizationJob>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExactlyFiveMB_ShouldProcessAsynchronously()
    {
        // Arrange - Test boundary condition
        var fileSize = FiveMB; // Exactly 5MB
        var command = CreateCommand(fileSize);
        var savedFilePath = "s3://bucket/boundary-file.csv";
        var dataSetId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        SetupSuccessfulValidation();
        SetupFileStorage(savedFilePath);
        SetupRepositoryForAsyncProcessing(dataSetId);
        SetupJobRepositoryAndQueue(jobId);
        SetupAuditService();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessingJobId.Should().NotBeNull();
        result.Message.Should().Contain("Processing in background");

        // Verify async path was taken
        _jobRepository.Verify(
            x => x.SaveAsync(It.IsAny<NormalizationJob>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidationFailure_ShouldReturnFailureResult()
    {
        // Arrange
        var command = CreateCommand(1024);
        var validationError = "Invalid file format";

        _fileProcessingService
            .Setup(x => x.ValidateFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(false, validationError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be(validationError);
        result.DataSetId.Should().BeNull();
        result.ProcessingJobId.Should().BeNull();

        // Verify no storage or processing occurred
        _fileStorageService.Verify(
            x => x.SaveFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StorageFailure_ShouldReturnFailureResult()
    {
        // Arrange
        var command = CreateCommand(1024);
        var storageError = "Failed to save file to S3";

        SetupSuccessfulValidation();

        _fileStorageService
            .Setup(x => x.SaveFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(storageError));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain(storageError);
        result.DataSetId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SyncProcessingFailure_ShouldSaveDataSetWithFailedStatus()
    {
        // Arrange
        var fileSize = 1 * 1024 * 1024; // Small file
        var command = CreateCommand(fileSize);
        var savedFilePath = "s3://bucket/file.csv";
        var processingError = "Failed to parse CSV";

        SetupSuccessfulValidation();
        SetupFileStorage(savedFilePath);

        _fileProcessingService
            .Setup(x => x.ProcessFileAsync(
                It.IsAny<string>(),
                It.Is<FileType>(ft => ft != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(
                IsSuccess: false,
                Error: processingError));

        SetupRepository();
        SetupAuditService();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue(); // Upload succeeded even if processing failed
        result.DataSetId.Should().NotBeNull();

        // Verify dataset was saved (with failed processing status)
        _dataSetRepository.Verify(
            x => x.AddAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Helper methods

    private static UploadDataSetCommand CreateCommand(long fileSize)
    {
        var stream = new MemoryStream();
        return new UploadDataSetCommand(
            Name: "Test Dataset",
            Description: "Test Description",
            UserId: "test-user-123",
            FileName: "test.csv",
            FilePath: "/temp/test.csv",
            FileSize: fileSize,
            FileStream: stream,
            RetentionDays: null);
    }

    private void SetupSuccessfulValidation()
    {
        _fileProcessingService
            .Setup(x => x.ValidateFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileValidationResult(true, null));
    }

    private void SetupFileStorage(string savedFilePath)
    {
        _fileStorageService
            .Setup(x => x.SaveFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedFilePath);
    }

    private void SetupSynchronousProcessing()
    {
        _fileProcessingService
            .Setup(x => x.ProcessFileAsync(
                It.IsAny<string>(),
                It.Is<FileType>(ft => ft != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileProcessingResult(
                IsSuccess: true,
                Schema: "test_schema",
                RowCount: 100,
                ColumnCount: 5,
                PreviewData: "preview_data"));
    }

    private void SetupRepository()
    {
        _dataSetRepository
            .Setup(x => x.AddAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataSet ds, CancellationToken _) => ds);
    }

    private void SetupRepositoryForAsyncProcessing(Guid dataSetId)
    {
        _dataSetRepository
            .Setup(x => x.AddAsync(It.IsAny<DataSet>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DataSet ds, CancellationToken _) =>
            {
                // Use reflection to set the Id for async scenario
                var idProperty = typeof(DataSet).GetProperty("Id")!;
                idProperty.SetValue(ds, dataSetId);
                return ds;
            });
    }

    private void SetupJobRepositoryAndQueue(Guid jobId)
    {
        _jobRepository
            .Setup(x => x.SaveAsync(It.IsAny<NormalizationJob>()))
            .Callback<NormalizationJob>(job =>
            {
                // Use reflection to set the job Id
                var idProperty = typeof(NormalizationJob).GetProperty("Id")!;
                idProperty.SetValue(job, jobId);
            })
            .Returns(Task.CompletedTask);

        _jobQueue
            .Setup(x => x.EnqueueAsync(It.IsAny<NormalizationJob>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupAuditService()
    {
        _auditService
            .Setup(x => x.LogDataSetActionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }
}
