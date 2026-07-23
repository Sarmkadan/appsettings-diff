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

    /// <summary>
    /// Gets a human-readable explanation of why this conflict was recorded, if any.
    /// This is set for conflicts that arise from special handling (such as whole-array
    /// merges) rather than a plain scalar value mismatch.
    /// </summary>
    public string? Reason { get; init; }
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

        // Group keys that belong to a flattened array (e.g. "Section:0", "Section:1:Name")
        // by their array root ("Section"). These are merged as whole units below instead of
        // being merged key-by-key, because index shifts (insert/delete/reorder) on one side
        // make positional keys line up with unrelated elements on the other side.
        var arrayGroups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var key in allKeys)
        {
            var arrayRoot = DetectArrayRoot(key);
            if (arrayRoot is null)
                continue;

            if (!arrayGroups.TryGetValue(arrayRoot, out var groupKeys))
            {
                groupKeys = new List<string>();
                arrayGroups[arrayRoot] = groupKeys;
            }

            groupKeys.Add(key);
        }

        var arrayKeys = new HashSet<string>(arrayGroups.Values.SelectMany(k => k), StringComparer.Ordinal);

        foreach (var (arrayRoot, groupKeys) in arrayGroups)
        {
            MergeArrayGroup(arrayRoot, groupKeys, baseConfig, ours, theirs, strategy, merged, conflicts);
        }

        foreach (var key in allKeys)
        {
            if (arrayKeys.Contains(key))
                continue;

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

    /// <summary>
    /// Determines whether a flattened configuration key is part of an array element
    /// (i.e. it contains a purely numeric colon-separated segment representing an
    /// array index) and, if so, returns the key path preceding that index - the
    /// "array root". Returns <see langword="null"/> when the key does not belong to
    /// an array.
    /// </summary>
    /// <param name="key">The flattened configuration key to inspect.</param>
    /// <returns>
    /// The array root path (which may be an empty string for a top-level array), or
    /// <see langword="null"/> if <paramref name="key"/> contains no numeric index segment.
    /// </returns>
    private static string? DetectArrayRoot(string key)
    {
        var segments = key.Split(':');
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i].Length > 0 && segments[i].All(char.IsAsciiDigit))
                return string.Join(':', segments.Take(i));
        }

        return null;
    }

    /// <summary>
    /// Merges a single flattened array (all keys sharing an array root) as one atomic
    /// unit rather than key-by-key, so that index shifts caused by an insert, delete or
    /// reorder on either side cannot be misread as unrelated per-index conflicts or
    /// silently produce duplicated/misaligned elements.
    /// </summary>
    /// <param name="arrayRoot">The array root path shared by all keys in <paramref name="groupKeys"/>.</param>
    /// <param name="groupKeys">All keys (from any of the three sides) that belong to this array.</param>
    /// <param name="baseConfig">The base configuration.</param>
    /// <param name="ours">Our local configuration.</param>
    /// <param name="theirs">Their remote configuration.</param>
    /// <param name="strategy">The conflict resolution strategy to use when both sides changed the array.</param>
    /// <param name="merged">The merged dictionary being built; winning entries are written into it.</param>
    /// <param name="conflicts">The conflict list being built; an array-level conflict is appended to it when both sides diverge.</param>
    private static void MergeArrayGroup(
        string arrayRoot,
        List<string> groupKeys,
        Dictionary<string, string> baseConfig,
        Dictionary<string, string> ours,
        Dictionary<string, string> theirs,
        ConflictResolutionStrategy strategy,
        Dictionary<string, string> merged,
        List<MergeConflict> conflicts)
    {
        var baseSlice = Slice(baseConfig, groupKeys);
        var ourSlice = Slice(ours, groupKeys);
        var theirSlice = Slice(theirs, groupKeys);

        var baseEqualsOurs = SliceEquals(baseSlice, ourSlice);
        var baseEqualsTheirs = SliceEquals(baseSlice, theirSlice);
        var oursEqualsTheirs = SliceEquals(ourSlice, theirSlice);

        if (baseEqualsOurs && baseEqualsTheirs)
        {
            WriteSlice(merged, ourSlice);
            return;
        }

        if (baseEqualsOurs)
        {
            // Only theirs changed the array - take their side as a whole.
            WriteSlice(merged, theirSlice);
            return;
        }

        if (baseEqualsTheirs)
        {
            // Only ours changed the array - take our side as a whole.
            WriteSlice(merged, ourSlice);
            return;
        }

        if (oursEqualsTheirs)
        {
            // Both sides made the identical change to the array.
            WriteSlice(merged, ourSlice);
            return;
        }

        // Both sides changed the array differently (insert/delete/reorder on either or
        // both sides). Positional per-index comparison is unsafe here because a shifted
        // index can make an inserted element line up against an unrelated pre-existing
        // one, so the whole array is treated as a conflicting unit rather than merged
        // element-by-element.
        var autoResolved = false;

        switch (strategy)
        {
            case ConflictResolutionStrategy.Manual:
                // Default to our side for backward compatibility with scalar conflicts.
                WriteSlice(merged, ourSlice);
                break;

            case ConflictResolutionStrategy.PreferOurs:
                WriteSlice(merged, ourSlice);
                autoResolved = true;
                break;

            case ConflictResolutionStrategy.PreferTheirs:
                WriteSlice(merged, theirSlice);
                autoResolved = true;
                break;
        }

        conflicts.Add(new MergeConflict
        {
            Key = arrayRoot,
            BaseValue = SerializeSlice(baseSlice),
            OurValue = SerializeSlice(ourSlice),
            TheirValue = SerializeSlice(theirSlice),
            AutoResolved = autoResolved,
            Reason = "Both sides modified array elements under this key (insert, delete or reorder). " +
                     "Flattened array indices are not stable across such changes, so the array was " +
                     "merged as a whole value instead of per-index to avoid misaligned or duplicated elements."
        });
    }

    /// <summary>
    /// Extracts the subset of a configuration dictionary restricted to a given set of keys.
    /// </summary>
    /// <param name="config">The configuration dictionary to slice.</param>
    /// <param name="keys">The keys to extract.</param>
    /// <returns>A dictionary containing only the entries from <paramref name="config"/> whose key is in <paramref name="keys"/>.</returns>
    private static Dictionary<string, string> Slice(Dictionary<string, string> config, List<string> keys)
    {
        var slice = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (config.TryGetValue(key, out var value))
                slice[key] = value;
        }

        return slice;
    }

    /// <summary>
    /// Determines whether two array slices contain exactly the same set of key/value pairs.
    /// </summary>
    /// <param name="left">The first slice to compare.</param>
    /// <param name="right">The second slice to compare.</param>
    /// <returns><see langword="true"/> if both slices have identical keys and values; otherwise <see langword="false"/>.</returns>
    private static bool SliceEquals(Dictionary<string, string> left, Dictionary<string, string> right) =>
        left.Count == right.Count &&
        left.All(kv => right.TryGetValue(kv.Key, out var value) && value == kv.Value);

    /// <summary>
    /// Writes every entry of an array slice into the merged dictionary.
    /// </summary>
    /// <param name="merged">The merged dictionary being built.</param>
    /// <param name="slice">The winning slice whose entries should be written.</param>
    private static void WriteSlice(Dictionary<string, string> merged, Dictionary<string, string> slice)
    {
        foreach (var (key, value) in slice)
            merged[key] = value;
    }

    /// <summary>
    /// Serializes an array slice into a deterministic, human-readable string for display in a <see cref="MergeConflict"/>.
    /// </summary>
    /// <param name="slice">The slice to serialize.</param>
    /// <returns>A semicolon-separated "key=value" representation ordered by key, or <see langword="null"/> if the slice is empty.</returns>
    private static string? SerializeSlice(Dictionary<string, string> slice) =>
        slice.Count == 0
            ? null
            : string.Join(';', slice.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));
}