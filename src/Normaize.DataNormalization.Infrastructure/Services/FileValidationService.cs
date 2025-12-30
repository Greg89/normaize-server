using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Normaize.DataNormalization.Application.Interfaces;
using Normaize.DataNormalization.Infrastructure.Configuration;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Implementation of file validation service with configuration-driven validation rules
/// </summary>
public class FileValidationService : IFileValidationService
{
    private readonly ILogger<FileValidationService> _logger;
    private readonly FileUploadOptions _options;

    public FileValidationService(
        ILogger<FileValidationService> logger,
        IOptions<FileUploadOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<FileValidationResult> ValidateFileAsync(
        string? fileName,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating file: {FileName}, size: {FileSize}", fileName, fileSize);

        // Validate file name is provided
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.FromResult(new FileValidationResult(false, "File name is required"));
        }

        // Validate file size is positive
        if (fileSize <= 0)
        {
            return Task.FromResult(new FileValidationResult(false, "File size must be greater than zero"));
        }

        // Validate file name safety (prevent path traversal)
        if (!IsFileNameSafe(fileName))
        {
            _logger.LogWarning("File name contains potentially dangerous characters: {FileName}", fileName);
            return Task.FromResult(new FileValidationResult(false, "File name contains invalid characters or path traversal attempts"));
        }

        // Validate file size
        if (!IsFileSizeValid(fileSize))
        {
            _logger.LogWarning("File size {FileSize} exceeds maximum allowed size {MaxFileSize}", fileSize, _options.MaxFileSizeBytes);
            return Task.FromResult(new FileValidationResult(
                false,
                $"File size exceeds maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB"));
        }

        // Validate file extension
        if (!IsFileExtensionValid(fileName))
        {
            var extension = GetFileExtension(fileName);
            _logger.LogWarning("File extension {Extension} is not allowed", extension);
            return Task.FromResult(new FileValidationResult(
                false,
                $"File type '{extension}' is not supported. Allowed types: {string.Join(", ", _options.AllowedExtensions)}"));
        }

        _logger.LogInformation("File validation passed: {FileName}", fileName);
        return Task.FromResult(new FileValidationResult(true));
    }

    public bool IsFileSizeValid(long fileSize, long? maxFileSize = null)
    {
        var maxSize = maxFileSize ?? _options.MaxFileSizeBytes;
        return fileSize > 0 && fileSize <= maxSize;
    }

    public bool IsFileExtensionValid(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var extension = GetFileExtension(fileName);

        // Check if extension is blocked
        if (_options.BlockedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Extension {Extension} is in blocked list", extension);
            return false;
        }

        // Check if extension is allowed
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Extension {Extension} is not in allowed list", extension);
            return false;
        }

        return true;
    }

    public bool IsFileNameSafe(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        // Check for path traversal attempts
        if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            return false;
        }

        // Check for other dangerous patterns
        if (fileName.StartsWith('.') && fileName.Length > 1 && fileName[1] != '.')
        {
            // Allow hidden files like .gitignore but prevent path traversal
            return true;
        }

        return true;
    }

    public string GetFileExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        return Path.GetExtension(fileName).ToLowerInvariant();
    }

    public string[] GetAllowedExtensions()
    {
        return _options.AllowedExtensions.ToArray();
    }

    public string[] GetBlockedExtensions()
    {
        return _options.BlockedExtensions.ToArray();
    }

    public long GetMaxFileSize()
    {
        return _options.MaxFileSizeBytes;
    }
}
