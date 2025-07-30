using System.Text.Json;
using System.Text.Encodings.Web;
using static Normaize.Core.Constants.AppConstants;
using System.Text.Json.Serialization;

namespace Normaize.Core.Configuration;

/// <summary>
/// Global JSON serialization configuration for consistent camelCase output
/// </summary>
public static class JsonConfiguration
{
    /// <summary>
    /// Default JSON serialization options configured for TypeScript/JavaScript compatibility
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = JsonSerialization.DEFAULT_WRITE_INDENTED,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// JSON options for API responses (optimized for size)
    /// </summary>
    public static readonly JsonSerializerOptions ApiResponseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// JSON options for logging (includes more details)
    /// </summary>
    public static readonly JsonSerializerOptions LoggingOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serialize an object to JSON string using default options
    /// </summary>
    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, DefaultOptions);
    }

    /// <summary>
    /// Serialize an object to JSON string using specified options
    /// </summary>
    public static string Serialize<T>(T obj, JsonSerializerOptions options)
    {
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Deserialize JSON string to object using default options
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>
    /// Deserialize JSON string to object using specified options
    /// </summary>
    public static T? Deserialize<T>(string json, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(json, options);
    }
}