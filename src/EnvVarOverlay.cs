namespace AppsettingsDiff;

/// <summary>
/// Provides methods to overlay environment variables onto a configuration dictionary
/// following ASP.NET Core conventions (handling special prefixes and '__' to ':' conversion).
/// </summary>
public static class EnvVarOverlay
{
    /// <summary>
    /// Reads environment variables that start with the specified <paramref name="prefix"/>.
    /// </summary>
    /// <param name="prefix">
    /// The prefix used to filter environment variables. Must not be <c>null</c> or empty.
    /// </param>
    /// <returns>
    /// A dictionary containing the matching environment variables (key/value pairs) with
    /// case‑insensitive keys.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix"/> is <c>null</c> or empty.</exception>
    public static Dictionary<string, string> ReadFromEnvironment(string? prefix = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Normalizes environment variable keys by removing ASP.NET Core specific prefixes
    /// and converting double underscores to colons.
    /// </summary>
    /// <param name="envVars">The source environment variables. Must not be <c>null</c>.</param>
    /// <returns>
    /// A new dictionary with normalized keys (case‑insensitive) and the original values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envVars"/> is <c>null</c>.</exception>
    /// <exception cref="EnvVarOverlayException">Thrown when a key contains invalid characters, bad path separators, or malformed array indices.</exception>
    public static Dictionary<string, string> Normalize(IDictionary<string, string> envVars)
    {
        ArgumentNullException.ThrowIfNull(envVars);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in envVars)
        {
            string originalKey = entry.Key;
            string key = entry.Key;
            string value = entry.Value;

            if (string.IsNullOrEmpty(key) || value is null)
            {
                continue;
            }

            // Strip ASP.NET Core prefixes
            if (key.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase))
            {
                key = key["ASPNETCORE_".Length..];
            }
            else if (key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))
            {
                key = key["DOTNET_".Length..];
            }

            // Replace '__' with ':'
            key = key.Replace("__", ":", StringComparison.Ordinal);

            // Validate the normalized key
            ValidateKey(originalKey, key);

            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Overlays the supplied environment variables onto the given configuration dictionary.
    /// </summary>
    /// <param name="config">The original configuration dictionary. Must not be <c>null</c>.</param>
    /// <param name="envVars">The environment variables to overlay. Must not be <c>null</c>.</param>
    /// <param name="overriddenKeys">
    /// An output list that will contain the keys from <paramref name="config"/> that were
    /// overridden by <paramref name="envVars"/>.
    /// </param>
    /// <returns>
    /// A new configuration dictionary that includes the applied environment variables.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="config"/> or <paramref name="envVars"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="EnvVarOverlayException">Thrown when a key contains invalid characters, bad path separators, or malformed array indices.</exception>
    public static Dictionary<string, string> Apply(Dictionary<string, string> config, IDictionary<string, string> envVars, out List<string> overriddenKeys)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(envVars);

        // Filter and strip the custom prefix (if any)
        var prefixed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in envVars)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (string.IsNullOrEmpty(key) || value is null)
            {
                continue;
            }

            if (key.Contains("__", StringComparison.Ordinal))
            {
                key = key.Replace("__", ":", StringComparison.Ordinal);
            }

            prefixed[key] = value;
        }

        // Apply the existing normalization (ASP.NET Core prefixes and '__' handling)
        var normalized = Normalize(prefixed);

        overriddenKeys = [];
        var result = new Dictionary<string, string>(config, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in normalized)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (result.TryGetValue(key, out _))
            {
                overriddenKeys.Add(key);
            }

            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Overlays the supplied environment variables onto the given configuration dictionary,
    /// optionally filtering by a custom <paramref name="prefix"/>.
    /// </summary>
    /// <param name="config">The original configuration dictionary. Must not be <c>null</c>.</param>
    /// <param name="envVars">The environment variables to overlay. Must not be <c>null</c>.</param>
    /// <param name="prefix">
    /// An optional prefix used to filter and strip keys from <paramref name="envVars"/>.
    /// If <c>null</c> or empty, no custom prefix filtering is applied.
    /// </param>
    /// <param name="overriddenKeys">
    /// An output list that will contain the keys from <paramref name="config"/> that were
    /// overridden by the processed environment variables.
    /// </param>
    /// <returns>
    /// A new configuration dictionary that includes the applied environment variables.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="config"/> or <paramref name="envVars"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="EnvVarOverlayException">Thrown when a key contains invalid characters, bad path separators, or malformed array indices.</exception>
    public static Dictionary<string, string> Apply(Dictionary<string, string> config, IDictionary<string, string> envVars, string? prefix, out List<string> overriddenKeys)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(envVars);

        // Filter and strip the custom prefix (if any)
        var prefixed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in envVars)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (string.IsNullOrEmpty(key) || value is null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Skip variables that do not match the custom prefix
                    continue;
                }

                // Strip the custom prefix
                key = key[prefix.Length..];
            }

            if (key.Contains("__", StringComparison.Ordinal))
            {
                key = key.Replace("__", ":", StringComparison.Ordinal);
            }

            prefixed[key] = value;
        }

        // Apply the existing normalization (ASP.NET Core prefixes and '__' handling)
        var normalized = Normalize(prefixed);

        overriddenKeys = [];
        var result = new Dictionary<string, string>(config, StringComparer.OrdinalIgnoreCase);

        foreach (var entry in normalized)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (result.TryGetValue(key, out _))
            {
                overriddenKeys.Add(key);
            }

            result[key] = value;
        }

        return result;
    }

    /// <summary>
    /// Validates a normalized configuration key for invalid characters, bad path separators,
    /// and malformed array indices.
    /// </summary>
    /// <param name="originalEnvVarName">The original environment variable name for error reporting.</param>
    /// <param name="key">The normalized configuration key to validate.</param>
    /// <exception cref="EnvVarOverlayException">Thrown when the key is invalid.</exception>
    private static void ValidateKey(string originalEnvVarName, string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new EnvVarOverlayException($"Environment variable '{originalEnvVarName}' resulted in an empty config key after normalization.");
        }

        // Check for invalid characters (allow alphanumeric, _, -, ., :, [, ])
        foreach (char c in key)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.' && c != ':' && c != '[' && c != ']')
            {
                throw new EnvVarOverlayException($"Environment variable '{originalEnvVarName}' contains invalid characters in key '{key}'.");
            }
        }

        // Check for bad path separators
        if (key.StartsWith(':') || key.EndsWith(':') || key.Contains("::"))
        {
            throw new EnvVarOverlayException($"Environment variable '{originalEnvVarName}' has invalid path separators in key '{key}'.");
        }

        // Check for invalid array indices
        int i = 0;
        while ((i = key.IndexOf('[', i)) != -1)
        {
            int closeBracket = key.IndexOf(']', i);
            if (closeBracket == -1)
            {
                throw new EnvVarOverlayException($"Environment variable '{originalEnvVarName}' has malformed array index in key '{key}'.");
            }
            string indexStr = key[(i + 1)..closeBracket];
            if (!int.TryParse(indexStr, out _))
            {
                throw new EnvVarOverlayException($"Environment variable '{originalEnvVarName}' has invalid array index '{indexStr}' in key '{key}'.");
            }
            i = closeBracket + 1;
        }
    }
}

/// <summary>
/// Exception thrown when an environment variable key cannot be parsed or contains invalid configuration path elements.
/// </summary>
public class EnvVarOverlayException : Exception
{
    public EnvVarOverlayException(string message) : base(message) { }
    public EnvVarOverlayException(string message, Exception? inner) : base(message, inner) { }
}
