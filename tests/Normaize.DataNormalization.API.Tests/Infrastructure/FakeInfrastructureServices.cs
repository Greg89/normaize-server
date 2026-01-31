using System.Collections.Concurrent;

namespace Normaize.DataNormalization.API.Tests.Infrastructure;

public sealed class InMemoryFileStorageService : IFileStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.OrdinalIgnoreCase);

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string userId, CancellationToken cancellationToken = default)
    {
        var key = $"/test/{userId}/{Guid.NewGuid():N}_{fileName}";
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms, cancellationToken);
        _store[key] = ms.ToArray();
        return key;
    }

    public Task<Stream> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(filePath, out var bytes))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        Stream stream = new MemoryStream(bytes, writable: false);
        return Task.FromResult(stream);
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(filePath, out _);
        return Task.CompletedTask;
    }

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.ContainsKey(filePath));
}

public sealed class FakeFileProcessingService : IFileProcessingService
{
    public Task<FileValidationResult> ValidateFileAsync(Stream fileStream, string fileName, long fileSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new FileValidationResult(IsValid: true));

    public Task<FileProcessingResult> ProcessFileAsync(string filePath, Domain.ValueObjects.FileType fileType, CancellationToken cancellationToken = default)
        => Task.FromResult(new FileProcessingResult(
            IsSuccess: true,
            Schema: "{\"columns\":[{\"name\":\"Id\",\"type\":\"integer\"}]}",
            RowCount: 10,
            ColumnCount: 1,
            PreviewData: "{\"rows\":[{\"Id\":1}]}",
            Error: null));
}

public sealed class NoopAuditService : IAuditService
{
    public Task LogDataSetActionAsync(Guid dataSetId, string userId, string action, Dictionary<string, object> metadata, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
