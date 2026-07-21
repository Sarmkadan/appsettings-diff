using System;

namespace AppsettingsDiff;

/// <summary>
/// Factory for creating <see cref="IDiffReportWriter"/> instances based on format name.
/// </summary>
public static class DiffReportWriterFactory
{
    /// <summary>
    /// Creates an appropriate <see cref="IDiffReportWriter"/> based on the format name.
    /// </summary>
    /// <param name="format">The output format name (json, markdown, html, jsonpatch, summary-json, or null for console).</param>
    /// <param name="detector">The sensitive key detector.</param>
    /// <param name="showSecrets">Whether to show sensitive values.</param>
    /// <param name="maskSensitive">Whether to mask sensitive values with *** instead of [REDACTED].</param>
    /// <returns>An <see cref="IDiffReportWriter"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public static IDiffReportWriter Create(string? format, SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(detector);

        return format?.ToLowerInvariant() switch
        {
            "json" => new ConsoleDiffReportWriter(detector, showSecrets, maskSensitive),
            "markdown" => new MarkdownDiffReportWriter(detector, showSecrets, maskSensitive),
            "html" => new HtmlDiffReportWriter(detector, showSecrets, maskSensitive),
            "jsonpatch" => new JsonPatchDiffReportWriter(detector, showSecrets, maskSensitive),
            "summary-json" => new SummaryJsonDiffReportWriter(detector, showSecrets, maskSensitive),
            _ => new ConsoleDiffReportWriter(detector, showSecrets, maskSensitive) // default to console
        };
    }
}