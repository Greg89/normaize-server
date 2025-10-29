using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Normaize.DataNormalization.Infrastructure.Services;
using Xunit;

namespace Normaize.DataNormalization.Infrastructure.Tests.Services;

public class FileHashServiceTests
{
    private readonly FileHashService _sut;

    public FileHashServiceTests()
    {
        _sut = new FileHashService();
    }

    [Fact]
    public async Task GenerateHashAsync_WithValidStream_ShouldReturnHash()
    {
        // Arrange
        var content = "Test file content for hashing";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await _sut.GenerateHashAsync(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64); // SHA256 produces 64 hex characters
        hash.Should().MatchRegex("^[a-f0-9]{64}$"); // Should be lowercase hex
    }

    [Fact]
    public async Task GenerateHashAsync_WithSameContent_ShouldProduceSameHash()
    {
        // Arrange
        var content = "Same content";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash1 = await _sut.GenerateHashAsync(stream1);
        var hash2 = await _sut.GenerateHashAsync(stream2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task GenerateHashAsync_WithDifferentContent_ShouldProduceDifferentHash()
    {
        // Arrange
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Content 1"));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("Content 2"));

        // Act
        var hash1 = await _sut.GenerateHashAsync(stream1);
        var hash2 = await _sut.GenerateHashAsync(stream2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task GenerateHashAsync_WithEmptyStream_ShouldReturnValidHash()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var hash = await _sut.GenerateHashAsync(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64);
        // Empty stream should produce known SHA256 hash
        hash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact]
    public async Task GenerateHashAsync_ShouldRestoreStreamPosition()
    {
        // Arrange
        var content = "Test content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        stream.Position = 5; // Set to middle position

        // Act
        await _sut.GenerateHashAsync(stream);

        // Assert
        stream.Position.Should().Be(5); // Should be restored to original position
    }

    [Fact]
    public async Task GenerateHashAsync_WithLargeFile_ShouldGenerateHash()
    {
        // Arrange
        var largeContent = new string('A', 10 * 1024 * 1024); // 10 MB
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeContent));

        // Act
        var hash = await _sut.GenerateHashAsync(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64);
    }

    [Fact]
    public async Task GenerateHashAsync_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = async () => await _sut.GenerateHashAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("fileStream");
    }

    [Fact]
    public async Task GenerateHashAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        var largeContent = new string('B', 50 * 1024 * 1024); // 50 MB to ensure cancellation has time
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(largeContent));
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await _sut.GenerateHashAsync(stream, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GenerateHashAsync_WithSpecialCharacters_ShouldGenerateValidHash()
    {
        // Arrange
        var content = "Test with special chars: 你好世界 🚀 @#$%^&*()";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash = await _sut.GenerateHashAsync(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task GenerateHashAsync_WithBinaryData_ShouldGenerateValidHash()
    {
        // Arrange
        var binaryData = new byte[] { 0x00, 0xFF, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        using var stream = new MemoryStream(binaryData);

        // Act
        var hash = await _sut.GenerateHashAsync(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task GenerateHashAsync_CalledMultipleTimes_ShouldProduceSameHash()
    {
        // Arrange
        var content = "Consistency test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var hash1 = await _sut.GenerateHashAsync(stream);
        var hash2 = await _sut.GenerateHashAsync(stream);
        var hash3 = await _sut.GenerateHashAsync(stream);

        // Assert
        hash1.Should().Be(hash2);
        hash2.Should().Be(hash3);
    }

    [Fact]
    public async Task GenerateHashAsync_WithSeekableStream_ShouldWork()
    {
        // Arrange
        var content = "Seekable stream test";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        stream.Seek(10, SeekOrigin.Begin); // Move position

        // Act
        var hash = await _sut.GenerateHashAsync(stream);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        stream.Position.Should().Be(10); // Should restore to original position
    }

    [Fact]
    public async Task GenerateHashAsync_WithKnownContent_ShouldMatchExpectedHash()
    {
        // Arrange - "hello world" has a known SHA256 hash
        var content = "hello world";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var expectedHash = "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";

        // Act
        var hash = await _sut.GenerateHashAsync(stream);

        // Assert
        hash.Should().Be(expectedHash);
    }

    [Fact]
    public async Task GenerateHashAsync_WithNewlineVariations_ShouldProduceDifferentHashes()
    {
        // Arrange
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("Line1\nLine2")); // Unix
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("Line1\r\nLine2")); // Windows

        // Act
        var hash1 = await _sut.GenerateHashAsync(stream1);
        var hash2 = await _sut.GenerateHashAsync(stream2);

        // Assert
        hash1.Should().NotBe(hash2); // Different line endings should produce different hashes
    }
}
