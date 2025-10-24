using System;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing a file type with validation
/// </summary>
public record FileType
{
    public string Value { get; init; }
    
    private FileType(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static FileType CSV => new("CSV");
    public static FileType JSON => new("JSON"); 
    public static FileType Excel => new("Excel");
    public static FileType XML => new("XML");
    public static FileType Parquet => new("Parquet");
    public static FileType TXT => new("TXT");
    public static FileType Custom => new("Custom");

    public static FileType FromExtension(string extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            ".csv" => CSV,
            ".json" => JSON,
            ".xlsx" or ".xls" => Excel,
            ".xml" => XML,
            ".parquet" => Parquet,
            ".txt" => TXT,
            _ => Custom
        };
    }

    public static FileType FromString(string fileType)
    {
        return fileType?.ToUpperInvariant() switch
        {
            "CSV" => CSV,
            "JSON" => JSON,
            "EXCEL" => Excel,
            "XML" => XML,
            "PARQUET" => Parquet,
            "TXT" => TXT,
            _ => Custom
        };
    }

    public bool IsTextBased => this == CSV || this == JSON || this == XML || this == TXT;
    public bool RequiresSpecialHandling => this == Excel || this == Parquet;

    public static implicit operator string(FileType fileType) => fileType.Value;
    public override string ToString() => Value;
}