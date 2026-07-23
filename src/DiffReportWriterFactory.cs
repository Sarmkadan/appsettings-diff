using System;

namespace AppsettingsDiff;

/// <summary>
/// Factory for creating <see cref="IDiffReportWriter"/> instances based on format name.
///
/// <para>This factory delegates to <see cref="DiffReportWriterRegistry"/> for format resolution,
/// making it easy to extend with new writers by registering them in the registry.</para>
/// </summary>
public static class DiffReportWriterFactory
{
    /// <summary>
    /// Creates an appropriate <see cref="IDiffReportWriter"/> based on the format name.
    /// </summary>
    /// <param name="format">The output format name (console, json, markdown, html, jsonpatch, summary-json, or null for console).</param>
    /// <param name="detector">The sensitive key detector.</param>
    /// <param name="showSecrets">Whether to show sensitive values.</param>
    /// <param name="maskSensitive">Whether to mask sensitive values with *** instead of [REDACTED].</param>
    /// <returns>An <see cref="IDiffReportWriter"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public static IDiffReportWriter Create(string? format, SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
    {
        return DiffReportWriterRegistry.Create(format, detector, showSecrets, maskSensitive);
    }
}