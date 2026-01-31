using System;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing file metadata with validation
/// </summary>
public record FileMetadata
{
    public string FileName { get; init; }
    public string FilePath { get; init; }
    public FileType FileType { get; init; }
    public StorageProvider StorageProvider { get; init; }
    public long FileSize { get; init; }
    public string? DataHash { get; init; }

    // Parameterless constructor for EF Core
    private FileMetadata()
    {
        FileName = string.Empty;
        FilePath = string.Empty;
        FileType = FileType.Custom;
        StorageProvider = StorageProvider.S3; // Default to S3
        FileSize = 0;
    }

    public FileMetadata(
        string fileName,
        string filePath,
        FileType fileType,
        StorageProvider storageProvider,
        long fileSize,
        string? dataHash = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty", nameof(fileName));

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

        if (fileSize < 0)
            throw new ArgumentException("File size cannot be negative", nameof(fileSize));

        FileName = fileName.Trim();
        FilePath = filePath.Trim();
        FileType = fileType ?? throw new ArgumentNullException(nameof(fileType));
        StorageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
        FileSize = fileSize;
        DataHash = dataHash?.Trim();
    }

    /// <summary>
    /// Creates file metadata with auto-detected storage provider from path
    /// </summary>
    public static FileMetadata Create(string fileName, string filePath, FileType fileType, long fileSize, string? dataHash = null)
    {
        var storageProvider = StorageProvider.FromPath(filePath);
        return new FileMetadata(fileName, filePath, fileType, storageProvider, fileSize, dataHash);
    }

    /// <summary>
    /// Creates file metadata with auto-detected file type from extension
    /// </summary>
    public static FileMetadata CreateFromFileName(string fileName, string filePath, long fileSize, string? dataHash = null)
    {
        var fileType = FileType.FromExtension(System.IO.Path.GetExtension(fileName));
        return Create(fileName, filePath, fileType, fileSize, dataHash);
    }

    public bool IsLargeFile(long threshold = 50 * 1024 * 1024) => FileSize > threshold; // 50MB default
    public bool HasValidHash => !string.IsNullOrWhiteSpace(DataHash);
    public string FileExtension => System.IO.Path.GetExtension(FileName).ToLowerInvariant();

    /// <summary>
    /// Updates the data hash while preserving other metadata
    /// </summary>
    public FileMetadata WithDataHash(string dataHash) => this with { DataHash = dataHash };

    /// <summary>
    /// Updates the file path while preserving other metadata and auto-detecting storage provider
    /// </summary>
    public FileMetadata WithFilePath(string newFilePath)
    {
        var newStorageProvider = StorageProvider.FromPath(newFilePath);
        return this with { FilePath = newFilePath, StorageProvider = newStorageProvider };
    }
}