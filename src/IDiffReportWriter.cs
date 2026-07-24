using System;
using System.IO;

namespace AppsettingsDiff;

/// <summary>
/// Interface for writing diff results to various output formats.
/// </summary>
public interface IDiffReportWriter
{
    /// <summary>
    /// Writes a diff result to the console.
    /// </summary>
    void WriteConsole(DiffResult result, bool noColor = false);

    /// <summary>
    /// Serializes the diff result to indented JSON.
    /// </summary>
    string ToJson(DiffResult result);

    /// <summary>
    /// Serializes the diff result to JSON, indented or compact.
    /// </summary>
    string ToJson(DiffResult result, bool indented);

    /// <summary>
    /// Streams the diff result as indented JSON directly to the supplied writer, avoiding the
    /// intermediate full-string allocation that <see cref="ToJson(DiffResult)"/> requires.
    /// </summary>
    void WriteJson(DiffResult result, TextWriter writer);

    /// <summary>
    /// Streams the diff result as JSON, indented or compact, directly to the supplied writer,
    /// avoiding the intermediate full-string allocation that <see cref="ToJson(DiffResult, bool)"/> requires.
    /// </summary>
    void WriteJson(DiffResult result, TextWriter writer, bool indented);

    /// <summary>
    /// Writes a GitHub-flavored markdown report to the supplied writer.
    /// </summary>
    void WriteMarkdown(DiffResult result, TextWriter writer);

    /// <summary>
    /// Writes a self-contained HTML report to the supplied writer.
    /// </summary>
    void WriteHtml(DiffResult result, TextWriter writer);

    /// <summary>
    /// Generates a JSON Patch (RFC 6902) representation of the diff.
    /// </summary>
    string ToJsonPatch(DiffResult result);

    /// <summary>
    /// Streams a JSON Patch (RFC 6902) representation of the diff directly to the supplied writer,
    /// avoiding the intermediate full-string allocation that <see cref="ToJsonPatch(DiffResult)"/> requires.
    /// </summary>
    void WriteJsonPatch(DiffResult result, TextWriter writer);
}