using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Normaize.Core.DTOs;

namespace Normaize.Data.Converters;

public class FileTypeConverter : ValueConverter<FileType, string>
{
    public FileTypeConverter() : base(
        v => v.ToString().ToLowerInvariant(),
        v => ConvertFromString(v))
    {
    }

    private static FileType ConvertFromString(string value)
    {
        return value switch
        {
            ".csv" => FileType.CSV,
            ".json" => FileType.JSON,
            ".xlsx" => FileType.Excel,
            ".xls" => FileType.Excel,
            ".xml" => FileType.XML,
            ".parquet" => FileType.Parquet,
            ".txt" => FileType.TXT,
            _ => Enum.TryParse<FileType>(value, true, out var result) ? result : FileType.Custom
        };
    }
} 