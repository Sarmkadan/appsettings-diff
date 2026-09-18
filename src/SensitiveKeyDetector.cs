using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SensitiveKeyDetection
{
    /// <summary>
    /// Detects sensitive configuration keys using a compiled regular expression.
    /// </summary>
    public class SensitiveKeyDetector
    {
        // Pre‑compiled regex with invariant culture to avoid allocations in the hot path.
        private static readonly Regex _sensitiveKeyRegex =
            new Regex("[^\"]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Cache of lower‑cased key names to avoid repeated regex evaluations.
        private readonly ConcurrentDictionary<string, bool> _cache =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Determines whether the supplied key is considered sensitive.
        /// </summary>
        /// <param name="key">The configuration key to test.</param>
        /// <returns>True if the key matches the sensitive pattern; otherwise false.</returns>
        public bool IsSensitive(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            // The cache key is the original key; the dictionary is case‑insensitive,
            // so we don't need to explicitly lower‑case the key.
            return _cache.GetOrAdd(key, k => _sensitiveKeyRegex.IsMatch(k));
        }
    }
}
