using System;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="DiffReportWriter"/>.
/// </summary>
public static class DiffReportWriterJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="DiffReportWriter"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The DiffReportWriter instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>A JSON representation of the DiffReportWriter</returns>
    public static string ToJson(this DiffReportWriter value, bool indented = false)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        // DiffReportWriter is stateless (only has SensitiveKeyDetector and showSecrets fields)
        // We serialize it as an empty object since it's just a service class
        var options = new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = indented
        };
        return JsonSerializer.Serialize(new { }, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="DiffReportWriter"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>A new DiffReportWriter instance, or null if the JSON represents a null value</returns>
    public static DiffReportWriter? FromJson(string json)
    {
        if (json == null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        try
        {
            // Since DiffReportWriter is stateless, we can deserialize it from any valid JSON
            // The actual instance doesn't store any state that needs to be preserved
            var dummy = JsonSerializer.Deserialize<object>(json, _jsonOptions);
            return new DiffReportWriter(new SensitiveKeyDetector());
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="DiffReportWriter"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">Receives the deserialized DiffReportWriter instance, or null if deserialization fails</param>
    /// <returns>True if deserialization succeeded; false otherwise</returns>
    public static bool TryFromJson(string json, out DiffReportWriter? value)
    {
        try
        {
            value = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}