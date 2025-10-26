namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing the classification of data types for statistical analysis
/// </summary>
public record DataTypeClassification
{
    public string TypeName { get; init; }
    public bool IsNumeric { get; init; }
    public bool IsDateTime { get; init; }
    public bool IsBoolean { get; init; }
    public bool CanCalculateStatistics { get; init; }

    public DataTypeClassification(string typeName, bool isNumeric, bool isDateTime, bool isBoolean)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

        TypeName = typeName;
        IsNumeric = isNumeric;
        IsDateTime = isDateTime;
        IsBoolean = isBoolean;
        CanCalculateStatistics = isNumeric;
    }

    // Predefined data type classifications
    public static readonly DataTypeClassification Numeric = new("Numeric", isNumeric: true, isDateTime: false, isBoolean: false);
    public static readonly DataTypeClassification DateTime = new("DateTime", isNumeric: false, isDateTime: true, isBoolean: false);
    public static readonly DataTypeClassification Boolean = new("Boolean", isNumeric: false, isDateTime: false, isBoolean: true);
    public static readonly DataTypeClassification String = new("String", isNumeric: false, isDateTime: false, isBoolean: false);
    public static readonly DataTypeClassification Unknown = new("Unknown", isNumeric: false, isDateTime: false, isBoolean: false);

    /// <summary>
    /// Determines the data type classification from a collection of values
    /// </summary>
    public static DataTypeClassification DetermineFromValues(IEnumerable<object?> values)
    {
        var nonNullValues = values.Where(v => v != null).ToList();
        if (!nonNullValues.Any()) return Unknown;

        if (nonNullValues.All(IsNumericValue)) return Numeric;
        if (nonNullValues.All(IsDateTimeValue)) return DateTime;
        if (nonNullValues.All(IsBooleanValue)) return Boolean;
        return String;
    }

    private static bool IsNumericValue(object? value)
    {
        return value switch
        {
            int or long or float or double or decimal => true,
            string s => double.TryParse(s, out _),
            _ => false
        };
    }

    private static bool IsDateTimeValue(object? value)
    {
        return value switch
        {
            System.DateTime => true,
            string s => System.DateTime.TryParse(s, out _),
            _ => false
        };
    }

    private static bool IsBooleanValue(object? value)
    {
        return value switch
        {
            bool => true,
            string s => bool.TryParse(s, out _),
            _ => false
        };
    }
}