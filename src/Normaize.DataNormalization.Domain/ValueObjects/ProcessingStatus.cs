namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing the processing status of a dataset.
/// </summary>
public sealed class ProcessingStatus
{
    public bool IsProcessed { get; }
    public DateTime? ProcessedAt { get; }
    public string? ProcessingError { get; }
    public bool HasError => !string.IsNullOrWhiteSpace(ProcessingError);

    private ProcessingStatus(bool isProcessed, DateTime? processedAt, string? processingError)
    {
        IsProcessed = isProcessed;
        ProcessedAt = processedAt;
        ProcessingError = processingError;
    }

    public static ProcessingStatus Pending() 
        => new ProcessingStatus(false, null, null);

    public static ProcessingStatus Completed(DateTime? processedAt = null) 
        => new ProcessingStatus(true, processedAt ?? DateTime.UtcNow, null);

    public static ProcessingStatus Failed(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException("Error message cannot be null or empty", nameof(error));

        return new ProcessingStatus(false, null, error);
    }

    public ProcessingStatus MarkAsCompleted()
    {
        return Completed();
    }

    public ProcessingStatus MarkAsFailed(string error)
    {
        return Failed(error);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ProcessingStatus other)
            return false;

        return IsProcessed == other.IsProcessed &&
               ProcessedAt == other.ProcessedAt &&
               ProcessingError == other.ProcessingError;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsProcessed, ProcessedAt, ProcessingError);
    }
}
