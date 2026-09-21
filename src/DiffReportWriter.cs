using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results by building the full report in a single pre-sized StringBuilder
/// before writing it out. This simplifies the output logic and avoids multiple writes.
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

        var disableColour = noColor || Console.IsOutputRedirected;
        var separator = new string('-', 80);

        var sb = new StringBuilder(1024);
        sb.Append("Diff between \"").Append(result.BasePath).Append("\" and \"").Append(result.TargetPath).Append('"').Append('\n')
          .Append(separator).Append('\n')
          .AppendFormat("{0,-15} {1,-40} {2,-15} {3}", "Kind", "Key", "Old Value", "New Value").Append('\n')
          .Append(separator);

        foreach (var entry in result.Entries)
        {
            var oldVal = Redact(entry.OldValue, entry.IsSensitive);
            var newVal = Redact(entry.NewValue, entry.IsSensitive);

            var displayText = entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null
                ? $"{entry.Kind} ({entry.OldType}→{entry.NewType}) "
                : entry.Kind.ToString();

            sb.AppendFormat("{0,-15} {1,-40} {2,-15} {3}",
                displayText,
                Truncate(entry.Key, 40),
                Truncate(oldVal, 15),
                Truncate(newVal, 30));

            sb.Append('\n');
        }

        sb.Append(separator);
        Console.Out.WriteLine(sb.ToString());
        Console.Out.Flush();
    }

    /// <inheritdoc />
    public override void WriteMarkdown(DiffResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        var added = result.CountOf(DiffKind.Added);
        var removed = result.CountOf(DiffKind.Removed);
        var changed = result.CountOf(DiffKind.Changed);
        var typeChanged = result.CountOf(DiffKind.TypeChanged);

        var sb = new StringBuilder(1024);
        sb.Append($"**Summary:** Added: {added}, Removed: {removed}, Changed: {changed}, TypeChanged: {typeChanged}\n\n");
        sb.Append("| Key | Change | Old | New |\n");
        sb.Append("|---|---|---|---|\n");

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

            sb.Append($"| {key} | {change} | {oldVal} | {newVal} |\n");
        }

        writer.Write(sb.ToString());
    }

    /// <inheritdoc />
    public override void WriteHtml(DiffResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        var added = result.CountOf(DiffKind.Added);
        var removed = result.CountOf(DiffKind.Removed);
        var changed = result.CountOf(DiffKind.Changed);
        var typeChanged = result.CountOf(DiffKind.TypeChanged);

        var sb = new StringBuilder(2048);
        sb.Append("<!DOCTYPE html>\n<html lang=\"en\">\n<head>\n <meta charset=\"utf-8\">\n <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n <title>Configuration Diff Report</title>\n <style>\n body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; margin: 2rem; line-height: 1.6; color: #333; }\n h1 { color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 0.5rem; }\n h2 { color: #34495e; margin-top: 2rem; }\n .summary { background-color: #f8f9fa; padding: 1rem; border-radius: 4px; margin-bottom: 2rem; border-left: 4px solid #3498db; }\n table { width: 100%; border-collapse: collapse; margin-top: 1rem; }\n th, td { padding: 0.75rem; text-align: left; border-bottom: 1px solid #ddd; }\n th { background-color: #f1f3f5; font-weight: 600; }\n tr.added { background-color: #d4edda; }\n tr.removed { background-color: #f8d7da; }\n tr.changed { background-color: #fff3cd; }\n tr.typechanged { background-color: #e8c5ff; }\n .added { background-color: #d4edda !important; }\n .removed { background-color: #f8d7da !important; }\n .changed { background-color: #fff3cd !important; }\n .typechanged { background-color: #e8c5ff !important; }\n .sensitive { font-style: italic; color: #6c757d; }\n .footer { margin-top: 3rem; font-size: 0.85rem; color: #6c757d; border-top: 1px solid #eee; padding-top: 1rem; }\n </style>\n</head>\n<body>\n <h1>Configuration Diff Report</h1>\n <p>Comparing <strong>").Append(EscapeHtml(result.BasePath)).Append("</strong> with <strong>").Append(EscapeHtml(result.TargetPath)).Append("</strong></p>\n\n <div class=\"summary\">\n <h2>Summary</h2>\n <p><strong>Added:</strong> ").Append(added).Append("<br>\n <strong>Removed:</strong> ").Append(removed).Append("<br>\n <strong>Changed:</strong> ").Append(changed).Append("<br>\n <strong>TypeChanged:</strong> ").Append(typeChanged).Append("</p>\n </div>\n\n <h2>Details</h2>\n <table>\n <thead>\n <tr>\n <th>Key</th>\n <th>Change</th>\n <th>Old Value</th>\n <th>New Value</th>\n </tr>\n </thead>\n <tbody>\n");

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

            sb.Append(" <tr class=\"").Append(rowClass).Append("\">\n <td><code>").Append(key).Append("</code></td>\n <td><span class=\"").Append(rowClass).Append("\">").Append(change).Append("</span></td>\n <td><code>").Append(oldVal).Append("</code></td>\n <td><code>").Append(newVal).Append("</code></td>\n </tr>\n");
        }

        sb.Append(" </tbody>\n </table>\n\n <div class=\"footer\">\n <p>Generated by appsettings-diff at ").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("</p>\n <p>Base: ").Append(EscapeHtml(result.BasePath)).Append(" | Target: ").Append(EscapeHtml(result.TargetPath)).Append("</p>\n </div>\n\n</body>\n</html>");

        writer.Write(sb.ToString());
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

            var value = Redact(
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
