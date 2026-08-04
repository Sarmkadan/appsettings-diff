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
    /// The prefix used to filter environment variables. Must not be <c>null</c>.
    /// </param>
    /// <returns>
    /// A dictionary containing the matching environment variables (key/value pairs) with
    /// case‑insensitive keys.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefix"/> is <c>null</c>.</exception>
    public static Dictionary<string, string> ReadFromEnvironment(string? prefix = null)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                if (prefix == null || key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
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
    public static Dictionary<string, string> Normalize(IDictionary<string, string> envVars)
    {
        ArgumentNullException.ThrowIfNull(envVars);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in envVars)
        {
            string key = entry.Key;
            string value = entry.Value;

            // Удаление префиксов ASPNETCORE_ и DOTNET_
            if (key.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase))
            {
                key = key["ASPNETCORE_".Length..];
            }
            else if (key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase))
            {
                key = key["DOTNET_".Length..];
            }

            // Замена '__' на ':'
            key = key.Replace("__", ":", StringComparison.Ordinal);

            // Удаление повторяющихся двоеточий
            key = key.Replace("::", ":", StringComparison.Ordinal);

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

            if (!string.IsNullOrEmpty(key))
            {
                if (key.Contains("__", StringComparison.Ordinal))
                {
                    key = key.Replace("__", ":", StringComparison.Ordinal);
                }

                prefixed[key] = value;
            }
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
}
