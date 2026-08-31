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

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var rawLine = lines[lineIndex];
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

            if (value.StartsWith('\"'))
            {
                value = ReadDoubleQuotedValue(value, lines, ref lineIndex);
            }
            else if (value.StartsWith('\'') && value.EndsWith('\''))
            {
                value = value.Substring(1, value.Length - 2);
            }

            result[key] = value;
        }

        return result;
    }

    private static string ReadDoubleQuotedValue(string value, string[] lines, ref int lineIndex)
    {
        var accumulated = value;

        while (FindClosingDoubleQuote(accumulated) < 0 && lineIndex + 1 < lines.Length)
        {
            lineIndex++;
            accumulated += "\n" + lines[lineIndex];
        }

        int closingQuoteIndex = FindClosingDoubleQuote(accumulated);
        if (closingQuoteIndex < 0)
            return accumulated;

        return UnescapeDoubleQuotedValue(accumulated.Substring(1, closingQuoteIndex - 1));
    }

    private static int FindClosingDoubleQuote(string value)
    {
        for (int index = 1; index < value.Length; index++)
        {
            if (value[index] != '\"')
                continue;

            int precedingBackslashes = 0;
            for (int backslashIndex = index - 1;
                 backslashIndex >= 0 && value[backslashIndex] == '\\';
                 backslashIndex--)
            {
                precedingBackslashes++;
            }

            if (precedingBackslashes % 2 == 0)
                return index;
        }

        return -1;
    }

    private static string UnescapeDoubleQuotedValue(string value)
    {
        var result = new System.Text.StringBuilder(value.Length);

        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] != '\\' || index + 1 >= value.Length)
            {
                result.Append(value[index]);
                continue;
            }

            switch (value[index + 1])
            {
                case 'n':
                    result.Append('\n');
                    index++;
                    break;
                case 't':
                    result.Append('\t');
                    index++;
                    break;
                case 'r':
                    result.Append('\r');
                    index++;
                    break;
                case '\"':
                    result.Append('\"');
                    index++;
                    break;
                case '\\':
                    result.Append('\\');
                    index++;
                    break;
                default:
                    result.Append('\\');
                    break;
            }
        }

        return result.ToString();
    }
}
