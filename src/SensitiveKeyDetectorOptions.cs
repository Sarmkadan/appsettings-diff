using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SensitiveKeyDetection
{
    /// <summary>
    /// Immutable options that drive <see cref="SensitiveKeyDetector"/>.
    /// </summary>
    public sealed class SensitiveKeyDetectorOptions
    {
        /// <summary>
        /// Regular expression patterns that are evaluated directly.
        /// </summary>
        public IReadOnlyList<string> Patterns { get; }

        /// <summary>
        /// Literal keywords that are escaped and treated as regular‑expression literals.
        /// </summary>
        public IReadOnlyList<string> Keywords { get; }

        /// <summary>
        /// Determines whether matching should be case‑sensitive.
        /// </summary>
        public bool CaseSensitive { get; }

        public SensitiveKeyDetectorOptions(
            IEnumerable<string> patterns,
            IEnumerable<string> keywords,
            bool caseSensitive)
        {
            Patterns = new ReadOnlyCollection<string>(new List<string>(patterns ?? Array.Empty<string>()));
            Keywords = new ReadOnlyCollection<string>(new List<string>(keywords ?? Array.Empty<string>()));
            CaseSensitive = caseSensitive;
        }
    }
}
