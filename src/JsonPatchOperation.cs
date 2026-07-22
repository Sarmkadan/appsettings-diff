using System;
using System.Text.Json.Serialization;

namespace AppsettingsDiff;

/// <summary>
/// Represents a JSON Patch operation according to RFC 6902
/// </summary>
public sealed class JsonPatchOperation
{
    /// <summary>
    /// The operation type (e.g., "add", "remove", "replace", "move", "copy", "test").
    /// </summary>
    [JsonPropertyName("op")]
    public required string Op { get; set; }

    /// <summary>
    /// The target location for the operation as a JSON Pointer (RFC 6901).
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    /// <summary>
    /// The value to add, replace, or test. Omitted for "remove" operations.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// The source location for "move" or "copy" operations as a JSON Pointer (RFC 6901).
    /// Omitted for other operation types.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Creates a JSON Pointer path from a configuration key.
    /// Configuration keys use ':' as a section separator (e.g., "Section:Subsection:Key").
    /// This method converts such keys to JSON Pointer format by:
    /// 1. Splitting the key by ':' into segments
    /// 2. Escaping each segment for JSON Pointer (RFC 6901) by replacing '~' with '~0' and '/' with '~1'
    /// 3. Joining segments with '/' to form a valid JSON Pointer path
    /// </summary>
    /// <param name="configKey">The configuration key to convert (e.g., "ConnectionStrings:DefaultConnection").</param>
    /// <returns>A JSON Pointer path (e.g., "/ConnectionStrings/DefaultConnection").</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configKey"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="configKey"/> is empty or whitespace.</exception>
    public static string FromConfigKey(string configKey)
    {
        ArgumentNullException.ThrowIfNull(configKey);

        if (string.IsNullOrWhiteSpace(configKey))
        {
            throw new ArgumentException("Configuration key cannot be empty or whitespace.", nameof(configKey));
        }

        // Split the config key by ':' to get segments
        // Note: We don't escape ':' itself as it's not a special character in JSON Pointer
        var segments = configKey.Split(':', StringSplitOptions.RemoveEmptyEntries);

        // Escape each segment for JSON Pointer (RFC 6901)
        // '~' must be encoded as '~0' and '/' must be encoded as '~1'
        var escapedSegments = new string[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            escapedSegments[i] = EscapeJsonPointerSegment(segments[i]);
        }

        // Join segments with '/' to form a valid JSON Pointer path
        return "/" + string.Join('/', escapedSegments);
    }

    /// <summary>
    /// Escapes a single JSON Pointer segment according to RFC 6901.
    /// In a JSON Pointer, the characters '~' and '/' must be escaped:
    /// - '~' is encoded as '~0'
    /// - '/' is encoded as '~1'
    /// </summary>
    /// <param name="segment">The segment to escape.</param>
    /// <returns>The escaped segment.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="segment"/> is null.</exception>
    private static string EscapeJsonPointerSegment(string segment)
    {
        ArgumentNullException.ThrowIfNull(segment);

        // JSON Pointer requires ~ to be encoded as ~0 and / to be encoded as ~1
        // We need to do this replacement in the correct order to avoid double-escaping
        // First replace '~' with '~0', then replace '/' with '~1'
        return segment
            .Replace("~", "~0")
            .Replace("/", "~1");
    }
}