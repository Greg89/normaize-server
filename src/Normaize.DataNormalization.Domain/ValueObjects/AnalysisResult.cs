using System;
using System.Text.Json;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing analysis execution results
/// </summary>
public record AnalysisResult
{
    public string JsonResult { get; init; }

    public AnalysisResult(string jsonResult)
    {
        if (string.IsNullOrWhiteSpace(jsonResult))
            throw new ArgumentException("Result cannot be null or empty", nameof(jsonResult));

        // Validate JSON format
        try
        {
            JsonDocument.Parse(jsonResult);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON result: {ex.Message}", nameof(jsonResult));
        }

        JsonResult = jsonResult;
    }

    /// <summary>
    /// Creates result from an object
    /// </summary>
    public static AnalysisResult FromObject(object result)
    {
        var json = JsonSerializer.Serialize(result);
        return new(json);
    }

    /// <summary>
    /// Deserializes result to the specified type
    /// </summary>
    public T? Deserialize<T>()
    {
        return JsonSerializer.Deserialize<T>(JsonResult);
    }

    /// <summary>
    /// Gets the result as a dynamic object
    /// </summary>
    public object? AsObject()
    {
        return JsonSerializer.Deserialize<object>(JsonResult);
    }

    public override string ToString() => JsonResult;
}