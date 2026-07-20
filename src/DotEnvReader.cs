using System;
using System.Collections.Generic;
using System.IO;

namespace AppsettingsDiff;

/// <summary>
/// Reads .env files (KEY=VALUE) and returns a flat dictionary of configuration values.
/// Supports comments (lines starting with # or ;), optional leading "export" keyword,
/// and quoted values (single or double quotes).
/// </summary>
public static class DotEnvReader
{
    /// <summary>
    /// Parses a .env file into a dictionary.
    /// </summary>
    /// <param name="path">Path to the .env file.</param>
    /// <returns>Dictionary of key/value pairs.</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    public static Dictionary<string, string> ReadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"The .env file was not found: {path}");

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = File.ReadAllLines(path);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            // Remove optional leading "export"
            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                line = line.Substring(7).TrimStart();

            int equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
                continue; // malformed line, ignore

            var key = line.Substring(0, equalsIndex).Trim();
            var value = line.Substring(equalsIndex + 1).Trim();

            // Strip surrounding quotes if present
            if ((value.StartsWith('\"') && value.EndsWith('\"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value.Substring(1, value.Length - 2);
            }

            result[key] = value;
        }

        return result;
    }
}
