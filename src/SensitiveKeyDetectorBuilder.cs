using System;
using System.Collections.Generic;

namespace SensitiveKeyDetection
{
    /// <summary>
    /// Fluent builder for <see cref="SensitiveKeyDetector"/>.
    /// Allows callers to configure patterns, keywords and case‑sensitivity before creating an immutable detector.
    /// </summary>
    public sealed class SensitiveKeyDetectorBuilder
    {
        private readonly List<string> _patterns = new List<string>();
        private readonly List<string> _keywords = new List<string>();
        private bool _caseSensitive = false;

        /// <summary>
        /// Adds a regular‑expression pattern that will be used to identify sensitive keys.
        /// </summary>
        public SensitiveKeyDetectorBuilder AddPattern(string pattern)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            _patterns.Add(pattern);
            return this;
        }

        /// <summary>
        /// Adds a literal keyword that will be matched (case‑sensitivity follows the builder's setting).
        /// </summary>
        public SensitiveKeyDetectorBuilder AddKeyword(string keyword)
        {
            if (keyword == null) throw new ArgumentNullException(nameof(keyword));
            _keywords.Add(keyword);
            return this;
        }

        /// <summary>
        /// Adds a set of commonly‑used default patterns/keywords.
        /// The defaults are deliberately generic and can be extended later.
        /// </summary>
        public SensitiveKeyDetectorBuilder UseDefaults()
        {
            // Typical default keywords that often indicate secrets.
            AddKeyword("password");
            AddKeyword("secret");
            AddKeyword("apikey");
            AddKeyword("api_key");
            AddKeyword("token");
            AddKeyword("connectionstring");

            // A default pattern that matches any key containing the word "credential".
            AddPattern(@".*credential.*");

            return this;
        }

        /// <summary>
        /// Configures whether matching should be case‑sensitive.
        /// </summary>
        public SensitiveKeyDetectorBuilder CaseSensitive(bool caseSensitive = true)
        {
            _caseSensitive = caseSensitive;
            return this;
        }

        /// <summary>
        /// Builds an immutable <see cref="SensitiveKeyDetector"/> instance using the configured options.
        /// </summary>
        public SensitiveKeyDetector Build()
        {
            var options = new SensitiveKeyDetectorOptions(
                patterns: _patterns,
                keywords: _keywords,
                caseSensitive: _caseSensitive);

            return new SensitiveKeyDetector(options);
        }
    }
}
