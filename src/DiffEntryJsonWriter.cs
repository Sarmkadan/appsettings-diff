using System;
using System.IO;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Shared low-level JSON serialization for <see cref="DiffResult"/> instances.
/// Streams entries straight through a <see cref="Utf8JsonWriter"/> instead of projecting
/// them into an anonymous-type object graph and serializing that graph via reflection,
/// which is what every <see cref="IDiffReportWriter"/> implementation used to do independently.
/// This matters once a diff spans hundreds of environments and thousands of keys: the
/// object-graph approach allocates one throwaway object per entry before serialization can
/// even begin, while this writes each field directly to the destination buffer.
/// </summary>
internal static class DiffEntryJsonWriter
{
    /// <summary>
    /// Writes the full diff result (base/target paths and every entry) to the supplied
    /// <see cref="Utf8JsonWriter"/>.
    /// </summary>
    /// <param name="jsonWriter">The writer entries are streamed to.</param>
    /// <param name="result">The diff result to serialize.</param>
    /// <param name="redact">Callback that redacts a raw value according to the caller's redaction policy.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="jsonWriter"/>, <paramref name="result"/>, or <paramref name="redact"/> is <see langword="null"/>.
    /// </exception>
    public static void WriteFullResult(Utf8JsonWriter jsonWriter, DiffResult result, Func<string?, bool, string> redact)
    {
        ArgumentNullException.ThrowIfNull(jsonWriter);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(redact);

        jsonWriter.WriteStartObject();
        jsonWriter.WriteString("BasePath", result.BasePath);
        jsonWriter.WriteString("TargetPath", result.TargetPath);
        jsonWriter.WriteStartArray("Entries");

        var entriesSinceFlush = 0;
        foreach (var entry in result.Entries)
        {
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("Kind", entry.Kind.ToString());
            jsonWriter.WriteString("Key", entry.Key);
            jsonWriter.WriteString("OldValue", redact(entry.OldValue, entry.IsSensitive));
            jsonWriter.WriteString("NewValue", redact(entry.NewValue, entry.IsSensitive));

            if (entry.Path is null)
                jsonWriter.WriteNull("Path");
            else
                jsonWriter.WriteString("Path", entry.Path);

            jsonWriter.WriteBoolean("IsSensitive", entry.IsSensitive);

            if (entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null)
            {
                jsonWriter.WriteString("OldType", entry.OldType);
                jsonWriter.WriteString("NewType", entry.NewType);
            }
            else
            {
                jsonWriter.WriteNull("OldType");
                jsonWriter.WriteNull("NewType");
            }

            jsonWriter.WriteEndObject();

            // Flush the underlying buffer every FlushEveryEntries entries so memory use for
            // very large diffs (thousands of keys) stays bounded instead of growing with the
            // full result size before a single flush at the very end.
            if (++entriesSinceFlush >= FlushEveryEntries)
            {
                jsonWriter.Flush();
                entriesSinceFlush = 0;
            }
        }

        jsonWriter.WriteEndArray();
        jsonWriter.WriteEndObject();
    }

    /// <summary>
    /// Number of entries written between periodic <see cref="Utf8JsonWriter.Flush"/> calls
    /// while streaming a large diff result.
    /// </summary>
    private const int FlushEveryEntries = 512;

    /// <summary>
    /// Serializes the full diff result directly to the supplied <see cref="TextWriter"/> using a
    /// <see cref="Utf8JsonWriter"/> over a <see cref="Utf8TextWriterStream"/> adapter, decoding and
    /// forwarding bytes as they are produced instead of buffering the whole payload in memory first.
    /// Avoids both the anonymous-type object graph the reflection-based
    /// <see cref="JsonSerializer.Serialize{TValue}(TValue, JsonSerializerOptions?)"/> path requires and
    /// the full in-memory copy a <see cref="MemoryStream"/> intermediary would otherwise force.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="result">The diff result to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <param name="redact">Callback that redacts a raw value according to the caller's redaction policy.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="writer"/>, <paramref name="result"/>, or <paramref name="redact"/> is <see langword="null"/>.
    /// </exception>
    public static void WriteFullResultTo(TextWriter writer, DiffResult result, bool indented, Func<string?, bool, string> redact)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(redact);

        using var stream = new Utf8TextWriterStream(writer);
        var jsonOptions = new JsonWriterOptions { Indented = indented };
        using var jsonWriter = new Utf8JsonWriter(stream, jsonOptions);
        WriteFullResult(jsonWriter, result, redact);
    }
}
