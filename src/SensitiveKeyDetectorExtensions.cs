namespace AppsettingsDiff;

/// <summary>
/// Extension methods for <see cref="SensitiveKeyDetector"/> that provide additional utility functionality
/// for detecting sensitive configuration keys and patterns.
/// </summary>
public static class SensitiveKeyDetectorExtensions
{
    /// <summary>
    /// Determines whether the specified key is a sensitive key based on common patterns.
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>True if the key is sensitive; otherwise, false</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static bool IsSensitiveKey(this SensitiveKeyDetector detector, string key)
    {
        ArgumentNullException.ThrowIfNull(detector);
        return detector.IsSensitive(key);
    }

    /// <summary>
    /// Determines whether the specified key matches any of the sensitive patterns.
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>True if the key matches sensitive patterns; otherwise, false</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static bool IsPotentiallySensitive(this SensitiveKeyDetector detector, string key)
    {
        ArgumentNullException.ThrowIfNull(detector);
        return detector.IsSensitive(key);
    }

    /// <summary>
    /// Checks if a key is sensitive and also contains common database-related patterns.
    /// Useful for identifying database connection strings and credentials.
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns>True if the key is both sensitive and database-related; otherwise, false</returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static bool IsDatabaseCredential(this SensitiveKeyDetector detector, string key)
    {
        ArgumentNullException.ThrowIfNull(detector);

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
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static bool IsApiCredential(this SensitiveKeyDetector detector, string key)
    {
        ArgumentNullException.ThrowIfNull(detector);

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
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is <see langword="null"/> or empty</exception>
    public static int GetSensitivityLevel(this SensitiveKeyDetector detector, string key)
    {
        ArgumentNullException.ThrowIfNull(detector);
        ArgumentException.ThrowIfNullOrEmpty(key);

        int level = detector.IsSensitive(key) ? 1 : 0;

        var lowerKey = key.ToLowerInvariant();
        level += lowerKey.Contains("password") || lowerKey.Contains("pwd") ? 2 :
                lowerKey.Contains("secret") ? 2 :
                lowerKey.Contains("token") ? 1 :
                lowerKey.Contains("key") || lowerKey.Contains("api") ? 1 : 0;

        level += lowerKey.Contains("connectionstring") ? 2 : 0;

        return Math.Min(level, 5);
    }

    /// <summary>
    /// Determines if a key should be treated with extra caution based on its sensitivity.
    /// Returns <see langword="true"/> for keys that contain "password", "secret", or "connectionstring".
    /// </summary>
    /// <param name="detector">The detector instance</param>
    /// <param name="key">The configuration key to check</param>
    /// <returns><see langword="true"/> if the key requires extra caution; otherwise <see langword="false"/></returns>
    /// <exception cref="ArgumentNullException"><paramref name="detector"/> is <see langword="null"/></exception>
    public static bool RequiresExtraCaution(this SensitiveKeyDetector detector, string key)
    {
        ArgumentNullException.ThrowIfNull(detector);
        return !string.IsNullOrWhiteSpace(key) &&
               (key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("connectionstring", StringComparison.OrdinalIgnoreCase));
    }
}