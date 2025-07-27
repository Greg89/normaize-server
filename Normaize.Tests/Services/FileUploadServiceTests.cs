using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.Core.DTOs;
using Normaize.Core.Interfaces;
using Normaize.Core.Models;
using Normaize.Core.Services;
using System.Text;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Configuration.Memory;
using Normaize.Core.Configuration;

namespace Normaize.Tests.Services;

public class FileUploadServiceTests
{
    private readonly Mock<IFileUploadServices> _mockFileUploadServices;
    private readonly Mock<IFileValidationService> _mockValidationService;
    private readonly Mock<IFileProcessingService> _mockProcessingService;
    private readonly Mock<IFileConfigurationService> _mockConfigurationService;
    private readonly Mock<IFileUtilityService> _mockUtilityService;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<IDataProcessingInfrastructure> _mockInfrastructure;
    private readonly FileUploadService _service;
    private readonly FileUploadConfiguration _fileUploadConfig;
    private readonly DataProcessingConfiguration _dataProcessingConfig;

    public FileUploadServiceTests()
    {
        // Setup configuration objects
        _fileUploadConfig = new FileUploadConfiguration
        {
            MaxFileSize = 10485760, // 10MB
            AllowedExtensions = new[] { ".csv", ".json", ".xlsx", ".xls", ".xml", ".parquet", ".txt" },
            MaxPreviewRows = 100,
            MaxConcurrentUploads = 5,
            EnableCompression = true,
            BlockedExtensions = new[] { ".exe", ".bat", ".cmd", ".ps1", ".sh", ".dll", ".so", ".dylib" }
        };

        _dataProcessingConfig = new DataProcessingConfiguration
        {
            MaxRowsPerDataset = 10000,
            MaxColumnsPerDataset = 100,
            MaxPreviewRows = 10,
            EnableDataValidation = true,
            EnableSchemaInference = true,
            MaxProcessingTimeSeconds = 30
        };

        // Create mock sub-services
        _mockValidationService = new Mock<IFileValidationService>();
        _mockProcessingService = new Mock<IFileProcessingService>();
        _mockConfigurationService = new Mock<IFileConfigurationService>();
        _mockUtilityService = new Mock<IFileUtilityService>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockInfrastructure = new Mock<IDataProcessingInfrastructure>();

        // Setup infrastructure mocks
        SetupInfrastructureMocks();

        // Setup configuration service to handle constructor calls
        _mockConfigurationService.Setup(x => x.ValidateConfiguration()).Verifiable();
        _mockConfigurationService.Setup(x => x.LogConfiguration()).Verifiable();

        // Setup mock composite service
        _mockFileUploadServices = new Mock<IFileUploadServices>();
        _mockFileUploadServices.Setup(x => x.Validation).Returns(_mockValidationService.Object);
        _mockFileUploadServices.Setup(x => x.Processing).Returns(_mockProcessingService.Object);
        _mockFileUploadServices.Setup(x => x.Configuration).Returns(_mockConfigurationService.Object);
        _mockFileUploadServices.Setup(x => x.Utility).Returns(_mockUtilityService.Object);
        _mockFileUploadServices.Setup(x => x.Storage).Returns(_mockFileStorageService.Object);

        _service = new FileUploadService(
            _mockFileUploadServices.Object,
            _mockInfrastructure.Object);
    }

    #region SaveFileAsync Tests

    [Fact]
    public async Task SaveFileAsync_ValidRequest_ReturnsFilePath()
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        var expectedPath = "uploads/test.csv";

        _mockValidationService.Setup(x => x.ValidateFileAsync(fileRequest)).ReturnsAsync(true);
        _mockFileStorageService.Setup(x => x.SaveFileAsync(fileRequest)).ReturnsAsync(expectedPath);

        // Act
        var result = await _service.SaveFileAsync(fileRequest);

        // Assert
        Assert.Equal(expectedPath, result);
        _mockValidationService.Verify(x => x.ValidateFileAsync(fileRequest), Times.Once);
        _mockFileStorageService.Verify(x => x.SaveFileAsync(fileRequest), Times.Once);
    }

    [Fact]
    public async Task SaveFileAsync_ValidationFails_ThrowsException()
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        _mockValidationService.Setup(x => x.ValidateFileAsync(fileRequest)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<FileValidationException>(() =>
            _service.SaveFileAsync(fileRequest));
    }

    [Fact]
    public async Task SaveFileAsync_StorageServiceThrowsException_PropagatesException()
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        var expectedException = new InvalidOperationException("Storage error");

        _mockValidationService.Setup(x => x.ValidateFileAsync(fileRequest)).ReturnsAsync(true);
        _mockFileStorageService.Setup(x => x.SaveFileAsync(fileRequest)).ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileUploadException>(() =>
            _service.SaveFileAsync(fileRequest));
        Assert.Contains("Failed to save file", exception.Message);
    }

    #endregion

    #region ValidateFileAsync Tests

    [Fact]
    public async Task ValidateFileAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        _mockValidationService.Setup(x => x.ValidateFileAsync(fileRequest)).ReturnsAsync(true);

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.True(result);
        _mockValidationService.Verify(x => x.ValidateFileAsync(fileRequest), Times.Once);
    }

    [Fact]
    public async Task ValidateFileAsync_InvalidFile_ReturnsFalse()
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        _mockValidationService.Setup(x => x.ValidateFileAsync(fileRequest)).ReturnsAsync(false);

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.False(result);
        _mockValidationService.Verify(x => x.ValidateFileAsync(fileRequest), Times.Once);
    }

    [Theory]
    [InlineData(".csv")]
    [InlineData(".json")]
    [InlineData(".xlsx")]
    [InlineData(".xls")]
    [InlineData(".xml")]
    [InlineData(".parquet")]
    [InlineData(".txt")]
    public async Task ValidateFileAsync_ValidExtension_ReturnsTrue(string validExtension)
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        fileRequest.FileName = $"test{validExtension}";
        _mockValidationService.Setup(x => x.ValidateFileAsync(fileRequest)).ReturnsAsync(true);

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.True(result);
        _mockValidationService.Verify(x => x.ValidateFileAsync(fileRequest), Times.Once);
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".doc")]
    [InlineData(".exe")]
    [InlineData(".zip")]
    public async Task ValidateFileAsync_InvalidExtension_ReturnsFalse(string invalidExtension)
    {
        // Arrange
        var fileRequest = CreateValidFileUploadRequest();
        fileRequest.FileName = $"test{invalidExtension}";
        _mockValidationService.Setup(x => x.ValidateFileAsync(fileRequest)).ReturnsAsync(false);

        // Act
        var result = await _service.ValidateFileAsync(fileRequest);

        // Assert
        Assert.False(result);
        _mockValidationService.Verify(x => x.ValidateFileAsync(fileRequest), Times.Once);
    }

    #endregion

    #region ProcessFileAsync Tests

    [Fact]
    public async Task ProcessFileAsync_ValidFile_ReturnsDataSet()
    {
        // Arrange
        var filePath = "test.csv";
        var fileType = ".csv";
        var expectedDataSet = new DataSet
        {
            FileName = "test.csv",
            FileType = FileType.CSV,
            RowCount = 10,
            ColumnCount = 5
        };

        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType)).ReturnsAsync(expectedDataSet);

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        Assert.Equal(expectedDataSet, result);
        _mockProcessingService.Verify(x => x.ProcessFileAsync(filePath, fileType), Times.Once);
    }

    [Fact]
    public async Task ProcessFileAsync_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = "nonexistent.csv";
        var fileType = ".csv";
        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.ProcessFileAsync(filePath, fileType));
        Assert.Contains("File not found", exception.Message);
    }

    [Fact]
    public async Task ProcessFileAsync_UnsupportedFileType_ThrowsUnsupportedFileTypeException()
    {
        // Arrange
        var filePath = "test.unsupported";
        var fileType = ".unsupported";
        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType))
            .ThrowsAsync(new UnsupportedFileTypeException("File type .unsupported is not supported"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnsupportedFileTypeException>(() =>
            _service.ProcessFileAsync(filePath, fileType));
        Assert.Contains("File type .unsupported is not supported", exception.Message);
    }

    [Fact]
    public async Task ProcessFileAsync_ProcessingError_ThrowsException()
    {
        // Arrange
        var filePath = "test.csv";
        var fileType = ".csv";
        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType))
            .ThrowsAsync(new FileProcessingException("Processing error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileProcessingException>(() =>
            _service.ProcessFileAsync(filePath, fileType));
        Assert.Contains("Processing error", exception.Message);
    }

    [Fact]
    public async Task ProcessFileAsync_CsvFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.csv";
        var fileType = ".csv";
        var expectedDataSet = new DataSet
        {
            FileName = "test.csv",
            FileType = FileType.CSV,
            RowCount = 5,
            ColumnCount = 3
        };

        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType)).ReturnsAsync(expectedDataSet);

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        Assert.Equal(expectedDataSet, result);
        Assert.Equal(FileType.CSV, result.FileType);
        Assert.Equal(5, result.RowCount);
        Assert.Equal(3, result.ColumnCount);
    }

    [Fact]
    public async Task ProcessFileAsync_JsonFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.json";
        var fileType = ".json";
        var expectedDataSet = new DataSet
        {
            FileName = "test.json",
            FileType = FileType.JSON,
            RowCount = 10,
            ColumnCount = 4
        };

        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType)).ReturnsAsync(expectedDataSet);

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        Assert.Equal(expectedDataSet, result);
        Assert.Equal(FileType.JSON, result.FileType);
    }

    [Fact]
    public async Task ProcessFileAsync_ExcelFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.xlsx";
        var fileType = ".xlsx";
        var expectedDataSet = new DataSet
        {
            FileName = "test.xlsx",
            FileType = FileType.Excel,
            RowCount = 15,
            ColumnCount = 6
        };

        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType)).ReturnsAsync(expectedDataSet);

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        Assert.Equal(expectedDataSet, result);
        Assert.Equal(FileType.Excel, result.FileType);
    }

    [Fact]
    public async Task ProcessFileAsync_XmlFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.xml";
        var fileType = ".xml";
        var expectedDataSet = new DataSet
        {
            FileName = "test.xml",
            FileType = FileType.XML,
            RowCount = 8,
            ColumnCount = 5
        };

        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType)).ReturnsAsync(expectedDataSet);

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        Assert.Equal(expectedDataSet, result);
        Assert.Equal(FileType.XML, result.FileType);
    }

    [Fact]
    public async Task ProcessFileAsync_TextFile_ProcessesCorrectly()
    {
        // Arrange
        var filePath = "test.txt";
        var fileType = ".txt";
        var expectedDataSet = new DataSet
        {
            FileName = "test.txt",
            FileType = FileType.TXT,
            RowCount = 20,
            ColumnCount = 1
        };

        _mockProcessingService.Setup(x => x.ProcessFileAsync(filePath, fileType)).ReturnsAsync(expectedDataSet);

        // Act
        var result = await _service.ProcessFileAsync(filePath, fileType);

        // Assert
        Assert.Equal(expectedDataSet, result);
        Assert.Equal(FileType.TXT, result.FileType);
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_ValidPath_CallsStorageService()
    {
        // Arrange
        var filePath = "test.csv";
        _mockFileStorageService.Setup(x => x.DeleteFileAsync(filePath)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteFileAsync(filePath);

        // Assert
        _mockFileStorageService.Verify(x => x.DeleteFileAsync(filePath), Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_StorageServiceThrowsException_LogsError()
    {
        // Arrange
        var filePath = "test.csv";
        var expectedException = new InvalidOperationException("Delete error");
        _mockFileStorageService.Setup(x => x.DeleteFileAsync(filePath)).ThrowsAsync(expectedException);

        // Act & Assert - should not throw, just log error
        await _service.DeleteFileAsync(filePath);
        _mockFileStorageService.Verify(x => x.DeleteFileAsync(filePath), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupInfrastructureMocks()
    {
        var mockStructuredLogging = new Mock<IStructuredLoggingService>();
        var mockChaosEngineering = new Mock<IChaosEngineeringService>();
        var mockOperationContext = new Mock<IOperationContext>();

        _mockInfrastructure.Setup(x => x.StructuredLogging).Returns(mockStructuredLogging.Object);
        _mockInfrastructure.Setup(x => x.ChaosEngineering).Returns(mockChaosEngineering.Object);

        mockStructuredLogging.Setup(x => x.CreateContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns(mockOperationContext.Object);
        mockStructuredLogging.Setup(x => x.LogStep(It.IsAny<IOperationContext>(), It.IsAny<string>(), It.IsAny<Dictionary<string, object>>())).Verifiable();
        mockStructuredLogging.Setup(x => x.LogSummary(It.IsAny<IOperationContext>(), It.IsAny<bool>(), It.IsAny<string>())).Verifiable();

        mockChaosEngineering.Setup(x => x.ExecuteChaosAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<Func<Task>>(), It.IsAny<Dictionary<string, object>>()))
            .ReturnsAsync(true);
    }

    private static FileUploadRequest CreateValidFileUploadRequest()
    {
        return new FileUploadRequest
        {
            FileName = "test.csv",
            FileSize = 1024,
            FileStream = new MemoryStream(Encoding.UTF8.GetBytes("test,data\n1,2\n3,4"))
        };
    }

    #endregion
}