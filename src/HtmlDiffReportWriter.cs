using System;
using System.IO;
using System.Text;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results as self-contained HTML reports with inline CSS styling.
/// </summary>
public sealed class HtmlDiffReportWriter : DiffReportWriterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlDiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public HtmlDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
        : base(detector, showSecrets, maskSensitive)
    {
    }

    /// <summary>
    /// Writes a colour‑coded table to the console.
    /// </summary>
    public override void WriteConsole(DiffResult result, bool noColor = false)
    {
        throw new NotSupportedException("HtmlDiffReportWriter only supports HTML output. Use ConsoleDiffReportWriter for console output.");
    }

    /// <summary>
    /// Writes a GitHub‑flavored markdown report to the supplied writer.
    /// </summary>
    public override void WriteMarkdown(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("HtmlDiffReportWriter only supports HTML output. Use MarkdownDiffReportWriter for markdown output.");
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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    public override void WriteHtml(DiffResult result, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        var sb = new StringBuilder();
        
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine(" <meta charset=\"utf-8\">");
        sb.AppendLine(" <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine(" <title>Configuration Diff Report</title>");
        sb.AppendLine(" <style>");
        sb.AppendLine(" body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif; margin: 2rem; line-height: 1.6; color: #333; }");
        sb.AppendLine(" h1 { color: #2c3e50; border-bottom: 2px solid #3498db; padding-bottom: 0.5rem; }");
        sb.AppendLine(" h2 { color: #34495e; margin-top: 2rem; }");
        sb.AppendLine(" .summary { background-color: #f8f9fa; padding: 1rem; border-radius: 4px; margin-bottom: 2rem; border-left: 4px solid #3498db; }");
        sb.AppendLine(" table { width: 100%; border-collapse: collapse; margin-top: 1rem; }");
        sb.AppendLine(" th, td { padding: 0.75rem; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine(" th { background-color: #f1f3f5; font-weight: 600; }");
        sb.AppendLine(" tr.added { background-color: #d4edda; }");
        sb.AppendLine(" tr.removed { background-color: #f8d7da; }");
        sb.AppendLine(" tr.changed { background-color: #fff3cd; }");
        sb.AppendLine(" tr.typechanged { background-color: #e8c5ff; }");
        sb.AppendLine(" .added { background-color: #d4edda !important; }");
        sb.AppendLine(" .removed { background-color: #f8d7da !important; }");
        sb.AppendLine(" .changed { background-color: #fff3cd !important; }");
        sb.AppendLine(" .typechanged { background-color: #e8c5ff !important; }");
        sb.AppendLine(" .sensitive { font-style: italic; color: #6c757d; }");
        sb.AppendLine(" .footer { margin-top: 3rem; font-size: 0.85rem; color: #6c757d; border-top: 1px solid #eee; padding-top: 1rem; }");
        sb.AppendLine(" </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine(" <h1>Configuration Diff Report</h1>");
        
        var basePathEscaped = EscapeHtml(result.BasePath);
        var targetPathEscaped = EscapeHtml(result.TargetPath);
        sb.AppendLine(" <p>Comparing <strong>" + basePathEscaped + "</strong> with <strong>" + targetPathEscaped + "</strong></p>");

        // Summary section
        sb.AppendLine(" <div class=\"summary\">");
        sb.AppendLine(" <h2>Summary</h2>");
        var added = result.CountOf(DiffKind.Added);
        var removed = result.CountOf(DiffKind.Removed);
        var changed = result.CountOf(DiffKind.Changed);
        var typeChanged = result.CountOf(DiffKind.TypeChanged);
        sb.AppendLine(" <p><strong>Added:</strong> " + added + "<br>");
        sb.AppendLine(" <strong>Removed:</strong> " + removed + "<br>");
        sb.AppendLine(" <strong>Changed:</strong> " + changed + "<br>");
        sb.AppendLine(" <strong>TypeChanged:</strong> " + typeChanged + "</p>");
        sb.AppendLine(" </div>");

        // Table section
        sb.AppendLine(" <h2>Details</h2>");
        sb.AppendLine(" <table>");
        sb.AppendLine(" <thead>");
        sb.AppendLine(" <tr>");
        sb.AppendLine(" <th>Key</th>");
        sb.AppendLine(" <th>Change</th>");
        sb.AppendLine(" <th>Old Value</th>");
        sb.AppendLine(" <th>New Value</th>");
        sb.AppendLine(" </tr>");
        sb.AppendLine(" </thead>");
        sb.AppendLine(" <tbody>");

        foreach (var entry in result.Entries)
        {
            var keyEscaped = EscapeHtml(entry.Key);
            string change;
            string oldVal;
            string newVal;
            string rowClass;

            if (entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null)
            {
                change = entry.Kind + " (" + entry.OldType + "→" + entry.NewType + ") ";
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

            sb.AppendLine(" <tr class=\"" + rowClass + "\">");
            sb.AppendLine(" <td><code>" + keyEscaped + "</code></td>");
            sb.AppendLine(" <td><span class=\"" + rowClass + "\">" + change + "</span></td>");
            sb.AppendLine(" <td><code>" + oldVal + "</code></td>");
            sb.AppendLine(" <td><code>" + newVal + "</code></td>");
            sb.AppendLine(" </tr>");
        }

        sb.AppendLine(" </tbody>");
        sb.AppendLine(" </table>");

        sb.AppendLine(" <div class=\"footer\">");
        sb.AppendLine(" <p>Generated by appsettings-diff at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "</p>");
        sb.AppendLine(" <p>Base: " + basePathEscaped + " | Target: " + targetPathEscaped + "</p>");
        sb.AppendLine(" </div>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        writer.Write(sb.ToString());
        writer.Flush();
    }

    /// <summary>
    /// Streams a JSON Patch (RFC 6902) representation of the diff directly to the supplied writer.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown; use <see cref="JsonPatchDiffReportWriter"/> instead.</exception>
    public override void WriteJsonPatch(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("HtmlDiffReportWriter does not support JSON Patch output. Use JsonPatchDiffReportWriter for JSON Patch output.");
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
