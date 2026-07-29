using System;

namespace AppsettingsDiff;

/// <summary>
/// Provides fluent extension methods for <see cref="DiffEntry"/> instances.
/// </summary>
public static class DiffEntryFluentExtensions
{
    /// <summary>
    /// Determines whether the diff entry represents a sensitive configuration path.
    /// </summary>
    /// <param name="entry">The diff entry to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the entry was flagged as sensitive; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <see langword="null"/>.</exception>
    public static bool IsSensitivePath(this DiffEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        return entry.IsSensitive;
    }

    /// <summary>
    /// Calculates the depth of the configuration key path.
    /// The depth is defined as the number of colon (<c>:</c>) separators in the key.
    /// For example, <c>"Section:Subsection:Key"</c> has a depth of <c>2</c>.
    /// </summary>
    /// <param name="entry">The diff entry whose key depth is to be calculated.</param>
    /// <returns>The number of colon separators in <see cref="DiffEntry.Key"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <see langword="null"/>.</exception>
    public static int PathDepth(this DiffEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        if (string.IsNullOrEmpty(entry.Key))
            return 0;

        // Depth is the count of ':' characters; each ':' represents a nesting level.
        return entry.Key.Split(':').Length - 1;
    }

    /// <summary>
    /// Retrieves the top‑level section of the configuration key.
    /// The top‑level section is the substring before the first colon (<c>:</c>).
    /// If the key does not contain a colon, the entire key is returned.
    /// </summary>
    /// <param name="entry">The diff entry whose top‑level section is required.</param>
    /// <returns>The top‑level section of <see cref="DiffEntry.Key"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is <see langword="null"/>.</exception>
    public static string TopLevelSection(this DiffEntry entry)
    {
        if (entry is null) throw new ArgumentNullException(nameof(entry));
        if (string.IsNullOrEmpty(entry.Key))
            return string.Empty;

        int colonIndex = entry.Key.IndexOf(':');
        return colonIndex >= 0 ? entry.Key.Substring(0, colonIndex) : entry.Key;
    }
}
