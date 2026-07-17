using System.Globalization;
using System.Linq;

namespace AppsettingsDiff;

/// <summary>
/// Provides extension methods for <see cref="MergeResult"/> to facilitate common merge operations and conflict resolution.
/// </summary>
public static class MergeResultExtensions
{
    /// <summary>
    /// Gets the merged value for the specified key, or a default value if the key does not exist.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The merged value for the key, or the default value if the key does not exist.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static string GetValueOrDefault(this MergeResult result, string key, string defaultValue = "")
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(key);

        return result.Merged.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Gets the merged value for the specified key as an integer, or a default value if the key does not exist or cannot be parsed.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or parsing fails.</param>
    /// <returns>The merged value for the key as an integer, or the default value if parsing fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static int GetValueOrDefaultAsInt(this MergeResult result, string key, int defaultValue = 0)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(key);

        if (result.Merged.TryGetValue(key, out var value))
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the merged value for the specified key as a boolean, or a default value if the key does not exist or cannot be parsed.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or parsing fails.</param>
    /// <returns>The merged value for the key as a boolean, or the default value if parsing fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static bool GetValueOrDefaultAsBool(this MergeResult result, string key, bool defaultValue = false)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(key);

        if (result.Merged.TryGetValue(key, out var value))
        {
            if (bool.TryParse(value, out var parsedValue))
            {
                return parsedValue;
            }

            // Handle common string representations
            if (string.Equals(value, "1", StringComparison.Ordinal) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "0", StringComparison.Ordinal) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the merged value for the specified key as a decimal, or a default value if the key does not exist or cannot be parsed.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or parsing fails.</param>
    /// <returns>The merged value for the key as a decimal, or the default value if parsing fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static decimal GetValueOrDefaultAsDecimal(this MergeResult result, string key, decimal defaultValue = 0m)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(key);

        if (result.Merged.TryGetValue(key, out var value))
        {
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedValue))
            {
                return parsedValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Determines whether the merged configuration contains the specified key.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <param name="key">The configuration key to check.</param>
    /// <returns><see langword="true"/> if the key exists in the merged configuration; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static bool ContainsKey(this MergeResult result, string key)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(key);

        return result.Merged.ContainsKey(key);
    }

    /// <summary>
    /// Gets the number of keys in the merged configuration.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <returns>The number of keys in the merged configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static int Count(this MergeResult result)
        => result.Merged.Count;

    /// <summary>
    /// Gets an enumerable of all keys in the merged configuration.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <returns>An enumerable of all keys in the merged configuration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static IEnumerable<string> GetKeys(this MergeResult result)
        => result.Merged.Keys;

    /// <summary>
    /// Attempts to get the merged value for the specified key.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <param name="key">The configuration key to look up.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns><see langword="true"/> if the key was found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static bool TryGetValue(this MergeResult result, string key, out string? value)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(key);

        return result.Merged.TryGetValue(key, out value);
    }

    /// <summary>
    /// Gets a read-only list of all conflicts encountered during the merge.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <returns>A read-only list of all conflicts encountered during the merge.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<MergeConflict> GetConflicts(this MergeResult result)
        => result.Conflicts;

    /// <summary>
    /// Determines whether the merge result has any conflicts.
    /// </summary>
    /// <param name="result">The merge result to check.</param>
    /// <returns><see langword="true"/> if the merge result has one or more conflicts; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static bool HasConflicts(this MergeResult result)
        => result.Conflicts.Count > 0;

    /// <summary>
    /// Gets the first conflict with the specified key, if any.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <param name="key">The configuration key to find a conflict for.</param>
    /// <returns>The first conflict with the specified key, or <see langword="null"/> if no conflict exists for that key.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public static MergeConflict? GetConflict(this MergeResult result, string key)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(key);

        return result.Conflicts.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets a read-only list of all keys that have conflicts.
    /// </summary>
    /// <param name="result">The merge result to query.</param>
    /// <returns>A read-only list of all keys that have conflicts.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> GetConflictedKeys(this MergeResult result)
        => result.Conflicts.Select(c => c.Key).ToList().AsReadOnly();
}