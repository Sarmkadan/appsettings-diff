namespace AppsettingsDiff;

/// <summary>
/// Detects placeholder values in configuration keys.
/// </summary>
public sealed class PlaceholderDetector
{
    private readonly IReadOnlyList<string> _patterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderDetector"/> class.
    /// </summary>
    /// <param name="extraPatterns">Optional additional patterns to match as placeholders.</param>
    public PlaceholderDetector(IEnumerable<string>? extraPatterns = null)
    {
        var patterns = new List<string>
        {
            "TODO",
            "changeme",
            "your-",
            "xxx",
            "localhost"
        };

        if (extraPatterns != null)
        {
            patterns.AddRange(extraPatterns);
        }

        _patterns = patterns.AsReadOnly();
    }

    /// <summary>
    /// Scans configuration dictionary for placeholder values.
    /// </summary>
    /// <param name="config">Configuration dictionary to scan.</param>
    /// <returns>List of placeholder findings.</returns>
    public IReadOnlyList<PlaceholderFinding> Scan(Dictionary<string, string> config)
    {
        var findings = new List<PlaceholderFinding>();

        foreach (var (key, value) in config)
        {
            if (LooksLikePlaceholder(value))
            {
                findings.Add(new PlaceholderFinding(key, value, DetermineReason(value)));
            }
        }

        return findings.AsReadOnly();
    }

    /// <summary>
    /// Determines if a value looks like a placeholder.
    /// </summary>
    /// <param name="value">Value to check.</param>
    /// <returns>True if the value looks like a placeholder; otherwise false.</returns>
    public static bool LooksLikePlaceholder(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();

        // Check for common placeholder patterns
        if (normalized.Contains("TODO", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("changeme", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("your-", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("xxx", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for angle bracket placeholders like <...>
        if (normalized.StartsWith('<') && normalized.EndsWith('>'))
        {
            return true;
        }

        // Check for localhost in production-like contexts
        if (normalized.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check against custom patterns
        return false;
    }

    private string DetermineReason(string value)
    {
        var normalized = value.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Empty value";
        }

        if (normalized.Contains("TODO", StringComparison.OrdinalIgnoreCase))
        {
            return "Contains TODO marker";
        }

        if (normalized.Contains("changeme", StringComparison.OrdinalIgnoreCase))
        {
            return "Contains changeme marker";
        }

        if (normalized.StartsWith("your-", StringComparison.OrdinalIgnoreCase))
        {
            return "Starts with your- prefix";
        }

        if (normalized.Contains("xxx", StringComparison.OrdinalIgnoreCase))
        {
            return "Contains xxx placeholder";
        }

        if (normalized.StartsWith('<') && normalized.EndsWith('>'))
        {
            return "Uses angle bracket placeholder syntax";
        }

        if (normalized.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return "Contains localhost in production context";
        }

        return "Unknown placeholder pattern";
    }
}

/// <summary>
/// Represents a detected placeholder in configuration.
/// </summary>
/// <param name="Key">Configuration key containing the placeholder.</param>
/// <param name="Value">The placeholder value found.</param>
/// <param name="Reason">Reason why this was identified as a placeholder.</param>
public sealed record PlaceholderFinding(string Key, string Value, string Reason);