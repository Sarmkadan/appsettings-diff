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

    public bool IsSensitive(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        string lowerKey = key.ToLowerInvariant();

        return SensitivePatterns.Any(pattern =>
            pattern.Contains('*')
                ? SimpleMatch(lowerKey, pattern.Replace("*", ""))
                : lowerKey.Contains(pattern));
    }

    private static bool SimpleMatch(string text, string pattern)
    {
        int patternIndex = 0;
        int textIndex = 0;

        while (patternIndex < pattern.Length && textIndex < text.Length)
        {
            if (pattern[patternIndex] == text[textIndex])
            {
                patternIndex++;
                textIndex++;
            }
            else if (pattern[patternIndex] == ' ')
            {
                patternIndex++;
            }
            else
            {
                return false;
            }
        }

        return patternIndex == pattern.Length;
    }
}

/// <summary>
/// Represents a flat configuration with key-value pairs
/// </summary>
public class FlatConfig
{
    public Dictionary<string, string> Values { get; } = [];

    public string GetValue(string key) => Values.TryGetValue(key, out var value) ? value : string.Empty;

    public bool ContainsKey(string key) => Values.ContainsKey(key);
}

/// <summary>
/// Represents the type of difference
/// </summary>
public enum DiffKind
{
    Added,
    Removed,
    Changed
}

/// <summary>
/// Represents a single difference entry
/// </summary>
public class DiffEntry
{
    public required DiffKind Kind { get; init; }
    public required string Key { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
    public bool IsSensitive { get; init; }
    public string? Path { get; init; }
}

/// <summary>
/// Result of a diff operation
/// </summary>
public class DiffResult
{
    public List<DiffEntry> Entries { get; } = [];
    public string BasePath { get; init; } = string.Empty;
    public string TargetPath { get; init; } = string.Empty;

    public bool HasDifferences => Entries.Count > 0;

    public int CountOf(DiffKind kind) => Entries.Count(e => e.Kind == kind);
}

/// <summary>
/// Main diffing class that compares two configurations
/// </summary>
public class ConfigDiffer
{
    private readonly SensitiveKeyDetector _detector;

    public ConfigDiffer(SensitiveKeyDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    public DiffResult Diff(
        FlatConfig baseline,
        FlatConfig target,
        IEnumerable<string>? ignoreKeys = null)
    {
        if (baseline == null)
            throw new ArgumentNullException(nameof(baseline));

        if (target == null)
            throw new ArgumentNullException(nameof(target));

        var result = new DiffResult
        {
            BasePath = "baseline",
            TargetPath = "target"
        };

        var ignoreSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (ignoreKeys != null)
        {
            foreach (var pattern in ignoreKeys)
            {
                ignoreSet.Add(pattern);
            }
        }

        // Check for removed keys (in baseline but not in target)
        foreach (var kvp in baseline.Values)
        {
            string key = kvp.Key;
            if (ShouldIgnore(key, ignoreSet))
                continue;

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
                continue;

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

        return result;
    }

    private bool ShouldIgnore(string key, HashSet<string> ignoreSet)
    {
        if (ignoreSet.Count == 0)
            return false;

        string lowerKey = key.ToLowerInvariant();

        return ignoreSet.Any(pattern =>
            pattern.Contains('*')
                ? SimpleMatch(lowerKey, pattern.Replace("*", ""))
                : lowerKey.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static bool SimpleMatch(string text, string pattern)
    {
        int patternIndex = 0;
        int textIndex = 0;

        while (patternIndex < pattern.Length && textIndex < text.Length)
        {
            if (pattern[patternIndex] == text[textIndex])
            {
                patternIndex++;
                textIndex++;
            }
            else if (pattern[patternIndex] == ' ')
            {
                patternIndex++;
            }
            else
            {
                return false;
            }
        }

        return patternIndex == pattern.Length;
    }
}