namespace Normaize.DataNormalization.Domain.Events;

public record DataSetUploadedEvent(
    string UserId,
    string DataSetName,
    string FileName,
    long FileSize);

public record DataSetProcessedEvent(
    Guid DataSetId,
    string UserId,
    int RowCount,
    int ColumnCount);

public record DataSetProcessingFailedEvent(
    Guid DataSetId,
    string UserId,
    string Error);

public record DataSetMetadataUpdatedEvent(
    Guid DataSetId,
    string Name,
    string? Description);

public record DataSetUpdatedEvent(
    Guid DataSetId,
    string UserId);

public record RetentionPolicyUpdatedEvent(
    Guid DataSetId,
    string UserId,
    int RetentionDays,
    DateTime NewExpiryDate);

public record DataSetDeletedEvent(
    Guid DataSetId,
    string UserId,
    string DeletedBy);

public record DataSetRestoredEvent(
    Guid DataSetId,
    string UserId);

public record DataSetResetEvent(
    Guid DataSetId,
    string UserId);
