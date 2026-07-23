using System;
using System.IO;

namespace AppsettingsDiff;

/// <summary>
/// Common base for <see cref="IDiffReportWriter"/> implementations. Centralizes construction
/// and secret redaction behind a single <see cref="RedactionPolicy"/> instance so that every
/// output format applies the exact same rules to sensitive values, and provides a shared,
/// streaming-first JSON implementation on top of <see cref="DiffEntryJsonWriter"/>.
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
    public virtual string ToJson(DiffResult result, bool indented)
    {
        ArgumentNullException.ThrowIfNull(result);

        using var stringWriter = new StringWriter();
        WriteJson(result, stringWriter, indented);
        return stringWriter.ToString();
    }

    /// <inheritdoc />
    public void WriteJson(DiffResult result, TextWriter writer) => WriteJson(result, writer, indented: true);

    /// <summary>
    /// Streams the diff result as JSON, indented or compact, directly to the supplied writer via a
    /// <see cref="System.Text.Json.Utf8JsonWriter"/>, avoiding the intermediate anonymous-type object
    /// graph the reflection-based serializer would otherwise require.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    public virtual void WriteJson(DiffResult result, TextWriter writer, bool indented)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        DiffEntryJsonWriter.WriteFullResultTo(writer, result, indented, Redact);
    }

    /// <inheritdoc />
    public abstract void WriteMarkdown(DiffResult result, TextWriter writer);

    /// <inheritdoc />
    public abstract void WriteHtml(DiffResult result, TextWriter writer);

    /// <inheritdoc />
    public string ToJsonPatch(DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        using var stringWriter = new StringWriter();
        WriteJsonPatch(result, stringWriter);
        return stringWriter.ToString();
    }

    /// <inheritdoc />
    public abstract void WriteJsonPatch(DiffResult result, TextWriter writer);
}
