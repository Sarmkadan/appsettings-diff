using System;
using System.IO;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results to the console with color coding.
/// </summary>
public sealed class ConsoleDiffReportWriter : DiffReportWriterBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleDiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public ConsoleDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
        : base(detector, showSecrets, maskSensitive)
    {
    }

    /// <summary>
    /// Writes a colour‑coded table to the console.
    /// Added – green
    /// Removed – red
    /// Changed – yellow
    /// TypeChanged – magenta
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    public override void WriteConsole(DiffResult result, bool noColor = false)
    {
        ArgumentNullException.ThrowIfNull(result);

        var separator = new string('-', 80);
        var disableColour = noColor || Console.IsOutputRedirected;
        var originalColour = Console.ForegroundColor;
        var currentColour = originalColour;

        // Header rows are batched into a single StringBuilder/Write instead of four separate
        // Console.WriteLine calls, each of which is its own console round-trip.
        var header = new System.Text.StringBuilder(256)
            .Append("Diff between \"").Append(result.BasePath).Append("\" and \"").Append(result.TargetPath).Append('"').Append('\n')
            .Append(separator).Append('\n')
            .AppendFormat("{0,-15} {1,-40} {2,-15} {3}", "Kind", "Key", "Old Value", "New Value").Append('\n')
            .Append(separator);
        Console.Out.WriteLine(header.ToString());

        var line = new System.Text.StringBuilder(128);

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

            // For TypeChanged entries, show type information
            var displayText = entry.Kind == DiffKind.TypeChanged && entry.OldType != null && entry.NewType != null
                ? $"{entry.Kind} ({entry.OldType}→{entry.NewType}) "
                : entry.Kind.ToString();

            line.Clear();
            line.AppendFormat("{0,-15} {1,-40} {2,-15} {3}",
                displayText,
                Truncate(entry.Key, 40),
                Truncate(oldVal, 15),
                Truncate(newVal, 30));

            // Only touch the console color API when the color actually changes from the
            // previous row - for large diffs most consecutive rows share a kind, so this
            // avoids a redundant syscall-backed color switch per row.
            if (colour != currentColour)
            {
                Console.ForegroundColor = colour;
                currentColour = colour;
            }

            Console.Out.WriteLine(line.ToString());
        }

        if (currentColour != originalColour)
        {
            Console.ForegroundColor = originalColour;
        }

        Console.Out.WriteLine(separator);
    }

    /// <summary>
    /// Writes a GitHub‑flavored markdown report to the supplied writer.
    /// </summary>
    public override void WriteMarkdown(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("ConsoleDiffReportWriter only supports console output. Use MarkdownDiffReportWriter for markdown output.");
    }

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// </summary>
    public override void WriteHtml(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("ConsoleDiffReportWriter only supports console output. Use HtmlDiffReportWriter for HTML output.");
    }

    /// <summary>
    /// Streams a JSON Patch (RFC 6902) representation of the diff directly to the supplied writer.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown; use <see cref="JsonPatchDiffReportWriter"/> instead.</exception>
    public override void WriteJsonPatch(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("ConsoleDiffReportWriter does not support JSON Patch output. Use JsonPatchDiffReportWriter for JSON Patch output.");
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }
}
