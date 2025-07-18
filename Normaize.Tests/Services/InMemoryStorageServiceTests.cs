using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Normaize.Core.Models;
using Normaize.Data.Services;
using Xunit;
using FluentAssertions;

namespace Normaize.Tests.Services;

public class InMemoryStorageServiceTests
{
    private readonly Mock<ILogger<InMemoryStorageService>> _mockLogger;
    private readonly Mock<IOptions<InMemoryStorageOptions>> _mockOptions;
    private readonly InMemoryStorageService _service;

    public InMemoryStorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<InMemoryStorageService>>();
        _mockOptions = new Mock<IOptions<InMemoryStorageOptions>>();
        
        var options = new InMemoryStorageOptions();
        _mockOptions.Setup(x => x.Value).Returns(options);
        
        _service = new InMemoryStorageService(_mockLogger.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task SaveFileAsync_WhenValidFile_ShouldSaveAndReturnPath()
    {
        // Arrange
        var fileName = "test.csv";
        var fileContent = "test,data,content";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        // Act
        var result = await _service.SaveFileAsync(fileRequest);

        // Assert
        result.Should().StartWith("memory://");
        result.Should().Contain(fileName);
        
        // Verify file was actually saved
        var exists = await _service.FileExistsAsync(result);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task SaveFileAsync_WhenMultipleFiles_ShouldSaveAllFiles()
    {
        // Arrange
        var file1 = new FileUploadRequest
        {
            FileName = "file1.csv",
            FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content1"))
        };
        
        var file2 = new FileUploadRequest
        {
            FileName = "file2.csv",
            FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content2"))
        };

        // Act
        var path1 = await _service.SaveFileAsync(file1);
        var path2 = await _service.SaveFileAsync(file2);

        // Assert
        path1.Should().NotBe(path2);
        
        var exists1 = await _service.FileExistsAsync(path1);
        var exists2 = await _service.FileExistsAsync(path2);
        
        exists1.Should().BeTrue();
        exists2.Should().BeTrue();
    }

    [Fact]
    public async Task GetFileAsync_WhenFileExists_ShouldReturnCorrectContent()
    {
        // Arrange
        var fileName = "test.csv";
        var fileContent = "test,data,content";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        var savedPath = await _service.SaveFileAsync(fileRequest);

        // Act
        var retrievedStream = await _service.GetFileAsync(savedPath);
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync();

        // Assert
        retrievedContent.Should().Be(fileContent);
    }

    [Fact]
    public async Task GetFileAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = "memory://nonexistent.csv";

        // Act & Assert
        var action = () => _service.GetFileAsync(nonExistentPath);
        await action.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage($"File not found in memory: {nonExistentPath}");
    }

    [Fact]
    public async Task DeleteFileAsync_WhenFileExists_ShouldDeleteFile()
    {
        // Arrange
        var fileName = "test.csv";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        var savedPath = await _service.SaveFileAsync(fileRequest);
        
        // Verify file exists before deletion
        var existsBefore = await _service.FileExistsAsync(savedPath);
        existsBefore.Should().BeTrue();

        // Act
        await _service.DeleteFileAsync(savedPath);

        // Assert
        var existsAfter = await _service.FileExistsAsync(savedPath);
        existsAfter.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_WhenFileDoesNotExist_ShouldNotThrowException()
    {
        // Arrange
        var nonExistentPath = "memory://nonexistent.csv";

        // Act & Assert
        var action = () => _service.DeleteFileAsync(nonExistentPath);
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FileExistsAsync_WhenFileExists_ShouldReturnTrue()
    {
        // Arrange
        var fileName = "test.csv";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        var savedPath = await _service.SaveFileAsync(fileRequest);

        // Act
        var exists = await _service.FileExistsAsync(savedPath);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_WhenFileDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentPath = "memory://nonexistent.csv";

        // Act
        var exists = await _service.FileExistsAsync(nonExistentPath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SaveFileAsync_WhenLargeFile_ShouldHandleCorrectly()
    {
        // Arrange
        var largeContent = new string('x', 1024 * 1024); // 1MB
        var fileName = "large.csv";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(largeContent));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        // Act
        var result = await _service.SaveFileAsync(fileRequest);

        // Assert
        result.Should().StartWith("memory://");
        
        var retrievedStream = await _service.GetFileAsync(result);
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync();
        
        retrievedContent.Should().Be(largeContent);
        retrievedContent.Length.Should().Be(1024 * 1024);
    }

    [Fact]
    public async Task SaveFileAsync_WhenEmptyFile_ShouldHandleCorrectly()
    {
        // Arrange
        var fileName = "empty.csv";
        var fileStream = new MemoryStream();
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        // Act
        var result = await _service.SaveFileAsync(fileRequest);

        // Assert
        result.Should().StartWith("memory://");
        
        var retrievedStream = await _service.GetFileAsync(result);
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync();
        
        retrievedContent.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFileAsync_WhenFileDeleted_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var fileName = "test.csv";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content"));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        var savedPath = await _service.SaveFileAsync(fileRequest);
        await _service.DeleteFileAsync(savedPath);

        // Act & Assert
        var action = () => _service.GetFileAsync(savedPath);
        await action.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldHandleThreadSafety()
    {
        // Arrange
        var tasks = new List<Task<string>>();
        var fileCount = 10;

        // Act - Save multiple files concurrently
        for (int i = 0; i < fileCount; i++)
        {
            var fileRequest = new FileUploadRequest
            {
                FileName = $"file{i}.csv",
                FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes($"content{i}"))
            };
            
            tasks.Add(_service.SaveFileAsync(fileRequest));
        }

        var savedPaths = await Task.WhenAll(tasks);

        // Assert - All files should be saved successfully
        savedPaths.Should().HaveCount(fileCount);
        savedPaths.Should().OnlyContain(path => path.StartsWith("memory://"));

        // Verify all files exist
        foreach (var path in savedPaths)
        {
            var exists = await _service.FileExistsAsync(path);
            exists.Should().BeTrue();
        }

        // Delete all files concurrently
        var deleteTasks = savedPaths.Select(path => _service.DeleteFileAsync(path));
        await Task.WhenAll(deleteTasks);

        // Verify all files are deleted
        foreach (var path in savedPaths)
        {
            var exists = await _service.FileExistsAsync(path);
            exists.Should().BeFalse();
        }
    }

    [Fact]
    public async Task SaveFileAsync_WhenStreamIsDisposed_ShouldStillWork()
    {
        // Arrange
        var fileName = "test.csv";
        var fileContent = "test,data,content";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        // Act
        var result = await _service.SaveFileAsync(fileRequest);
        
        // Dispose the original stream
        fileStream.Dispose();

        // Assert - Should still be able to retrieve the file
        var retrievedStream = await _service.GetFileAsync(result);
        using var reader = new StreamReader(retrievedStream);
        var retrievedContent = await reader.ReadToEndAsync();
        
        retrievedContent.Should().Be(fileContent);
    }

    [Fact]
    public async Task GetFileAsync_WhenCalledMultipleTimes_ShouldReturnIndependentStreams()
    {
        // Arrange
        var fileName = "test.csv";
        var fileContent = "test,data,content";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        var savedPath = await _service.SaveFileAsync(fileRequest);

        // Act
        var stream1 = await _service.GetFileAsync(savedPath);
        var stream2 = await _service.GetFileAsync(savedPath);

        // Assert - Both streams should be independent
        stream1.Should().NotBeSameAs(stream2);
        
        using var reader1 = new StreamReader(stream1);
        using var reader2 = new StreamReader(stream2);
        
        var content1 = await reader1.ReadToEndAsync();
        var content2 = await reader2.ReadToEndAsync();
        
        content1.Should().Be(fileContent);
        content2.Should().Be(fileContent);
    }

    [Fact]
    public async Task SaveFileAsync_WhenFileTooLarge_ShouldThrowException()
    {
        // Arrange
        var largeContent = new string('x', 101 * 1024 * 1024); // 101MB (exceeds 100MB limit)
        var fileName = "large.csv";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(largeContent));
        
        var fileRequest = new FileUploadRequest
        {
            FileName = fileName,
            FileStream = fileStream
        };

        // Act & Assert
        var action = () => _service.SaveFileAsync(fileRequest);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds maximum allowed size*");
    }

    [Fact]
    public async Task SaveFileAsync_WhenNullFileRequest_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _service.SaveFileAsync(null!);
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("fileRequest");
    }

    [Fact]
    public async Task SaveFileAsync_WhenNullFileName_ShouldThrowException()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = null!,
            FileStream = new MemoryStream()
        };

        // Act & Assert
        var action = () => _service.SaveFileAsync(fileRequest);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*FileName cannot be null or empty*");
    }

    [Fact]
    public async Task SaveFileAsync_WhenNullFileStream_ShouldThrowException()
    {
        // Arrange
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            FileStream = null!
        };

        // Act & Assert
        var action = () => _service.SaveFileAsync(fileRequest);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*FileStream cannot be null*");
    }

    [Fact]
    public async Task GetFileAsync_WhenInvalidFilePath_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _service.GetFileAsync("invalid://path");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must start with 'memory://'*");
    }

    [Fact]
    public async Task GetFileAsync_WhenNullFilePath_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _service.GetFileAsync(null!);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*FilePath cannot be null or empty*");
    }

    [Fact]
    public async Task DeleteFileAsync_WhenInvalidFilePath_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _service.DeleteFileAsync("invalid://path");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must start with 'memory://'*");
    }

    [Fact]
    public async Task FileExistsAsync_WhenInvalidFilePath_ShouldThrowException()
    {
        // Act & Assert
        var action = () => _service.FileExistsAsync("invalid://path");
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*must start with 'memory://'*");
    }

    [Fact]
    public void GetStorageStatistics_WhenNoFiles_ShouldReturnZero()
    {
        // Act
        var stats = _service.GetStorageStatistics();

        // Assert
        stats.FileCount.Should().Be(0);
        stats.TotalSizeBytes.Should().Be(0);
    }

    [Fact]
    public async Task GetStorageStatistics_WhenFilesExist_ShouldReturnCorrectStats()
    {
        // Arrange
        var file1 = new FileUploadRequest
        {
            FileName = "file1.csv",
            FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content1"))
        };
        
        var file2 = new FileUploadRequest
        {
            FileName = "file2.csv",
            FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content2"))
        };

        await _service.SaveFileAsync(file1);
        await _service.SaveFileAsync(file2);

        // Act
        var stats = _service.GetStorageStatistics();

        // Assert
        stats.FileCount.Should().Be(2);
        stats.TotalSizeBytes.Should().Be(16); // "content1" (8) + "content2" (8)
    }

    [Fact]
    public async Task ClearAllFiles_WhenFilesExist_ShouldRemoveAllFiles()
    {
        // Arrange
        var file1 = new FileUploadRequest
        {
            FileName = "file1.csv",
            FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content1"))
        };
        
        var file2 = new FileUploadRequest
        {
            FileName = "file2.csv",
            FileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content2"))
        };

        var path1 = await _service.SaveFileAsync(file1);
        var path2 = await _service.SaveFileAsync(file2);

        // Verify files exist before clearing
        var existsBefore1 = await _service.FileExistsAsync(path1);
        var existsBefore2 = await _service.FileExistsAsync(path2);
        existsBefore1.Should().BeTrue();
        existsBefore2.Should().BeTrue();

        // Act
        _service.ClearAllFiles();

        // Assert
        var existsAfter1 = await _service.FileExistsAsync(path1);
        var existsAfter2 = await _service.FileExistsAsync(path2);
        existsAfter1.Should().BeFalse();
        existsAfter2.Should().BeFalse();

        var stats = _service.GetStorageStatistics();
        stats.FileCount.Should().Be(0);
        stats.TotalSizeBytes.Should().Be(0);
    }

    [Fact]
    public async Task SaveFileAsync_WhenStreamNotReadable_ShouldThrowException()
    {
        // Arrange
        var fileStream = new MemoryStream();
        fileStream.Close(); // Make stream not readable
        
        var fileRequest = new FileUploadRequest
        {
            FileName = "test.csv",
            FileStream = fileStream
        };

        // Act & Assert
        var action = () => _service.SaveFileAsync(fileRequest);
        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*FileStream must be readable*");
    }
} 