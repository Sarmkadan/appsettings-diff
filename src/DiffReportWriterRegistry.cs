using System;
using System.Collections.Generic;

namespace AppsettingsDiff;

/// <summary>
/// Registry for mapping format names to <see cref="IDiffReportWriter"/> factory functions.
/// This allows new writers to register themselves without modifying switch statements.
/// </summary>
public static class DiffReportWriterRegistry
{
    /// <summary>
    /// Delegate for creating a diff report writer instance.
    /// </summary>
    /// <param name="detector">The sensitive key detector.</param>
    /// <param name="showSecrets">Whether to show sensitive values.</param>
    /// <param name="maskSensitive">Whether to mask sensitive values with *** instead of [REDACTED].</param>
    /// <returns>An <see cref="IDiffReportWriter"/> instance.</returns>
    public delegate IDiffReportWriter WriterFactory(SensitiveKeyDetector detector, bool showSecrets, bool maskSensitive);

    /// <summary>
    /// Delegate for writing a diff result in a specific format.
    /// </summary>
    /// <param name="writer">The writer instance.</param>
    /// <param name="result">The diff result to write.</param>
    /// <param name="schemaViolations">List of schema violations to include.</param>
    /// <param name="noColor">Whether to disable ANSI color output.</param>
    public delegate void FormatWriter(IDiffReportWriter writer, DiffResult result, List<SchemaViolation> schemaViolations, bool noColor);

    private static readonly Dictionary<string, WriterFactory> _registry = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, FormatWriter> _formatWriters = new(StringComparer.OrdinalIgnoreCase);

    static DiffReportWriterRegistry()
    {
        // Register built-in writers
        Register("console", (detector, showSecrets, maskSensitive) => new ConsoleDiffReportWriter(detector, showSecrets, maskSensitive));
        Register("json", (detector, showSecrets, maskSensitive) => new ConsoleDiffReportWriter(detector, showSecrets, maskSensitive));
        Register("markdown", (detector, showSecrets, maskSensitive) => new MarkdownDiffReportWriter(detector, showSecrets, maskSensitive));
        Register("html", (detector, showSecrets, maskSensitive) => new HtmlDiffReportWriter(detector, showSecrets, maskSensitive));
        Register("jsonpatch", (detector, showSecrets, maskSensitive) => new JsonPatchDiffReportWriter(detector, showSecrets, maskSensitive));
        Register("summary-json", (detector, showSecrets, maskSensitive) => new SummaryJsonDiffReportWriter(detector, showSecrets, maskSensitive));

        // Register format writers
        RegisterFormat("console", (writer, result, schemaViolations, noColor) =>
        {
            writer.WriteConsole(result, noColor);
        });
        RegisterFormat("json", (writer, result, schemaViolations, noColor) =>
        {
            Console.WriteLine(writer.ToJson(result));
        });
        RegisterFormat("markdown", (writer, result, schemaViolations, noColor) =>
        {
            writer.WriteMarkdown(result, Console.Out);
        });
        RegisterFormat("html", (writer, result, schemaViolations, noColor) =>
        {
            writer.WriteHtml(result, Console.Out);
        });
        RegisterFormat("jsonpatch", (writer, result, schemaViolations, noColor) =>
        {
            Console.WriteLine(writer.ToJsonPatch(result));
        });
        RegisterFormat("summary-json", (writer, result, schemaViolations, noColor) =>
        {
            var summary = new
            {
                added = result.Entries
                    .Where(e => e.Kind == DiffKind.Added)
                    .Select(e => e.Key)
                    .ToArray(),
                removed = result.Entries
                    .Where(e => e.Kind == DiffKind.Removed)
                    .Select(e => e.Key)
                    .ToArray(),
                changed = result.Entries
                    .Where(e => e.Kind == DiffKind.Changed)
                    .Select(e => e.Key)
                    .ToArray()
            };
            var json = System.Text.Json.JsonSerializer.Serialize(summary);
            Console.WriteLine(json);
        });
    }

    /// <summary>
    /// Registers a new format with the registry.
    /// </summary>
    /// <param name="formatName">The format name to register (e.g., "console", "json", "markdown").</param>
    /// <param name="factory">The factory function that creates the writer instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="formatName"/> or <paramref name="factory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="formatName"/> is empty or whitespace.</exception>
    public static void Register(string formatName, WriterFactory factory)
    {
        ArgumentNullException.ThrowIfNull(formatName);
        ArgumentNullException.ThrowIfNull(factory);

        if (string.IsNullOrWhiteSpace(formatName))
            throw new ArgumentException("Format name cannot be empty or whitespace.", nameof(formatName));

        _registry[formatName] = factory;
    }

    /// <summary>
    /// Gets all registered format names.
    /// </summary>
    /// <returns>An enumerable of registered format names.</returns>
    public static IEnumerable<string> GetRegisteredFormats()
    {
        return _registry.Keys;
    }

    /// <summary>
    /// Creates a diff report writer for the specified format.
    /// </summary>
    /// <param name="format">The output format name (console, json, markdown, html, jsonpatch, summary-json, or null for console).</param>
    /// <param name="detector">The sensitive key detector.</param>
    /// <param name="showSecrets">Whether to show sensitive values.</param>
    /// <param name="maskSensitive">Whether to mask sensitive values with *** instead of [REDACTED].</param>
    /// <returns>An <see cref="IDiffReportWriter"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public static IDiffReportWriter Create(string? format, SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(detector);

        // Default to console if format is null or empty
        var formatName = string.IsNullOrWhiteSpace(format) ? "console" : format;

        if (_registry.TryGetValue(formatName, out var factory))
        {
            return factory(detector, showSecrets, maskSensitive);
        }

        // Return console writer as default for unknown formats
        return new ConsoleDiffReportWriter(detector, showSecrets, maskSensitive);
    }

    /// <summary>
    /// Registers a new format writer with the registry.
    /// </summary>
    /// <param name="formatName">The format name to register (e.g., "console", "json", "markdown").</param>
    /// <param name="writer">The format writer function that handles output.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="formatName"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="formatName"/> is empty or whitespace.</exception>
    public static void RegisterFormat(string formatName, FormatWriter writer)
    {
        ArgumentNullException.ThrowIfNull(formatName);
        ArgumentNullException.ThrowIfNull(writer);

        if (string.IsNullOrWhiteSpace(formatName))
            throw new ArgumentException("Format name cannot be empty or whitespace.", nameof(formatName));

        _formatWriters[formatName] = writer;
    }

    /// <summary>
    /// Gets the description for a format.
    /// </summary>
    /// <param name="format">The format name.</param>
    /// <returns>A description of what the format does, or null if format is not found.</returns>
    public static string? GetFormatDescription(string format)
    {
        return format?.ToLowerInvariant() switch
        {
            "console" => "Colored console output (default)",
            "json" => "JSON serialization of the full diff result",
            "markdown" => "GitHub-flavored markdown report",
            "html" => "Self-contained HTML report with inline CSS",
            "jsonpatch" => "JSON Patch (RFC 6902) representation of changes",
            "summary-json" => "Compact JSON summary showing only change counts and keys",
            _ => null
        };
    }

    /// <summary>
    /// Gets the format writer for the specified format.
    /// </summary>
    /// <param name="format">The output format name.</param>
    /// <returns>A format writer function, or null if format is not found.</returns>
    public static FormatWriter? GetFormatWriter(string? format)
    {
        // Default to console if format is null or empty
        var formatName = string.IsNullOrWhiteSpace(format) ? "console" : format;

        if (_formatWriters.TryGetValue(formatName, out var writer))
        {
            return writer;
        }

        // Return console writer as default for unknown formats
        return _formatWriters.TryGetValue("console", out var defaultWriter) ? defaultWriter : null;
    }
}