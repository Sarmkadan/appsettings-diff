using System;
using System.IO;
using System.Linq;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results as GitHub-flavored markdown reports.
/// </summary>
public sealed class MarkdownDiffReportWriter : DiffReportWriterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownDiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public MarkdownDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
        : base(detector, showSecrets, maskSensitive)
    {
    }

    /// <summary>
    /// Writes a colour‑coded table to the console.
    /// </summary>
    public override void WriteConsole(DiffResult result, bool noColor = false)
    {
        throw new NotSupportedException("MarkdownDiffReportWriter only supports markdown output. Use ConsoleDiffReportWriter for console output.");
    }

    /// <summary>
    /// Writes a GitHub‑flavored markdown report to the supplied writer.
    /// Includes a summary line and a table with columns: Key | Change | Old | New.
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    public override void WriteMarkdown(DiffResult result, TextWriter writer)
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

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// </summary>
    public override void WriteHtml(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("MarkdownDiffReportWriter only supports markdown output. Use HtmlDiffReportWriter for HTML output.");
    }

    /// <summary>
    /// Streams a JSON Patch (RFC 6902) representation of the diff directly to the supplied writer.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown; use <see cref="JsonPatchDiffReportWriter"/> instead.</exception>
    public override void WriteJsonPatch(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("MarkdownDiffReportWriter does not support JSON Patch output. Use JsonPatchDiffReportWriter for JSON Patch output.");
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
}
