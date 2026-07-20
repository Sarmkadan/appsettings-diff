using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results to various output formats.
/// </summary>
public sealed class DiffReportWriter
{
    private readonly SensitiveKeyDetector _detector;
    private readonly bool _showSecrets;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public DiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false)
    {
        ArgumentNullException.ThrowIfNull(detector);

        _detector = detector;
        _showSecrets = showSecrets;
    }

    /// <summary>
    /// Writes a colour‑coded table to the console.
    /// Added   – green
    /// Removed – red
    /// Changed – yellow
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    public void WriteConsole(DiffResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        // Header
        Console.WriteLine($"Diff between \"{result.BasePath}\" and \"{result.TargetPath}\"");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine("{0,-10} {1,-40} {2,-15} {3}", "Kind", "Key", "Old Value", "New Value");
        Console.WriteLine(new string('-', 80));

        foreach (var entry in result.Entries)
        {
            var colour = entry.Kind switch
            {
                DiffKind.Added => ConsoleColor.Green,
                DiffKind.Removed => ConsoleColor.Red,
                DiffKind.Changed => ConsoleColor.Yellow,
                _ => ConsoleColor.Gray
            };

            var oldVal = Redact(entry.OldValue, entry.IsSensitive);
            var newVal = Redact(entry.NewValue, entry.IsSensitive);

            var originalColour = Console.ForegroundColor;
            Console.ForegroundColor = colour;
            Console.WriteLine("{0,-10} {1,-40} {2,-15} {3}",
                entry.Kind,
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
                e.IsSensitive
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
        writer.WriteLine($"**Summary:** Added: {added}, Removed: {removed}, Changed: {changed}");
        writer.WriteLine();

        // Table header
        writer.WriteLine("| Key | Change | Old | New |");
        writer.WriteLine("|---|---|---|---|");

        foreach (var entry in result.Entries)
        {
            var key = EscapeMarkdown(entry.Key);
            var change = EscapeMarkdown(entry.Kind.ToString());
            var oldVal = EscapeMarkdown(Redact(entry.OldValue, entry.IsSensitive));
            var newVal = EscapeMarkdown(Redact(entry.NewValue, entry.IsSensitive));

            writer.WriteLine($"| {key} | {change} | {oldVal} | {newVal} |");
        }
    }

    private string Redact(string? value, bool isSensitive)
    {
        if (isSensitive && !_showSecrets)
            return "[REDACTED]";

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
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    public void WriteHtml(DiffResult result, TextWriter writer)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WriteLine("<!DOCTYPE html>");
        writer.WriteLine("<html lang=\"en\">");
        writer.WriteLine("<head>");
        writer.WriteLine("  <meta charset=\"utf-8\">");
        writer.WriteLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        writer.WriteLine("  <title>Configuration Diff Report</title>");
        writer.WriteLine("  <style>");
        writer.WriteLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; margin: 2rem; line-height: 1.6; color: #333; }");
        writer.WriteLine("    h1 { color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 0.5rem; }");
        writer.WriteLine("    h2 { color: #34495e; margin-top: 2rem; }");
        writer.WriteLine("    .summary { background-color: #f8f9fa; padding: 1rem; border-radius: 4px; margin-bottom: 2rem; border-left: 4px solid #3498db; }");
        writer.WriteLine("    table { width: 100%; border-collapse: collapse; margin-top: 1rem; }");
        writer.WriteLine("    th, td { padding: 0.75rem; text-align: left; border-bottom: 1px solid #ddd; }");
        writer.WriteLine("    th { background-color: #f1f3f5; font-weight: 600; }");
        writer.WriteLine("    tr.added { background-color: #d4edda; }");
        writer.WriteLine("    tr.removed { background-color: #f8d7da; }");
        writer.WriteLine("    tr.changed { background-color: #fff3cd; }");
        writer.WriteLine("    .added { background-color: #d4edda !important; }");
        writer.WriteLine("    .removed { background-color: #f8d7da !important; }");
        writer.WriteLine("    .changed { background-color: #fff3cd !important; }");
        writer.WriteLine("    .sensitive { font-style: italic; color: #6c757d; }");
        writer.WriteLine("    .footer { margin-top: 3rem; font-size: 0.85rem; color: #6c757d; border-top: 1px solid #eee; padding-top: 1rem; }");
        writer.WriteLine("  </style>");
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");
        writer.WriteLine("  <h1>Configuration Diff Report</h1>");
        writer.WriteLine("  <p>Comparing <strong>{0}</strong> with <strong>{1}</strong></p>", EscapeHtml(result.BasePath), EscapeHtml(result.TargetPath));

        // Summary section
        writer.WriteLine("  <div class=\"summary\">");
        writer.WriteLine("    <h2>Summary</h2>");
        var added = result.CountOf(DiffKind.Added);
        var removed = result.CountOf(DiffKind.Removed);
        var changed = result.CountOf(DiffKind.Changed);
        writer.WriteLine("    <p><strong>Added:</strong> {0}<br>", added);
        writer.WriteLine("       <strong>Removed:</strong> {0}<br>", removed);
        writer.WriteLine("       <strong>Changed:</strong> {0}</p>", changed);
        writer.WriteLine("  </div>");

        // Table section
        writer.WriteLine("  <h2>Details</h2>");
        writer.WriteLine("  <table>");
        writer.WriteLine("    <thead>");
        writer.WriteLine("      <tr>");
        writer.WriteLine("        <th>Key</th>");
        writer.WriteLine("        <th>Change</th>");
        writer.WriteLine("        <th>Old Value</th>");
        writer.WriteLine("        <th>New Value</th>");
        writer.WriteLine("      </tr>");
        writer.WriteLine("    </thead>");
        writer.WriteLine("    <tbody>");

        foreach (var entry in result.Entries)
        {
            var key = EscapeHtml(entry.Key);
            var change = EscapeHtml(entry.Kind.ToString());
            var oldVal = EscapeHtml(Redact(entry.OldValue, entry.IsSensitive));
            var newVal = EscapeHtml(Redact(entry.NewValue, entry.IsSensitive));
            var rowClass = entry.Kind switch
            {
                DiffKind.Added => "added",
                DiffKind.Removed => "removed",
                DiffKind.Changed => "changed",
                _ => ""
            };

            writer.WriteLine("      <tr class=\"{0}\">", rowClass);
            writer.WriteLine("        <td><code>{0}</code></td>", key);
            writer.WriteLine("        <td><span class=\"{0}\">{1}</span></td>", rowClass, change);
            writer.WriteLine("        <td><code>{0}</code></td>", oldVal);
            writer.WriteLine("        <td><code>{0}</code></td>", newVal);
            writer.WriteLine("      </tr>");
        }

        writer.WriteLine("    </tbody>");
        writer.WriteLine("  </table>");

        writer.WriteLine("  <div class=\"footer\">");
        writer.WriteLine("    <p>Generated by appsettings-diff at {0}</p>", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        writer.WriteLine("    <p>Base: {0} | Target: {1}</p>", EscapeHtml(result.BasePath), EscapeHtml(result.TargetPath));
        writer.WriteLine("  </div>");

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
}
