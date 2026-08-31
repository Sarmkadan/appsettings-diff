using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AppsettingsDiff;

/// <summary>
/// Options for configuring the diff operation.
/// </summary>
public record ConfigDiffOptions
{
    /// <summary>
    /// Gets a value indicating whether to compare arrays by value-set (unordered) instead of by index.
    /// When <c>true</c>, arrays are compared as sets – order doesn't matter.
    /// </summary>
    public bool UnorderedArrays { get; init; }

    /// <summary>
    /// Gets the maximum depth to compare nested structures.
    /// <c>null</c> or <c>0</c> means no depth limit; a positive value limits comparison depth.
    /// </summary>
    public int? MaxDepth { get; init; }

    /// <summary>
    /// Gets the path prefix to filter keys by.
    /// Only keys starting with this prefix are compared when set.
    /// </summary>
    public string? PathPrefix { get; init; }

    /// <summary>
    /// Gets a value indicating whether key comparisons should be case‑sensitive.
    /// </summary>
    public bool CaseSensitiveKeys { get; init; } = true;

    /// <summary>
    /// Gets additional key patterns to ignore during comparison.
    /// These patterns are combined with any patterns passed to the <see cref="ConfigDiffer.Diff"/> method.
    /// </summary>
    public IEnumerable<string>? IgnorePaths { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigDiffOptions"/> with default values.
    /// </summary>
    public ConfigDiffOptions()
    {
    }
}

/// <summary>
/// Provides case‑insensitive wildcard matching for configuration key patterns,
/// where <c>*</c> matches any (possibly empty) sequence of characters.
/// </summary>
internal static class KeyPatternMatcher
{
    /// <summary>
    /// Determines whether <paramref name="text"/> matches <paramref name="pattern"/>.
    /// </summary>
    /// <param name="text">The text to test.</param>
    /// <param name="pattern">The pattern that may contain <c>*</c> wildcards.</param>
    /// <param name="comparison">The string comparison to use (default is case‑insensitive).</param>
    /// <returns><c>true</c> if the text matches the pattern; otherwise <c>false</c>.</returns>
    public static bool IsMatch(string text, string pattern, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (!pattern.Contains('*'))
            return text.Equals(pattern, comparison);

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
                if (!text.StartsWith(segment, comparison))
                    return false;

                position = segment.Length;
            }
            else if (i == segments.Length - 1 && anchoredEnd)
            {
                // The final segment must sit flush against the end of the text.
                int endIndex = text.Length - segment.Length;
                if (endIndex < position || !text.EndsWith(segment, comparison))
                    return false;

                position = text.Length;
            }
            else
            {
                int index = text.IndexOf(segment, position, comparison);
                if (index < 0)
                    return false;

                position = index + segment.Length;
            }
        }

        return true;
    }
}

/// <summary>
/// Represents a flat configuration with key‑value pairs.
/// </summary>
public class FlatConfig
{
    /// <summary>
    /// Gets the flattened key‑value pairs of the configuration.
    /// </summary>
    public Dictionary<string, string> Values { get; } = [];

    /// <summary>
    /// Gets the value for <paramref name="key"/>, or an empty string when the key is absent.
    /// </summary>
    /// <param name="key">The configuration key to look up.</param>
    /// <returns>The associated value, or <c>string.Empty</c> if the key does not exist.</returns>
    public string GetValue(string key) => Values.TryGetValue(key, out var value) ? value : string.Empty;

    /// <summary>
    /// Determines whether the configuration contains <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <returns><c>true</c> if the key exists; otherwise <c>false</c>.</returns>
    public bool ContainsKey(string key) => Values.ContainsKey(key);
}

/// <summary>
/// Represents the type of difference between two configurations.
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
/// Represents a single difference entry.
/// </summary>
public class DiffEntry
{
    /// <summary>Gets the type of the difference.</summary>
    public required DiffKind Kind { get; init; }

    /// <summary>Gets the configuration key the difference applies to.</summary>
    public required string Key { get; init; }

    /// <summary>Gets the baseline value, or <c>null</c> for added keys.</summary>
    public string? OldValue { get; init; }

    /// <summary>Gets the target value, or <c>null</c> for removed keys.</summary>
    public string? NewValue { get; init; }

    /// <summary>Gets a value indicating whether the key is considered sensitive.</summary>
    public bool IsSensitive { get; init; }

    /// <summary>Gets the optional source path associated with the entry.</summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets the type of the baseline value (e.g., "string", "number", "boolean", "object", "null").
    /// Only relevant for <see cref="DiffKind.TypeChanged"/> differences.
    /// </summary>
    public string? OldType { get; init; }

    /// <summary>
    /// Gets the type of the target value (e.g., "string", "number", "boolean", "object", "null").
    /// Only relevant for <see cref="DiffKind.TypeChanged"/> differences.
    /// </summary>
    public string? NewType { get; init; }
}

/// <summary>
/// Result of a diff operation.
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
    /// <returns>The number of entries of that kind.</returns>
    public int CountOf(DiffKind kind) => Entries.Count(e => e.Kind == kind);

    /// <summary>
    /// Gets the number of keys that were ignored due to the ignore patterns.
    /// </summary>
    public int IgnoredCount { get; set; }
}

/// <summary>
/// Main diffing class that compares two configurations.
/// </summary>
public class ConfigDiffer
{
    private readonly SensitiveKeyDetector _detector;

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigDiffer"/>.
    /// </summary>
    /// <param name="detector">Detector used to flag sensitive keys in the produced entries.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is <c>null</c>.</exception>
    public ConfigDiffer(SensitiveKeyDetector detector)
    {
        ArgumentNullException.ThrowIfNull(detector);
        _detector = detector;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ConfigDiffer"/> with case‑sensitive key comparisons.
    /// </summary>
    /// <param name="detector">Detector used to flag sensitive keys in the produced entries.</param>
    /// <param name="caseSensitiveKeys">Whether key comparisons should be case‑sensitive.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="detector"/> is <c>null</c>.</exception>
    public ConfigDiffer(SensitiveKeyDetector detector, bool caseSensitiveKeys)
    {
        ArgumentNullException.ThrowIfNull(detector);
        _detector = new SensitiveKeyDetector(detector.GetPatterns(), caseSensitiveKeys);
    }

    /// <summary>
    /// Compares two flat configurations and reports added, removed and changed keys.
    /// </summary>
    /// <param name="baseline">The baseline configuration.</param>
    /// <param name="target">The target configuration.</param>
    /// <param name="ignoreKeys">Optional key patterns to skip; supports <c>*</c> wildcards, otherwise matched as a case‑insensitive substring.</param>
    /// <param name="basePath">Optional identifier for the baseline (e.g. a file path) recorded in the result.</param>
    /// <param name="targetPath">Optional identifier for the target (e.g. a file path) recorded in the result.</param>
    /// <param name="options">Optional configuration options for the diff operation.</param>
    /// <param name="depth">Internal recursion depth counter. Do not set manually.</param>
    /// <returns>A <see cref="DiffResult"/> describing the differences.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseline"/> or <paramref name="target"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the maximum recursion depth is exceeded.</exception>
    public DiffResult Diff(
        FlatConfig baseline,
        FlatConfig target,
        IEnumerable<string>? ignoreKeys = null,
        string? basePath = null,
        string? targetPath = null,
        ConfigDiffOptions? options = null,
        int depth = 0)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(target);

        options ??= new ConfigDiffOptions();
        var result = new DiffResult
        {
            BasePath = basePath ?? "baseline",
            TargetPath = targetPath ?? "target"
        };

        var ignoreSet = CreateIgnoreSet(ignoreKeys, options.IgnorePaths);
        result.IgnoredCount = CompareBaselineKeys(baseline, target, result, ignoreSet, options);
        result.IgnoredCount += CompareTargetKeys(baseline, target, result, ignoreSet, options);
        return result;
    }

    private static HashSet<string> CreateIgnoreSet(
        IEnumerable<string>? ignoreKeys,
        IEnumerable<string>? ignorePaths)
    {
        var capacity = 0;
        if (ignoreKeys is ICollection<string> collection1) capacity += collection1.Count;
        if (ignorePaths is ICollection<string> collection2) capacity += collection2.Count;

        var ignoreSet = new HashSet<string>(capacity, StringComparer.OrdinalIgnoreCase);
        AddIgnorePatterns(ignoreSet, ignoreKeys);
        AddIgnorePatterns(ignoreSet, ignorePaths);
        return ignoreSet;
    }

    private static void AddIgnorePatterns(HashSet<string> ignoreSet, IEnumerable<string>? patterns)
    {
        if (patterns == null)
            return;

        foreach (var pattern in patterns)
            ignoreSet.Add(pattern);
    }

    private int CompareBaselineKeys(
        FlatConfig baseline,
        FlatConfig target,
        DiffResult result,
        HashSet<string> ignoreSet,
        ConfigDiffOptions options)
    {
        int ignoredCount = 0;
        foreach (var kvp in baseline.Values)
        {
            string key = kvp.Key;
            if (ShouldSkipKey(key, ignoreSet, options))
            {
                ignoredCount++;
                continue;
            }

            CompareBaselineValue(key, kvp.Value, target, result, options);
        }

        return ignoredCount;
    }

    private void CompareBaselineValue(
        string key,
        string value,
        FlatConfig target,
        DiffResult result,
        ConfigDiffOptions options)
    {
        if (!target.ContainsKey(key))
        {
            result.Entries.Add(CreateEntry(DiffKind.Removed, key, oldValue: value));
            return;
        }

        string targetValue = target.GetValue(key);
        if (ExceedsMaxDepth(key, options.MaxDepth))
        {
            if (!AreValuesEqualAsBlobs(value, targetValue))
            {
                result.Entries.Add(CreateEntry(DiffKind.Changed, key, value, targetValue));
            }

            return;
        }

        if (HasDifferentTypes(value, targetValue))
        {
            result.Entries.Add(new DiffEntry
            {
                Kind = DiffKind.TypeChanged,
                Key = key,
                OldValue = value,
                NewValue = targetValue,
                OldType = DetectJsonType(value),
                NewType = DetectJsonType(targetValue),
                IsSensitive = _detector.IsSensitive(key)
            });
            return;
        }

        if (!AreValuesEqual(value, targetValue, options))
            result.Entries.Add(CreateEntry(DiffKind.Changed, key, value, targetValue));
    }

    private int CompareTargetKeys(
        FlatConfig baseline,
        FlatConfig target,
        DiffResult result,
        HashSet<string> ignoreSet,
        ConfigDiffOptions options)
    {
        int ignoredCount = 0;
        foreach (var kvp in target.Values)
        {
            string key = kvp.Key;
            if (ShouldSkipKey(key, ignoreSet, options))
            {
                ignoredCount++;
                continue;
            }

            if (!baseline.ContainsKey(key))
                result.Entries.Add(CreateEntry(DiffKind.Added, key, newValue: kvp.Value));
        }

        return ignoredCount;
    }

    private DiffEntry CreateEntry(DiffKind kind, string key, string? oldValue = null, string? newValue = null)
    {
        return new DiffEntry
        {
            Kind = kind,
            Key = key,
            OldValue = oldValue,
            NewValue = newValue,
            IsSensitive = _detector.IsSensitive(key)
        };
    }

    private static bool ShouldSkipKey(string key, HashSet<string> ignoreSet, ConfigDiffOptions options)
    {
        return ShouldIgnore(key, ignoreSet, options.CaseSensitiveKeys) ||
            !MatchesPathPrefix(key, options.PathPrefix, options.CaseSensitiveKeys);
    }

    /// <summary>
    /// Determines if a key path exceeds the maximum depth.
    /// </summary>
    /// <param name="key">The configuration key path.</param>
    /// <param name="maxDepth">The maximum allowed depth (<c>null</c> or <c>0</c> means no limit).</param>
    /// <returns><c>true</c> if the key path exceeds the maximum depth; otherwise <c>false</c>.</returns>
    private static bool ExceedsMaxDepth(string key, int? maxDepth)
    {
        if (maxDepth == null || maxDepth <= 0)
            return false;

        // Count the number of colons in the key path
        // Each colon represents a level of nesting (e.g., "Section:Subsection:Key" has depth 2)
        int depth = 0;
        foreach (char c in key)
        {
            if (c == ':')
            {
                depth++;
            }
        }
        return depth >= maxDepth;
    }

    /// <summary>
    /// Compares two configuration values as opaque blobs when they exceed max depth.
    /// </summary>
    /// <param name="value1">The baseline value.</param>
    /// <param name="value2">The target value.</param>
    /// <returns><c>true</c> if the values are equal as opaque blobs; otherwise <c>false</c>.</returns>
    private static bool AreValuesEqualAsBlobs(string? value1, string? value2)
    {
        // If either value is null, use standard comparison
        if (value1 == null || value2 == null)
        {
            return value1 == value2;
        }

        // For blobs, we use Ordinal comparison
        return TimingSafeComparer.FixedTimeEquals(value1, value2, StringComparison.Ordinal);
    }

    private static bool ShouldIgnore(string key, HashSet<string> ignoreSet, bool caseSensitive)
    {
        if (ignoreSet.Count == 0)
            return false;

        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return ignoreSet.Any(pattern =>
            pattern.Contains('*')
                ? KeyPatternMatcher.IsMatch(key, pattern, comparison)
                : key.Contains(pattern, comparison));
    }

    /// <summary>
    /// Compares two configuration values for equality, with special handling for arrays when unordered comparison is enabled.
    /// </summary>
    /// <param name="value1">The first value to compare.</param>
    /// <param name="value2">The second value to compare.</param>
    /// <param name="options">The diff options that may enable unordered array comparison.</param>
    /// <returns><c>true</c> if the values are equal; otherwise <c>false</c>.</returns>
    private bool AreValuesEqual(string? value1, string? value2, ConfigDiffOptions options)
    {
        if (value1 == null || value2 == null)
            return value1 == value2;

        if (DetectJsonType(value1) == "boolean" && DetectJsonType(value2) == "boolean")
            return TimingSafeComparer.FixedTimeEquals(value1.ToLowerInvariant(), value2.ToLowerInvariant());

        var comparison = options.CaseSensitiveKeys
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        if (!options.UnorderedArrays)
            return TimingSafeComparer.FixedTimeEquals(value1, value2, comparison);

        return AreUnorderedArrayValuesEqual(value1, value2, comparison);
    }

    private static bool AreUnorderedArrayValuesEqual(
        string value1,
        string value2,
        StringComparison comparison)
    {
        if (!IsArrayValue(value1) || !IsArrayValue(value2))
            return TimingSafeComparer.FixedTimeEquals(value1, value2, comparison);

        var arrayKey1 = ExtractArrayKey(value1);
        var arrayKey2 = ExtractArrayKey(value2);
        if (!string.Equals(arrayKey1, arrayKey2, comparison))
            return TimingSafeComparer.FixedTimeEquals(value1, value2, comparison);

        return ExtractArrayValues(value1).SetEquals(ExtractArrayValues(value2));
    }

    /// <summary>
    /// Determines if a value represents an array element (contains array index notation).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is an array element; otherwise <c>false</c>.</returns>
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
    /// <returns><c>true</c> if the types are different; otherwise <c>false</c>.</returns>
    private static bool HasDifferentTypes(string? value1, string? value2)
    {
        var type1 = DetectJsonType(value1);
        var type2 = DetectJsonType(value2);
        return type1 != type2;
    }

    /// <summary>
    /// Determines if a key matches the path prefix filter.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <param name="pathPrefix">The path prefix to filter by (<c>null</c> or empty means no filtering).</param>
    /// <param name="caseSensitive">Whether the comparison should be case‑sensitive.</param>
    /// <returns><c>true</c> if the key matches the prefix filter; otherwise <c>false</c>.</returns>
    private static bool MatchesPathPrefix(string key, string? pathPrefix, bool caseSensitive = false)
    {
        if (string.IsNullOrEmpty(pathPrefix))
            return true;

        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return key.StartsWith(pathPrefix, comparison);
    }

    /// <summary>
    /// Extracts the base array key from an array element key (e.g., "MyArray[0]" → "MyArray").
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
    /// For "MyArray[0]:value1\nMyArray[1]:value2", returns a set containing {"value1", "value2"}.
    /// </summary>
    /// <param name="arrayText">The text containing array elements.</param>
    /// <returns>A set of array values.</returns>
    private static HashSet<string> ExtractArrayValues(string arrayText)
    {
        // Estimate capacity based on newlines, plus 1
        int capacity = 1;
        foreach (char c in arrayText) { if (c == '\n') capacity++; }

        var values = new HashSet<string>(capacity, StringComparer.OrdinalIgnoreCase);

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
