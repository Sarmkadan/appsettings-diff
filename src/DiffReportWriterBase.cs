using System;
using System.IO;

namespace AppsettingsDiff;

/// <summary>
/// Common base for <see cref="IDiffReportWriter"/> implementations. Centralizes construction
/// and secret redaction behind a single <see cref="RedactionPolicy"/> instance so that every
/// output format applies the exact same rules to sensitive values.
/// </summary>
public abstract class DiffReportWriterBase : IDiffReportWriter
{
    /// <summary>
    /// Gets the redaction policy shared by all output methods on this writer.
    /// </summary>
    protected RedactionPolicy Policy { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffReportWriterBase"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with *** instead of showing [REDACTED].</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    protected DiffReportWriterBase(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(detector);

        Policy = new RedactionPolicy(detector, showSecrets, maskSensitive);
    }

    /// <summary>
    /// Redacts a value according to the shared <see cref="RedactionPolicy"/>.
    /// </summary>
    /// <param name="value">The raw value to redact.</param>
    /// <param name="isSensitive">Whether the value has been flagged as sensitive.</param>
    /// <returns>The redacted or original value.</returns>
    protected string Redact(string? value, bool isSensitive) => Policy.Redact(value, isSensitive);

    /// <inheritdoc />
    public abstract void WriteConsole(DiffResult result, bool noColor = false);

    /// <inheritdoc />
    public string ToJson(DiffResult result) => ToJson(result, indented: true);

    /// <inheritdoc />
    public abstract string ToJson(DiffResult result, bool indented);

    /// <inheritdoc />
    public abstract void WriteMarkdown(DiffResult result, TextWriter writer);

    /// <inheritdoc />
    public abstract void WriteHtml(DiffResult result, TextWriter writer);

    /// <inheritdoc />
    public abstract string ToJsonPatch(DiffResult result);
}
