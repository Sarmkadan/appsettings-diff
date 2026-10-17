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
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The pattern list is <see langword="null"/> or empty, or a pattern is <see langword="null"/> or whitespace.</exception>
        /// <exception cref="InvalidSensitivePatternException">A pattern is not a valid regular expression.</exception>
        public SensitiveKeyDetector(SensitiveKeyDetectorOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            ValidatePatterns(options.Patterns);

            // Build a single regex that matches any of the supplied patterns or keywords.
            // Keywords are escaped so they are treated as literals.
            var allPatterns = new List<string>();

            allPatterns.AddRange(options.Patterns);

            if (options.Keywords != null && options.Keywords.Count > 0)
                allPatterns.AddRange(options.Keywords.Select(Regex.Escape));

            var regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            if (!options.CaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;

            var combinedPattern = string.Join("|", allPatterns);

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
        /// Fails fast on invalid patterns so the error surfaces at construction rather than on first use.
        /// Each pattern is compiled on its own so the error names the exact pattern that is broken.
        /// </summary>
        private static void ValidatePatterns(IReadOnlyList<string>? patterns)
        {
            if (patterns == null || patterns.Count == 0)
                throw new ArgumentException("At least one sensitive key pattern is required.", "patterns");

            for (var i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];

                if (string.IsNullOrWhiteSpace(pattern))
                    throw new ArgumentException($"Sensitive key pattern at index {i} is null or whitespace.", "patterns");

                try
                {
                    _ = new Regex(pattern, RegexOptions.CultureInvariant);
                }
                catch (RegexParseException ex)
                {
                    throw new InvalidSensitivePatternException(pattern, i, ex);
                }
            }
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
