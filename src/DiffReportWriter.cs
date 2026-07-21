using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppsettingsDiff;

/// <summary>
/// Represents a JSON Patch operation according to RFC 6902
/// </summary>
public sealed class JsonPatchOperation
{
    [JsonPropertyName("op")]
    public required string Op { get; set; }

    [JsonPropertyName("path")]
    public required string Path { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }
}


/// <summary>
/// Writes diff results to various output formats.
/// </summary>
public sealed class DiffReportWriter
{
    private readonly SensitiveKeyDetector _detector;
    private readonly bool _showSecrets;
    private readonly bool _maskSensitive;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public DiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(detector);

        _detector = detector;
        _showSecrets = showSecrets;
    _maskSensitive = maskSensitive;
    }

    /// <summary>
    /// Writes a colour‑coded table to the console.
    /// Added – green
    /// Removed – red
    /// Changed – yellow
    /// TypeChanged – magenta
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    public void WriteConsole(DiffResult result, bool noColor = false)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        // Header
        Console.WriteLine($"Diff between \"{result.BasePath}\" and \"{result.TargetPath}\"");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("{0,-15} {1,-40} {2,-15} {3}", "Kind", "Key", "Old Value", "New Value");
        Console.WriteLine(new string('-', 80));

        foreach (var entry in result.Entries)
        {
            var colour = entry.Kind switch
            {
                DiffKind.Added => ConsoleColor.Green,
                DiffKind.Removed => ConsoleColor.Red,
                DiffKind.Changed => ConsoleColor.Yellow,
                DiffKind.TypeChanged => ConsoleColor.Magenta,
                _ => ConsoleColor.Gray
            };

            var oldVal = Redact(entry.OldValue, entry.IsSensitive);
            var newVal = Redact(entry.NewValue, entry.IsSensitive);

            // Auto-disable colors when output is redirected or --no-color flag is set
            if (noColor || Console.IsOutputRedirected)
            {
                colour = ConsoleColor.Gray;
            }

            var originalColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;

            // For TypeChanged entries, show type information
            string displayText;
            if (entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null)
            {
                displayText = $"{entry.Kind} ({entry.OldType}→{entry.NewType}) ";
            }
            else
            {
                displayText = entry.Kind.ToString();
            }

            Console.WriteLine("{0,-15} {1,-40} {2,-15} {3}",
                displayText,
                Truncate(entry.Key, 40),
                Truncate(oldVal, 15),
                Truncate(newVal, 30));
            Console.ForegroundColor = originalColour;
        }

        Console.WriteLine(new string('-', 80));
    }

    /// <summary>
    /// Serialises the diff result to indented JSON.
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is <see langword="null"/>.</exception>
    public string ToJson(DiffResult result) => ToJson(result, indented: true);

    /// <summary>
    /// Serialises the diff result to JSON, indented or compact.
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is <see langword="null"/>.</exception>
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
    /// Includes a summary line and a table with columns: Key | Change | Old | New.
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    public void WriteMarkdown(DiffResult result, TextWriter writer)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        // Summary line
        var added = result.CountOf(DiffKind.Added);
        var removed = result.CountOf(DiffKind.Removed);
        var changed = result.CountOf(DiffKind.Changed);
        var typeChanged = result.CountOf(DiffKind.TypeChanged);
        writer.WriteLine($"**Summary:** Added: {added}, Removed: {removed}, Changed: {changed}, TypeChanged: {typeChanged}");
        writer.WriteLine();

        // Table header
        writer.WriteLine("| Key | Change | Old | New |");
        writer.WriteLine("|---|---|---|---|");

        foreach (var entry in result.Entries)
        {
            var key = EscapeMarkdown(entry.Key);
            string change;
            string oldVal;
            string newVal;

            if (entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null)
            {
                change = $"{entry.Kind} ({entry.OldType}→{entry.NewType}) ";
                oldVal = EscapeMarkdown(Redact(entry.OldValue, entry.IsSensitive));
                newVal = EscapeMarkdown(Redact(entry.NewValue, entry.IsSensitive));
            }
            else
            {
                change = EscapeMarkdown(entry.Kind.ToString());
                oldVal = EscapeMarkdown(Redact(entry.OldValue, entry.IsSensitive));
                newVal = EscapeMarkdown(Redact(entry.NewValue, entry.IsSensitive));
            }

            writer.WriteLine($"| {key} | {change} | {oldVal} | {newVal} |");
        }
    }

    private string Redact(string? value, bool isSensitive)
    {
        if (isSensitive && !_showSecrets)
            return _maskSensitive ? "***" : "[REDACTED]";

        return value ?? string.Empty;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    private static string EscapeMarkdown(string text)
    {
        // Escape pipe and backticks which break markdown tables
        return text
            .Replace("|", "\\|")
            .Replace("`", "\\`")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// Uses inline CSS for styling with color-coded sections:
    /// Added – green background
    /// Removed – red background
    /// Changed – yellow background
    /// TypeChanged – purple background
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    public void WriteHtml(DiffResult result, TextWriter writer)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WriteLine("<!DOCTYPE html>");
        writer.WriteLine("<html lang=\"en\">");
        writer.WriteLine("<head>");
        writer.WriteLine(" <meta charset=\"utf-8\">");
        writer.WriteLine(" <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        writer.WriteLine(" <title>Configuration Diff Report</title>");
        writer.WriteLine(" <style>");
        writer.WriteLine(" body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; margin: 2rem; line-height: 1.6; color: #333; }");
        writer.WriteLine(" h1 { color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 0.5rem; }");
        writer.WriteLine(" h2 { color: #34495e; margin-top: 2rem; }");
        writer.WriteLine(" .summary { background-color: #f8f9fa; padding: 1rem; border-radius: 4px; margin-bottom: 2rem; border-left: 4px solid #3498db; }");
        writer.WriteLine(" table { width: 100%; border-collapse: collapse; margin-top: 1rem; }");
        writer.WriteLine(" th, td { padding: 0.75rem; text-align: left; border-bottom: 1px solid #ddd; }");
        writer.WriteLine(" th { background-color: #f1f3f5; font-weight: 600; }");
        writer.WriteLine(" tr.added { background-color: #d4edda; }");
        writer.WriteLine(" tr.removed { background-color: #f8d7da; }");
        writer.WriteLine(" tr.changed { background-color: #fff3cd; }");
        writer.WriteLine(" tr.typechanged { background-color: #e8c5ff; }");
        writer.WriteLine(" .added { background-color: #d4edda !important; }");
        writer.WriteLine(" .removed { background-color: #f8d7da !important; }");
        writer.WriteLine(" .changed { background-color: #fff3cd !important; }");
        writer.WriteLine(" .typechanged { background-color: #e8c5ff !important; }");
        writer.WriteLine(" .sensitive { font-style: italic; color: #6c757d; }");
        writer.WriteLine(" .footer { margin-top: 3rem; font-size: 0.85rem; color: #6c757d; border-top: 1px solid #eee; padding-top: 1rem; }");
        writer.WriteLine(" </style>");
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");
        writer.WriteLine(" <h1>Configuration Diff Report</h1>");
        writer.WriteLine(" <p>Comparing <strong>{0}</strong> with <strong>{1}</strong></p>", EscapeHtml(result.BasePath), EscapeHtml(result.TargetPath));

        // Summary section
        writer.WriteLine(" <div class=\"summary\">");
        writer.WriteLine(" <h2>Summary</h2>");
        var added = result.CountOf(DiffKind.Added);
        var removed = result.CountOf(DiffKind.Removed);
        var changed = result.CountOf(DiffKind.Changed);
        var typeChanged = result.CountOf(DiffKind.TypeChanged);
        writer.WriteLine(" <p><strong>Added:</strong> {0}<br>", added);
        writer.WriteLine(" <strong>Removed:</strong> {0}<br>", removed);
        writer.WriteLine(" <strong>Changed:</strong> {0}<br>", changed);
        writer.WriteLine(" <strong>TypeChanged:</strong> {0}</p>", typeChanged);
        writer.WriteLine(" </div>");

        // Table section
        writer.WriteLine(" <h2>Details</h2>");
        writer.WriteLine(" <table>");
        writer.WriteLine(" <thead>");
        writer.WriteLine(" <tr>");
        writer.WriteLine(" <th>Key</th>");
        writer.WriteLine(" <th>Change</th>");
        writer.WriteLine(" <th>Old Value</th>");
        writer.WriteLine(" <th>New Value</th>");
        writer.WriteLine(" </tr>");
        writer.WriteLine(" </thead>");
        writer.WriteLine(" <tbody>");

        foreach (var entry in result.Entries)
        {
            var key = EscapeHtml(entry.Key);
            string change;
            string oldVal;
            string newVal;
            string rowClass;

            if (entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null)
            {
                change = $"{entry.Kind} ({entry.OldType}→{entry.NewType}) ";
                oldVal = EscapeHtml(Redact(entry.OldValue, entry.IsSensitive));
                newVal = EscapeHtml(Redact(entry.NewValue, entry.IsSensitive));
                rowClass = "typechanged";
            }
            else
            {
                change = EscapeHtml(entry.Kind.ToString());
                oldVal = EscapeHtml(Redact(entry.OldValue, entry.IsSensitive));
                newVal = EscapeHtml(Redact(entry.NewValue, entry.IsSensitive));
                rowClass = entry.Kind switch
                {
                    DiffKind.Added => "added",
                    DiffKind.Removed => "removed",
                    DiffKind.Changed => "changed",
                    _ => ""
                };
            }

            writer.WriteLine(" <tr class=\"{0}\">", rowClass);
            writer.WriteLine(" <td><code>{0}</code></td>", key);
            writer.WriteLine(" <td><span class=\"{0}\">{1}</span></td>", rowClass, change);
            writer.WriteLine(" <td><code>{0}</code></td>", oldVal);
            writer.WriteLine(" <td><code>{0}</code></td>", newVal);
            writer.WriteLine(" </tr>");
        }

        writer.WriteLine(" </tbody>");
        writer.WriteLine(" </table>");

        writer.WriteLine(" <div class=\"footer\">");
        writer.WriteLine(" <p>Generated by appsettings-diff at {0}</p>", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        writer.WriteLine(" <p>Base: {0} | Target: {1}</p>", EscapeHtml(result.BasePath), EscapeHtml(result.TargetPath));
        writer.WriteLine(" </div>");

        writer.WriteLine("</body>");
        writer.WriteLine("</html>");
    }

    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        return System.Net.WebUtility.HtmlEncode(text)
            .Replace(" ", "&nbsp;")
            .Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
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
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        var operations = new List<JsonPatchOperation>();

        foreach (var entry in result.Entries)
        {
            var path = EscapeJsonPointer(entry.Key);
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

    private static string EscapeJsonPointer(string path)
    {
        // JSON Pointer requires ~ to be encoded as ~0 and / to be encoded as ~1
        return path
            .Replace("~", "~0")
            .Replace("/", "~1");
    }

}
