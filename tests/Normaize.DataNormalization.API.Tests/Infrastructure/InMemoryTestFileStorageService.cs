using System.Collections.Concurrent;
using Normaize.DataNormalization.Application.Interfaces;

namespace Normaize.DataNormalization.API.Tests.Infrastructure;

/// <summary>
/// Deterministic file storage for integration tests.
///
/// The production implementation is S3-backed; for tests we keep a small in-memory store.
/// If a file path was never saved, we still return a minimal CSV stream to support
/// handlers that expect to read the dataset's original CSV during reprocessing.
/// </summary>
public sealed class InMemoryTestFileStorageService : IFileStorageService
{
    private static readonly byte[] DefaultCsvBytes = "Email,FirstName\nuser@example.com,Test\n"u8.ToArray();

    private readonly ConcurrentDictionary<string, byte[]> _files = new();

    public Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName,
        string userId,
        CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);

        var filePath = $"/test/{userId}/{Guid.NewGuid():N}_{fileName}";
        _files[filePath] = memoryStream.ToArray();

        return Task.FromResult(filePath);
    }

    public Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (_files.TryGetValue(filePath, out var bytes))
        {
            return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
        }

        // Many tests seed datasets with a static file path without actually uploading.
        // Returning a minimal CSV avoids brittle failures while still exercising parsing logic.
        return Task.FromResult<Stream>(new MemoryStream(DefaultCsvBytes, writable: false));
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _files.TryRemove(filePath, out _);
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Behave as if files exist to allow reprocess flows to run in integration tests.
        return Task.FromResult(true);
    }
}
