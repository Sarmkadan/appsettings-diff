namespace AppsettingsDiff;

/// <summary>
/// Detects sensitive keys in configuration
/// </summary>
public class SensitiveKeyDetector
{
    private static readonly string[] SensitivePatterns =
    [
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

    /// <summary>
    /// Determines whether the given configuration key matches any of the known sensitive patterns.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <returns><see langword="true"/> if the key looks sensitive; otherwise <see langword="false"/>.</returns>
    public bool IsSensitive(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return SensitivePatterns.Any(pattern =>
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
    Changed
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

    /// <summary>Counts the entries of the specified <paramref name="kind"/>.</summary>
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
    /// <returns>A <see cref="DiffResult"/> describing the differences.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseline"/> or <paramref name="target"/> is <see langword="null"/>.</exception>
    public DiffResult Diff(
        FlatConfig baseline,
        FlatConfig target,
        IEnumerable<string>? ignoreKeys = null,
        string? basePath = null,
        string? targetPath = null)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(target);

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
            else if (target.GetValue(key) != kvp.Value)
            {
                // Check if changed
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
        }

        result.IgnoredCount = ignoredCount;
        return result;
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
}
