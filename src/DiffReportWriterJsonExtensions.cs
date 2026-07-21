using System;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="IDiffReportWriter"/>.
/// </summary>
public static class DiffReportWriterJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a diff result to JSON using the specified <see cref="IDiffReportWriter"/> instance.
    /// </summary>
    /// <param name="writer">The <see cref="IDiffReportWriter"/> instance used for serialization configuration</param>
    /// <param name="result">The diff result to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>A JSON representation of the diff result</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="writer"/> or <paramref name="result"/> is null</exception>
    public static string ToJson(this IDiffReportWriter writer, DiffResult result, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(result);

        return writer.ToJson(result, indented);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="DiffResult"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="detector">The <see cref="SensitiveKeyDetector"/> used for redaction configuration</param>
    /// <param name="showSecrets">Whether to include sensitive values in the output</param>
    /// <returns>A new <see cref="DiffResult"/> instance, or null if the JSON represents a null value</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null</exception>
    public static DiffResult? FromJson(string json, SensitiveKeyDetector detector, bool showSecrets = false)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(detector);

        try
        {
            var options = new JsonSerializerOptions(_jsonOptions)
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<DiffResultJson>(json, options);
            if (result == null)
            {
                return null;
            }

            var diffResult = new DiffResult
            {
                BasePath = result.BasePath ?? string.Empty,
                TargetPath = result.TargetPath ?? string.Empty
            };

            if (result.Entries != null)
            {
                foreach (var e in result.Entries)
                {
                    var key = e.Key ?? string.Empty;
                    var isSensitive = e.IsSensitive || detector.IsSensitive(key);
                    var redact = isSensitive && !showSecrets;

                    diffResult.Entries.Add(new DiffEntry
                    {
                        Kind = Enum.TryParse<DiffKind>(e.Kind ?? string.Empty, out var kind) ? kind : DiffKind.Changed,
                        Key = key,
                        OldValue = redact && e.OldValue is not null ? "[REDACTED]" : e.OldValue,
                        NewValue = redact && e.NewValue is not null ? "[REDACTED]" : e.NewValue,
                        Path = e.Path,
                        IsSensitive = isSensitive
                    });
                }
            }

            return diffResult;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="DiffResult"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="detector">The <see cref="SensitiveKeyDetector"/> used for redaction configuration</param>
    /// <param name="value">Receives the deserialized <see cref="DiffResult"/> instance, or null if deserialization fails</param>
    /// <param name="showSecrets">Whether to include sensitive values in the output</param>
    /// <returns>True if deserialization succeeded; false otherwise</returns>
    public static bool TryFromJson(
        string json,
        SensitiveKeyDetector detector,
        out DiffResult? value,
        bool showSecrets = false)
    {
        try
        {
            value = FromJson(json, detector, showSecrets);
            return value != null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Internal DTO for JSON deserialization.
    /// </summary>
    private sealed class DiffResultJson
    {
        public string? BasePath { get; init; }
        public string? TargetPath { get; init; }
        public List<DiffEntryJson>? Entries { get; init; }
    }

    /// <summary>
    /// Internal DTO for JSON deserialization.
    /// </summary>
    private sealed class DiffEntryJson
    {
        public string? Kind { get; init; }
        public string? Key { get; init; }
        public string? OldValue { get; init; }
        public string? NewValue { get; init; }
        public bool IsSensitive { get; init; }
        public string? Path { get; init; }
    }
}