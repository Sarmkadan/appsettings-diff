using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SensitiveKeyDetection
{
    /// <summary>
    /// Detects sensitive configuration keys using compiled regular expressions and a cache.
    /// The detection logic is driven by immutable <see cref="SensitiveKeyDetectorOptions"/>.
    /// </summary>
    public class SensitiveKeyDetector
    {
        private readonly Regex _sensitiveKeyRegex;
        private readonly ConcurrentDictionary<string, bool> _cache;

        /// <summary>
        /// Creates a detector with the supplied options.
        /// </summary>
        /// <param name="options">Immutable options that define the detection behaviour.</param>
        public SensitiveKeyDetector(SensitiveKeyDetectorOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            // Build a single regex that matches any of the supplied patterns or keywords.
            // Keywords are escaped so they are treated as literals.
            var allPatterns = new List<string>();

            if (options.Patterns != null && options.Patterns.Count > 0)
                allPatterns.AddRange(options.Patterns);

            if (options.Keywords != null && options.Keywords.Count > 0)
                allPatterns.AddRange(options.Keywords.Select(Regex.Escape));

            // If nothing was supplied we use a regex that never matches.
            var combinedPattern = allPatterns.Count > 0 ? string.Join("|", allPatterns) : "$^";

            var regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            if (!options.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;

            _sensitiveKeyRegex = new Regex(combinedPattern, regexOptions);
            _cache = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parameter‑less constructor retained for backward compatibility.
        /// It creates a detector that uses the historic default pattern "[^\"]+".
        /// </summary>
        public SensitiveKeyDetector()
            : this(new SensitiveKeyDetectorOptions(
                  patterns: new[] { "[^\"]+" },
                  keywords: Array.Empty<string>(),
                  caseSensitive: false))
        {
        }

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
