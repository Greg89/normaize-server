using System;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing a storage provider with validation
/// </summary>
public record StorageProvider
{
    public string Value { get; init; }

    private StorageProvider(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static StorageProvider S3 => new("S3");
    public static StorageProvider Azure => new("Azure");

    public static StorageProvider FromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return S3; // Default to S3

        return filePath switch
        {
            var path when path.StartsWith("s3://", StringComparison.OrdinalIgnoreCase) => S3,
            var path when path.StartsWith("azure://", StringComparison.OrdinalIgnoreCase) => Azure,
            _ => S3 // Default to S3 for all paths
        };
    }

    public static StorageProvider FromString(string provider)
    {
        return provider?.ToLowerInvariant() switch
        {
            "s3" => S3,
            "azure" => Azure,
            _ => S3 // Default to S3
        };
    }

    public bool IsCloudBased => true; // All storage is cloud-based now
    public bool RequiresCredentials => true; // All storage requires credentials
    public bool SupportsDirectAccess => false; // No local storage support

    public static implicit operator string(StorageProvider provider) => provider.Value;
    public override string ToString() => Value;
}