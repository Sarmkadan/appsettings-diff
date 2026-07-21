using System;
using System.IO;

namespace AppsettingsDiff;

/// <summary>
/// Writes diff results to the console with color coding.
/// </summary>
public sealed class ConsoleDiffReportWriter : IDiffReportWriter
{
    private readonly SensitiveKeyDetector _detector;
    private readonly bool _showSecrets;
    private readonly bool _maskSensitive;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleDiffReportWriter"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public ConsoleDiffReportWriter(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
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
    public string ToJson(DiffResult result) => ToJson(result, indented: true);

    /// <summary>
    /// Serialises the diff result to JSON, indented or compact.
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
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
    /// </summary>
    public void WriteMarkdown(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("ConsoleDiffReportWriter only supports console output. Use MarkdownDiffReportWriter for markdown output.");
    }

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// </summary>
    public void WriteHtml(DiffResult result, TextWriter writer)
    {
        throw new NotSupportedException("ConsoleDiffReportWriter only supports console output. Use HtmlDiffReportWriter for HTML output.");
    }

    /// <summary>
    /// Generates a JSON Patch (RFC 6902) representation of the diff.
    /// </summary>
    public string ToJsonPatch(DiffResult result)
    {
        throw new NotSupportedException("ConsoleDiffReportWriter does not support JSON Patch output. Use JsonPatchDiffReportWriter for JSON Patch output.");
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
}