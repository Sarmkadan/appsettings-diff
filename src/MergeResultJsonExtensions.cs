using System;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Provides JSON serialization helpers for <see cref="MergeResult"/>.
/// </summary>
public static class MergeResultJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the <see cref="MergeResult"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="MergeResult"/> to serialize.</param>
    /// <param name="indented">If <c>true</c>, the output JSON will be formatted with indentation.</param>
    /// <returns>A JSON representation of the <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this MergeResult value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(
            value,
            indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="MergeResult"/> instance.
    /// </summary>
    /// <param name="json">The JSON string representing a <see cref="MergeResult"/>.</param>
    /// <returns>The deserialized <see cref="MergeResult"/>, or <c>null</c> if the JSON document is the literal <c>null</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or an empty string.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized into a <see cref="MergeResult"/>.</exception>
    public static MergeResult? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        return JsonSerializer.Deserialize<MergeResult>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="MergeResult"/> instance.
    /// </summary>
    /// <param name="json">The JSON string representing a <see cref="MergeResult"/>.</param>
    /// <param name="value">
    /// When this method returns, contains the deserialized <see cref="MergeResult"/> if the operation succeeded;
    /// otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or an empty string.</exception>
    public static bool TryFromJson(string json, out MergeResult? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            value = JsonSerializer.Deserialize<MergeResult>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
