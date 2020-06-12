namespace AppsettingsDiff;

/// <summary>
/// Provides extension methods for <see cref="PlaceholderDetector"/> to enhance placeholder detection capabilities.
/// </summary>
public static class PlaceholderDetectorExtensions
{
    /// <summary>
    /// Filters findings to only those matching specific placeholder patterns.
    /// </summary>
    /// <param name="detector">The placeholder detector instance.</param>
    /// <param name="findings">Findings to filter.</param>
    /// <param name="pattern">Pattern to match against the reason (case-insensitive).</param>
    /// <returns>Filtered findings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when detector or findings is null.</exception>
    public static IReadOnlyList<PlaceholderFinding> FilterByPattern(
        this PlaceholderDetector detector,
        IReadOnlyList<PlaceholderFinding> findings,
        string pattern)
    {
        ArgumentNullException.ThrowIfNull(detector);
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        return findings.Where(f => f.Reason.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Groups findings by their reason, allowing for analysis of common placeholder types.
    /// </summary>
    /// <param name="detector">The placeholder detector instance.</param>
    /// <param name="findings">Findings to group.</param>
    /// <returns>Dictionary mapping reasons to collections of findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when detector or findings is null.</exception>
    public static IReadOnlyDictionary<string, IReadOnlyList<PlaceholderFinding>> GroupByReason(
        this PlaceholderDetector detector,
        IReadOnlyList<PlaceholderFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(detector);
        ArgumentNullException.ThrowIfNull(findings);

        return findings
            .GroupBy(f => f.Reason)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<PlaceholderFinding>)g.ToList().AsReadOnly()
            )
            .AsReadOnly();
    }

    /// <summary>
    /// Checks if any placeholder findings exist in the provided findings collection.
    /// </summary>
    /// <param name="detector">The placeholder detector instance.</param>
    /// <param name="findings">Findings to check.</param>
    /// <returns>True if findings collection contains any items; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when detector or findings is null.</exception>
    public static bool HasAnyFindings(
        this PlaceholderDetector detector,
        IReadOnlyList<PlaceholderFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(detector);
        ArgumentNullException.ThrowIfNull(findings);

        return findings.Count > 0;
    }

    /// <summary>
    /// Gets the count of findings grouped by their severity level.
    /// </summary>
    /// <param name="detector">The placeholder detector instance.</param>
    /// <param name="findings">Findings to analyze.</param>
    /// <returns>Dictionary mapping severity levels to finding counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when detector or findings is null.</exception>
    public static IReadOnlyDictionary<string, int> GetFindingsCountBySeverity(
        this PlaceholderDetector detector,
        IReadOnlyList<PlaceholderFinding> findings)
    {
        ArgumentNullException.ThrowIfNull(detector);
        ArgumentNullException.ThrowIfNull(findings);

        return findings
            .GroupBy(f => GetSeverityLevel(f.Reason))
            .ToDictionary(
                g => g.Key,
                g => g.Count()
            )
            .AsReadOnly();
    }

    private static string GetSeverityLevel(string reason)
    {
        if (reason.Contains("Empty value", StringComparison.OrdinalIgnoreCase))
        {
            return "Critical";
        }

        if (reason.Contains("TODO", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("changeme", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        if (reason.Contains("your-", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("xxx", StringComparison.OrdinalIgnoreCase))
        {
            return "Medium";
        }

        if (reason.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("angle bracket", StringComparison.OrdinalIgnoreCase))
        {
            return "Low";
        }

        return "Unknown";
    }
}