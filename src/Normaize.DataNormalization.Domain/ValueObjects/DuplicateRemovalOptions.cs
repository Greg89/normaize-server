using System;
using System.Collections.Generic;
using System.Linq;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing options for duplicate removal operations
/// </summary>
public record DuplicateRemovalOptions
{
    public IReadOnlyList<string> KeyColumns { get; init; }
    public CaseSensitivity CaseSensitivity { get; init; }
    public RetentionStrategy RetentionStrategy { get; init; }
    public bool PreserveOriginalOrder { get; init; }

    public DuplicateRemovalOptions(
        IEnumerable<string> keyColumns,
        CaseSensitivity caseSensitivity = CaseSensitivity.Sensitive,
        RetentionStrategy retentionStrategy = RetentionStrategy.First,
        bool preserveOriginalOrder = true)
    {
        if (keyColumns == null)
            throw new ArgumentNullException(nameof(keyColumns));

        var columnList = keyColumns.ToList();
        if (!columnList.Any())
            throw new ArgumentException("At least one key column must be specified", nameof(keyColumns));

        if (columnList.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Key columns cannot be null or empty", nameof(keyColumns));

        KeyColumns = columnList.AsReadOnly();
        CaseSensitivity = caseSensitivity;
        RetentionStrategy = retentionStrategy;
        PreserveOriginalOrder = preserveOriginalOrder;
    }

    /// <summary>
    /// Creates options for duplicate removal with first occurrence retention
    /// </summary>
    public static DuplicateRemovalOptions KeepFirst(IEnumerable<string> keyColumns, CaseSensitivity caseSensitivity = CaseSensitivity.Sensitive)
        => new(keyColumns, caseSensitivity, RetentionStrategy.First);

    /// <summary>
    /// Creates options for duplicate removal with last occurrence retention
    /// </summary>
    public static DuplicateRemovalOptions KeepLast(IEnumerable<string> keyColumns, CaseSensitivity caseSensitivity = CaseSensitivity.Sensitive)
        => new(keyColumns, caseSensitivity, RetentionStrategy.Last);

    /// <summary>
    /// Serializes the options to a JSON string for storage
    /// </summary>
    public string Serialize()
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            KeyColumns,
            CaseSensitivity = CaseSensitivity.ToString(),
            RetentionStrategy = RetentionStrategy.ToString(),
            PreserveOriginalOrder
        });
    }

    /// <summary>
    /// Deserializes DuplicateRemovalOptions from JSON string
    /// </summary>
    public static DuplicateRemovalOptions Deserialize(string json)
    {
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        var keyColumns = root.GetProperty("KeyColumns").EnumerateArray()
            .Select(x => x.GetString()!)
            .ToList();

        // Handle both CaseSensitive (boolean) and CaseSensitivity (enum string) formats
        CaseSensitivity caseSensitivity;
        if (root.TryGetProperty("CaseSensitivity", out var caseSensitivityProp))
        {
            caseSensitivity = Enum.Parse<CaseSensitivity>(caseSensitivityProp.GetString()!);
        }
        else if (root.TryGetProperty("CaseSensitive", out var caseSensitiveProp))
        {
            // Convert boolean to enum
            caseSensitivity = caseSensitiveProp.GetBoolean() ? CaseSensitivity.Sensitive : CaseSensitivity.Insensitive;
        }
        else
        {
            caseSensitivity = CaseSensitivity.Insensitive; // Default
        }

        // Handle RetentionStrategy with mapping from older format
        RetentionStrategy retentionStrategy;
        if (root.TryGetProperty("RetentionStrategy", out var retentionProp))
        {
            var retentionStr = retentionProp.GetString()!;
            retentionStrategy = retentionStr switch
            {
                "KeepFirst" => RetentionStrategy.First,
                "KeepLast" => RetentionStrategy.Last,
                "First" => RetentionStrategy.First,
                "Last" => RetentionStrategy.Last,
                "MaxValue" => RetentionStrategy.MaxValue,
                "MinValue" => RetentionStrategy.MinValue,
                _ => RetentionStrategy.First // Default
            };
        }
        else
        {
            retentionStrategy = RetentionStrategy.First; // Default
        }

        // PreserveOriginalOrder is optional, default to false
        var preserveOriginalOrder = root.TryGetProperty("PreserveOriginalOrder", out var preserveProp) &&
                                   preserveProp.GetBoolean();

        return new DuplicateRemovalOptions(keyColumns, caseSensitivity, retentionStrategy, preserveOriginalOrder);
    }
}

/// <summary>
/// Defines how duplicate comparison should handle case sensitivity
/// </summary>
public enum CaseSensitivity
{
    Sensitive,
    Insensitive
}