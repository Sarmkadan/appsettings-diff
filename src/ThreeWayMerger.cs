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

    /// <summary>
    /// Gets a value indicating whether this conflict was automatically resolved.
    /// </summary>
    public bool AutoResolved { get; init; }
}

/// <summary>
/// Strategy for automatically resolving merge conflicts.
/// </summary>
public enum ConflictResolutionStrategy
{
    /// <summary>
    /// Conflicts are not automatically resolved; they are recorded in the result.
    /// </summary>
    Manual,

    /// <summary>
    /// Prefer our (local) changes when resolving conflicts.
    /// </summary>
    PreferOurs,

    /// <summary>
    /// Prefer their (remote) changes when resolving conflicts.
    /// </summary>
    PreferTheirs
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
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="baseConfig"/>, <paramref name="ours"/> or <paramref name="theirs"/> is <see langword="null"/>.
    /// </exception>
    public static MergeResult Merge(
        Dictionary<string, string> baseConfig,
        Dictionary<string, string> ours,
        Dictionary<string, string> theirs)
    {
        return Merge(baseConfig, ours, theirs, ConflictResolutionStrategy.Manual);
    }

    /// <summary>
    /// Merges three configuration dictionaries using three-way merge algorithm with automatic conflict resolution.
    /// </summary>
    /// <param name="baseConfig">The base configuration to merge from.</param>
    /// <param name="ours">Our local configuration with changes.</param>
    /// <param name="theirs">Their remote configuration with changes.</param>
    /// <param name="strategy">The conflict resolution strategy to use.</param>
    /// <returns>A <see cref="MergeResult"/> containing the merged configuration and any conflicts.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="baseConfig"/>, <paramref name="ours"/>, <paramref name="theirs"/> or <paramref name="strategy"/> is <see langword="null"/>.
    /// </exception>
    public static MergeResult Merge(
        Dictionary<string, string> baseConfig,
        Dictionary<string, string> ours,
        Dictionary<string, string> theirs,
        ConflictResolutionStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(baseConfig);
        ArgumentNullException.ThrowIfNull(ours);
        ArgumentNullException.ThrowIfNull(theirs);
        ArgumentNullException.ThrowIfNull(strategy);

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

            // Determine the merge strategy based on the changes.
            // A null value means the key is absent on that side, so a null
            // winner keeps the key out of the merged dictionary (deletion).
            if (baseValue == ourValue && ourValue == theirValue)
            {
                // All three are the same - no change
                if (ourValue is not null)
                    merged[key] = ourValue;
            }
            else if (baseValue == ourValue)
            {
                // Only theirs changed (modified, added or deleted) - take their side
                if (theirValue is not null)
                    merged[key] = theirValue;
            }
            else if (baseValue == theirValue)
            {
                // Only ours changed (modified, added or deleted) - take our side
                if (ourValue is not null)
                    merged[key] = ourValue;
            }
            else if (ourValue == theirValue)
            {
                // Both sides made the same change (including both deleting the key)
                if (ourValue is not null)
                    merged[key] = ourValue;
            }
            else
            {
                // Conflict: both sides changed the key differently.
                string? resolvedValue = null;
                bool autoResolved = false;

                switch (strategy)
                {
                    case ConflictResolutionStrategy.Manual:
                        // Keep both values, mark as not auto-resolved
                        resolvedValue = ourValue; // Default to our side for backward compatibility
                        break;

                    case ConflictResolutionStrategy.PreferOurs:
                        resolvedValue = ourValue;
                        autoResolved = true;
                        break;

                    case ConflictResolutionStrategy.PreferTheirs:
                        resolvedValue = theirValue;
                        autoResolved = true;
                        break;
                }

                if (resolvedValue is not null)
                    merged[key] = resolvedValue;

                conflicts.Add(new MergeConflict
                {
                    Key = key,
                    BaseValue = baseValue,
                    OurValue = ourValue,
                    TheirValue = theirValue,
                    AutoResolved = autoResolved
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