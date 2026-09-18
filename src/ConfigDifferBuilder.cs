using System;
using System.Collections.Generic;

namespace AppsettingsDiff;

/// <summary>
/// Fluent builder for configuring and creating <see cref="ConfigDiffer"/> instances.
/// </summary>
public class ConfigDifferBuilder
{
    private SensitiveKeyDetector? _detector;
    private bool _caseSensitiveKeys = true;
    private IEnumerable<string>? _ignorePatterns;
    private IDiffReportWriter? _outputWriter;

    /// <summary>
    /// Configures the builder to use case-sensitive key comparisons.
    /// </summary>
    public ConfigDifferBuilder WithCaseSensitiveKeys(bool caseSensitive = true)
    {
        _caseSensitiveKeys = caseSensitive;
        return this;
    }

    /// <summary>
    /// Configures the builder with patterns for keys to ignore during comparison.
    /// </summary>
    public ConfigDifferBuilder WithIgnorePatterns(IEnumerable<string> patterns)
    {
        _ignorePatterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
        return this;
    }

    /// <summary>
    /// Configures the builder with the sensitive key detector.
    /// </summary>
    public ConfigDifferBuilder WithSensitiveKeyDetector(SensitiveKeyDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        return this;
    }

    /// <summary>
    /// Configures the builder with the output writer for diff reports.
    /// </summary>
    public ConfigDifferBuilder WithOutputWriter(IDiffReportWriter writer)
    {
        _outputWriter = writer ?? throw new ArgumentNullException(nameof(writer));
        return this;
    }

    /// <summary>
    /// Builds and returns a configured <see cref="ConfigDiffer"/> instance.
    /// </summary>
    public ConfigDiffer Build()
    {
        if (_detector == null)
        {
            throw new InvalidOperationException("SensitiveKeyDetector must be configured before building.");
        }

        return new ConfigDiffer(_detector, _caseSensitiveKeys);
    }
}
