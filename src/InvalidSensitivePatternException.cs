using System;
using System.Text.RegularExpressions;

namespace SensitiveKeyDetection
{
    /// <summary>
    /// Thrown when a sensitive-key regular expression pattern cannot be compiled.
    /// The exception message and <see cref="Pattern"/> carry the offending pattern text.
    /// </summary>
    public sealed class InvalidSensitivePatternException : ArgumentException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InvalidSensitivePatternException"/>.
        /// </summary>
        /// <param name="pattern">The pattern text that failed to compile.</param>
        /// <param name="index">The position of the pattern in the supplied pattern list.</param>
        /// <param name="inner">The parse error reported by the regular expression engine.</param>
        public InvalidSensitivePatternException(string pattern, int index, RegexParseException inner)
            : base($"Sensitive key pattern at index {index} is not a valid regular expression: '{pattern}'. {inner.Message}", "patterns", inner)
        {
            Pattern = pattern;
            Index = index;
        }

        /// <summary>
        /// Gets the pattern text that failed to compile.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Gets the position of the invalid pattern in the supplied pattern list.
        /// </summary>
        public int Index { get; }
    }
}
