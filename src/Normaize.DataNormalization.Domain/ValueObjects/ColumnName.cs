using System;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing a column name with validation
/// </summary>
public record ColumnName
{
    public string Value { get; init; }

    public ColumnName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Column name cannot be null or empty", nameof(value));

        var trimmedValue = value.Trim();

        if (trimmedValue.Length > 128)
            throw new ArgumentException("Column name cannot exceed 128 characters", nameof(value));

        // Basic validation for SQL-safe column names
        if (!IsValidColumnName(trimmedValue))
            throw new ArgumentException($"Invalid column name format: {value}", nameof(value));

        Value = trimmedValue;
    }

    private static bool IsValidColumnName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        // Must start with letter or underscore
        if (!char.IsLetter(value[0]) && value[0] != '_')
            return false;

        // Can contain letters, digits, underscores
        foreach (char c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                return false;
        }

        return true;
    }

    public static implicit operator string(ColumnName columnName) => columnName.Value;
    public static implicit operator ColumnName(string value) => new(value);

    public override string ToString() => Value;
}