using System;
using System.IO;
using System.Linq;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results as GitHub-flavored markdown reports.
/// </summary>
public sealed class MarkdownDiffReportWriter : IDiffReportWriter
{
    private readonly SensitiveKeyDetector _detector;
    private readonly bool _showSecrets;
    private readonly bool _maskSensitive;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownDiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public MarkdownDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
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
        throw new NotSupportedException("MarkdownDiffReportWriter only supports markdown output. Use ConsoleDiffReportWriter for console output.");
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

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = indented
        };

        return System.Text.Json.JsonSerializer.Serialize(serialisable, options);
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

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// </summary>
    public void WriteHtml(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("MarkdownDiffReportWriter only supports markdown output. Use HtmlDiffReportWriter for HTML output.");
    }

    /// <summary>
    /// Generates a JSON Patch (RFC 6902) representation of the diff.
    /// </summary>
    public string ToJsonPatch(DiffResult result)
    {
        throw new NotSupportedException("MarkdownDiffReportWriter does not support JSON Patch output. Use JsonPatchDiffReportWriter for JSON Patch output.");
    }

    private string Redact(string? value, bool isSensitive)
    {
        if (isSensitive && !_showSecrets)
            return _maskSensitive ? "***" : "[REDACTED]";

        return value ?? string.Empty;
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