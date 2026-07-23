using System;
using System.Linq;
using System.Text.Json;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results as compact JSON summary showing only counts and keys of changes.
/// </summary>
public sealed class SummaryJsonDiffReportWriter : DiffReportWriterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryJsonDiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public SummaryJsonDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
        : base(detector, showSecrets, maskSensitive)
    {
    }

    /// <summary>
    /// Writes a colour‑coded table to the console.
    /// </summary>
    public override void WriteConsole(DiffResult result, bool noColor = false)
    {
        throw new NotSupportedException("SummaryJsonDiffReportWriter only supports summary JSON output. Use ConsoleDiffReportWriter for console output.");
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

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(serialisable, options);
    }

    /// <summary>
    /// Writes a GitHub‑flavored markdown report to the supplied writer.
    /// </summary>
    public override void WriteMarkdown(DiffResult result, System.IO.TextWriter writer)
    {
        throw new NotSupportedException("SummaryJsonDiffReportWriter only supports summary JSON output. Use MarkdownDiffReportWriter for markdown output.");
    }

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// </summary>
    public override void WriteHtml(DiffResult result, System.IO.TextWriter writer)
    {
        throw new NotSupportedException("SummaryJsonDiffReportWriter only supports summary JSON output. Use HtmlDiffReportWriter for HTML output.");
    }

    /// <summary>
    /// Generates a JSON Patch (RFC 6902) representation of the diff.
    /// </summary>
    public override string ToJsonPatch(DiffResult result)
    {
        throw new NotSupportedException("SummaryJsonDiffReportWriter does not support JSON Patch output. Use JsonPatchDiffReportWriter for JSON Patch output.");
    }
}
