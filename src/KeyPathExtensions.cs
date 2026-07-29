using System;

namespace AppsettingsDiff
{
    /// <summary>
    /// Extension methods for handling configuration key paths represented as strings.
    /// Paths are assumed to be dot‑separated (e.g. "Logging.LogLevel.Default").
    /// All methods gracefully handle <c>null</c> or empty strings.
    /// </summary>
    public static class KeyPathExtensions
    {
        /// <summary>
        /// Returns the parent path of the supplied <paramref name="path"/>.
        /// For example, "A.B.C" → "A.B". Returns <c>null</c> if the path is
        /// <c>null</c>, empty, or has no parent segment.
        /// </summary>
        public static string? GetParentPath(this string? path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var lastDot = path.LastIndexOf('.');
            if (lastDot <= 0) // no dot or dot at the start (invalid)
                return null;

            return path.Substring(0, lastDot);
        }

        /// <summary>
        /// Returns the leaf key (the last segment) of the supplied <paramref name="path"/>.
        /// For example, "A.B.C" → "C". Returns <c>null</c> if the path is <c>null</c> or empty.
        /// </summary>
        public static string? GetLeafKey(this string? path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var lastDot = path.LastIndexOf('.');
            if (lastDot < 0)
                return path; // whole string is a single segment

            return path.Substring(lastDot + 1);
        }

        /// <summary>
        /// Splits the path into its individual segments.
        /// Returns an empty array for <c>null</c> or empty input.
        /// </summary>
        public static string[] GetSegments(this string? path)
        {
            if (string.IsNullOrEmpty(path))
                return Array.Empty<string>();

            return path.Split('.');
        }

        /// <summary>
        /// Determines whether <paramref name="path"/> is a direct or indirect child of <paramref name="parent"/>.
        /// A path is considered a child if it starts with "<c>parent.</c>" and is not equal to the parent.
        /// Returns <c>false</c> for <c>null</c>, empty, or equal strings.
        /// </summary>
        public static bool IsChildOf(this string? path, string? parent)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(parent))
                return false;

            if (path.Equals(parent, StringComparison.Ordinal))
                return false;

            return path.StartsWith(parent + ".", StringComparison.Ordinal);
        }
    }
}
