namespace AppsettingsDiff;

/// <summary>
/// Extension methods for <see cref="SensitiveKeyDetector"/> that provide additional utility functionality
/// for detecting sensitive configuration keys and patterns.
/// </summary>
public static class SensitiveKeyDetectorExtensions
{
    /// <summary>
    /// Determines whether the specified key is a sensitive key based on common patterns.
    /// This is an alias for the existing <see cref="SensitiveKeyDetector.IsSensitive(string)"/> method.
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>True if the key is sensitive; otherwise, false</returns>
    public static bool IsSensitiveKey(this SensitiveKeyDetector detector, string key)
    {
        if (detector == null)
            throw new ArgumentNullException(nameof(detector));

        return detector.IsSensitive(key);
    }

    /// <summary>
    /// Determines whether the specified key matches any of the sensitive patterns.
    /// This method provides a more readable alias for the detector's functionality.
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>True if the key matches sensitive patterns; otherwise, false</returns>
    public static bool IsPotentiallySensitive(this SensitiveKeyDetector detector, string key)
    {
        if (detector == null)
            throw new ArgumentNullException(nameof(detector));

        return detector.IsSensitive(key);
    }

    /// <summary>
    /// Checks if a key is sensitive and also contains common database-related patterns.
    /// Useful for identifying database connection strings and credentials.
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>True if the key is both sensitive and database-related; otherwise, false</returns>
    public static bool IsDatabaseCredential(this SensitiveKeyDetector detector, string key)
    {
        if (detector == null)
            throw new ArgumentNullException(nameof(detector));

        if (string.IsNullOrWhiteSpace(key))
            return false;

        var lowerKey = key.ToLowerInvariant();
        return detector.IsSensitive(key) &&
               (lowerKey.Contains("database") ||
                lowerKey.Contains("db") ||
                lowerKey.Contains("connectionstring"));
    }

    /// <summary>
    /// Checks if a key is sensitive and also contains API-related patterns.
    /// Useful for identifying API keys and tokens.
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>True if the key is both sensitive and API-related; otherwise, false</returns>
    public static bool IsApiCredential(this SensitiveKeyDetector detector, string key)
    {
        if (detector == null)
            throw new ArgumentNullException(nameof(detector));

        if (string.IsNullOrWhiteSpace(key))
            return false;

        var lowerKey = key.ToLowerInvariant();
        return detector.IsSensitive(key) &&
               (lowerKey.Contains("api") ||
                lowerKey.Contains("token") ||
                lowerKey.Contains("key"));
    }

    /// <summary>
    /// Gets a severity level for the sensitivity of a key based on pattern matching.
    /// Returns an integer where higher values indicate higher sensitivity.
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>An integer severity level (0-5)</returns>
    public static int GetSensitivityLevel(this SensitiveKeyDetector detector, string key)
    {
        if (detector == null)
            throw new ArgumentNullException(nameof(detector));

        if (string.IsNullOrWhiteSpace(key))
            return 0;

        var lowerKey = key.ToLowerInvariant();
        int level = 0;

        if (detector.IsSensitive(key))
        {
            level += 1;
        }

        if (lowerKey.Contains("password") || lowerKey.Contains("pwd"))
        {
            level += 2;
        }
        else if (lowerKey.Contains("secret"))
        {
            level += 2;
        }
        else if (lowerKey.Contains("token"))
        {
            level += 1;
        }
        else if (lowerKey.Contains("key") || lowerKey.Contains("api"))
        {
            level += 1;
        }

        if (lowerKey.Contains("connectionstring"))
        {
            level += 2;
        }

        return Math.Min(level, 5);
    }

    /// <summary>
    /// Determines if a key should be treated with extra caution based on its sensitivity.
    /// Returns true for keys that contain "password", "secret", or "connectionstring".
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>True if the key requires extra caution; otherwise, false</returns>
    public static bool RequiresExtraCaution(this SensitiveKeyDetector detector, string key)
    {
        if (detector == null)
            throw new ArgumentNullException(nameof(detector));

        if (string.IsNullOrWhiteSpace(key))
            return false;

        var lowerKey = key.ToLowerInvariant();
        return lowerKey.Contains("password") ||
               lowerKey.Contains("secret") ||
               lowerKey.Contains("connectionstring");
    }
}
