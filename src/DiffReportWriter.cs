using System;
using System.Collections.Generic;
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
    /// Generates a markdown table suitable for PR comments.
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    public string ToMarkdown(DiffResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));

        var sb = new StringBuilder();

        sb.AppendLine($"**Diff between `{result.BasePath}` and `{result.TargetPath}`**");
        sb.AppendLine();
        sb.AppendLine("| Kind | Key | Old Value | New Value |");
        sb.AppendLine("|------|-----|-----------|-----------|");

        foreach (var entry in result.Entries)
        {
            var oldVal = EscapeMarkdown(Redact(entry.OldValue, entry.IsSensitive));
            var newVal = EscapeMarkdown(Redact(entry.NewValue, entry.IsSensitive));

            sb.AppendLine($"| {entry.Kind} | `{EscapeMarkdown(entry.Key)}` | {oldVal} | {newVal} |");
        }

        return sb.ToString();
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
}
