namespace AppsettingsDiff;

/// <summary>
/// Result of a three-way merge operation.
/// </summary>
public sealed class MergeResult
{
    /// <summary>
    /// Gets the merged configuration dictionary.
    /// </summary>
    public Dictionary<string, string> Merged { get; init; } = null!;

    /// <summary>
    /// Gets the list of merge conflicts encountered during the merge.
    /// </summary>
    public IReadOnlyList<MergeConflict> Conflicts { get; init; } = null!;

    /// <summary>
    /// Gets a value indicating whether the merge resulted in any conflicts.
    /// </summary>
    public bool HasConflicts => Conflicts.Count > 0;
}

/// <summary>
/// Represents a merge conflict encountered during a three-way merge.
/// </summary>
public sealed class MergeConflict
{
    /// <summary>
    /// Gets the configuration key that has a conflict.
    /// </summary>
    public string Key { get; init; } = null!;

    /// <summary>
    /// Gets the value from the base configuration, if present.
    /// </summary>
    public string? BaseValue { get; init; }

    /// <summary>
    /// Gets the value from the "ours" (local) configuration, if present.
    /// </summary>
    public string? OurValue { get; init; }

    /// <summary>
    /// Gets the value from the "theirs" (remote) configuration, if present.
    /// </summary>
    public string? TheirValue { get; init; }
}

/// <summary>
/// Performs a three-way merge of configuration dictionaries (base, ours, theirs).
/// </summary>
public static class ThreeWayMerger
{
    /// <summary>
    /// Merges three configuration dictionaries using three-way merge algorithm.
    /// </summary>
    /// <param name="baseConfig">The base configuration to merge from.</param>
    /// <param name="ours">Our local configuration with changes.</param>
    /// <param name="theirs">Their remote configuration with changes.</param>
    /// <returns>A <see cref="MergeResult"/> containing the merged configuration and any conflicts.</returns>
    public static MergeResult Merge(
        Dictionary<string, string> baseConfig,
        Dictionary<string, string> ours,
        Dictionary<string, string> theirs)
    {
        var merged = new Dictionary<string, string>(StringComparer.Ordinal);
        var conflicts = new List<MergeConflict>();

        // Collect all unique keys from all three configurations
        var allKeys = new HashSet<string>(StringComparer.Ordinal);
        allKeys.UnionWith(baseConfig.Keys);
        allKeys.UnionWith(ours.Keys);
        allKeys.UnionWith(theirs.Keys);

        foreach (var key in allKeys)
        {
            var baseValue = baseConfig.TryGetValue(key, out var bv) ? bv : null;
            var ourValue = ours.TryGetValue(key, out var ov) ? ov : null;
            var theirValue = theirs.TryGetValue(key, out var tv) ? tv : null;

            // Determine the merge strategy based on the changes
            if (baseValue == ourValue && ourValue == theirValue)
            {
                // All three are the same - no change
                merged[key] = ourValue ?? string.Empty;
            }
            else if (baseValue == ourValue && ourValue != theirValue)
            {
                // Only theirs changed - take their value
                merged[key] = theirValue ?? string.Empty;
            }
            else if (baseValue == theirValue && theirValue != ourValue)
            {
                // Only ours changed - take our value
                merged[key] = ourValue ?? string.Empty;
            }
            else if (ourValue == theirValue)
            {
                // Both ours and theirs changed to the same value - take that value
                merged[key] = ourValue ?? string.Empty;
            }
            else if (baseValue != null && ourValue == null && theirValue == null)
            {
                // Key removed in both ours and theirs - don't include in merged
                // (key was present in base but removed from both sides)
            }
            else if (baseValue == null && ourValue != null && theirValue == null)
            {
                // Only added in ours - take our value
                merged[key] = ourValue;
            }
            else if (baseValue == null && ourValue == null && theirValue != null)
            {
                // Only added in theirs - take their value
                merged[key] = theirValue;
            }
            else
            {
                // Conflict: both sides changed the key differently
                // Add to merged with our value (prefer ours in case of conflict)
                merged[key] = ourValue ?? string.Empty;
                conflicts.Add(new MergeConflict
                {
                    Key = key,
                    BaseValue = baseValue,
                    OurValue = ourValue,
                    TheirValue = theirValue
                });
            }
        }

        return new MergeResult
        {
            Merged = merged,
            Conflicts = conflicts.AsReadOnly()
        };
    }
}
