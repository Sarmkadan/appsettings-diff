using System;
using System.Collections.Generic;
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
public sealed class JsonPatchDiffReportWriter : IDiffReportWriter
{
    private readonly SensitiveKeyDetector _detector;
    private readonly bool _showSecrets;
    private readonly bool _maskSensitive;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPatchDiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public JsonPatchDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(detector);

        _detector = detector;
        _showSecrets = showSecrets;
        _maskSensitive = maskSensitive;
    }

    /// <summary>
    /// Writes a colour‑coded table to the console.
    /// </summary>
    public void WriteConsole(DiffResult result, bool noColor = false)
    {
        throw new NotSupportedException("JsonPatchDiffReportWriter only supports JSON Patch output. Use ConsoleDiffReportWriter for console output.");
    }

    /// <summary>
    /// Serialises the diff result to indented JSON.
    /// </summary>
    public string ToJson(DiffResult result) => ToJson(result, indented: true);

    /// <summary>
    /// Serialises the diff result to JSON, indented or compact.
    /// </summary>
    public string ToJson(DiffResult result, bool indented)
    {
        ArgumentNullException.ThrowIfNull(result);

        var serialisable = new
        {
            result.BasePath,
            result.TargetPath,
            Entries = result.Entries.Select(e => new
            {
                Kind = e.Kind.ToString(),
                e.Key,
                OldValue = Redact(e.OldValue, e.IsSensitive),
                NewValue = Redact(e.NewValue, e.IsSensitive),
                e.Path,
                e.IsSensitive,
                OldType = e.Kind == DiffKind.TypeChanged ? e.OldType : null,
                NewType = e.Kind == DiffKind.TypeChanged ? e.NewType : null
            })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(serialisable, options);
    }

    /// <summary>
    /// Writes a GitHub‑flavored markdown report to the supplied writer.
    /// </summary>
    public void WriteMarkdown(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("JsonPatchDiffReportWriter only supports JSON Patch output. Use MarkdownDiffReportWriter for markdown output.");
    }

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// </summary>
    public void WriteHtml(DiffResult result, TextWriter writer)
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
    /// <returns>JSON Patch array as a string.</returns>
    public string ToJsonPatch(DiffResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        var operations = new List<JsonPatchOperation>();

        foreach (var entry in result.Entries)
        {
            var path = JsonPatchOperation.FromConfigKey(entry.Key);
            var value = entry.IsSensitive && !_showSecrets
                ? "[REDACTED]"
                : (entry.Kind == DiffKind.Removed ? entry.OldValue : entry.NewValue) ?? string.Empty;

            string op;
            switch (entry.Kind)
            {
                case DiffKind.Added:
                    op = "add";
                    break;

                case DiffKind.Removed:
                    op = "remove";
                    break;

                case DiffKind.Changed:
                case DiffKind.TypeChanged:
                    op = "replace";
                    break;

                default:
                    op = "replace";
                    break;
            }

            var operation = new JsonPatchOperation
            {
                Op = op,
                Path = path,
                Value = value
            };

            // Add type information for TypeChanged entries
            if (entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null)
            {
                operation.Value = $"[TYPE_CHANGED: {entry.OldType}→{entry.NewType}] {value}";
            }

            operations.Add(operation);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(operations, options);
    }

    private string Redact(string? value, bool isSensitive)
    {
        if (isSensitive && !_showSecrets)
            return _maskSensitive ? "***" : "[REDACTED]";

        return value ?? string.Empty;
    }

}
