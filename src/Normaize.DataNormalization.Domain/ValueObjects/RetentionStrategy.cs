namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Defines which duplicate record to retain when duplicates are found
/// </summary>
public enum RetentionStrategy
{
    /// <summary>
    /// Keep the first occurrence of duplicate records
    /// </summary>
    First,
    
    /// <summary>
    /// Keep the last occurrence of duplicate records
    /// </summary>
    Last,
    
    /// <summary>
    /// Keep the record with the highest value in a specified column
    /// </summary>
    MaxValue,
    
    /// <summary>
    /// Keep the record with the lowest value in a specified column
    /// </summary>
    MinValue
}