using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Normaize.DataNormalization.Infrastructure.Services;

/// <summary>
/// Service for generating file content hashes
/// </summary>
public interface IFileHashService
{
    /// <summary>
    /// Generates a SHA256 hash for the given file stream
    /// </summary>
    Task<string> GenerateHashAsync(Stream fileStream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of file hash generation service
/// </summary>
public class FileHashService : IFileHashService
{
    /// <summary>
    /// Generates a SHA256 hash for the given file stream
    /// </summary>
    /// <param name="fileStream">The file stream to hash</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hexadecimal string representation of the hash</returns>
    /// <exception cref="ArgumentNullException">Thrown when fileStream is null</exception>
    public async Task<string> GenerateHashAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        if (fileStream == null)
            throw new ArgumentNullException(nameof(fileStream));

        // Save current position to restore later
        var originalPosition = fileStream.Position;

        try
        {
            // Reset stream to beginning
            fileStream.Position = 0;

            // Compute SHA256 hash
            using var sha256 = SHA256.Create();
            var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);

            // Convert to hexadecimal string
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        finally
        {
            // Restore original position
            fileStream.Position = originalPosition;
        }
    }
}
