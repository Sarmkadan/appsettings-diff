using System;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Generates JSON Patch (RFC 6902) representations of diffs.
/// Each difference is converted to a JSON Patch operation:
/// - Added keys become "add" operations
/// - Removed keys become "remove" operations
/// - Changed keys become "replace" operations
/// - TypeChanged keys become "replace" operations with type information in a custom property
/// Sensitive values are redacted unless <c>showSecrets</c> is true.
/// </summary>
public sealed class JsonPatchDiffReportWriter : DiffReportWriterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPatchDiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public JsonPatchDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
        : base(detector, showSecrets, maskSensitive)
    {
    }

    /// <summary>
    /// Writes a colour‑coded table to the console.
    /// </summary>
    public override void WriteConsole(DiffResult result, bool noColor = false)
    {
        throw new NotSupportedException("JsonPatchDiffReportWriter only supports JSON Patch output. Use ConsoleDiffReportWriter for console output.");
    }

    /// <summary>
    /// Writes a GitHub‑flavored markdown report to the supplied writer.
    /// </summary>
    public override void WriteMarkdown(DiffResult result, System.IO.TextWriter writer)
    {
        throw new NotSupportedException("JsonPatchDiffReportWriter only supports JSON Patch output. Use MarkdownDiffReportWriter for markdown output.");
    }

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// </summary>
    public override void WriteHtml(DiffResult result, System.IO.TextWriter writer)
    {
        throw new NotSupportedException("JsonPatchDiffReportWriter only supports JSON Patch output. Use HtmlDiffReportWriter for HTML output.");
    }

    /// <summary>
    /// Generates a JSON Patch (RFC 6902) representation of the diff.
    /// Each difference is converted to a JSON Patch operation:
    /// - Added keys become "add" operations
    /// - Removed keys become "remove" operations
    /// - Changed keys become "replace" operations
    /// - TypeChanged keys become "replace" operations with type information in a custom property
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    /// <param name="result">The diff result to convert.</param>
    /// <param name="writer">The destination writer.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    public override void WriteJsonPatch(DiffResult result, System.IO.TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        using var stream = new Utf8TextWriterStream(writer);
        var jsonOptions = new JsonWriterOptions { Indented = true };
        using var jsonWriter = new Utf8JsonWriter(stream, jsonOptions);

        jsonWriter.WriteStartArray();

        var entriesSinceFlush = 0;
        foreach (var entry in result.Entries)
        {
            var path = JsonPatchOperation.FromConfigKey(entry.Key);
            var value = Policy.Redact(
                entry.Kind == DiffKind.Removed ? entry.OldValue : entry.NewValue,
                entry.IsSensitive);

            var op = entry.Kind switch
            {
                DiffKind.Added => "add",
                DiffKind.Removed => "remove",
                DiffKind.Changed => "replace",
                DiffKind.TypeChanged => "replace",
                _ => "replace"
            };

            // Add type information for TypeChanged entries
            if (entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null)
            {
                value = $"[TYPE_CHANGED: {entry.OldType}→{entry.NewType}] {value}";
            }

            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("op", op);
            jsonWriter.WriteString("path", path);
            jsonWriter.WriteString("value", value);
            jsonWriter.WriteEndObject();

            // Flush the underlying buffer periodically so memory use for very large diffs
            // stays bounded instead of growing with the full patch size before a single
            // flush at the very end.
            if (++entriesSinceFlush >= FlushEveryEntries)
            {
                jsonWriter.Flush();
                entriesSinceFlush = 0;
            }
        }

        jsonWriter.WriteEndArray();
    }

    /// <summary>
    /// Number of entries written between periodic <see cref="Utf8JsonWriter.Flush"/> calls
    /// while streaming a large diff result.
    /// </summary>
    private const int FlushEveryEntries = 512;
}
