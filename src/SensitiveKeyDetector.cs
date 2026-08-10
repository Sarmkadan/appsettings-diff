using System.Text.RegularExpressions;

namespace SensitiveKeyDetector
{
    public class SensitiveKeyDetector
    {
        private static readonly Regex sensitiveKeyRegex = new Regex("[^"]+", RegexOptions.Compiled);
    }
}