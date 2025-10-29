using System;
using System.Text.Json;

namespace Normaize.DataNormalization.Domain.ValueObjects;

/// <summary>
/// Value object representing analysis configuration parameters
/// </summary>
public record AnalysisConfiguration
{
    public string JsonConfiguration { get; init; }

    public AnalysisConfiguration(string jsonConfiguration)
    {
        if (string.IsNullOrWhiteSpace(jsonConfiguration))
            throw new ArgumentException("Configuration cannot be null or empty", nameof(jsonConfiguration));

        // Validate JSON format
        try
        {
            JsonDocument.Parse(jsonConfiguration);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON configuration: {ex.Message}", nameof(jsonConfiguration));
        }

        JsonConfiguration = jsonConfiguration;
    }

    /// <summary>
    /// Creates an empty configuration
    /// </summary>
    public static AnalysisConfiguration Empty => new("{}");

    /// <summary>
    /// Creates configuration from an object
    /// </summary>
    public static AnalysisConfiguration FromObject(object configuration)
    {
        var json = JsonSerializer.Serialize(configuration);
        return new(json);
    }

    /// <summary>
    /// Deserializes configuration to the specified type
    /// </summary>
    public T? Deserialize<T>()
    {
        return JsonSerializer.Deserialize<T>(JsonConfiguration);
    }

    public override string ToString() => JsonConfiguration;
}