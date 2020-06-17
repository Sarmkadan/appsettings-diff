using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppsettingsDiff;

/// <summary>
/// Extension methods for <see cref="DiffReportWriter"/> that provide additional
/// convenience methods for working with diff results.
/// </summary>
/// <remarks>
/// These extension methods delegate to the underlying <see cref="DiffReportWriter"/> instance
/// while providing a more fluent API surface for common operations.
/// </remarks>
public static class DiffReportWriterExtensions
{
    /// <summary>
    /// Writes a summary of differences to the console with counts by type.
    /// Includes a summary header showing total changes and breakdown by type.
    /// </summary>
    /// <param name="writer">The <see cref="DiffReportWriter"/> instance.</param>
    /// <param name="result">The diff result to write.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/> or <paramref name="result"/> is <see langword="null"/>.</exception>
    public static void WriteConsoleSummary(this DiffReportWriter writer, DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(result);

        Console.WriteLine($"Configuration Diff Summary");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"Base: {result.BasePath}");
        Console.WriteLine($"Target: {result.TargetPath}");
        Console.WriteLine($"Total Changes: {result.Entries.Count}");
        Console.WriteLine($" Added: {result.CountOf(DiffKind.Added)}");
        Console.WriteLine($" Removed: {result.CountOf(DiffKind.Removed)}");
        Console.WriteLine($" Changed: {result.CountOf(DiffKind.Changed)}");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine();

        if (result.HasDifferences)
        {
            writer.WriteConsole(result);
        }
        else
        {
            Console.WriteLine("No differences found.");
        }
    }

    /// <summary>
    /// Serializes the diff result to a compact JSON string (single line).
    /// Useful for logging or embedding in other JSON structures.
    /// Sensitive values are redacted unless <paramref name="showSecrets"/> is true.
    /// </summary>
    /// <param name="writer">The <see cref="DiffReportWriter"/> instance.</param>
    /// <param name="result">The diff result to serialize.</param>
    /// <param name="showSecrets">Whether to include sensitive values in the output.</param>
    /// <returns>A compact JSON representation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/> or <paramref name="result"/> is <see langword="null"/>.</exception>
    public static string ToCompactJson(this DiffReportWriter writer, DiffResult result, bool showSecrets = false)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(result);

        var serialisable = new
        {
            result.BasePath,
            result.TargetPath,
            Entries = result.Entries.Select(e => new
            {
                Kind = e.Kind.ToString(),
                e.Key,
                OldValue = Redact(writer, e.OldValue, e.IsSensitive, showSecrets),
                NewValue = Redact(writer, e.NewValue, e.IsSensitive, showSecrets),
                e.Path,
                e.IsSensitive
            })
        };

        var options = new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false
        };

        return System.Text.Json.JsonSerializer.Serialize(serialisable, options);
    }

    /// <summary>
    /// Generates a markdown summary table showing only the counts and statistics.
    /// Useful for brief overview in documentation or README files.
    /// </summary>
    /// <param name="writer">The <see cref="DiffReportWriter"/> instance.</param>
    /// <param name="result">The diff result to format.</param>
    /// <returns>A markdown formatted summary.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/> or <paramref name="result"/> is <see langword="null"/>.</exception>
    public static string ToMarkdownSummary(this DiffReportWriter writer, DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(result);

        var sb = new StringBuilder();

        sb.AppendLine($"### Configuration Diff Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Count |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| **Total Changes** | {result.Entries.Count} |");
        sb.AppendLine($"| **Added** | {result.CountOf(DiffKind.Added)} |");
        sb.AppendLine($"| **Removed** | {result.CountOf(DiffKind.Removed)} |");
        sb.AppendLine($"| **Modified** | {result.CountOf(DiffKind.Changed)} |");
        sb.AppendLine();
        sb.AppendLine($"**Base Path:** `{result.BasePath}`");
        sb.AppendLine($"**Target Path:** `{result.TargetPath}`");

        if (result.HasDifferences)
        {
            sb.AppendLine();
            sb.AppendLine("### Detailed Changes");
            sb.AppendLine();
            sb.AppendLine("| Kind | Key | Old Value | New Value |");
            sb.AppendLine("|------|-----|-----------|-----------|");

            foreach (var entry in result.Entries)
            {
                var oldVal = EscapeMarkdown(Redact(writer, entry.OldValue, entry.IsSensitive, false));
                var newVal = EscapeMarkdown(Redact(writer, entry.NewValue, entry.IsSensitive, false));

                sb.AppendLine($"| {entry.Kind} | `{EscapeMarkdown(entry.Key)}` | {oldVal} | {newVal} |");
            }
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("*No differences found.*");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks if there are any differences in the result.
    /// </summary>
    /// <param name="writer">The <see cref="DiffReportWriter"/> instance.</param>
    /// <param name="result">The diff result to check.</param>
    /// <returns>True if there are differences, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/> or <paramref name="result"/> is <see langword="null"/>.</exception>
    public static bool HasDifferences(this DiffReportWriter writer, DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(result);

        return result.HasDifferences;
    }

    /// <summary>
    /// Gets the total number of differences.
    /// </summary>
    /// <param name="writer">The <see cref="DiffReportWriter"/> instance.</param>
    /// <param name="result">The diff result to count.</param>
    /// <returns>The total count of differences.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="writer"/> or <paramref name="result"/> is <see langword="null"/>.</exception>
    public static int GetTotalDifferenceCount(this DiffReportWriter writer, DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(result);

        return result.Entries.Count;
    }

    private static string Redact(DiffReportWriter writer, string? value, bool isSensitive, bool showSecrets)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (isSensitive && !showSecrets)
        {
            return "[REDACTED]";
        }

        return value ?? string.Empty;
    }

    private static string EscapeMarkdown(string text)
    {
        if (text == null)
        {
            return string.Empty;
        }

        return text
            .Replace("|", "\\|")
            .Replace("`", "\\`")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }
}