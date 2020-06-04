using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppsettingsDiff;

/// <summary>
/// Extension methods for <see cref="DiffReportWriter"/> that provide additional
/// convenience methods for working with diff results.
/// </summary>
public static class DiffReportWriterExtensions
{
    /// <summary>
    /// Writes a summary of differences to the console with counts by type.
    /// Includes a summary header showing total changes and breakdown by type.
    /// </summary>
    /// <param name="writer">The DiffReportWriter instance</param>
    /// <param name="result">The diff result to write</param>
    public static void WriteConsoleSummary(this DiffReportWriter writer, DiffResult result)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        Console.WriteLine($"Configuration Diff Summary");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"Base: {result.BasePath}");
        Console.WriteLine($"Target: {result.TargetPath}");
        Console.WriteLine($"Total Changes: {result.Entries.Count}");
        Console.WriteLine($"  Added: {result.CountOf(DiffKind.Added)}");
        Console.WriteLine($"  Removed: {result.CountOf(DiffKind.Removed)}");
        Console.WriteLine($"  Changed: {result.CountOf(DiffKind.Changed)}");
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
    /// Sensitive values are redacted unless <c>showSecrets</c> is true.
    /// </summary>
    /// <param name="writer">The DiffReportWriter instance</param>
    /// <param name="result">The diff result to serialize</param>
    /// <returns>A compact JSON representation</returns>
    public static string ToCompactJson(this DiffReportWriter writer, DiffResult result)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        var serialisable = new
        {
            result.BasePath,
            result.TargetPath,
            Entries = result.Entries.Select(e => new
            {
                Kind = e.Kind.ToString(),
                e.Key,
                OldValue = writer.Redact(e.OldValue, e.IsSensitive),
                NewValue = writer.Redact(e.NewValue, e.IsSensitive),
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
    /// <param name="writer">The DiffReportWriter instance</param>
    /// <param name="result">The diff result to format</param>
    /// <returns>A markdown formatted summary</returns>
    public static string ToMarkdownSummary(this DiffReportWriter writer, DiffResult result)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        var sb = new StringBuilder();

        sb.AppendLine($"### Configuration Diff Summary");
        sb.AppendLine();
        sb.AppendLine($"| Metric | Count ||");
        sb.AppendLine($"|--------|-------||");
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
                var oldVal = EscapeMarkdown(writer.Redact(entry.OldValue, entry.IsSensitive));
                var newVal = EscapeMarkdown(writer.Redact(entry.NewValue, entry.IsSensitive));

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
    /// <param name="result">The diff result to check</param>
    /// <returns>True if there are differences, false otherwise</returns>
    public static bool HasDifferences(this DiffReportWriter writer, DiffResult result)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        return result.HasDifferences;
    }

    /// <summary>
    /// Gets the total number of differences.
    /// </summary>
    /// <param name="result">The diff result to count</param>
    /// <returns>The total count of differences</returns>
    public static int GetTotalDifferenceCount(this DiffReportWriter writer, DiffResult result)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        return result.Entries.Count;
    }

    private static string Redact(this DiffReportWriter writer, string? value, bool isSensitive)
    {
        if (writer == null)
            throw new ArgumentNullException(nameof(writer));

        if (isSensitive && !(writer as dynamic)._showSecrets)
            return "[REDACTED]";

        return value ?? string.Empty;
    }

    private static string EscapeMarkdown(string text)
    {
        if (text == null)
            return string.Empty;

        return text
            .Replace("|", "\\|")
            .Replace("`", "\\`")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }
}