using System;

namespace AppsettingsDiff;

/// <summary>
/// Encapsulates the redaction rules applied uniformly by every <see cref="IDiffReportWriter"/>
/// implementation, so that secret handling cannot drift between output formats.
/// </summary>
public sealed class RedactionPolicy
{
    /// <summary>
    /// The token written in place of a sensitive value when <see cref="MaskSensitive"/> is <see langword="false"/>.
    /// </summary>
    public const string RedactedToken = "[REDACTED]";

    /// <summary>
    /// The token written in place of a sensitive value when <see cref="MaskSensitive"/> is <see langword="true"/>.
    /// </summary>
    public const string MaskedToken = "***";

    private readonly SensitiveKeyDetector _detector;

    /// <summary>
    /// Gets a value indicating whether sensitive values should be written verbatim instead of redacted.
    /// </summary>
    public bool ShowSecrets { get; }

    /// <summary>
    /// Gets a value indicating whether redacted values should be masked with <see cref="MaskedToken"/>
    /// instead of <see cref="RedactedToken"/>.
    /// </summary>
    public bool MaskSensitive { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RedactionPolicy"/> class.
    /// </summary>
    /// <param name="detector">Detector used to identify sensitive keys.</param>
    /// <param name="showSecrets">When <see langword="true"/>, sensitive values are written verbatim instead of redacted.</param>
    /// <param name="maskSensitive">When <see langword="true"/>, sensitive values are masked with <see cref="MaskedToken"/> instead of <see cref="RedactedToken"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="detector"/> is <see langword="null"/>.</exception>
    public RedactionPolicy(SensitiveKeyDetector detector, bool showSecrets = false, bool maskSensitive = false)
    {
        ArgumentNullException.ThrowIfNull(detector);

        _detector = detector;
        ShowSecrets = showSecrets;
        MaskSensitive = maskSensitive;
    }

    /// <summary>
    /// Determines whether the given configuration key is considered sensitive by the underlying detector.
    /// </summary>
    /// <param name="key">The configuration key to inspect.</param>
    /// <returns><see langword="true"/> if the key is sensitive; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is <see langword="null"/>.</exception>
    public bool IsSensitiveKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _detector.IsSensitive(key);
    }

    /// <summary>
    /// Applies the redaction policy to a value, returning the redaction token when the value
    /// is sensitive and <see cref="ShowSecrets"/> is <see langword="false"/>, or the original
    /// value (empty string instead of <see langword="null"/>) otherwise.
    /// </summary>
    /// <param name="value">The raw value to redact.</param>
    /// <param name="isSensitive">Whether the value has been flagged as sensitive.</param>
    /// <returns>The redacted or original value.</returns>
    public string Redact(string? value, bool isSensitive) =>
        isSensitive && !ShowSecrets
            ? MaskSensitive ? MaskedToken : RedactedToken
            : value ?? string.Empty;
}
