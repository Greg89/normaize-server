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

    public static StorageProvider Local => new("Local");
    public static StorageProvider S3 => new("S3");
    public static StorageProvider Azure => new("Azure");
    public static StorageProvider Memory => new("Memory");

    public static StorageProvider FromPath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Local;

        return filePath switch
        {
            var path when path.StartsWith("s3://", StringComparison.OrdinalIgnoreCase) => S3,
            var path when path.StartsWith("azure://", StringComparison.OrdinalIgnoreCase) => Azure,
            var path when path.StartsWith("memory://", StringComparison.OrdinalIgnoreCase) => Memory,
            _ => Local
        };
    }

    public static StorageProvider FromString(string provider)
    {
        return provider?.ToLowerInvariant() switch
        {
            "s3" => S3,
            "azure" => Azure,
            "memory" => Memory,
            "local" => Local,
            _ => Local
        };
    }

    public bool IsCloudBased => this == S3 || this == Azure;
    public bool RequiresCredentials => IsCloudBased;
    public bool SupportsDirectAccess => this == Local || this == Memory;

    public static implicit operator string(StorageProvider provider) => provider.Value;
    public override string ToString() => Value;
}