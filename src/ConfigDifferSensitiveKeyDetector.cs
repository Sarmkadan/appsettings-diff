using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AppsettingsDiff;

/// <summary>
/// Detects sensitive keys in configuration based on wildcard patterns.
/// </summary>
public class SensitiveKeyDetector
{
    private readonly string[] _sensitivePatterns;
    private readonly bool _caseSensitive;

    /// <summary>
    /// Initializes a new instance of <see cref="SensitiveKeyDetector"/> with the default patterns.
    /// </summary>
    public SensitiveKeyDetector() : this(LoadDefaultPatterns(), caseSensitive: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SensitiveKeyDetector"/> with custom patterns.
    /// </summary>
    /// <param name="customPatterns">Custom sensitive patterns to use.</param>
    public SensitiveKeyDetector(IEnumerable<string> customPatterns) : this(customPatterns, caseSensitive: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SensitiveKeyDetector"/> with custom patterns and case sensitivity.
    /// </summary>
    /// <param name="customPatterns">Custom sensitive patterns to use.</param>
    /// <param name="caseSensitive">Whether pattern matching should be case-sensitive.</param>
    public SensitiveKeyDetector(IEnumerable<string> customPatterns, bool caseSensitive)
    {
        _sensitivePatterns = customPatterns?.ToArray() ?? Array.Empty<string>();
        _caseSensitive = caseSensitive;
    }

    /// <summary>
    /// Loads sensitive patterns from a file (one wildcard pattern per line, <c>#</c> comments allowed) and combines them with the defaults.
    /// </summary>
    /// <param name="path">Path to the file containing custom patterns.</param>
    /// <returns>A <see cref="SensitiveKeyDetector"/> configured with the combined patterns.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the file cannot be read.</exception>
    public static SensitiveKeyDetector LoadWithCustomPatterns(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Custom patterns file not found: {path}");

        var patterns = new List<string>(LoadDefaultPatterns());

        try
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                    continue;

                patterns.Add(trimmedLine);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"Failed to read custom patterns file: {ex.Message}", ex);
        }

        return new SensitiveKeyDetector(patterns);
    }

    /// <summary>
    /// Returns the loaded sensitive patterns.
    /// </summary>
    /// <returns>An array of sensitive patterns.</returns>
    internal string[] GetPatterns() => _sensitivePatterns;

    /// <summary>
    /// Determines whether the given configuration key matches any of the known sensitive patterns.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <returns><c>true</c> if the key looks sensitive; otherwise <c>false</c>.</returns>
    public bool IsSensitive(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var comparison = _caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return _sensitivePatterns.Any(pattern =>
            pattern.Contains('*')
                ? KeyPatternMatcher.IsMatch(key, pattern, comparison)
                : key.Contains(pattern, comparison));
    }

    private static string[] LoadDefaultPatterns()
    {
        return [
            "*secret*",
            "*password*",
            "*token*",
            "*key*",
            "*api*",
            "*credential*",
            "*connection*string*",
            "*pwd*",
            "*access*key*"
        ];
    }
}
