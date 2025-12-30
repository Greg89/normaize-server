namespace Normaize.DataNormalization.Application.DTOs;

/// <summary>
/// DTO for dataset information
/// </summary>
public record DataSetDto(
    Guid Id,
    string Name,
    string? Description,
    string UserId,
    string FileName,
    string FilePath,
    long FileSize,
    string FileType,
    string StorageProvider,
    int RowCount,
    int ColumnCount,
    bool IsProcessed,
    DateTime? ProcessedAt,
    DateTime UploadedAt,
    DateTime? RetentionExpiryDate,
    bool IsDeleted,
    DateTime? DeletedAt,
    string? DeletedBy,
    DateTime LastModifiedAt,
    string? LastModifiedBy);

/// <summary>
/// DTO for dataset preview
/// </summary>
public record DataSetPreviewDto(
    Guid DataSetId,
    int PreviewRowCount,
    int TotalRows,
    List<Dictionary<string, object>> Rows,
    List<string> Columns);

/// <summary>
/// DTO for dataset schema
/// </summary>
public record DataSetSchemaDto(
    Guid DataSetId,
    string Schema,
    List<ColumnInfo> Columns);

public record ColumnInfo(
    string Name,
    string DataType,
    bool IsNullable);
