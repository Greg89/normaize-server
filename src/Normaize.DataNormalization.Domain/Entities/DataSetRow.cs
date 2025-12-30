using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Normaize.DataNormalization.Domain.Entities;

/// <summary>
/// Represents a row of data within a dataset
/// </summary>
public class DataSetRow
{
    public Guid Id { get; private set; }
    public Guid DataSetId { get; private set; }
    public int RowIndex { get; private set; }
    public string Data { get; private set; } // JSON serialized row data
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation property
    public DataSet DataSet { get; private set; } = null!;

    // Private constructor for EF Core
    private DataSetRow()
    {
        Data = string.Empty;
    }

    private DataSetRow(Guid dataSetId, int rowIndex, string data)
    {
        Id = Guid.NewGuid();
        DataSetId = dataSetId;
        RowIndex = rowIndex;
        Data = data ?? throw new ArgumentNullException(nameof(data));
        CreatedAt = DateTime.UtcNow;
    }

    public static DataSetRow Create(Guid dataSetId, int rowIndex, string data)
    {
        if (dataSetId == Guid.Empty)
            throw new ArgumentException("DataSet ID cannot be empty", nameof(dataSetId));

        if (rowIndex < 0)
            throw new ArgumentException("Row index cannot be negative", nameof(rowIndex));

        return new DataSetRow(dataSetId, rowIndex, data);
    }

    public T? GetValue<T>(string columnName)
    {
        try
        {
            var rowData = JsonSerializer.Deserialize<Dictionary<string, object?>>(Data);
            if (rowData?.TryGetValue(columnName, out var value) == true)
            {
                if (value is null)
                    return default(T);

                if (value is T typedValue)
                    return typedValue;

                // Handle JsonElement conversion for deserialized JSON
                if (value is JsonElement jsonElement)
                {
                    return ConvertJsonElement<T>(jsonElement);
                }

                return (T?)Convert.ChangeType(value, typeof(T));
            }
        }
        catch
        {
            // Return default if parsing fails
        }

        return default(T);
    }

    private static T? ConvertJsonElement<T>(JsonElement jsonElement)
    {
        var targetType = typeof(T);

        return jsonElement.ValueKind switch
        {
            JsonValueKind.String when targetType == typeof(string) => (T)(object)jsonElement.GetString()!,
            JsonValueKind.String when targetType == typeof(int) && int.TryParse(jsonElement.GetString(), out var intVal) => (T)(object)intVal,
            JsonValueKind.String when targetType == typeof(bool) && bool.TryParse(jsonElement.GetString(), out var boolVal) => (T)(object)boolVal,

            JsonValueKind.Number when targetType == typeof(string) => (T)(object)jsonElement.GetRawText(),
            JsonValueKind.Number when targetType == typeof(int) => (T)(object)jsonElement.GetInt32(),
            JsonValueKind.Number when targetType == typeof(double) => (T)(object)jsonElement.GetDouble(),
            JsonValueKind.Number when targetType == typeof(decimal) => (T)(object)jsonElement.GetDecimal(),

            JsonValueKind.True when targetType == typeof(bool) => (T)(object)true,
            JsonValueKind.True when targetType == typeof(string) => (T)(object)"true",

            JsonValueKind.False when targetType == typeof(bool) => (T)(object)false,
            JsonValueKind.False when targetType == typeof(string) => (T)(object)"false",

            JsonValueKind.Null => default(T),

            _ => jsonElement.Deserialize<T>()
        };
    }

    public Dictionary<string, object?> GetAllValues()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(Data) ?? new Dictionary<string, object?>();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    /// <summary>
    /// Set all row values from a dictionary
    /// </summary>
    public void SetAllValues(Dictionary<string, object?> values)
    {
        Data = JsonSerializer.Serialize(values);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the row data with new JSON content
    /// </summary>
    public void UpdateData(string newData)
    {
        Data = newData ?? throw new ArgumentNullException(nameof(newData));
        UpdatedAt = DateTime.UtcNow;
    }
}