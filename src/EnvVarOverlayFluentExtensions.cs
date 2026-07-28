using System;
using System.Collections.Generic;

namespace AppsettingsDiff;

/// <summary>
/// Extension methods that provide a fluent API for working with environment variables
/// and the <see cref="EnvVarOverlay"/> helper class.
/// </summary>
public static class EnvVarOverlayFluentExtensions
{
    /// <summary>
    /// Returns a new dictionary containing only the environment variables that start with the specified prefix.
    /// The prefix comparison is case‑insensitive.
    /// </summary>
    /// <param name="envVars">The source dictionary of environment variables.</param>
    /// <param name="prefix">The prefix to filter by.</param>
    /// <returns>A dictionary with the filtered key/value pairs.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="envVars"/> or <paramref name="prefix"/> is <c>null</c>.
    /// </exception>
    public static Dictionary<string, string> WithPrefix(this IDictionary<string, string> envVars, string prefix)
    {
        ArgumentNullException.ThrowIfNull(envVars);
        ArgumentNullException.ThrowIfNull(prefix);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in envVars)
        {
            if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates a snapshot copy of the dictionary using a case‑insensitive string comparer.
    /// </summary>
    /// <param name="envVars">The source dictionary of environment variables.</param>
    /// <returns>A new dictionary that contains the same key/value pairs as <paramref name="envVars"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="envVars"/> is <c>null</c>.
    /// </exception>
    public static Dictionary<string, string> ToDictionarySnapshot(this IDictionary<string, string> envVars)
    {
        ArgumentNullException.ThrowIfNull(envVars);
        return new Dictionary<string, string>(envVars, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Applies the environment variables to the configuration and returns the resulting configuration
    /// along with the list of keys that were overridden.
    /// </summary>
    /// <param name="config">The original configuration dictionary.</param>
    /// <param name="envVars">The environment variables to apply.</param>
    /// <returns>
    /// A tuple containing the new configuration dictionary and a list of overridden keys.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="config"/> or <paramref name="envVars"/> is <c>null</c>.
    /// </exception>
    public static (Dictionary<string, string> Config, List<string> OverriddenKeys) ApplyWithOverlay(
        this Dictionary<string, string> config,
        IDictionary<string, string> envVars)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(envVars);

        var overridden = new List<string>();
        var result = EnvVarOverlay.Apply(config, envVars, out overridden);
        return (result, overridden);
    }

    /// <summary>
    /// Applies the environment variables with a custom prefix to the configuration and returns the resulting configuration
    /// along with the list of keys that were overridden.
    /// </summary>
    /// <param name="config">The original configuration dictionary.</param>
    /// <param name="envVars">The environment variables to apply.</param>
    /// <param name="prefix">The optional prefix to filter and strip from the environment variable keys.</param>
    /// <returns>
    /// A tuple containing the new configuration dictionary and a list of overridden keys.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="config"/> or <paramref name="envVars"/> is <c>null</c>.
    /// </exception>
    public static (Dictionary<string, string> Config, List<string> OverriddenKeys) ApplyWithOverlay(
        this Dictionary<string, string> config,
        IDictionary<string, string> envVars,
        string? prefix)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(envVars);

        var overridden = new List<string>();
        var result = EnvVarOverlay.Apply(config, envVars, prefix, out overridden);
        return (result, overridden);
    }
}
