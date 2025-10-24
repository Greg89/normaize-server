using System;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing dataset processing statistics
/// </summary>
public record DatasetStatistics
{
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public DateTime? ProcessedAt { get; init; }
    public bool IsProcessed { get; init; }
    public bool UseSeparateTable { get; init; }

    public DatasetStatistics(
        int rowCount, 
        int columnCount, 
        bool isProcessed = false, 
        bool useSeparateTable = false,
        DateTime? processedAt = null)
    {
        if (rowCount < 0)
            throw new ArgumentException("Row count cannot be negative", nameof(rowCount));
        
        if (columnCount < 0)
            throw new ArgumentException("Column count cannot be negative", nameof(columnCount));

        RowCount = rowCount;
        ColumnCount = columnCount;
        IsProcessed = isProcessed;
        UseSeparateTable = useSeparateTable;
        ProcessedAt = processedAt;
    }

    public static DatasetStatistics Empty => new(0, 0);
    
    public static DatasetStatistics Create(int rowCount, int columnCount) => new(rowCount, columnCount);

    /// <summary>
    /// Marks the dataset as processed with current timestamp
    /// </summary>
    public DatasetStatistics MarkAsProcessed() => this with 
    { 
        IsProcessed = true, 
        ProcessedAt = DateTime.UtcNow 
    };

    /// <summary>
    /// Updates statistics with new row and column counts
    /// </summary>
    public DatasetStatistics UpdateCounts(int newRowCount, int newColumnCount) => this with
    {
        RowCount = newRowCount >= 0 ? newRowCount : throw new ArgumentException("Row count cannot be negative"),
        ColumnCount = newColumnCount >= 0 ? newColumnCount : throw new ArgumentException("Column count cannot be negative")
    };

    /// <summary>
    /// Determines if the dataset should use a separate table based on size thresholds
    /// </summary>
    public DatasetStatistics WithSeparateTableDecision(int maxRowsThreshold = 10000, long maxFileSizeThreshold = 50 * 1024 * 1024)
    {
        var shouldUseSeparateTable = RowCount >= maxRowsThreshold;
        return this with { UseSeparateTable = shouldUseSeparateTable };
    }

    public bool IsEmpty => RowCount == 0 && ColumnCount == 0;
    public bool IsLargeDataset => RowCount > 10000 || ColumnCount > 100;
    public TimeSpan? ProcessingAge => ProcessedAt.HasValue ? DateTime.UtcNow - ProcessedAt.Value : null;
}