using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results by streaming entries directly to the output instead of
/// building the full report in memory first. This implementation focuses on
/// efficient memory usage for large diffs.
/// </summary>
public sealed class DiffReportWriter : DiffReportWriterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public DiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
        : base(detector, showSecrets, maskSensitive)
    {
    }

    /// <inheritdoc />
    public override void WriteConsole(DiffResult result, bool noColor = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Stream console output directly without building intermediate strings
        var separator = new string('-', 80);
        var disableColour = noColor || Console.IsOutputRedirected;
        var originalColour = Console.ForegroundColor;
        var currentColour = originalColour;

        // Header
        var header = new StringBuilder(256)
            .Append("Diff between \"").Append(result.BasePath).Append("\" and \"").Append(result.TargetPath).Append('"').Append('\n')
            .Append(separator).Append('\n')
            .AppendFormat("{0,-15} {1,-40} {2,-15} {3}", "Kind", "Key", "Old Value", "New Value").Append('\n')
            .Append(separator);
        Console.Out.WriteLine(header.ToString());

        // Stream entries directly
        var lineBuffer = new StringBuilder(1024);
        var linesSinceFlush = 0;
        const int FlushThreshold = 64;

        foreach (var entry in result.Entries)
        {
            var colour = disableColour
                ? ConsoleColor.Gray
                : entry.Kind switch
                {
                    DiffKind.Added => ConsoleColor.Green,
                    DiffKind.Removed => ConsoleColor.Red,
                    DiffKind.Changed => ConsoleColor.Yellow,
                    DiffKind.TypeChanged => ConsoleColor.Magenta,
                    _ => ConsoleColor.Gray
                };

            var oldVal = Redact(entry.OldValue, entry.IsSensitive);
            var newVal = Redact(entry.NewValue, entry.IsSensitive);

            var displayText = entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null
                ? $"{entry.Kind} ({entry.OldType}→{entry.NewType}) "
                : entry.Kind.ToString();

            var sb = lineBuffer;
            sb.Clear();
            sb.AppendFormat("{0,-15} {1,-40} {2,-15} {3}",
                displayText,
                Truncate(entry.Key, 40),
                Truncate(oldVal, 15),
                Truncate(newVal, 30));

            if (colour != currentColour)
            {
                Console.ForegroundColor = colour;
                currentColour = colour;
            }

            Console.Out.WriteLine(sb.ToString());
            linesSinceFlush++;

            if (linesSinceFlush >= FlushThreshold)
            {
                Console.Out.Flush();
                linesSinceFlush = 0;
            }
        }

        if (currentColour != originalColour)
        {
            Console.ForegroundColor = originalColour;
        }

        Console.Out.WriteLine(separator);
        Console.Out.Flush();
    }

    /// <inheritdoc />
    public override void WriteMarkdown(DiffResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        // Stream markdown directly
        var added = result.CountOf(DiffKind.Added);
        var removed = result.CountOf(DiffKind.Removed);
        var changed = result.CountOf(DiffKind.Changed);
        var typeChanged = result.CountOf(DiffKind.TypeChanged);
        writer.WriteLine($"**Summary:** Added: {added}, Removed: {removed}, Changed: {changed}, TypeChanged: {typeChanged}");
        writer.WriteLine();

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

        writer.Flush();
    }

    /// <inheritdoc />
    public override void WriteHtml(DiffResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

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
        writer.WriteLine($" <p>Comparing <strong>{EscapeHtml(result.BasePath)}</strong> with <strong>{EscapeHtml(result.TargetPath)}</strong></p>");

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

        writer.Flush();
    }

    /// <inheritdoc />
    public override void WriteJsonPatch(DiffResult result, TextWriter writer)
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
            if (!IsValidPath(path))
            {
                throw new ArgumentException("Invalid path", nameof(entry.Key));
            }

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
            if (++entriesSinceFlush >= 512)
            {
                jsonWriter.Flush();
                entriesSinceFlush = 0;
            }
        }

        jsonWriter.WriteEndArray();
    }

    // Helper methods copied from existing writers to avoid duplication
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

    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        return System.Net.WebUtility.HtmlEncode(text)
            .Replace(" ", "&nbsp;")
            .Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
    }

    private bool IsValidPath(string path)
    {
        // Simple validation for now, can be improved based on specific requirements
        return !string.IsNullOrWhiteSpace(path) && path.StartsWith("/");
    }
}