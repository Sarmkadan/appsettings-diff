using System;
using System.IO;
using System.Linq;

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
    /// Serialises the diff result to JSON, indented or compact.
    /// </summary>
    public override string ToJson(DiffResult result, bool indented)
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
    public override void WriteHtml(DiffResult result, TextWriter writer)
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
    }

    /// <summary>
    /// Generates a JSON Patch (RFC 6902) representation of the diff.
    /// </summary>
    public override string ToJsonPatch(DiffResult result)
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
