using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Normaize.DataNormalization.Infrastructure.Services;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

public class FileStorageServiceTests : IDisposable
{
    private readonly Mock<ILogger<FileStorageService>> _mockLogger;
    private readonly FileStorageService _fileStorageService;
    private readonly string _testBaseDirectory;
    private readonly string _testUserId = "test-user-123";

    public FileStorageServiceTests()
    {
        _testBaseDirectory = Path.Combine(Path.GetTempPath(), "FileStorageTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBaseDirectory);

        _mockLogger = new Mock<ILogger<FileStorageService>>();
        _fileStorageService = new FileStorageService(_mockLogger.Object, _testBaseDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBaseDirectory))
        {
            Directory.Delete(_testBaseDirectory, true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_ShouldSaveFileToUserDirectory()
    {
        // Arrange
        var fileName = "test-file.csv";
        var fileContent = "col1,col2\nval1,val2"u8.ToArray();
        using var stream = new MemoryStream(fileContent);

        // Act
        var resultPath = await _fileStorageService.SaveFileAsync(
            stream,
            fileName,
            _testUserId,
            CancellationToken.None);

        // Assert
        resultPath.Should().NotBeNullOrEmpty();
        resultPath.Should().Contain(_testUserId);
        File.Exists(resultPath).Should().BeTrue();

        var savedContent = await File.ReadAllBytesAsync(resultPath);
        savedContent.Should().BeEquivalentTo(fileContent);
    }

    [Fact]
    public async Task SaveFileAsync_ShouldGenerateUniqueFileName_WhenFileExists()
    {
        // Arrange
        var fileName = "duplicate.csv";
        var content1 = "first"u8.ToArray();
        var content2 = "second"u8.ToArray();

        using var stream1 = new MemoryStream(content1);
        using var stream2 = new MemoryStream(content2);

        // Act
        var path1 = await _fileStorageService.SaveFileAsync(stream1, fileName, _testUserId, CancellationToken.None);
        var path2 = await _fileStorageService.SaveFileAsync(stream2, fileName, _testUserId, CancellationToken.None);

        // Assert
        path1.Should().NotBe(path2);
        File.Exists(path1).Should().BeTrue();
        File.Exists(path2).Should().BeTrue();
    }

    [Fact]
    public async Task GetFileAsync_ShouldReturnFileStream()
    {
        // Arrange
        var fileName = "read-test.csv";
        var originalContent = "data to read"u8.ToArray();
        using var saveStream = new MemoryStream(originalContent);
        var filePath = await _fileStorageService.SaveFileAsync(saveStream, fileName, _testUserId, CancellationToken.None);

        // Act
        using var readStream = await _fileStorageService.GetFileAsync(filePath, CancellationToken.None);

        // Assert
        readStream.Should().NotBeNull();
        using var memoryStream = new MemoryStream();
        await readStream.CopyToAsync(memoryStream);
        memoryStream.ToArray().Should().BeEquivalentTo(originalContent);
    }

    [Fact]
    public async Task GetFileAsync_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testBaseDirectory, "non-existent-file.csv");

        // Act
        Func<Task> act = async () => await _fileStorageService.GetFileAsync(nonExistentPath, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile()
    {
        // Arrange
        var fileName = "delete-test.csv";
        var content = "to be deleted"u8.ToArray();
        using var stream = new MemoryStream(content);
        var filePath = await _fileStorageService.SaveFileAsync(stream, fileName, _testUserId, CancellationToken.None);

        // Act
        await _fileStorageService.DeleteFileAsync(filePath, CancellationToken.None);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldNotThrow_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testBaseDirectory, "non-existent-file.csv");

        // Act
        Func<Task> act = async () => await _fileStorageService.DeleteFileAsync(nonExistentPath, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveFileAsync_ShouldCreateUserDirectory_WhenNotExists()
    {
        // Arrange
        var newUserId = "new-user-456";
        var fileName = "first-file.csv";
        var content = "content"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        var resultPath = await _fileStorageService.SaveFileAsync(stream, fileName, newUserId, CancellationToken.None);

        // Assert
        resultPath.Should().Contain(newUserId);
        var userDirectory = Path.Combine(_testBaseDirectory, newUserId);
        Directory.Exists(userDirectory).Should().BeTrue();
    }
}
