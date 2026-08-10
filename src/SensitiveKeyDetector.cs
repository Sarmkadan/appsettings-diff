using System.Text.RegularExpressions;

namespace SensitiveKeyDetector
{
    /// <summary>
    /// Detects sensitive configuration keys using a compiled regular expression.
    /// </summary>
    public class SensitiveKeyDetector
    {
        private static readonly Regex sensitiveKeyRegex = new Regex("[^"]+", RegexOptions.Compiled);
    }
}
