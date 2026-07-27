using System;
using System.Collections.Generic;
using System.Linq;

namespace AppsettingsDiff
{
    /// <summary>
    /// Provides fluent extension methods for <see cref="PlaceholderDetector"/> to analyze
    /// placeholder patterns within strings.
    /// </summary>
    public static class PlaceholderDetectorFluentExtensions
    {
        /// <summary>
        /// Counts the number of placeholder patterns found in the specified text.
        /// </summary>
        /// <param name="detector">The <see cref="PlaceholderDetector"/> instance to use for pattern matching.</param>
        /// <param name="text">The text to scan for placeholder patterns.</param>
        /// <returns>The total count of placeholder occurrences in <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <c>null</c>.</exception>
        public static int CountPlaceholders(this PlaceholderDetector detector, string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            if (detector == null) throw new ArgumentNullException(nameof(detector));

            int count = 0;
            foreach (var pattern in detector.Patterns)
            {
                int index = 0;
                while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
                {
                    count++;
                    index += pattern.Length;
                }
            }

            return count;
        }

        /// <summary>
        /// Determines whether the specified text contains any placeholder patterns.
        /// </summary>
        /// <param name="detector">The <see cref="PlaceholderDetector"/> instance to use for pattern matching.</param>
        /// <param name="text">The text to inspect for placeholder patterns.</param>
        /// <returns><c>true</c> if at least one placeholder pattern is found; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <c>null</c>.</exception>
        public static bool HasUnresolvedPlaceholders(this PlaceholderDetector detector, string text)
        {
            return detector.CountPlaceholders(text) > 0;
        }

        /// <summary>
        /// Extracts the distinct placeholder patterns that appear in the specified text.
        /// </summary>
        /// <param name="detector">The <see cref="PlaceholderDetector"/> instance to use for pattern matching.</param>
        /// <param name="text">The text to scan for placeholder patterns.</param>
        /// <returns>An <see cref="IEnumerable{String}"/> containing the unique placeholder patterns found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <c>null</c>.</exception>
        public static IEnumerable<string> ExtractPlaceholderNames(this PlaceholderDetector detector, string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            if (detector == null) throw new ArgumentNullException(nameof(detector));

            return detector.Patterns
                .Where(pattern => text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) != -1)
                .Distinct();
        }
    }
}
