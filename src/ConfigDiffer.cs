using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AppsettingsDiff;

/// <summary>
/// Options for configuring the diff operation
/// </summary>
public class ConfigDifferOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to compare arrays by value-set (unordered) instead of by index.
    /// When true, arrays are compared as sets - order doesn't matter.
    /// </summary>
    public bool UnorderedArrays { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth to compare nested structures.
    /// When null or 0, no depth limit is applied.
    /// When greater than 0, subtrees deeper than this level are compared as opaque blobs.
    /// </summary>
    public int? MaxDepth { get; set; }
}

/// <summary>
/// Detects sensitive keys in configuration
/// </summary>
public class SensitiveKeyDetector
{
    private readonly string[] _sensitivePatterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="SensitiveKeyDetector"/> class with default patterns.
    /// </summary>
    public SensitiveKeyDetector() : this(LoadDefaultPatterns())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SensitiveKeyDetector"/> class with custom patterns.
    /// </summary>
    /// <param name="customPatterns">Custom sensitive patterns to use.</param>
    public SensitiveKeyDetector(IEnumerable<string> customPatterns)
    {
        _sensitivePatterns = customPatterns?.ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Loads default sensitive patterns.
    /// </summary>
    /// <returns>Array of default sensitive patterns.</returns>
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

    /// <summary>
    /// Loads sensitive patterns from a file (one wildcard pattern per line, # comments allowed).
    /// </summary>
    /// <param name="path">Path to the file containing custom patterns.</param>
    /// <returns>Array of patterns loaded from file, combined with default patterns.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    public static SensitiveKeyDetector LoadWithCustomPatterns(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Custom patterns file not found: {path}");

        var patterns = new List<string>(LoadDefaultPatterns());

        try
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
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
    /// Determines whether the given configuration key matches any of the known sensitive patterns.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <returns><see langword="true"/> if the key looks sensitive; otherwise <see langword="false"/>.</returns>
    public bool IsSensitive(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return _sensitivePatterns.Any(pattern =>
            pattern.Contains('*')
                ? KeyPatternMatcher.IsMatch(key, pattern)
                : key.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Case-insensitive wildcard matching for configuration key patterns,
/// where <c>*</c> matches any (possibly empty) sequence of characters.
/// </summary>
internal static class KeyPatternMatcher
{
    public static bool IsMatch(string text, string pattern)
    {
        if (!pattern.Contains('*'))
            return text.Equals(pattern, StringComparison.OrdinalIgnoreCase);

        var segments = pattern.Split('*');
        bool anchoredStart = segments[0].Length > 0;
        bool anchoredEnd = segments[^1].Length > 0;

        int position = 0;
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.Length == 0)
                continue;

            if (i == 0 && anchoredStart)
            {
                if (!text.StartsWith(segment, StringComparison.OrdinalIgnoreCase))
                    return false;

                position = segment.Length;
            }
            else if (i == segments.Length - 1 && anchoredEnd)
            {
                // The final segment must sit flush against the end of the text.
                int endIndex = text.Length - segment.Length;
                if (endIndex < position || !text.EndsWith(segment, StringComparison.OrdinalIgnoreCase))
                    return false;

                position = text.Length;
            }
            else
            {
                int index = text.IndexOf(segment, position, StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    return false;

                position = index + segment.Length;
            }
        }

        return true;
    }
}

/// <summary>
/// Represents a flat configuration with key-value pairs
/// </summary>
public class FlatConfig
{
    /// <summary>Gets the flattened key-value pairs of the configuration.</summary>
    public Dictionary<string, string> Values { get; } = [];

    /// <summary>Gets the value for <paramref name="key"/>, or an empty string when the key is absent.</summary>
    /// <param name="key">The configuration key to look up.</param>
    public string GetValue(string key) => Values.TryGetValue(key, out var value) ? value : string.Empty;

    /// <summary>Determines whether the configuration contains <paramref name="key"/>.</summary>
    /// <param name="key">The configuration key to check.</param>
    public bool ContainsKey(string key) => Values.ContainsKey(key);
}

/// <summary>
/// Represents the type of difference
/// </summary>
public enum DiffKind
{
    /// <summary>The key exists in the target but not in the baseline.</summary>
    Added,

    /// <summary>The key exists in the baseline but not in the target.</summary>
    Removed,

    /// <summary>The key exists in both configurations with different values.</summary>
    Changed,

    /// <summary>The key exists in both configurations but the value types differ.</summary>
    TypeChanged
}

/// <summary>
/// Represents a single difference entry
/// </summary>
public class DiffEntry
{
    /// <summary>Gets the type of the difference.</summary>
    public required DiffKind Kind { get; init; }

    /// <summary>Gets the configuration key the difference applies to.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the baseline value, or <see langword="null"/> for added keys.</summary>
    public string? OldValue { get; init; }

    /// <summary>Gets the target value, or <see langword="null"/> for removed keys.</summary>
    public string? NewValue { get; init; }

    /// <summary>Gets a value indicating whether the key is considered sensitive.</summary>
    public bool IsSensitive { get; init; }

    /// <summary>Gets the optional source path associated with the entry.</summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets the type of the baseline value (e.g., "string", "number", "boolean", "object", "null").
    /// Only relevant for TypeChanged differences.
    /// </summary>
    public string? OldType { get; init; }

    /// <summary>
    /// Gets the type of the target value (e.g., "string", "number", "boolean", "object", "null").
    /// Only relevant for TypeChanged differences.
    /// </summary>
    public string? NewType { get; init; }
}

/// <summary>
/// Result of a diff operation
/// </summary>
public class DiffResult
{
    /// <summary>Gets the individual difference entries.</summary>
    public List<DiffEntry> Entries { get; } = [];

    /// <summary>Gets the identifier of the baseline configuration.</summary>
    public string BasePath { get; init; } = string.Empty;

    /// <summary>Gets the identifier of the target configuration.</summary>
    public string TargetPath { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether any differences were found.</summary>
    public bool HasDifferences => Entries.Count > 0;

    /// <summary>
    /// Counts the entries of the specified <paramref name="kind"/>.
    /// </summary>
    /// <param name="kind">The kind of difference to count.</param>
    public int CountOf(DiffKind kind) => Entries.Count(e => e.Kind == kind);

    /// <summary>
    /// Gets the number of keys that were ignored due to the ignore patterns.
    /// </summary>
    public int IgnoredCount { get; set; }
}

/// <summary>
/// Main diffing class that compares two configurations
/// </summary>
public class ConfigDiffer
{
    private readonly SensitiveKeyDetector _detector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigDiffer"/> class.
    /// </summary>
    /// <param name="detector">Detector used to flag sensitive keys in the produced entries.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is <see langword="null"/>.</exception>
    public ConfigDiffer(SensitiveKeyDetector detector)
    {
        ArgumentNullException.ThrowIfNull(detector);
        _detector = detector;
    }

    /// <summary>
    /// Compares two flat configurations and reports added, removed and changed keys.
    /// </summary>
    /// <param name="baseline">The baseline configuration.</param>
    /// <param name="target">The target configuration.</param>
    /// <param name="ignoreKeys">Optional key patterns to skip; supports <c>*</c> wildcards, otherwise matched as a case-insensitive substring.</param>
    /// <param name="basePath">Optional identifier for the baseline (e.g. a file path) recorded in the result.</param>
    /// <param name="targetPath">Optional identifier for the target (e.g. a file path) recorded in the result.</param>
    /// <param name="options">Optional configuration options for the diff operation.</param>
    /// <returns>A <see cref="DiffResult"/> describing the differences.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseline"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    public DiffResult Diff(
        FlatConfig baseline,
        FlatConfig target,
        IEnumerable<string>? ignoreKeys = null,
        string? basePath = null,
        string? targetPath = null,
        ConfigDifferOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(target);

        options ??= new ConfigDifferOptions();
        int? maxDepth = options.MaxDepth;

        var result = new DiffResult
        {
            BasePath = basePath ?? "baseline",
            TargetPath = targetPath ?? "target"
        };

        var ignoreSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ignoreKeys != null)
        {
            foreach (var pattern in ignoreKeys)
            {
                ignoreSet.Add(pattern);
            }
        }

        int ignoredCount = 0;

        // Check for removed keys (in baseline but not in target)
        foreach (var kvp in baseline.Values)
        {
            string key = kvp.Key;
            if (ShouldIgnore(key, ignoreSet))
            {
                ignoredCount++;
                continue;
            }

            if (!target.ContainsKey(key))
            {
                result.Entries.Add(new DiffEntry
                {
                    Kind = DiffKind.Removed,
                    Key = key,
                    OldValue = kvp.Value,
                    IsSensitive = _detector.IsSensitive(key)
                });
            }
            else if (ExceedsMaxDepth(key, maxDepth))
            {
                // For keys exceeding max depth, compare as opaque blobs
                // Only report a difference if the entire subtree changed
                if (!AreValuesEqualAsBlobs(key, kvp.Value, target.GetValue(key), maxDepth))
                {
                    result.Entries.Add(new DiffEntry
                    {
                        Kind = DiffKind.Changed,
                        Key = key,
                        OldValue = kvp.Value,
                        NewValue = target.GetValue(key),
                        IsSensitive = _detector.IsSensitive(key)
                    });
                }
            }
            else if (HasDifferentTypes(kvp.Value, target.GetValue(key)))
            {
                // Type changed - use TypeChanged kind
                result.Entries.Add(new DiffEntry
                {
                    Kind = DiffKind.TypeChanged,
                    Key = key,
                    OldValue = kvp.Value,
                    NewValue = target.GetValue(key),
                    OldType = DetectJsonType(kvp.Value),
                    NewType = DetectJsonType(target.GetValue(key)),
                    IsSensitive = _detector.IsSensitive(key)
                });
            }
            else if (!AreValuesEqual(kvp.Value, target.GetValue(key), options))
            {
                // Value changed but types are the same
                result.Entries.Add(new DiffEntry
                {
                    Kind = DiffKind.Changed,
                    Key = key,
                    OldValue = kvp.Value,
                    NewValue = target.GetValue(key),
                    IsSensitive = _detector.IsSensitive(key)
                });
            }
        }

        // Check for added keys (in target but not in baseline)
        foreach (var kvp in target.Values)
        {
            string key = kvp.Key;
            if (ShouldIgnore(key, ignoreSet))
            {
                ignoredCount++;
                continue;
            }

            if (!baseline.ContainsKey(key))
            {
                result.Entries.Add(new DiffEntry
                {
                    Kind = DiffKind.Added,
                    Key = key,
                    NewValue = kvp.Value,
                    IsSensitive = _detector.IsSensitive(key)
                });
            }
            // Note: Keys exceeding max depth that exist in both baseline and target are handled
            // in the removed keys loop above, so we don't need to handle them here
        }

        result.IgnoredCount = ignoredCount;
        return result;
    }

    /// <summary>
    /// Determines if a key path exceeds the maximum depth.
    /// </summary>
    /// <param name="key">The configuration key path.</param>
    /// <param name="maxDepth">The maximum allowed depth (null or 0 means no limit).</param>
    /// <returns>True if the key path exceeds the maximum depth; otherwise false.</returns>
    private bool ExceedsMaxDepth(string key, int? maxDepth)
    {
        if (maxDepth == null || maxDepth <= 0)
            return false;

        // Count the number of colons in the key path
        // Each colon represents a level of nesting (e.g., "Section:Subsection:Key" has depth 2)
        int depth = key.Split(':').Length - 1;
        return depth >= maxDepth;
    }

    /// <summary>
    /// Compares two configuration values as opaque blobs when they exceed max depth.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value1">The baseline value.</param>
    /// <param name="value2">The target value.</param>
    /// <param name="maxDepth">The maximum allowed depth.</param>
    /// <returns>True if the values are equal as opaque blobs; otherwise false.</returns>
    private bool AreValuesEqualAsBlobs(string key, string? value1, string? value2, int? maxDepth)
    {
        // If either value is null, use standard comparison
        if (value1 == null || value2 == null)
            return value1 == value2;

        // If max depth is not set or we're within the limit, use standard comparison
        if (maxDepth == null || maxDepth <= 0)
            return value1 == value2;

        // If we exceed max depth, compare as opaque blobs
        // Only report a difference if the entire subtree changed
        return value1 == value2;
    }

    private static bool ShouldIgnore(string key, HashSet<string> ignoreSet)
    {
        if (ignoreSet.Count == 0)
            return false;

        return ignoreSet.Any(pattern =>
            pattern.Contains('*')
                ? KeyPatternMatcher.IsMatch(key, pattern)
                : key.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Compares two configuration values for equality, with special handling for arrays when unordered comparison is enabled.
    /// </summary>
    /// <param name="value1">The first value to compare.</param>
    /// <param name="value2">The second value to compare.</param>
    /// <param name="options">The diff options that may enable unordered array comparison.</param>
    /// <returns>True if the values are equal; otherwise false.</returns>
    private bool AreValuesEqual(string? value1, string? value2, ConfigDifferOptions options)
    {
        // If either value is null, use standard comparison
        if (value1 == null || value2 == null)
            return value1 == value2;

        // If unordered array comparison is not enabled, use standard comparison
        if (!options.UnorderedArrays)
            return value1 == value2;

        // Check if both values represent arrays (contain array index notation like [0], [1], etc.)
        if (IsArrayValue(value1) && IsArrayValue(value2))
        {
            // Extract array keys (e.g., "MyArray[0]" -> "MyArray")
            var arrayKey1 = ExtractArrayKey(value1);
            var arrayKey2 = ExtractArrayKey(value2);

            // If they're not the same array, use standard comparison
            if (!string.Equals(arrayKey1, arrayKey2, StringComparison.OrdinalIgnoreCase))
                return value1 == value2;

            // Extract all values for each array
            var values1 = ExtractArrayValues(value1);
            var values2 = ExtractArrayValues(value2);

            // Compare as sets (unordered)
            return values1.SetEquals(values2);
        }

        // Standard string comparison for non-arrays or mixed types
        return value1 == value2;
    }

    /// <summary>
    /// Determines if a value represents an array element (contains array index notation).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is an array element; otherwise false.</returns>
    private static bool IsArrayValue(string value)
    {
        return value.Contains('[') && value.EndsWith(']');
    }

    /// <summary>
    /// Detects the JSON type of a configuration value string.
    /// </summary>
    /// <param name="value">The value to analyze.</param>
    /// <returns>The detected type ("string", "number", "boolean", "object", "null", or "array").</returns>
    private static string DetectJsonType(string? value)
    {
        if (value == null)
            return "null";

        if (string.IsNullOrWhiteSpace(value))
            return "string"; // Empty string is still a string

        // Check for JSON literals
        var trimmed = value.Trim();

        if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("false", StringComparison.OrdinalIgnoreCase))
            return "boolean";

        if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
            return "null";

        // Check for numbers (including scientific notation)
        if (double.TryParse(trimmed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            return "number";

        // Check if it looks like a JSON object (starts with { and ends with })
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return "object";

        // Check if it looks like a JSON array (starts with [ and ends with ])
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            return "array";

        // Default to string
        return "string";
    }

    /// <summary>
    /// Determines if two values have different types.
    /// </summary>
    /// <param name="value1">The first value.</param>
    /// <param name="value2">The second value.</param>
    /// <returns>True if the types are different; otherwise false.</returns>
    private static bool HasDifferentTypes(string? value1, string? value2)
    {
        var type1 = DetectJsonType(value1);
        var type2 = DetectJsonType(value2);
        return type1 != type2;
    }

    /// <summary>
    /// Extracts the base array key from an array element key (e.g., "MyArray[0]" -> "MyArray").
    /// </summary>
    /// <param name="arrayElementKey">The array element key.</param>
    /// <returns>The base array key.</returns>
    private static string ExtractArrayKey(string arrayElementKey)
    {
        int bracketIndex = arrayElementKey.IndexOf('[');
        if (bracketIndex < 0)
            return arrayElementKey;

        return arrayElementKey.Substring(0, bracketIndex);
    }

    /// <summary>
    /// Extracts all values from an array representation.
    /// For "MyArray[0]:value1\nMyArray[1]:value2", returns {"value1", "value2"}.
    /// </summary>
    /// <param name="arrayText">The text containing array elements.</param>
    /// <returns>A set of array values.</returns>
    private static HashSet<string> ExtractArrayValues(string arrayText)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);

        // Split by newlines to get individual array elements
        var lines = arrayText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Find the colon that separates key from value
            int colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                string value = line.Substring(colonIndex + 1).Trim();
                values.Add(value);
            }
        }

        return values;
    }
}